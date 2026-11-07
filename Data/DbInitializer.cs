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

    public static async Task SeedAdminAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["BootstrapAdmin:Email"]?.Trim();
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await users.FindByEmailAsync(email);

        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await users.CreateAsync(user, password);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(
                    "Bootstrap Admin could not be created: " +
                    string.Join("; ", createResult.Errors.Select(x => x.Description)));
        }

        if (!await users.IsInRoleAsync(user, "Admin"))
        {
            var roleResult = await users.AddToRoleAsync(user, "Admin");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(
                    "Bootstrap Admin role could not be assigned: " +
                    string.Join("; ", roleResult.Errors.Select(x => x.Description)));
        }
    }
}
