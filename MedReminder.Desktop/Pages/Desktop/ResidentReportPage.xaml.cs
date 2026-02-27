using System.Collections.Generic;
using MedReminder.ViewModels;
using Microsoft.Maui.Controls;

namespace MedReminder.Pages.Desktop
{
    public partial class ResidentReportPage : ContentPage, IQueryAttributable
    {
        private readonly ResidentReportViewModel _vm;
        private string? ReturnTo { get; set; }

        public ResidentReportPage(ResidentReportViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = _vm;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query is null)
                return;

            if (query.TryGetValue("returnTo", out var returnValue) && returnValue != null)
                ReturnTo = returnValue.ToString();

            if (query.TryGetValue("residentId", out var value) && value != null)
            {
                if (value is Guid gid)
                {
                    _vm.ResidentId = gid;
                    return;
                }

                var s = value.ToString();
                if (!string.IsNullOrWhiteSpace(s) && Guid.TryParse(s, out var parsedGuid))
                {
                    _vm.ResidentId = parsedGuid;
                    return;
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

            await Shell.Current.GoToAsync($"//{nameof(FloorPlanPage)}");
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            if (Shell.Current is AppShell shell)
                await shell.LogoutAsync();
        }
    }
}
