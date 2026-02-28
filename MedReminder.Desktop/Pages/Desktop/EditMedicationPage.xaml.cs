using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using MedReminder.Models;
using MedReminder.Pages.UI.Popups;
using MedReminder.Services.Abstractions;
using MedReminder.ViewModels;

namespace MedReminder.Pages.Desktop
{
    [QueryProperty(nameof(Item), "Item")]
    [QueryProperty(nameof(ResidentId), "residentId")]
    [QueryProperty(nameof(ResidentName), "residentName")]
    [QueryProperty(nameof(ReturnTo), "returnTo")]

    public partial class EditMedicationPage : ContentPage, Pages.IUnsavedChangesPage
    {
        private readonly MedicationViewModel _vm;
        private readonly IMedicationService _medService;
        private bool _isDirty;

        public bool HasUnsavedChanges => _isDirty;

        public async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(WorkingCopy.MedName))
                return;

            if (WorkingCopy.ExpiryDate == default)
                WorkingCopy.ExpiryDate = new DateTimeOffset(DateTime.UtcNow.Date.AddMonths(6), TimeSpan.Zero);

            if (Radio1Time.IsChecked) WorkingCopy.TimesPerDay = 1;
            else if (Radio2Times.IsChecked) WorkingCopy.TimesPerDay = 2;
            else WorkingCopy.TimesPerDay = 3;

