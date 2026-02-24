using MedReminder.ViewModels;

namespace MedReminder.Pages.Desktop;

public partial class ResidentObservationsPage : ContentPage
{
    private readonly ResidentObservationsViewModel _vm;

    public ResidentObservationsPage(ResidentObservationsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // If navigation provided ResidentId and VM hasn't loaded yet, load now.
        if (!_vm.HasLoadedOnce)
        {
            await _vm.InitializeAsync();
        }
    }
}