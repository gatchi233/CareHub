using CareHub.Models;
using CareHub.Mobile.ViewModels;

namespace CareHub.Mobile.Pages;

[QueryProperty(nameof(Resident), "Resident")]
public partial class ResidentDetailPage : ContentPage
{
    private readonly ResidentDetailViewModel _vm;

    public ResidentDetailPage(ResidentDetailViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    public Resident? Resident { get; set; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Resident is null)
            return;

        await _vm.LoadAsync(Resident);
    }
}
