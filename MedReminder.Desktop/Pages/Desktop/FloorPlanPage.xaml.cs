using MedReminder.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MedReminder.Pages.Desktop
{
    public partial class FloorPlanPage : AuthPage
    {
        public FloorPlanPage()
        {
            InitializeComponent();
            BindingContext = Application.Current!.Handler!.MauiContext!
                .Services.GetRequiredService<FloorPlanViewModel>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ((FloorPlanViewModel)BindingContext).LoadAsync();
        }

        private async void OnResidentTapped(object sender, TappedEventArgs e)
        {
            if ((sender as BindableObject)?.BindingContext is ResidentPreview r)
                await Shell.Current.GoToAsync($"{nameof(ViewResidentPage)}?id={r.Id}");
        }

        private void OnFloor1Clicked(object sender, EventArgs e)
        {
            if (BindingContext is FloorPlanViewModel vm)
                vm.Floor = 1;
        }

        private void OnFloor2Clicked(object sender, EventArgs e)
        {
            if (BindingContext is FloorPlanViewModel vm)
                vm.Floor = 2;
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            if (Shell.Current is AppShell shell)
                await shell.LogoutAsync();
        }
    }
}
