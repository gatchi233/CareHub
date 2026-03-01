namespace CareHub.Api.Entities;

public sealed class AppUser
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? ResidentId { get; set; }
}
