using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedReminder.Models
{
    public sealed class Observation
    {
        public Guid Id { get; set; }
        public Guid ResidentId { get; set; }

        // (Optional improvement later) This is denormalized; can be removed once DB exists.
        public string ResidentName { get; set; } = "";

        public DateTime ObservedAt { get; set; }
        public string ObservedByStaffId { get; set; } = "";
        public string Category { get; set; } = "";   // e.g. "Vital Signs"
        public string Severity { get; set; } = "";   // e.g. "Normal", "Low", "Medium"
        public string Note { get; set; } = "";

        // NEW (only used when Category == "Vital Signs")
        public VitalReading? Vitals { get; set; }
    }

    public sealed class VitalReading
    {
        public decimal? TemperatureC { get; set; }      // e.g. 36.7
        public int? Systolic { get; set; }              // e.g. 120
        public int? Diastolic { get; set; }             // e.g. 80
        public int? HeartRate { get; set; }             // optional
        public int? OxygenSaturation { get; set; }      // SpO2 %, e.g. 96
    }
}

