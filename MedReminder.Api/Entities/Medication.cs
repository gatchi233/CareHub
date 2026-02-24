using System.ComponentModel.DataAnnotations;

namespace MedReminder.Api.Entities
{
    public sealed class Medication
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string MedName { get; set; } = "";

        public string Dosage { get; set; } = "";
        public string? Usage { get; set; }

        public int Quantity { get; set; }
        public string QuantityUnit { get; set; } = "";

        // Inventory
        public int StockQuantity { get; set; } = 0;
        public int ReorderLevel { get; set; } = 10;

        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddMonths(6);

        // Link to resident
        public Guid? ResidentId { get; set; }
        public string? ResidentName { get; set; }

        // Schedule/state
        public bool IsDone { get; set; }
        public int TimesPerDay { get; set; } = 3;

        public bool ReminderMon { get; set; } = true;
        public bool ReminderTue { get; set; } = true;
        public bool ReminderWed { get; set; } = true;
        public bool ReminderThu { get; set; } = true;
        public bool ReminderFri { get; set; } = true;
        public bool ReminderSat { get; set; } = true;
        public bool ReminderSun { get; set; } = true;

        // NOTE: TimeSpan will map to interval in Postgres via Npgsql.
        public TimeSpan MonTime1 { get; set; } = new(8, 0, 0);
        public TimeSpan MonTime2 { get; set; } = new(14, 0, 0);
        public TimeSpan MonTime3 { get; set; } = new(20, 0, 0);

        public TimeSpan TueTime1 { get; set; } = new(8, 0, 0);
        public TimeSpan TueTime2 { get; set; } = new(14, 0, 0);
        public TimeSpan TueTime3 { get; set; } = new(20, 0, 0);

        public TimeSpan WedTime1 { get; set; } = new(8, 0, 0);
        public TimeSpan WedTime2 { get; set; } = new(14, 0, 0);
        public TimeSpan WedTime3 { get; set; } = new(20, 0, 0);

        public TimeSpan ThuTime1 { get; set; } = new(8, 0, 0);
        public TimeSpan ThuTime2 { get; set; } = new(14, 0, 0);
        public TimeSpan ThuTime3 { get; set; } = new(20, 0, 0);

        public TimeSpan FriTime1 { get; set; } = new(8, 0, 0);
        public TimeSpan FriTime2 { get; set; } = new(14, 0, 0);
        public TimeSpan FriTime3 { get; set; } = new(20, 0, 0);

        public TimeSpan SatTime1 { get; set; } = new(8, 0, 0);
        public TimeSpan SatTime2 { get; set; } = new(14, 0, 0);
        public TimeSpan SatTime3 { get; set; } = new(20, 0, 0);

        public TimeSpan SunTime1 { get; set; } = new(8, 0, 0);
        public TimeSpan SunTime2 { get; set; } = new(14, 0, 0);
        public TimeSpan SunTime3 { get; set; } = new(20, 0, 0);
    }
}