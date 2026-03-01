namespace CareHub.Pages
{
    public interface IUnsavedChangesPage
    {
        bool HasUnsavedChanges { get; }
        Task SaveAsync();
    }
}
