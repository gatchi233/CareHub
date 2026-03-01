using CareHub.ViewModels;

namespace CareHub.Pages.Desktop
{
    public partial class StaffManagementPage : ContentPage
    {
        private readonly StaffManagementViewModel _vm;

        public StaffManagementPage(StaffManagementViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.RefreshAsync();
        }

    }
}
