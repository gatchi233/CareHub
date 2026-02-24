using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MedReminder.Models
{
    public class Medication
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string MedName { get; set; } = "";
        public string Dosage { get; set; } = "";

        // Optional (some meds may not have a usage/condition)
        public string? Usage { get; set; }

        public int Quantity { get; set; }

        public string QuantityUnit { get; set; } = "";

        // Inventory fields
        [JsonPropertyName("StockQuantity")]
        public int StockQuantity { get; set; } = 0;
        public int ReorderLevel { get; set; } = 10;

        public DateTimeOffset ExpiryDate { get; set; } =
            new DateTimeOffset(DateTime.UtcNow.Date.AddMonths(6), TimeSpan.Zero);

        [JsonIgnore]
        public DateTime ExpiryDateLocal
        {
            get => ExpiryDate.UtcDateTime.Date;
            set => ExpiryDate = new DateTimeOffset(value.Date, TimeSpan.Zero);
        }

        [JsonIgnore]
        public int StockQty
        {
            get => StockQuantity;
            set => StockQuantity = value;
        }

        public Guid? ResidentId { get; set; }
        public string? ResidentName { get; set; }

        public bool IsDone { get; set; }

        public int TimesPerDay { get; set; } = 3;

        public bool ReminderMon { get; set; } = true;
        public bool ReminderTue { get; set; } = true;
        public bool ReminderWed { get; set; } = true;
        public bool ReminderThu { get; set; } = true;
        public bool ReminderFri { get; set; } = true;
        public bool ReminderSat { get; set; } = true;
        public bool ReminderSun { get; set; } = true;

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

        public TimeSpan ReminderTime
        {
            get
            {
                var today = DateTime.Today.DayOfWeek;
                return today switch
                {
                    DayOfWeek.Monday => MonTime1,
                    DayOfWeek.Tuesday => TueTime1,
                    DayOfWeek.Wednesday => WedTime1,
                    DayOfWeek.Thursday => ThuTime1,
                    DayOfWeek.Friday => FriTime1,
                    DayOfWeek.Saturday => SatTime1,
                    DayOfWeek.Sunday => SunTime1,
                    _ => new TimeSpan(8, 0, 0)
                };
            }
            set
            {
                MonTime1 = TueTime1 = WedTime1 = ThuTime1 = FriTime1 = SatTime1 = SunTime1 = value;
            }
        }

        public string ScheduleSummary
        {
            get
            {
                var days = new List<string>();
                if (ReminderSun) days.Add("Sun");
                if (ReminderMon) days.Add("Mon");
                if (ReminderTue) days.Add("Tue");
                if (ReminderWed) days.Add("Wed");
                if (ReminderThu) days.Add("Thu");
                if (ReminderFri) days.Add("Fri");
                if (ReminderSat) days.Add("Sat");

                string dayText = days.Count > 0 ? string.Join(", ", days) : "No days set";

                var times = new HashSet<TimeSpan>();

                void AddDayTimes(bool dayEnabled, TimeSpan t1, TimeSpan t2, TimeSpan t3)
                {
                    if (!dayEnabled) return;

                    int n = TimesPerDay <= 0 ? 3 : TimesPerDay;

                    if (n >= 1) times.Add(t1);
                    if (n >= 2) times.Add(t2);
                    if (n >= 3) times.Add(t3);
                }

                AddDayTimes(ReminderMon, MonTime1, MonTime2, MonTime3);
                AddDayTimes(ReminderTue, TueTime1, TueTime2, TueTime3);
                AddDayTimes(ReminderWed, WedTime1, WedTime2, WedTime3);
                AddDayTimes(ReminderThu, ThuTime1, ThuTime2, ThuTime3);
                AddDayTimes(ReminderFri, FriTime1, FriTime2, FriTime3);
                AddDayTimes(ReminderSat, SatTime1, SatTime2, SatTime3);
                AddDayTimes(ReminderSun, SunTime1, SunTime2, SunTime3);

                if (times.Count == 0 && ReminderTime != default)
                    times.Add(ReminderTime);

                var ordered = times.OrderBy(t => t).ToList();

                string timeText = ordered.Count > 0
                    ? string.Join(", ", ordered.Select(t => t.ToString(@"hh\:mm")))
                    : "No time set";

                return $"{dayText} • {timeText}";
            }
        }

        public bool IsExpired => DateTimeOffset.UtcNow.Date > ExpiryDate.Date;

        public int DaysUntilExpiry =>
            (int)Math.Ceiling((ExpiryDate.Date - DateTimeOffset.UtcNow.Date).TotalDays);


        // Inventory helper
        public bool IsLowStock => StockQuantity <= ReorderLevel;
    }
}
