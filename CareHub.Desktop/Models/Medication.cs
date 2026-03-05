using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace CareHub.Models
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

        public DateTimeOffset? PurchaseDate { get; set; }

        [JsonIgnore]
        public DateTime? PurchaseDateLocal
        {
            get => PurchaseDate?.UtcDateTime.Date;
            set => PurchaseDate = value.HasValue
                ? new DateTimeOffset(value.Value.Date, TimeSpan.Zero)
                : null;
        }

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

                return $"{dayText} · {timeText}";
            }
        }

        // Per-slot completion (IsDone = slot 1 for backward compat)
        public bool IsDoneSlot2 { get; set; }
        public bool IsDoneSlot3 { get; set; }

        [JsonIgnore]
        public bool HasSlot2 => TimesPerDay >= 2;
        [JsonIgnore]
        public bool HasSlot3 => TimesPerDay >= 3;

        [JsonIgnore]
        public TimeSpan TodayTime1 => GetTodayTime(1);
        [JsonIgnore]
        public TimeSpan TodayTime2 => GetTodayTime(2);
        [JsonIgnore]
        public TimeSpan TodayTime3 => GetTodayTime(3);

        private TimeSpan GetTodayTime(int slot)
        {
            var day = DateTime.Today.DayOfWeek;
            return (day, slot) switch
            {
                (DayOfWeek.Monday, 1) => MonTime1, (DayOfWeek.Monday, 2) => MonTime2, (DayOfWeek.Monday, 3) => MonTime3,
                (DayOfWeek.Tuesday, 1) => TueTime1, (DayOfWeek.Tuesday, 2) => TueTime2, (DayOfWeek.Tuesday, 3) => TueTime3,
                (DayOfWeek.Wednesday, 1) => WedTime1, (DayOfWeek.Wednesday, 2) => WedTime2, (DayOfWeek.Wednesday, 3) => WedTime3,
                (DayOfWeek.Thursday, 1) => ThuTime1, (DayOfWeek.Thursday, 2) => ThuTime2, (DayOfWeek.Thursday, 3) => ThuTime3,
                (DayOfWeek.Friday, 1) => FriTime1, (DayOfWeek.Friday, 2) => FriTime2, (DayOfWeek.Friday, 3) => FriTime3,
                (DayOfWeek.Saturday, 1) => SatTime1, (DayOfWeek.Saturday, 2) => SatTime2, (DayOfWeek.Saturday, 3) => SatTime3,
                (DayOfWeek.Sunday, 1) => SunTime1, (DayOfWeek.Sunday, 2) => SunTime2, (DayOfWeek.Sunday, 3) => SunTime3,
                _ => new TimeSpan(8, 0, 0)
            };
        }

        [JsonIgnore]
        public int DisplayIndex { get; set; }

        // MAR status per slot (populated from MAR entries, display-only on HomePage)
        [JsonIgnore]
        public string Slot1Status { get; set; } = "Pending";
        [JsonIgnore]
        public string Slot2Status { get; set; } = "Pending";
        [JsonIgnore]
        public string Slot3Status { get; set; } = "Pending";

        [JsonIgnore]
        public string? Slot1AdminTime { get; set; }
        [JsonIgnore]
        public string? Slot2AdminTime { get; set; }
        [JsonIgnore]
        public string? Slot3AdminTime { get; set; }

        [JsonIgnore]
        public bool Slot1HasTime => Slot1Status == "Given" || Slot1Status == "Refused";
        [JsonIgnore]
        public bool Slot2HasTime => Slot2Status == "Given" || Slot2Status == "Refused";
        [JsonIgnore]
        public bool Slot3HasTime => Slot3Status == "Given" || Slot3Status == "Refused";

        [JsonIgnore]
        public Microsoft.Maui.Graphics.Color Slot1StatusColor => GetStatusColor(Slot1Status);
        [JsonIgnore]
        public Microsoft.Maui.Graphics.Color Slot2StatusColor => GetStatusColor(Slot2Status);
        [JsonIgnore]
        public Microsoft.Maui.Graphics.Color Slot3StatusColor => GetStatusColor(Slot3Status);

        private static Microsoft.Maui.Graphics.Color GetStatusColor(string status) => status switch
        {
            "Given" => Microsoft.Maui.Graphics.Color.FromArgb("#2E7D32"),
            "Refused" => Microsoft.Maui.Graphics.Color.FromArgb("#E65100"),
            "Held" => Microsoft.Maui.Graphics.Color.FromArgb("#F57F17"),
            "Missed" => Microsoft.Maui.Graphics.Color.FromArgb("#C62828"),
            "NotAvailable" => Microsoft.Maui.Graphics.Color.FromArgb("#616161"),
            _ => Microsoft.Maui.Graphics.Color.FromArgb("#757575") // Pending
        };

        public bool IsExpired => DateTimeOffset.UtcNow.Date > ExpiryDate.Date;

        public int DaysUntilExpiry =>
            (int)Math.Ceiling((ExpiryDate.Date - DateTimeOffset.UtcNow.Date).TotalDays);


        // Inventory helper
        public bool IsLowStock => StockQuantity <= ReorderLevel;
    }
}
