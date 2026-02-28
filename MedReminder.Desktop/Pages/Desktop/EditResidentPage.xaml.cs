using MedReminder.Models;
using MedReminder.Services;
using MedReminder.Services.Abstractions;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MedReminder.Pages.Desktop
{
    [QueryProperty(nameof(ResidentId), "id")]
    [QueryProperty(nameof(RoomNumber), "roomNumber")]
    [QueryProperty(nameof(RoomType), "roomType")]
    [QueryProperty(nameof(ReturnTo), "returnTo")]
    public partial class EditResidentPage : ContentPage, IUnsavedChangesPage
    {
        private readonly IResidentService _residentService;
        private bool _isDirty;

        public bool HasUnsavedChanges => _isDirty;

        public async Task SaveAsync()
        {
            if (!ValidateForm())
                return;

            CleanRelationshipPlaceholders();

            try
            {
                await _residentService.UpsertAsync(WorkingCopy);
                _isDirty = false;
            }
            catch
            {
                // Queued offline
            }
        }

        private Guid _residentId = Guid.Empty;
        public string? ResidentId
        {
            get => _residentId == Guid.Empty ? null : _residentId.ToString();
            set
            {
                _residentId = Guid.TryParse(value, out var id) ? id : Guid.Empty;
            }
        }
        public string? RoomNumber { get; set; }
        public string? RoomType { get; set; }
        public string? ReturnTo { get; set; }

        public Resident WorkingCopy { get; private set; } = new();

        private bool _isNew;
        private bool _updatingAllergies;

        public EditResidentPage(IResidentService residentService)
        {
            InitializeComponent();
            _residentService = residentService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // RBAC: only Admin/Nurse can edit residents
            var auth = MauiProgram.Services.GetService<AuthService>();
            var canEditResident = auth?.HasRole(StaffRole.Admin, StaffRole.Nurse) ?? false;

            if (!canEditResident)
            {
                await DisplayAlert("Access denied", "You don't have permission to edit resident records.", "OK");
                await Shell.Current.GoToAsync(GetReturnTarget());
                return;
            }

            if (_residentId != Guid.Empty)
            {
                List<Resident> list;
                try
                {
                    list = await _residentService.LoadAsync();
                }
                catch
                {
                    await DisplayAlert("Offline", "Cannot load resident data while offline.", "OK");
                    await Shell.Current.GoToAsync(GetReturnTarget());
                    return;
                }
                var existing = list.FirstOrDefault(r => r.Id == _residentId);

                if (existing is null)
                {
                    _isNew = true;
                    WorkingCopy = CreateNewResidentWithDefaults();

                    // If we navigated from FloorPlan, prefill room info
                    ApplyRoomPrefillIfProvided();
                }
                else
                {
                    _isNew = false;

                    WorkingCopy = new Resident
                    {
                        Id = existing.Id,

                        // Personal Info
                        ResidentFName = existing.ResidentFName,
                        ResidentLName = existing.ResidentLName,
                        DateOfBirth = existing.DateOfBirth,
                        SIN = existing.SIN,
                        Gender = existing.Gender,

                        // Address
                        Address = existing.Address,
                        City = existing.City,
                        Province = existing.Province,
                        PostalCode = existing.PostalCode,

                        // Room placement (Floor plan)
                        AdmissionDate = existing.AdmissionDate,
                        RoomNumber = existing.RoomNumber,
                        RoomType = existing.RoomType,
                        BedLabel = existing.BedLabel,

                        // Emergency Contacts
                        EmergencyContactName1 = existing.EmergencyContactName1,
                        EmergencyContactPhone1 = existing.EmergencyContactPhone1,
                        EmergencyRelationship1 = existing.EmergencyRelationship1,

                        EmergencyContactName2 = existing.EmergencyContactName2,
                        EmergencyContactPhone2 = existing.EmergencyContactPhone2,
                        EmergencyRelationship2 = existing.EmergencyRelationship2,

                        // Doctor
                        DoctorName = existing.DoctorName,
                        DoctorContact = existing.DoctorContact,

                        // Allergies & Remarks
                        AllergyPeanuts = existing.AllergyPeanuts,
                        AllergyTreeNuts = existing.AllergyTreeNuts,
                        AllergyMilk = existing.AllergyMilk,
                        AllergyEggs = existing.AllergyEggs,
                        AllergyShellfish = existing.AllergyShellfish,
                        AllergyFish = existing.AllergyFish,
                        AllergyWheat = existing.AllergyWheat,
                        AllergySoy = existing.AllergySoy,
                        AllergyLatex = existing.AllergyLatex,
                        AllergyPenicillin = existing.AllergyPenicillin,
                        AllergySulfa = existing.AllergySulfa,
                        AllergyAspirin = existing.AllergyAspirin,
                        AllergyOtherItems = existing.AllergyOtherItems,
                        Remarks = existing.Remarks
                    };

                    WorkingCopy.AllergyNone = !WorkingCopy.AllergyPeanuts && !WorkingCopy.AllergyTreeNuts &&
                                              !WorkingCopy.AllergyMilk && !WorkingCopy.AllergyEggs &&
                                              !WorkingCopy.AllergyShellfish && !WorkingCopy.AllergyFish &&
                                              !WorkingCopy.AllergyWheat && !WorkingCopy.AllergySoy &&
                                              !WorkingCopy.AllergyLatex && !WorkingCopy.AllergyPenicillin &&
                                              !WorkingCopy.AllergySulfa && !WorkingCopy.AllergyAspirin &&
                                              string.IsNullOrWhiteSpace(WorkingCopy.AllergyOtherItems);

                    // In case the user opened edit from FloorPlan for an occupied room,
                    // do NOT overwrite existing room fields.
                    EnsureRelationshipDefaults();
                }
            }
            else
            {
                _isNew = true;
                WorkingCopy = CreateNewResidentWithDefaults();

                // If we navigated from FloorPlan, prefill room info
                ApplyRoomPrefillIfProvided();
            }

            // Auto-assign room for new residents when no room was prefilled
            if (_isNew && string.IsNullOrWhiteSpace(WorkingCopy.RoomNumber))
            {
                try
                {
                    var allResidents = await _residentService.LoadAsync();
                    await AutoAssignRoomAsync(allResidents);
                }
                catch
                {
                    // Offline — skip auto-assign
                }
            }

            BindingContext = WorkingCopy;
            RefreshAllergyUI();

            // Track changes — mark dirty on any property change after initial load
            _isDirty = false;
            WorkingCopy.PropertyChanged += (_, _) => _isDirty = true;

            // Show DELETE only for existing residents
            if (DeleteAction != null)
                DeleteAction.IsVisible = !_isNew;
        }

        private void OnNoneCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (WorkingCopy == null || _updatingAllergies) return;

            _updatingAllergies = true;
            try
            {
                if (e.Value)
                {
                    ClearAllAllergies();
                }
                RefreshAllergyUI();
            }
            finally
            {
                _updatingAllergies = false;
            }
        }

        private void OnSpecificAllergyCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (WorkingCopy == null || _updatingAllergies) return;

            if (e.Value && WorkingCopy.AllergyNone)
            {
                _updatingAllergies = true;
                try
                {
                    WorkingCopy.AllergyNone = false;
                    RefreshAllergyUI();
                }
                finally
                {
                    _updatingAllergies = false;
                }
            }
        }

        private void OnOtherAllergyTextChanged(object sender, TextChangedEventArgs e)
        {
            if (WorkingCopy == null || _updatingAllergies) return;

            if (!string.IsNullOrWhiteSpace(e.NewTextValue) && WorkingCopy.AllergyNone)
            {
                _updatingAllergies = true;
                try
                {
                    WorkingCopy.AllergyNone = false;
                    RefreshAllergyUI();
                }
                finally
                {
                    _updatingAllergies = false;
                }
            }
        }

        private void ClearAllAllergies()
        {
            if (WorkingCopy == null) return;

            WorkingCopy.AllergyPeanuts = false;
            WorkingCopy.AllergyTreeNuts = false;
            WorkingCopy.AllergyMilk = false;
            WorkingCopy.AllergyEggs = false;
            WorkingCopy.AllergyShellfish = false;
            WorkingCopy.AllergyFish = false;
            WorkingCopy.AllergyWheat = false;
            WorkingCopy.AllergySoy = false;
            WorkingCopy.AllergyLatex = false;
            WorkingCopy.AllergyPenicillin = false;
            WorkingCopy.AllergySulfa = false;
            WorkingCopy.AllergyAspirin = false;
            WorkingCopy.AllergyOtherItems = string.Empty;
        }

        private void RefreshAllergyUI()
        {
            // Properties now fire PropertyChanged, so bindings update automatically.
            // IsEnabled bindings reference NoneCheck.IsChecked directly via x:Reference,
            // so they re-evaluate when AllergyNone changes.
        }

        private Resident CreateNewResidentWithDefaults()
        {
            return new Resident
            {
                AdmissionDate = DateTime.Today.ToString("yyyy-MM-dd"),
                RoomNumber = "",
                RoomType = "",
                EmergencyRelationship1 = "Select Relationship",
                EmergencyRelationship2 = null
            };
        }

        private void ApplyRoomPrefillIfProvided()
        {
            // Only apply when creating a NEW resident
            if (!_isNew)
                return;

            if (!string.IsNullOrWhiteSpace(RoomNumber))
                WorkingCopy.RoomNumber = RoomNumber;

            if (!string.IsNullOrWhiteSpace(RoomType))
                WorkingCopy.RoomType = RoomType;

            EnsureRelationshipDefaults();
        }

        private async Task AutoAssignRoomAsync(List<Resident> allResidents)
        {
            // Room layout: 12 rooms per floor, 1-based index positions 3,4,6,8,9 are Double
            bool IsDouble(int oneBasedIndex) => oneBasedIndex is 3 or 4 or 6 or 8 or 9;

            var occupancy = allResidents
                .Where(r => !string.IsNullOrWhiteSpace(r.RoomNumber))
                .GroupBy(r => r.RoomNumber!)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Try both floors
            foreach (int floorStart in new[] { 101, 201 })
            {
                var roomNumbers = Enumerable.Range(floorStart, 12).ToList();

                // Pass 1: find the first empty Single room
                for (int i = 0; i < roomNumbers.Count; i++)
                {
                    if (IsDouble(i + 1)) continue; // skip double rooms
                    var roomStr = roomNumbers[i].ToString();
                    occupancy.TryGetValue(roomStr, out var occ);
                    if (occ == null || occ.Count == 0)
                    {
                        WorkingCopy.RoomNumber = roomStr;
                        WorkingCopy.RoomType = "Single";
                        WorkingCopy.BedLabel = "None";
                        return;
                    }
                }

                // Pass 2: find a Double room with an open bed and matching gender
                for (int i = 0; i < roomNumbers.Count; i++)
                {
                    if (!IsDouble(i + 1)) continue; // only double rooms
                    var roomStr = roomNumbers[i].ToString();
                    occupancy.TryGetValue(roomStr, out var occ);

                    if (occ == null || occ.Count == 0)
                    {
                        // Empty double room — assign bed A
                        WorkingCopy.RoomNumber = roomStr;
                        WorkingCopy.RoomType = "Couple";
                        WorkingCopy.BedLabel = "A";
                        return;
                    }

                    if (occ.Count < 2)
                    {
                        // One occupant — check gender match
                        var existingGender = occ[0].Gender;
                        var newGender = WorkingCopy.Gender;

                        if (!string.IsNullOrWhiteSpace(existingGender) &&
                            !string.IsNullOrWhiteSpace(newGender) &&
                            string.Equals(existingGender, newGender, StringComparison.OrdinalIgnoreCase))
                        {
                            var takenBed = occ[0].BedLabel ?? "A";
                            WorkingCopy.RoomNumber = roomStr;
                            WorkingCopy.RoomType = "Couple";
                            WorkingCopy.BedLabel = takenBed == "A" ? "B" : "A";
                            return;
                        }
                    }
                }
            }

            // No room available — leave fields empty
        }

        private void EnsureRelationshipDefaults()
        {
            if (string.IsNullOrWhiteSpace(WorkingCopy.EmergencyRelationship1))
                WorkingCopy.EmergencyRelationship1 = "Select Relationship";

            if (string.IsNullOrWhiteSpace(WorkingCopy.EmergencyRelationship2))
                WorkingCopy.EmergencyRelationship2 = "Select Relationship";
        }

        private bool ValidateForm()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(WorkingCopy.ResidentFName))
                errors.Add("First Name");

            if (string.IsNullOrWhiteSpace(WorkingCopy.ResidentLName))
                errors.Add("Last Name");

            if (string.IsNullOrWhiteSpace(WorkingCopy.DateOfBirth))
                errors.Add("Date of Birth (DOB)");

            if (string.IsNullOrWhiteSpace(WorkingCopy.EmergencyContactName1))
                errors.Add("Emergency Contact 1 Name");

            if (string.IsNullOrWhiteSpace(WorkingCopy.EmergencyContactPhone1))
                errors.Add("Emergency Contact 1 Phone");

            if (string.IsNullOrWhiteSpace(WorkingCopy.EmergencyRelationship1) ||
                WorkingCopy.EmergencyRelationship1 == "Select Relationship")
                errors.Add("Emergency Contact 1 Relationship");

            if (!string.IsNullOrWhiteSpace(WorkingCopy.EmergencyContactName2) &&
                (string.IsNullOrWhiteSpace(WorkingCopy.EmergencyRelationship2) ||
                 WorkingCopy.EmergencyRelationship2 == "Select Relationship"))
            {
                errors.Add("Emergency Contact 2 Relationship (required when name is provided)");
            }

            if (string.IsNullOrWhiteSpace(WorkingCopy.DoctorName))
                errors.Add("Doctor Name");

            // Allergy Validation Logic
            bool hasAnySpecificAllergy = WorkingCopy.AllergyPeanuts || WorkingCopy.AllergyTreeNuts ||
                                         WorkingCopy.AllergyMilk || WorkingCopy.AllergyEggs ||
                                         WorkingCopy.AllergyShellfish || WorkingCopy.AllergyFish ||
                                         WorkingCopy.AllergyWheat || WorkingCopy.AllergySoy ||
                                         WorkingCopy.AllergyLatex || WorkingCopy.AllergyPenicillin ||
                                         WorkingCopy.AllergySulfa || WorkingCopy.AllergyAspirin ||
                                         !string.IsNullOrWhiteSpace(WorkingCopy.AllergyOtherItems);

            if (!WorkingCopy.AllergyNone && !hasAnySpecificAllergy)
            {
                errors.Add("Allergies (Must select 'None' or check specific allergies)");
            }

            if (errors.Count > 0)
            {
                DisplayAlert("Missing Required Fields", "Please fill in:\n� " + string.Join("\n� ", errors), "OK");
                return false;
            }

            return true;
        }

        private string GetReturnTarget()
        {
            return string.IsNullOrWhiteSpace(ReturnTo)
                ? $"//{nameof(ResidentsPage)}"
                : ReturnTo;
        }

        private void CleanRelationshipPlaceholders()
        {
            if (string.IsNullOrWhiteSpace(WorkingCopy.EmergencyContactName2))
                WorkingCopy.EmergencyRelationship2 = null;
            else if (WorkingCopy.EmergencyRelationship2 == "Select Relationship")
                WorkingCopy.EmergencyRelationship2 = null;

            if (WorkingCopy.EmergencyRelationship1 == "Select Relationship")
                WorkingCopy.EmergencyRelationship1 = "";
        }

        private async void OnSaveClicked(object sender, TappedEventArgs e)
        {
            // Re-check RBAC at save time (extra safety)
            var auth = MauiProgram.Services.GetService<AuthService>();
            var canEditResident = auth?.HasRole(StaffRole.Admin, StaffRole.Nurse) ?? false;
            if (!canEditResident)
            {
                await DisplayAlert("Access denied", "You don't have permission to save changes.", "OK");
                return;
            }

            if (!ValidateForm())
                return;

            CleanRelationshipPlaceholders();

            try
            {
                await _residentService.UpsertAsync(WorkingCopy);
                _isDirty = false;
            }
            catch
            {
                // Queued offline by wrapper
            }

            // Resolve the resident ID for navigation (new residents may not have one yet)
            if (WorkingCopy.Id == Guid.Empty)
            {
                try
                {
                    var list = await _residentService.LoadAsync();
                    var latest = list.OrderByDescending(r => r.Id).FirstOrDefault();
                    if (latest != null)
                        WorkingCopy.Id = latest.Id;
                }
                catch { }
            }

            bool addMed = await DisplayAlert("Record Saved", "Do you want to add medication?", "Yes", "No");

            if (addMed)
            {
                var parameters = new Dictionary<string, object?>
                {
                    ["Item"] = null,
                    ["residentId"] = WorkingCopy.Id,
                    ["residentName"] = WorkingCopy.ResidentName
                };
                await Shell.Current.GoToAsync(nameof(EditMedicationPage), true, parameters);
            }
            else
            {
                await Shell.Current.GoToAsync($"{nameof(ViewResidentPage)}?id={WorkingCopy.Id}");
            }
        }

        private async void OnSaveAndAddMedClicked(object sender, TappedEventArgs e)
        {
            // Re-check RBAC at save time (extra safety)
            var auth = MauiProgram.Services.GetService<AuthService>();
            var canEditResident = auth?.HasRole(StaffRole.Admin, StaffRole.Nurse) ?? false;
            if (!canEditResident)
            {
                await DisplayAlert("Access denied", "You don't have permission to save changes.", "OK");
                return;
            }

            if (!ValidateForm())
                return;

            CleanRelationshipPlaceholders();

            try
            {
                await _residentService.UpsertAsync(WorkingCopy);
            }
            catch
            {
                // Queued offline by wrapper
            }

            if (WorkingCopy.Id == Guid.Empty)
            {
                try
                {
                    var list = await _residentService.LoadAsync();
                    var latest = list.OrderByDescending(r => r.Id).FirstOrDefault();
                    if (latest != null)
                        WorkingCopy.Id = latest.Id;
                }
                catch
                {
                    // Offline — use local ID
                }
            }

            try
            {
                var parameters = new Dictionary<string, object?>
                {
                    ["Item"] = null,
                    ["residentId"] = WorkingCopy.Id,
                    ["residentName"] = WorkingCopy.ResidentName
                };

                await Shell.Current.GoToAsync(nameof(EditMedicationPage), true, parameters);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Navigation error", ex.Message, "OK");
            }
        }

        private async void OnDeleteClicked(object sender, TappedEventArgs e)
        {
            if (_isNew || WorkingCopy == null)
                return;

            var auth = MauiProgram.Services.GetService<AuthService>();
            if (!(auth?.HasRole(StaffRole.Admin, StaffRole.Nurse) ?? false))
            {
                await DisplayAlert("Access denied", "You don't have permission to delete residents.", "OK");
                return;
            }

            bool confirm = await DisplayAlert(
                "Delete resident",
                $"Are you sure you want to delete {WorkingCopy.ResidentName}?",
                "Delete", "Cancel");

            if (!confirm)
                return;

            try
            {
                await _residentService.DeleteAsync(WorkingCopy);

                if (!MedReminder.Desktop.Services.Sync.ConnectivityHelper.IsOnline())
                    await DisplayAlert("Deleted offline", "Delete queued — sync when online.", "OK");
                else
                    await DisplayAlert("Deleted", "Resident has been deleted.", "OK");
            }
            catch
            {
                await DisplayAlert("Deleted offline", "Delete queued — sync when online.", "OK");
            }

            await Shell.Current.GoToAsync(GetReturnTarget());
        }

        private async void OnCancelClicked(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync(GetReturnTarget());
        }

    }
}


