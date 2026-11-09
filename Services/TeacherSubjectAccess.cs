using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace ReliableAcademy.Services;

public static class TeacherSubjectAccess
{
    public const string ClaimType = "rci:assigned_subject";

    // Read persisted assignments on every request so admin changes apply immediately.
    public static async Task<HashSet<int>> GetAssignedSubjectIdsAsync(
        UserManager<IdentityUser> users,
        ClaimsPrincipal principal)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return new HashSet<int>();

        var claims = await users.GetClaimsAsync(user);
        return claims
            .Where(x => x.Type == ClaimType && int.TryParse(x.Value, out _))
            .Select(x => int.Parse(x.Value))
            .ToHashSet();
    }
}
