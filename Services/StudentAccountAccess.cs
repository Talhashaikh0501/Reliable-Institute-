using System.Security.Claims;

namespace ReliableAcademy.Services;

public static class StudentAccountAccess
{
    public const string StudentIdClaim = "rci:student_id";
    public const string MustChangePasswordClaim = "rci:must_change_password";

    public static int? GetStudentId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(StudentIdClaim);
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }
}
