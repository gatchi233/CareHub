namespace MedReminder.Desktop.Services.Sync;

public sealed class OfflineException : Exception
{
    public OfflineException(string message, Exception? inner = null) : base(message, inner) { }

    public static bool IsOffline(Exception ex)
        => ex is OfflineException
        || ex is HttpRequestException
        || ex is TaskCanceledException;
}