            SaveTimePickersToModel();
            await _vm.SaveDataAsync(WorkingCopy);
            _isDirty = false;
        }

        public Medication? Item { get; set; }
        public Guid ResidentId { get; set; }
        public string? ResidentName { get; set; }

        public Medication WorkingCopy { get; private set; } = new();

        private Picker[] _time2Pickers = null!;
        private Picker[] _time3Pickers = null!;
        private Label[] _time2Labels = null!;
        private Label[] _time3Labels = null!;

        private List<Medication> _inventoryMeds = new();
        private bool _suppressSuggestion;

        public EditMedicationPage(MedicationViewModel vm, IMedicationService medService)
        {
            InitializeComponent();
            _vm = vm;
            _medService = medService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Initialize picker arrays
            _time2Pickers = new[] { SunHH2, SunMM2, MonHH2, MonMM2, TueHH2, TueMM2, WedHH2, WedMM2, ThuHH2, ThuMM2, FriHH2, FriMM2, SatHH2, SatMM2 };
            _time3Pickers = new[] { SunHH3, SunMM3, MonHH3, MonMM3, TueHH3, TueMM3, WedHH3, WedMM3, ThuHH3, ThuMM3, FriHH3, FriMM3, SatHH3, SatMM3 };
            _time2Labels = new[] { LblHH2, LblMM2 };
            _time3Labels = new[] { LblHH3, LblMM3 };

            // Wire up schedule time changed for Mon–Sat (Sun is wired in XAML)
            foreach (var p in new[] { MonHH1, MonMM1, TueHH1, TueMM1, WedHH1, WedMM1,
                                      ThuHH1, ThuMM1, FriHH1, FriMM1, SatHH1, SatMM1,
                                      MonHH2, MonMM2, TueHH2, TueMM2, WedHH2, WedMM2,
                                      ThuHH2, ThuMM2, FriHH2, FriMM2, SatHH2, SatMM2,
                                      MonHH3, MonMM3, TueHH3, TueMM3, WedHH3, WedMM3,
                                      ThuHH3, ThuMM3, FriHH3, FriMM3, SatHH3, SatMM3 })
            {
                p.SelectedIndexChanged += OnScheduleTimeChanged;
            }

            if (string.IsNullOrWhiteSpace(ResidentName))
                ResidentHeaderLabel.Text = "Medication for";
            else
                ResidentHeaderLabel.Text = $"Medication for {ResidentName}";

            if (Item is null)
            {
                WorkingCopy = new Medication();

                if (ResidentId != Guid.Empty)
                    WorkingCopy.ResidentId = ResidentId;
            }
            else
            {
                WorkingCopy = new Medication
                {
                    Id = Item.Id,
                    MedName = Item.MedName,
                    Dosage = Item.Dosage,
                    Usage = Item.Usage,
                    Quantity = Item.Quantity,
                    QuantityUnit = Item.QuantityUnit,
                    ExpiryDate = Item.ExpiryDate,
                    ResidentId = Item.ResidentId,

                    ReminderMon = Item.ReminderMon,
                    ReminderTue = Item.ReminderTue,
                    ReminderWed = Item.ReminderWed,
                    ReminderThu = Item.ReminderThu,
                    ReminderFri = Item.ReminderFri,
                    ReminderSat = Item.ReminderSat,
                    ReminderSun = Item.ReminderSun,

                    ReminderTime = Item.ReminderTime,
                    TimesPerDay = Item.TimesPerDay,

                    MonTime1 = Item.MonTime1,
                    MonTime2 = Item.MonTime2,
                    MonTime3 = Item.MonTime3,
                    TueTime1 = Item.TueTime1,
                    TueTime2 = Item.TueTime2,
                    TueTime3 = Item.TueTime3,
                    WedTime1 = Item.WedTime1,
                    WedTime2 = Item.WedTime2,
                    WedTime3 = Item.WedTime3,
                    ThuTime1 = Item.ThuTime1,
                    ThuTime2 = Item.ThuTime2,
                    ThuTime3 = Item.ThuTime3,
                    FriTime1 = Item.FriTime1,
                    FriTime2 = Item.FriTime2,
                    FriTime3 = Item.FriTime3,
                    SatTime1 = Item.SatTime1,
                    SatTime2 = Item.SatTime2,
                    SatTime3 = Item.SatTime3,
                    SunTime1 = Item.SunTime1,
                    SunTime2 = Item.SunTime2,
                    SunTime3 = Item.SunTime3
                };
            }

            BindingContext = WorkingCopy;

            InitialiseUnitPicker();
            LoadTimePickersFromModel();
            InitialiseTimeFrequency();
            _ = LoadInventoryNamesAsync();

            // Track dirty state — any user edit marks the form as changed
            _isDirty = false;
            MedNameEntry.TextChanged += (_, _) => { if (!_suppressSuggestion) _isDirty = true; };
            UpdateIndicationBadges();
            UnitPicker.SelectedIndexChanged += (_, _) => { _isDirty = true; UpdateQtyForUnit(); };
            UpdateQtyForUnit();
        }

        private void UpdateQtyForUnit()
        {
            var unit = UnitPicker.SelectedItem as string;
            bool isThinLayer = string.Equals(unit, "thin layer", StringComparison.OrdinalIgnoreCase);

            QtyEntry.IsEnabled = !isThinLayer;

            if (isThinLayer)
            {
                WorkingCopy.Quantity = 0;
                QtyEntry.Text = "0";
                QtyEntry.Opacity = 0.4;
            }
            else
            {
                QtyEntry.Opacity = 1.0;
            }
        }

        private async Task LoadInventoryNamesAsync()
        {
            try
            {
                var all = await _medService.LoadAsync();
                _inventoryMeds = all
                    .Where(m => (m.ResidentId == null || m.ResidentId == Guid.Empty)
                                && !string.IsNullOrWhiteSpace(m.MedName))
                    .GroupBy(m => m.MedName, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .OrderBy(m => m.MedName)
                    .ToList();
            }
            catch
            {
                _inventoryMeds = new List<Medication>();
            }
        }

        private void OnMedNameTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressSuggestion)
                return;

            var text = (e.NewTextValue ?? "").Trim();
            if (text.Length < 1)
            {
                SuggestionList.IsVisible = false;
                SuggestionList.ItemsSource = null;
                return;
            }

            var matches = _inventoryMeds
                .Where(m => m.MedName.Contains(text, StringComparison.OrdinalIgnoreCase))
                .Take(8)
                .ToList();

            if (matches.Count == 0 || (matches.Count == 1 && string.Equals(matches[0].MedName, text, StringComparison.OrdinalIgnoreCase)))
            {
                SuggestionList.IsVisible = false;
                SuggestionList.ItemsSource = null;
                return;
            }

            SuggestionList.ItemsSource = matches;
            SuggestionList.IsVisible = true;
        }

        private void OnSuggestionSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Medication selected)
            {
                _suppressSuggestion = true;

                WorkingCopy.MedName = selected.MedName;
                MedNameEntry.Text = selected.MedName;

                // Auto-fill unit type from inventory
                if (!string.IsNullOrWhiteSpace(selected.QuantityUnit))
                {
                    WorkingCopy.QuantityUnit = selected.QuantityUnit;
                    InitialiseUnitPicker();
                }

                // Auto-fill indication from inventory
                if (!string.IsNullOrWhiteSpace(selected.Usage))
                {
                    WorkingCopy.Usage = selected.Usage;
                }

                UpdateIndicationBadges();

                SuggestionList.IsVisible = false;
                SuggestionList.ItemsSource = null;
                SuggestionList.SelectedItem = null;
                _suppressSuggestion = false;
            }
        }

        private void OnMedNameCompleted(object sender, EventArgs e)
        {
            if (SuggestionList.IsVisible && SuggestionList.ItemsSource is IList<Medication> items && items.Count > 0)
            {
                var first = items[0];
                _suppressSuggestion = true;

                WorkingCopy.MedName = first.MedName;
                MedNameEntry.Text = first.MedName;

                if (!string.IsNullOrWhiteSpace(first.QuantityUnit))
                {
                    WorkingCopy.QuantityUnit = first.QuantityUnit;
                    InitialiseUnitPicker();
                }

                if (!string.IsNullOrWhiteSpace(first.Usage))
                {
                    WorkingCopy.Usage = first.Usage;
                }

                UpdateIndicationBadges();

                SuggestionList.IsVisible = false;
                SuggestionList.ItemsSource = null;
                SuggestionList.SelectedItem = null;
                _suppressSuggestion = false;
            }
        }

        private void UpdateIndicationBadges()
        {
            IndicationBadges.Children.Clear();

            var usage = WorkingCopy?.Usage;
            if (string.IsNullOrWhiteSpace(usage))
            {
                IndicationSection.IsVisible = false;
                return;
            }

            var tokens = usage.Split(new[] { '&', ',', '/' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(t => t.Trim())
                              .Where(t => t.Length > 0)
                              .ToList();

            if (tokens.Count == 0)
            {
                IndicationSection.IsVisible = false;
                return;
            }

            foreach (var token in tokens)
            {
                var label = new Label
                {
                    Text = token,
                    TextColor = Colors.White,
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold
                };

                var badge = new Border
                {
                    BackgroundColor = Color.FromArgb("#A5A58D"),
                    Padding = new Thickness(8, 4),
                    Margin = new Thickness(0, 0, 6, 6),
                    StrokeThickness = 0,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                    Content = label
                };

                IndicationBadges.Children.Add(badge);
            }

            IndicationSection.IsVisible = true;
        }

        private void InitialiseTimeFrequency()
        {
            int times = WorkingCopy.TimesPerDay;
            if (times <= 0) times = 1;

            Radio1Time.IsChecked = (times == 1);
            Radio2Times.IsChecked = (times == 2);
            Radio3Times.IsChecked = (times == 3);

            UpdateTimePickersEnabled(times);
        }

        private void OnTimeFrequencyChanged(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value) return;

            int times = 3;
            if (Radio1Time.IsChecked) times = 1;
            else if (Radio2Times.IsChecked) times = 2;
            else if (Radio3Times.IsChecked) times = 3;

            WorkingCopy.TimesPerDay = times;
            UpdateTimePickersEnabled(times);
        }

        private void UpdateTimePickersEnabled(int times)
        {
            var enabledColor = Color.FromArgb("#6B6B5D");
            var disabledColor = Colors.LightGray;

            bool enable2 = times >= 2;
            foreach (var picker in _time2Pickers)
                picker.IsEnabled = enable2;
            foreach (var label in _time2Labels)
                label.TextColor = enable2 ? enabledColor : disabledColor;
            LblTime2.TextColor = enable2 ? Color.FromArgb("#3D3D35") : disabledColor;

            bool enable3 = times >= 3;
            foreach (var picker in _time3Pickers)
                picker.IsEnabled = enable3;
            foreach (var label in _time3Labels)
                label.TextColor = enable3 ? enabledColor : disabledColor;
            LblTime3.TextColor = enable3 ? Color.FromArgb("#3D3D35") : disabledColor;
        }

        private void LoadTimePickersFromModel()
        {
            SetPickerFromTime(SunHH1, SunMM1, WorkingCopy.SunTime1);
            SetPickerFromTime(SunHH2, SunMM2, WorkingCopy.SunTime2);
            SetPickerFromTime(SunHH3, SunMM3, WorkingCopy.SunTime3);

            SetPickerFromTime(MonHH1, MonMM1, WorkingCopy.MonTime1);
            SetPickerFromTime(MonHH2, MonMM2, WorkingCopy.MonTime2);
            SetPickerFromTime(MonHH3, MonMM3, WorkingCopy.MonTime3);

            SetPickerFromTime(TueHH1, TueMM1, WorkingCopy.TueTime1);
            SetPickerFromTime(TueHH2, TueMM2, WorkingCopy.TueTime2);
            SetPickerFromTime(TueHH3, TueMM3, WorkingCopy.TueTime3);

            SetPickerFromTime(WedHH1, WedMM1, WorkingCopy.WedTime1);
            SetPickerFromTime(WedHH2, WedMM2, WorkingCopy.WedTime2);
            SetPickerFromTime(WedHH3, WedMM3, WorkingCopy.WedTime3);

            SetPickerFromTime(ThuHH1, ThuMM1, WorkingCopy.ThuTime1);
            SetPickerFromTime(ThuHH2, ThuMM2, WorkingCopy.ThuTime2);
            SetPickerFromTime(ThuHH3, ThuMM3, WorkingCopy.ThuTime3);

            SetPickerFromTime(FriHH1, FriMM1, WorkingCopy.FriTime1);
            SetPickerFromTime(FriHH2, FriMM2, WorkingCopy.FriTime2);
            SetPickerFromTime(FriHH3, FriMM3, WorkingCopy.FriTime3);

            SetPickerFromTime(SatHH1, SatMM1, WorkingCopy.SatTime1);
            SetPickerFromTime(SatHH2, SatMM2, WorkingCopy.SatTime2);
            SetPickerFromTime(SatHH3, SatMM3, WorkingCopy.SatTime3);
        }

        private void SetPickerFromTime(Picker hourPicker, Picker minPicker, TimeSpan time)
        {
            hourPicker.SelectedIndex = time.Hours;
            int minIndex = time.Minutes / 15;
            if (minIndex < 0 || minIndex > 3) minIndex = 0;
            minPicker.SelectedIndex = minIndex;
        }

        private TimeSpan GetTimeFromPicker(Picker hourPicker, Picker minPicker)
        {
            int hour = hourPicker.SelectedIndex >= 0 ? hourPicker.SelectedIndex : 0;
            int minIndex = minPicker.SelectedIndex >= 0 ? minPicker.SelectedIndex : 0;
            int minute = minIndex * 15;
            return new TimeSpan(hour, minute, 0);
        }

        private void SaveTimePickersToModel()
        {
            WorkingCopy.SunTime1 = GetTimeFromPicker(SunHH1, SunMM1);
            WorkingCopy.SunTime2 = GetTimeFromPicker(SunHH2, SunMM2);
            WorkingCopy.SunTime3 = GetTimeFromPicker(SunHH3, SunMM3);

            WorkingCopy.MonTime1 = GetTimeFromPicker(MonHH1, MonMM1);
            WorkingCopy.MonTime2 = GetTimeFromPicker(MonHH2, MonMM2);
            WorkingCopy.MonTime3 = GetTimeFromPicker(MonHH3, MonMM3);

            WorkingCopy.TueTime1 = GetTimeFromPicker(TueHH1, TueMM1);
            WorkingCopy.TueTime2 = GetTimeFromPicker(TueHH2, TueMM2);
            WorkingCopy.TueTime3 = GetTimeFromPicker(TueHH3, TueMM3);

            WorkingCopy.WedTime1 = GetTimeFromPicker(WedHH1, WedMM1);
            WorkingCopy.WedTime2 = GetTimeFromPicker(WedHH2, WedMM2);
            WorkingCopy.WedTime3 = GetTimeFromPicker(WedHH3, WedMM3);

            WorkingCopy.ThuTime1 = GetTimeFromPicker(ThuHH1, ThuMM1);
            WorkingCopy.ThuTime2 = GetTimeFromPicker(ThuHH2, ThuMM2);
            WorkingCopy.ThuTime3 = GetTimeFromPicker(ThuHH3, ThuMM3);

            WorkingCopy.FriTime1 = GetTimeFromPicker(FriHH1, FriMM1);
            WorkingCopy.FriTime2 = GetTimeFromPicker(FriHH2, FriMM2);
            WorkingCopy.FriTime3 = GetTimeFromPicker(FriHH3, FriMM3);

            WorkingCopy.SatTime1 = GetTimeFromPicker(SatHH1, SatMM1);
            WorkingCopy.SatTime2 = GetTimeFromPicker(SatHH2, SatMM2);
            WorkingCopy.SatTime3 = GetTimeFromPicker(SatHH3, SatMM3);
        }

        private void InitialiseUnitPicker()
        {
            var unit = WorkingCopy.QuantityUnit;
            if (string.IsNullOrWhiteSpace(unit))
            {
                UnitPicker.SelectedIndex = 0;
                return;
            }

            var idx = -1;
            for (int i = 1; i < UnitPicker.Items.Count; i++)
            {
                if (string.Equals(UnitPicker.Items[i], unit, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }

            UnitPicker.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private bool _propagating;

        private void OnScheduleTimeChanged(object sender, EventArgs e)
        {
            if (_propagating || WorkingCopy == null)
                return;

            // Build ordered list of day rows: Sun, Mon, Tue, Wed, Thu, Fri, Sat
            var dayRows = new[]
            {
                (Active: WorkingCopy.ReminderSun, HH1: SunHH1, MM1: SunMM1, HH2: SunHH2, MM2: SunMM2, HH3: SunHH3, MM3: SunMM3),
                (Active: WorkingCopy.ReminderMon, HH1: MonHH1, MM1: MonMM1, HH2: MonHH2, MM2: MonMM2, HH3: MonHH3, MM3: MonMM3),
                (Active: WorkingCopy.ReminderTue, HH1: TueHH1, MM1: TueMM1, HH2: TueHH2, MM2: TueMM2, HH3: TueHH3, MM3: TueMM3),
                (Active: WorkingCopy.ReminderWed, HH1: WedHH1, MM1: WedMM1, HH2: WedHH2, MM2: WedMM2, HH3: WedHH3, MM3: WedMM3),
                (Active: WorkingCopy.ReminderThu, HH1: ThuHH1, MM1: ThuMM1, HH2: ThuHH2, MM2: ThuMM2, HH3: ThuHH3, MM3: ThuMM3),
                (Active: WorkingCopy.ReminderFri, HH1: FriHH1, MM1: FriMM1, HH2: FriHH2, MM2: FriMM2, HH3: FriHH3, MM3: FriMM3),
                (Active: WorkingCopy.ReminderSat, HH1: SatHH1, MM1: SatMM1, HH2: SatHH2, MM2: SatMM2, HH3: SatHH3, MM3: SatMM3),
            };

            // Find the first active day row
            var firstActive = dayRows.FirstOrDefault(d => d.Active);
            if (firstActive.HH1 == null)
                return;

            // Only propagate if the changed picker belongs to the first active row
            var changedPicker = sender as Picker;
            var firstPickers = new Picker[] { firstActive.HH1, firstActive.MM1, firstActive.HH2, firstActive.MM2, firstActive.HH3, firstActive.MM3 };
            if (changedPicker == null || !firstPickers.Contains(changedPicker))
                return;

            _propagating = true;
            try
            {
                foreach (var row in dayRows)
                {
                    if (!row.Active || row.HH1 == firstActive.HH1)
                        continue;

                    row.HH1.SelectedIndex = firstActive.HH1.SelectedIndex;
                    row.MM1.SelectedIndex = firstActive.MM1.SelectedIndex;
                    row.HH2.SelectedIndex = firstActive.HH2.SelectedIndex;
                    row.MM2.SelectedIndex = firstActive.MM2.SelectedIndex;
                    row.HH3.SelectedIndex = firstActive.HH3.SelectedIndex;
                    row.MM3.SelectedIndex = firstActive.MM3.SelectedIndex;
                }
            }
            finally
            {
                _propagating = false;
            }
        }

        private async void OnSave(object sender, TappedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(WorkingCopy.MedName))
            {
                await DisplayAlert("Missing name", "Please enter a medication name.", "OK");
                return;
            }

            var selectedUnit = UnitPicker.SelectedItem as string;
            bool isThinLayer = string.Equals(selectedUnit, "thin layer", StringComparison.OrdinalIgnoreCase);

            if (!isThinLayer && WorkingCopy.Quantity < 1)
            {
                await DisplayAlert("Invalid quantity", "Qty per dose must be at least 1.", "OK");
                return;
            }

            if (WorkingCopy.ExpiryDate == default)
                WorkingCopy.ExpiryDate = new DateTimeOffset(DateTime.UtcNow.Date.AddMonths(6), TimeSpan.Zero);

            if (Radio1Time.IsChecked) WorkingCopy.TimesPerDay = 1;
            else if (Radio2Times.IsChecked) WorkingCopy.TimesPerDay = 2;
            else WorkingCopy.TimesPerDay = 3;

            SaveTimePickersToModel();

            var saved = await _vm.SaveDataAsync(WorkingCopy);
            if (!saved) return;
            _isDirty = false;

            // Check if this medication exists in inventory
            var medName = WorkingCopy.MedName?.Trim();
            if (!string.IsNullOrWhiteSpace(medName))
            {
                var inInventory = _inventoryMeds.Any(m =>
                    string.Equals(m.MedName, medName, StringComparison.OrdinalIgnoreCase));

                if (!inInventory)
                {
                    bool addToInventory = await DisplayAlert(
                        "New Medication",
                        $"\"{medName}\" is not in inventory. Would you like to add it?",
                        "Yes", "No");

                    if (addToInventory)
                    {
                        var popup = new ActionPopup();
                        popup.ConfigureNewMedOrder("ADD TO INVENTORY", medName);

                        var result = await this.ShowPopupAsync(popup) as ActionPopup.PopupResult;
                        if (result?.Field1 != null)
                        {
                            var unit = result.Field2;
                            var indication = result.Field3;
                            int qty = 0;
                            if (!string.IsNullOrWhiteSpace(result.Field4))
                                int.TryParse(result.Field4, out qty);
                            int reorderLevel = 0;
                            if (!string.IsNullOrWhiteSpace(result.Field5))
                                int.TryParse(result.Field5, out reorderLevel);

                            var inventoryItem = new Medication
                            {
                                ResidentId = null,
                                MedName = result.Field1.Trim(),
                                StockQuantity = qty,
                                ReorderLevel = reorderLevel,
                                QuantityUnit = unit ?? "",
                                Usage = indication
                            };

                            try
                            {
                                await _medService.UpsertAsync(inventoryItem);
                            }
                            catch
                            {
                                // Queued offline
                            }
                        }
                    }
                }
            }

            // Navigate back after save + inventory check
            await Shell.Current.GoToAsync("..");
        }

        private async void OnCancel(object sender, TappedEventArgs e)
        {
            await GoBackAsync();
        }

        private async void OnDelete(object sender, TappedEventArgs e)
        {
            if (Item == null)
                return;

            var ok = await DisplayAlert("Delete", $"Delete \"{Item.MedName}\"?", "Delete", "Cancel");
            if (!ok)
                return;

            await _vm.DeleteAsync(Item);
            await GoBackAsync();
        }

        public string? ReturnTo { get; set; }

        private async Task GoBackAsync()
        {
            if (ResidentId != Guid.Empty)
            {
                var parameters = new Dictionary<string, object?>
                {
                    ["residentId"] = ResidentId,
                    ["residentName"] = ResidentName
                };

                if (!string.IsNullOrWhiteSpace(ReturnTo))
                    parameters["returnTo"] = ReturnTo;

                await Shell.Current.GoToAsync(nameof(ResidentMedicationsPage), true, parameters);
            }
            else if (!string.IsNullOrWhiteSpace(ReturnTo))
            {
                await Shell.Current.GoToAsync(ReturnTo);
            }
            else
            {
                await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
            }
        }

    }
}
