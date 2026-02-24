using MedReminder.Models;
using MedReminder.Services;
using MedReminder.Services.Abstractions;
using System.Windows.Input;

namespace MedReminder.ViewModels
{
    [QueryProperty(nameof(ResidentId), "residentId")]
    public class ResidentReportViewModel : BindableObject
    {
        private readonly IResidentService _residentService;
        private readonly IMedicationService _medicationService;
        private readonly IObservationService _observationService;
        private bool _isLoading;
        private Guid _lastLoadedResidentId = Guid.Empty;

        private Guid _residentId;
        public Guid ResidentId
        {
            get => _residentId;
            set
            {
                _residentId = value;
                _ = SafeLoadAsync(value);
            }
        }

        public string TitleText { get; private set; } = "Resident Report";
        public string SubtitleText { get; private set; } = "";
        public string HtmlPreview { get; private set; } = "";

        public ICommand ShareReportCommand { get; }

        public ResidentReportViewModel(
            IResidentService residentService,
            IMedicationService medicationService,
            IObservationService observationService)
        {
            _residentService = residentService;
            _medicationService = medicationService;
            _observationService = observationService;

            ShareReportCommand = new Command(async () => await ShareAsync());
        }

        public async Task LoadAsync(Guid residentId)
        {
            _residentId = residentId;

            var residents = await _residentService.LoadAsync();
            var resident = residents.FirstOrDefault(r => r.Id == residentId);

            if (resident == null)
            {
                TitleText = "Resident Report";
                SubtitleText = "Resident not found.";
                HtmlPreview = "";
                OnPropertyChanged(nameof(TitleText));
                OnPropertyChanged(nameof(SubtitleText));
                OnPropertyChanged(nameof(HtmlPreview));
                return;
            }

            var meds = await _medicationService.LoadAsync();
            var residentMeds = meds
                .Where(m => m.ResidentId == resident.Id)
                .ToList();

            var obs = await _observationService.LoadAsync();
            var recentObs = obs
                .Where(o => o.ResidentId == residentId)
                .OrderByDescending(o => o.ObservedAt)
                .ToList();

            // Demo placeholder (later plug in AuthService username)
            var generatedBy = "Demo Nurse";

            var report = new ResidentReport
            {
                ResidentId = resident.Id,
                ResidentName = resident.FullName,
                GeneratedAt = DateTime.Now,
                GeneratedByStaff = generatedBy,

                ResidentSnapshot = resident,
                Medications = residentMeds,
                Observations = recentObs
            };

            var html = ResidentReportBuilder.BuildHtml(report);

            TitleText = $"Resident Report — {report.ResidentName}";
            SubtitleText = $"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm}  •  By: {report.GeneratedByStaff}";
            HtmlPreview = html;

            OnPropertyChanged(nameof(TitleText));
            OnPropertyChanged(nameof(SubtitleText));
            OnPropertyChanged(nameof(HtmlPreview));
        }

        private async Task ShareAsync()
        {
            if (string.IsNullOrWhiteSpace(HtmlPreview))
            {
                await Application.Current!.MainPage!.DisplayAlert("Share", "No report content to share.", "OK");
                return;
            }

            var fileName = $"ResidentReport_{_residentId}_{DateTime.Now:yyyyMMdd_HHmm}.html";
            var path = Path.Combine(FileSystem.CacheDirectory, fileName);

            await File.WriteAllTextAsync(path, HtmlPreview);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Share Resident Report",
                File = new ShareFile(path)
            });
        }

        private async Task SafeLoadAsync(Guid residentId)
        {
            if (_isLoading) return;
            if (residentId == Guid.Empty) return;
            if (residentId == _lastLoadedResidentId) return;

            _isLoading = true;
            try
            {
                await LoadAsync(residentId);
                _lastLoadedResidentId = residentId;
            }
            finally
            {
                _isLoading = false;
            }
        }
    }
}
