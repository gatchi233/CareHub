namespace MedReminder.Desktop.Services.Sync;

public static class ConnectivityHelper
{
    private static bool _apiReachable = true;
    private static DateTime _lastFailUtc = DateTime.MinValue;
    private static string? _stateFilePath;

    // After an API failure, skip API calls for this duration before retrying.
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Loads the persisted API reachability state from disk (async version).
    /// </summary>
    public static async Task InitializeAsync()
    {
        try
        {
            _stateFilePath = Path.Combine(FileSystem.AppDataDirectory, "api_reachable.txt");

            if (File.Exists(_stateFilePath))
            {
                var content = await File.ReadAllTextAsync(_stateFilePath);
                if (bool.TryParse(content.Trim(), out var reachable))
                {
                    _apiReachable = reachable;
                    if (!reachable)
                        _lastFailUtc = DateTime.UtcNow;
                }
            }
        }
        catch
        {
            // If we can't read the file, default to optimistic (true)
        }
    }

    /// <summary>
    /// Synchronous version — safe to call from UI thread without deadlocking.
    /// </summary>
    public static void InitializeSync()
    {
        try
        {
            _stateFilePath = Path.Combine(FileSystem.AppDataDirectory, "api_reachable.txt");

            if (File.Exists(_stateFilePath))
            {
                var content = File.ReadAllText(_stateFilePath);
                if (bool.TryParse(content.Trim(), out var reachable))
                {
                    _apiReachable = reachable;
                    if (!reachable)
                        _lastFailUtc = DateTime.UtcNow;
                }
            }
        }
        catch
        {
            // Default to optimistic (true)
        }
    }

    /// <summary>
    /// Probes the API in the background and updates reachability state.
    /// Fire-and-forget after startup.
    /// </summary>
    public static async Task ProbeApiInBackgroundAsync(Uri apiBaseAddress)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            using var response = await client.GetAsync(apiBaseAddress);
            MarkOnline();
        }
        catch
        {
            MarkOffline();
        }
    }

    public static bool IsOnline()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return false;

        // If we recently failed to reach the API, skip until cooldown expires.
        if (!_apiReachable)
        {
            if (DateTime.UtcNow - _lastFailUtc < CooldownPeriod)
                return false;

            // Cooldown expired — allow one retry
            _apiReachable = true;
        }

        return true;
    }

    /// <summary>Call when an API request fails with a network error.</summary>
    public static void MarkOffline()
    {
        _apiReachable = false;
        _lastFailUtc = DateTime.UtcNow;
        PersistState(false);
    }

    /// <summary>Call when an API request succeeds.</summary>
    public static void MarkOnline()
    {
        _apiReachable = true;
        PersistState(true);
    }

    private static void PersistState(bool reachable)
    {
        if (_stateFilePath is null) return;

        // Fire-and-forget write — don't block the caller
        _ = Task.Run(async () =>
        {
            try
            {
                await File.WriteAllTextAsync(_stateFilePath, reachable.ToString());
            }
            catch
            {
                // Best-effort persistence; swallow errors
            }
        });
    }
}
