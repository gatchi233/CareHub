using CareHub.ViewModels;

namespace CareHub.Pages.Desktop;

public partial class MarPage : ContentPage, IQueryAttributable
{
    private readonly MarPageViewModel _vm;

    public string? ReturnTo { get; private set; }

    public MarPage(MarPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query is null)
            return;

        if (query.TryGetValue("returnTo", out var returnValue) && returnValue != null)
            ReturnTo = returnValue.ToString();

        if (query.TryGetValue("residentId", out var value) && value != null)
        {
            Guid residentId = Guid.Empty;

            if (value is Guid g)
                residentId = g;
            else if (value is string s && Guid.TryParse(s, out var parsed))
                residentId = parsed;

            if (residentId != Guid.Empty)
            {
                _vm.SetResident(residentId, string.Empty);
            }
        }

        if (query.TryGetValue("residentName", out var nameValue) && nameValue != null)
        {
            _vm.SetResident(_vm.ResidentId, nameValue.ToString() ?? "");
        }

        await _vm.LoadAsync();
    }

    private async void OnCloseClicked(object sender, TappedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ReturnTo))
        {
            await Shell.Current.GoToAsync(ReturnTo);
            return;
        }

        await Shell.Current.GoToAsync("..");
    }
}
