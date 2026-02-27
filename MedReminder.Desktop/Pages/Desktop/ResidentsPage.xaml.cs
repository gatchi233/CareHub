using MedReminder.Models;
using MedReminder.Services;
using MedReminder.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using static MedReminder.ViewModels.ResidentsPageViewModel;

namespace MedReminder.Pages.Desktop
{
    public partial class ResidentsPage : AuthPage
    {
        private readonly ResidentsPageViewModel _vm;
        private bool _canEditResidents;

        public ResidentsPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException("MAUI services not available.");

            _vm = services.GetRequiredService<ResidentsPageViewModel>();

            BindingContext = _vm;
        }
        public bool CanEditResidents
        {
            get => _canEditResidents;
            set
            {
                if (_canEditResidents == value) return;
                _canEditResidents = value;
                OnPropertyChanged(nameof(CanEditResidents));
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var auth = MauiProgram.Services.GetService<AuthService>();
            CanEditResidents = auth?.HasRole(StaffRole.Admin, StaffRole.Nurse) ?? false;
            OnPropertyChanged(nameof(CanEditResidents));

            // Hide the top ADD button
            if (AddAction != null)
                AddAction.IsVisible = CanEditResidents;

            await _vm.LoadResidentsAsync();
            _vm.UpdateFilters(NameSearchBar?.Text ?? string.Empty);
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            _vm.UpdateFilters(NameSearchBar?.Text ?? string.Empty);
        }

        private bool CanEdit()
        {
            var auth = MauiProgram.Services.GetService<AuthService>();
            return auth?.HasRole(StaffRole.Admin, StaffRole.Nurse) ?? false;
        }

        private async void OnAddResidentClicked(object sender, EventArgs e)
        {
            if (!CanEdit())
            {
                await DisplayAlert("Access denied", "You don't have permission to add residents.", "OK");
                return;
            }

            await Shell.Current.GoToAsync(nameof(EditResidentPage));
        }

        private async void OnEditResidentClicked(object sender, EventArgs e)
        {
            if (!CanEdit())
            {
                await DisplayAlert("Access denied", "You don't have permission to edit residents.", "OK");
                return;
            }

            if ((sender as BindableObject)?.BindingContext is not Resident r)
                return;

            await Shell.Current.GoToAsync($"{nameof(EditResidentPage)}?id={r.Id}");
        }

        private async void OnResidentClicked(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count == 0)
                return;

            if (e.CurrentSelection[0] is not Resident r)
                return;

            if (sender is CollectionView cv)
                cv.SelectedItem = null;

            await Shell.Current.GoToAsync($"{nameof(ViewResidentPage)}?id={r.Id}");
        }

        private async void OnViewResidentClicked(object sender, EventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is not Resident r)
                return;

            await Shell.Current.GoToAsync($"{nameof(ViewResidentPage)}?id={r.Id}");
        }


        //private async void OnViewMedClicked(object sender, EventArgs e)
        //{
        //    if ((sender as BindableObject)?.BindingContext is not Resident r)
        //        return;

        //    var parameters = new Dictionary<string, object?>
        //    {
        //        ["residentId"] = r.Id,
        //        ["residentName"] = r.ResidentName
        //    };

        //    await Shell.Current.GoToAsync(nameof(ResidentMedicationsPage), true, parameters);
        //}

        private async void OnDeleteResidentClicked(object sender, EventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is not Resident r)
                return;

            bool confirm = await DisplayAlert(
                "Delete resident",
                $"Are you sure you want to delete {r.ResidentName}?",
                "Delete", "Cancel");

            if (!confirm)
                return;

            await _vm.DeleteResidentAsync(r);
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            if (Shell.Current is AppShell shell)
                await shell.LogoutAsync();
        }

    }
}
