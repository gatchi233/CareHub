using System.Text.Json;

namespace MedReminder.Models
{
    public sealed class Observation
    {
        public Guid Id { get; set; }
        public Guid ResidentId { get; set; }
        public string ResidentName { get; set; } = "";
        public DateTime RecordedAt { get; set; }
        public string Type { get; set; } = "";   // "Vitals", "BP", "Temp", "Note", etc.
        public string Value { get; set; } = "";
        public string RecordedBy { get; set; } = "";

        // --- Vitals JSON helpers ---

        public bool IsVitals => Type == "Vitals";

        private VitalsData? _parsed;

        public VitalsData Vitals
        {
            get
            {
                if (_parsed != null) return _parsed;
                if (!IsVitals || string.IsNullOrWhiteSpace(Value))
                    return _parsed = new VitalsData();
                try { _parsed = JsonSerializer.Deserialize<VitalsData>(Value) ?? new VitalsData(); }
                catch { _parsed = new VitalsData(); }
                return _parsed;
            }
        }

        public void SetVitals(VitalsData data)
        {
            Type = "Vitals";
            _parsed = data;
            Value = JsonSerializer.Serialize(data);
        }

        // Display helpers for the record card
        public bool HasTemp => IsVitals && !string.IsNullOrWhiteSpace(Vitals.Temp);
        public bool HasBp => IsVitals && !string.IsNullOrWhiteSpace(Vitals.BpHigh);
        public bool HasPulse => IsVitals && !string.IsNullOrWhiteSpace(Vitals.Pulse);
        public bool HasSpo2 => IsVitals && !string.IsNullOrWhiteSpace(Vitals.Spo2);
        public bool HasNotes => IsVitals && !string.IsNullOrWhiteSpace(Vitals.Notes);

        public string DisplayTemp => HasTemp ? $"{Vitals.Temp} °C" : "";
        public string DisplayBp => HasBp ? $"{Vitals.BpHigh}/{Vitals.BpLow} mmHg" : "";
        public string DisplayPulse => HasPulse ? $"{Vitals.Pulse} bpm" : "";
        public string DisplaySpo2 => HasSpo2 ? $"{Vitals.Spo2} %" : "";
        public string DisplayNotes => HasNotes ? Vitals.Notes! : "";

        // For legacy single-type records
        public bool IsLegacy => !IsVitals;
    }

    public sealed class VitalsData
    {
        public string? Temp { get; set; }
        public string? BpHigh { get; set; }
        public string? BpLow { get; set; }
        public string? Pulse { get; set; }
        public string? Spo2 { get; set; }
        public string? Notes { get; set; }
    }
}
