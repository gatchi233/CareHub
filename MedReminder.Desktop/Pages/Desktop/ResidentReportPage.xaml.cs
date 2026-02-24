using System.Collections.Generic;
using MedReminder.ViewModels;
using Microsoft.Maui.Controls;

namespace MedReminder.Pages.Desktop
{
    public partial class ResidentReportPage : ContentPage, IQueryAttributable
    {
        private readonly ResidentReportViewModel _vm;

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

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"//{nameof(FloorPlanPage)}");
        }
    }
}
