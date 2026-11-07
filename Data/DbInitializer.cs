using Microsoft.AspNetCore.Identity;

namespace ReliableAcademy.Data;

public static class DbInitializer
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var name in new[] { "Admin", "Teacher" })
        {
            if (await roles.RoleExistsAsync(name)) continue;
            var result = await roles.CreateAsync(new IdentityRole(name));
            if (!result.Succeeded)
                throw new InvalidOperationException($"Unable to create role {name}: {string.Join("; ", result.Errors.Select(x => x.Description))}");
        }
    }
}
