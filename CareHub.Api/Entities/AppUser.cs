namespace CareHub.Api.Entities;

public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";          // Admin | Nurse | CareStaff | Observer
    public Guid? ResidentId { get; set; }            // nullable — links Observer to their Resident
}
