namespace CareHub.Models
{
    public class DoctorStaffEntry
    {
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorContact { get; set; } = string.Empty;
        public int ResidentCount { get; set; }
        public string ResidentSummary { get; set; } = string.Empty;
    }
}
