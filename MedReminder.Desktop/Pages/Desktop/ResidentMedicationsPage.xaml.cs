using MedReminder.Models;
using MedReminder.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MedReminder.Pages.Desktop
{
    [QueryProperty(nameof(ResidentId), "residentId")]
    [QueryProperty(nameof(ResidentName), "residentName")]
    public partial class ResidentMedicationsPage : AuthPage
    {
        private readonly IMedicationService _medicationService;

        public Guid ResidentId { get; set; }
        public string? ResidentName { get; set; }

        public ObservableCollection<Medication> Medications { get; } =
            new ObservableCollection<Medication>();

        public ResidentMedicationsPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException("MAUI services not available.");

            _medicationService = services.GetRequiredService<IMedicationService>();

            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            HeaderLabel.Text = string.IsNullOrWhiteSpace(ResidentName)
                ? "Medications"
                : $"Medications for {ResidentName}";

            var all = await _medicationService.LoadAsync();
            var filtered = all.Where(m => m.ResidentId == ResidentId)
                              .OrderBy(m => m.MedName ?? string.Empty);

            Medications.Clear();
            foreach (var med in filtered)
                Medications.Add(med);
        }

        private async void OnViewClicked(object sender, EventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is not Medication med)
                return;

            var parameters = new Dictionary<string, object?>
            {
                ["Item"] = med,
                ["residentId"] = ResidentId,
                ["residentName"] = ResidentName
            };

            await Shell.Current.GoToAsync(nameof(EditMedicationPage), true, parameters);
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is not Medication med)
                return;

            var parameters = new Dictionary<string, object?>
            {
                ["Item"] = med,
                ["residentId"] = ResidentId,
                ["residentName"] = ResidentName
            };

            await Shell.Current.GoToAsync(nameof(EditMedicationPage), true, parameters);
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is not Medication med)
                return;

            bool ok = await DisplayAlert(
                "Delete medication",
                $"Delete \"{med.MedName}\" for {ResidentName}?",
                "Delete", "Cancel");

            if (!ok)
                return;

            await _medicationService.DeleteAsync(med);

            Medications.Remove(med);
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"{nameof(EditResidentPage)}?id={ResidentId}");
        }

        private async void OnAddMedicationClicked(object sender, EventArgs e)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["residentId"] = ResidentId,
                ["residentName"] = ResidentName
            };

            await Shell.Current.GoToAsync(nameof(EditMedicationPage), true, parameters);
        }

        private async void OnEditResidentClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"{nameof(EditResidentPage)}?id={ResidentId}");
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            if (Shell.Current is AppShell shell)
                await shell.LogoutAsync();
        }
    }
}
