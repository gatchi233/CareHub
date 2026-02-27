using System;
using Microsoft.Maui.Controls;
using MedReminder.Models;
using MedReminder.ViewModels;

namespace MedReminder.Pages.Desktop
{
    [QueryProperty(nameof(Item), "Item")]
    [QueryProperty(nameof(ResidentId), "residentId")]
    [QueryProperty(nameof(ResidentName), "residentName")]
    [QueryProperty(nameof(ReturnTo), "returnTo")]

    public partial class EditMedicationPage : ContentPage
    {
        private readonly MedicationViewModel _vm;

        public Medication? Item { get; set; }
        public Guid ResidentId { get; set; }
        public string? ResidentName { get; set; }

        public Medication WorkingCopy { get; private set; } = new();

        private Picker[] _time2Pickers = null!;
        private Picker[] _time3Pickers = null!;
        private Label[] _time2Labels = null!;
        private Label[] _time3Labels = null!;

        public EditMedicationPage(MedicationViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Initialize picker arrays
            _time2Pickers = new[] { SunHH2, SunMM2, MonHH2, MonMM2, TueHH2, TueMM2, WedHH2, WedMM2, ThuHH2, ThuMM2, FriHH2, FriMM2, SatHH2, SatMM2 };
            _time3Pickers = new[] { SunHH3, SunMM3, MonHH3, MonMM3, TueHH3, TueMM3, WedHH3, WedMM3, ThuHH3, ThuMM3, FriHH3, FriMM3, SatHH3, SatMM3 };
            _time2Labels = new[] { LblHH2, LblMM2 };
            _time3Labels = new[] { LblHH3, LblMM3 };

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

            InitialiseConditionControls();
            InitialiseUnitPicker();
            LoadTimePickersFromModel();
            InitialiseTimeFrequency();
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
            bool enable2 = times >= 2;
            foreach (var picker in _time2Pickers)
                picker.IsEnabled = enable2;
            foreach (var label in _time2Labels)
                label.TextColor = enable2 ? Colors.Black : Colors.LightGray;

            bool enable3 = times >= 3;
            foreach (var picker in _time3Pickers)
                picker.IsEnabled = enable3;
            foreach (var label in _time3Labels)
                label.TextColor = enable3 ? Colors.Black : Colors.LightGray;
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

        private void InitialiseConditionControls()
        {
            var usage = WorkingCopy.Usage;

            if (string.IsNullOrWhiteSpace(usage))
            {
                ConditionPicker.SelectedIndex = 0;
                ConditionOtherEntry.Text = string.Empty;
                ConditionOtherEntry.IsEnabled = false;
                return;
            }

            var idx = -1;
            for (int i = 1; i < ConditionPicker.Items.Count - 1; i++)
            {
                if (string.Equals(ConditionPicker.Items[i], usage, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }

            if (idx > 0)
            {
                ConditionPicker.SelectedIndex = idx;
                ConditionOtherEntry.Text = string.Empty;
                ConditionOtherEntry.IsEnabled = false;
            }
            else
            {
                ConditionPicker.SelectedIndex = ConditionPicker.Items.Count - 1;
                ConditionOtherEntry.Text = usage;
                ConditionOtherEntry.IsEnabled = true;
            }
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

        private void OnConditionPickerChanged(object sender, EventArgs e)
        {
            if (ConditionPicker.SelectedIndex < 0)
                return;

            var selected = ConditionPicker.Items[ConditionPicker.SelectedIndex];

            if (selected == "Select condition")
            {
                ConditionOtherEntry.Text = string.Empty;
                ConditionOtherEntry.IsEnabled = false;
                WorkingCopy.Usage = null;
            }
            else if (selected == "Other")
            {
                ConditionOtherEntry.IsEnabled = true;
            }
            else
            {
                ConditionOtherEntry.Text = string.Empty;
                ConditionOtherEntry.IsEnabled = false;
                WorkingCopy.Usage = selected;
            }
        }

        private async void OnSave(object sender, TappedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(WorkingCopy.MedName))
            {
                await DisplayAlert("Missing name", "Please enter a medication name.", "OK");
                return;
            }

            if (ConditionPicker.SelectedIndex >= 0)
            {
                var selected = ConditionPicker.Items[ConditionPicker.SelectedIndex];

                if (selected == "Other")
                {
                    WorkingCopy.Usage = ConditionOtherEntry.Text;
                }
                else if (selected == "Select condition")
                {
                    WorkingCopy.Usage = ConditionOtherEntry.Text;
                }
                else
                {
                    WorkingCopy.Usage = selected;
                }
            }

            if (WorkingCopy.ExpiryDate == default)
                WorkingCopy.ExpiryDate = new DateTimeOffset(DateTime.UtcNow.Date.AddMonths(6), TimeSpan.Zero);

            if (Radio1Time.IsChecked) WorkingCopy.TimesPerDay = 1;
            else if (Radio2Times.IsChecked) WorkingCopy.TimesPerDay = 2;
            else WorkingCopy.TimesPerDay = 3;

            SaveTimePickersToModel();

            await _vm.SaveAsync(WorkingCopy);
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

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            if (Shell.Current is AppShell shell)
                await shell.LogoutAsync();
        }

    }
}
