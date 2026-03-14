using System.Collections.Concurrent;

namespace CareHub.Api.Services;

/// <summary>
/// In-memory rate limiter for AI endpoints, aligned to Groq free-tier limits.
/// Enforces two windows: per-minute (RPM) and per-day (RPD).
/// Limits are applied globally (all users share one Groq org key).
/// Per-user limits prevent a single user from hogging the quota.
/// </summary>
public sealed class AiRateLimiter
{
    private readonly int _globalRpm;
    private readonly int _globalRpd;
    private readonly int _perUserRpm;
    private readonly int _perUserRpd;

    private readonly List<DateTimeOffset> _globalMinuteBucket = new();
    private readonly List<DateTimeOffset> _globalDayBucket = new();
    private readonly object _globalLock = new();

    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _userMinuteBuckets = new();
    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _userDayBuckets = new();

    /// <param name="globalRpm">Global requests per minute (across all users).</param>
    /// <param name="globalRpd">Global requests per day (across all users).</param>
    /// <param name="perUserRpm">Per-user requests per minute.</param>
    /// <param name="perUserRpd">Per-user requests per day.</param>
    public AiRateLimiter(
        int globalRpm = 27,
        int globalRpd = 900,
        int perUserRpm = 10,
        int perUserRpd = 200)
    {
        _globalRpm = globalRpm;
        _globalRpd = globalRpd;
        _perUserRpm = perUserRpm;
        _perUserRpd = perUserRpd;
    }

    /// <summary>
    /// Returns null if allowed, or an error message if rate-limited.
    /// </summary>
    public string? TryAcquire(string userId)
    {
        var now = DateTimeOffset.UtcNow;
        var minuteCutoff = now.AddMinutes(-1);
        var dayCutoff = now.AddHours(-24);

        lock (_globalLock)
        {
            // Prune expired entries
            _globalMinuteBucket.RemoveAll(t => t < minuteCutoff);
            _globalDayBucket.RemoveAll(t => t < dayCutoff);

            if (_globalMinuteBucket.Count >= _globalRpm)
                return $"AI service is busy ({_globalRpm} requests/min reached). Please wait a moment.";

            if (_globalDayBucket.Count >= _globalRpd)
                return $"AI daily limit reached ({_globalRpd} requests/day). Please try again tomorrow.";
        }

        // Per-user per-minute check
        var userMin = _userMinuteBuckets.GetOrAdd(userId, _ => new List<DateTimeOffset>());
        lock (userMin)
        {
            userMin.RemoveAll(t => t < minuteCutoff);
            if (userMin.Count >= _perUserRpm)
                return $"Slow down — max {_perUserRpm} AI requests/min. Please wait a moment.";
        }

        // Per-user per-day check
        var userDay = _userDayBuckets.GetOrAdd(userId, _ => new List<DateTimeOffset>());
        lock (userDay)
        {
            userDay.RemoveAll(t => t < dayCutoff);
            if (userDay.Count >= _perUserRpd)
                return $"You've reached your daily limit of {_perUserRpd} AI requests. Please try again tomorrow.";
        }

        // All checks passed — record the request
        lock (userMin) { userMin.Add(now); }
        lock (userDay) { userDay.Add(now); }
        lock (_globalLock)
        {
            _globalMinuteBucket.Add(now);
            _globalDayBucket.Add(now);
        }

        return null;
    }
}
