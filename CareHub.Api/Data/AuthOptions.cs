namespace CareHub.Api.Data;

public sealed class AuthOptions
{
    public string Issuer { get; set; } = "CareHub.Api";
    public string Audience { get; set; } = "CareHub.Client";
    public string SigningKey { get; set; } = "CHANGE_ME_IN_PRODUCTION_32+_CHARS_MIN";
    public int TokenMinutes { get; set; } = 120;
    public List<AuthUser> Users { get; set; } = new();
}

public sealed class AuthUser
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? ResidentId { get; set; }
}
