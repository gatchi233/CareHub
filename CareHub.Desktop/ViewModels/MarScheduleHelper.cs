using CareHub.Desktop.Models;
using CareHub.Models;

namespace CareHub.ViewModels;

/// <summary>
/// Static helper for generating MAR schedule slots from medication data.
/// Shared by MarPageViewModel (full MAR page) and MedicationViewModel (dashboard).
/// </summary>
public static class MarScheduleHelper
{
    public static readonly TimeSpan MatchTolerance = TimeSpan.FromMinutes(90);

    public static List<MarSlotViewModel> GenerateSlots(List<Medication> meds, DateTime fromLocal, DateTime toLocal)
    {
        var slots = new List<MarSlotViewModel>();

        for (var date = fromLocal; date < toLocal; date = date.AddDays(1))
        {
            var dayOfWeek = date.DayOfWeek;

            foreach (var med in meds)
            {
                if (!med.ResidentId.HasValue)
                    continue;

                if (!IsDayEnabled(med, dayOfWeek))
                    continue;

                var times = GetTimesForDay(med, dayOfWeek);
                int slotsToUse = Math.Max(1, Math.Min(3, med.TimesPerDay));

                for (int i = 0; i < slotsToUse && i < times.Count; i++)
                {
                    var timeSpan = times[i];
                    if (timeSpan == TimeSpan.Zero)
                        continue;

                    var localDateTime = date.Add(timeSpan);
                    var utcDateTime = new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime))
                        .ToUniversalTime();

                    slots.Add(new MarSlotViewModel
                    {
                        ResidentId = med.ResidentId.Value,
                        MedicationId = med.Id,
                        LocalDate = date,
                        ScheduledLocalTime = timeSpan.ToString(@"hh\:mm"),
                        ScheduledForUtc = utcDateTime,
                        MedicationName = med.MedName,
                        DoseQuantity = med.Quantity,
                        DoseUnit = med.QuantityUnit,
                        Status = "Pending"
                    });
                }
            }
        }

        return slots;
    }

    public static void OverlayMarEntries(List<MarSlotViewModel> slots, List<MarEntry> entries, HashSet<Guid> matchedEntryIds)
    {
        foreach (var slot in slots)
        {
            var candidates = entries
                .Where(e =>
                    !e.IsVoided &&
                    e.MedicationId == slot.MedicationId &&
                    e.ResidentId == slot.ResidentId &&
                    Math.Abs((e.AdministeredAtUtc - slot.ScheduledForUtc).TotalMinutes) <= MatchTolerance.TotalMinutes)
                .OrderByDescending(e => e.AdministeredAtUtc)
                .ToList();

            if (candidates.Count > 0)
            {
                var best = candidates[0];
                slot.Status = best.Status;
                slot.LastAdministeredLocal = best.AdministeredAtUtc.ToLocalTime().ToString("HH:mm");
                slot.RecordedBy = best.RecordedBy;
                slot.NotesPreview = best.Notes;

                foreach (var c in candidates)
                    matchedEntryIds.Add(c.Id);
            }
        }
    }

    public static bool IsDayEnabled(Medication med, DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => med.ReminderMon,
        DayOfWeek.Tuesday => med.ReminderTue,
        DayOfWeek.Wednesday => med.ReminderWed,
        DayOfWeek.Thursday => med.ReminderThu,
        DayOfWeek.Friday => med.ReminderFri,
        DayOfWeek.Saturday => med.ReminderSat,
        DayOfWeek.Sunday => med.ReminderSun,
        _ => false
    };

    public static List<TimeSpan> GetTimesForDay(Medication med, DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => new() { med.MonTime1, med.MonTime2, med.MonTime3 },
        DayOfWeek.Tuesday => new() { med.TueTime1, med.TueTime2, med.TueTime3 },
        DayOfWeek.Wednesday => new() { med.WedTime1, med.WedTime2, med.WedTime3 },
        DayOfWeek.Thursday => new() { med.ThuTime1, med.ThuTime2, med.ThuTime3 },
        DayOfWeek.Friday => new() { med.FriTime1, med.FriTime2, med.FriTime3 },
        DayOfWeek.Saturday => new() { med.SatTime1, med.SatTime2, med.SatTime3 },
        DayOfWeek.Sunday => new() { med.SunTime1, med.SunTime2, med.SunTime3 },
        _ => new()
    };

    public static (DateTime fromUtc, DateTime toUtc, DateTime fromLocal, DateTime toLocal) GetTodayRange()
    {
        var todayLocal = DateTime.Now.Date;
        var toLocal = todayLocal.AddDays(1);
        var fromUtc = todayLocal.ToUniversalTime();
        var toUtc = toLocal.ToUniversalTime();
        return (fromUtc, toUtc, todayLocal, toLocal);
    }
}
