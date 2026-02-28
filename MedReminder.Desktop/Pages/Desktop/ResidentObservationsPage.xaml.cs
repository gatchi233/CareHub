using CommunityToolkit.Maui.Views;
using MedReminder.Models;
using MedReminder.Pages.UI.Popups;
using MedReminder.ViewModels;
using System.Windows.Input;
using static MedReminder.ViewModels.ResidentObservationsViewModel;

namespace MedReminder.Pages.Desktop;

public partial class ResidentObservationsPage : ContentPage, IQueryAttributable
{
    private readonly ResidentObservationsViewModel _vm;

    public string? ReturnTo { get; private set; }

    public ResidentObservationsPage(ResidentObservationsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        _vm.AddRequested = () => _ = ShowAddObservationPopupAsync();
        _vm.EditRequested = (obs) => _ = ShowEditObservationPopupAsync(obs);
    }

    private async Task ShowAddObservationPopupAsync()
    {
        var popup = new ActionPopup();
        popup.ConfigureObservation("ADD OBSERVATION");

        var result = await this.ShowPopupAsync(popup);

        if (result is not ActionPopup.PopupResult r)
            return;

        await _vm.AddObservationFromPopupAsync(
            r.Field1, r.Field2, r.Field3,
            r.Field4, r.Field5, r.Field6);
    }

    private async Task ShowEditObservationPopupAsync(Observation obs)
    {
        var popup = new ActionPopup();

        if (obs.IsVitals)
        {
            var v = obs.Vitals;
            popup.ConfigureObservation("EDIT OBSERVATION",
                temp: v.Temp, bpHigh: v.BpHigh, bpLow: v.BpLow,
                pulse: v.Pulse, spo2: v.Spo2, notes: v.Notes);
        }
        else
        {
            // Legacy single-type records: map into the appropriate field
            switch (obs.Type)
            {
                case "Temp":
                    popup.ConfigureObservation("EDIT OBSERVATION",
                        temp: obs.Value.Replace(" °C", "").Trim());
                    break;
                case "BP":
                    var parts = obs.Value.Replace(" mmHg", "").Split('/');
                    popup.ConfigureObservation("EDIT OBSERVATION",
                        bpHigh: parts.Length > 0 ? parts[0].Trim() : null,
                        bpLow: parts.Length > 1 ? parts[1].Trim() : null);
                    break;
                case "Pulse":
                    popup.ConfigureObservation("EDIT OBSERVATION",
                        pulse: obs.Value.Replace(" bpm", "").Trim());
                    break;
                case "SPO2":
                    popup.ConfigureObservation("EDIT OBSERVATION",
                        spo2: obs.Value.Replace(" %", "").Trim());
                    break;
                default:
                    popup.ConfigureObservation("EDIT OBSERVATION",
                        notes: obs.Value);
                    break;
            }
        }

        var result = await this.ShowPopupAsync(popup);

        if (result is not ActionPopup.PopupResult r)
            return;

        await _vm.EditObservationFromPopupAsync(obs,
            r.Field1, r.Field2, r.Field3,
            r.Field4, r.Field5, r.Field6);
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
                await _vm.LoadAsync();
            }
        }
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
