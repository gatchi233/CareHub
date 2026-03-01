using CareHub.Models;
using CareHub.Mobile.ViewModels;

namespace CareHub.Mobile.Pages;

public partial class ResidentsPage : ContentPage
{
    private readonly ResidentsViewModel _vm;

    public ResidentsPage(ResidentsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is not Resident selected)
            return;

        ((CollectionView)sender).SelectedItem = null;

        await Microsoft.Maui.Controls.Shell.Current.GoToAsync(nameof(ResidentDetailPage), new Dictionary<string, object>
        {
            ["Resident"] = selected
        });
    }
}
