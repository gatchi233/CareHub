using MedReminder.Models;
using MedReminder.Services;
using MedReminder.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;

namespace MedReminder.Pages.Desktop
{
    public partial class ResidentsPage : AuthPage
    {
        private readonly ResidentsPageViewModel _vm;

        public ResidentsPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException("MAUI services not available.");

            _vm = services.GetRequiredService<ResidentsPageViewModel>();

            BindingContext = _vm;
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            NameSearchBar?.Unfocus();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var auth = MauiProgram.Services.GetService<AuthService>();
            var canEdit = auth?.HasRole(StaffRole.Admin, StaffRole.Nurse) ?? false;

            // Hide the top ADD button
            if (AddAction != null)
                AddAction.IsVisible = canEdit;

            await _vm.LoadResidentsAsync();
            _vm.UpdateFilters(NameSearchBar?.Text ?? string.Empty);
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            _vm.UpdateFilters(NameSearchBar?.Text ?? string.Empty);
        }

        private async void OnAddResidentClicked(object sender, EventArgs e)
        {
            var auth = MauiProgram.Services.GetService<AuthService>();
            if (!(auth?.HasRole(StaffRole.Admin, StaffRole.Nurse) ?? false))
            {
                await DisplayAlert("Access denied", "You don't have permission to add residents.", "OK");
                return;
            }

            await Shell.Current.GoToAsync(nameof(EditResidentPage));
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

    }
}
