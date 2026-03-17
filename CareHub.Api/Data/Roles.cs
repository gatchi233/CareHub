namespace CareHub.Api.Data;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Nurse = "Nurse";
    public const string GeneralCareStaff = "General CareStaff";
    public const string Observer = "Observer";

    // Backward-compatible aliases for existing controller checks.
    public const string Staff = Nurse;
    public const string Resident = Observer;
}
