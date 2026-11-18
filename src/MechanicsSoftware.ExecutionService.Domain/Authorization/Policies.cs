namespace MechanicsSoftware.ExecutionService.Domain.Authorization;

public static class Policies
{
    public const string Staff = nameof(Staff);

    public static readonly string[] StaffRoles = ["ADMIN", "ATTENDANT", "MECHANIC"];
}
