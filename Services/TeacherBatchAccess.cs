using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace ReliableAcademy.Services;

public static class TeacherBatchAccess
{
    public const string ClaimType = "rci:assigned_batch";

    // Read assignments from the Identity store on every request. Do not trust
    // cookie claims for authorization because an administrator may change access
    // while a teacher is signed in.
    public static async Task<HashSet<int>> GetAssignedBatchIdsAsync(
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
