using System;
using System.Collections.Generic;

namespace MedReminder.Models
{
    public class ResidentReport
    {
        // Report identity
        public Guid ReportId { get; set; } = Guid.NewGuid();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        // Who generated it (placeholder for demo polish)
        public string GeneratedByStaff { get; set; } = "Demo Nurse";

        // Who the report is about
        public Guid ResidentId { get; set; }
        public string ResidentName { get; set; } = "";

        // Optional future extension
        public string? StaffId { get; set; }
        public string? StaffName { get; set; }

        // Snapshot data
        public Resident ResidentSnapshot { get; set; } = new();
        public List<Medication> Medications { get; set; } = new();
        public List<Observation> Observations { get; set; } = new();
    }
}
