using Ecommerce.Application.Common;
using Ecommerce.Domain.Identity;
using Ecommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Persistence;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeeder));
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var adminOptions = services.GetRequiredService<IOptions<InitialAdminOptions>>().Value;

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
                logger.LogInformation("Created role {RoleName}.", roleName);
            }
        }

        if (string.IsNullOrWhiteSpace(adminOptions.Email) || string.IsNullOrWhiteSpace(adminOptions.Password))
        {
            logger.LogInformation("InitialAdmin is not configured; skipping bootstrap admin user seeding.");
            return;
        }

        var existingAdmin = await userManager.FindByEmailAsync(adminOptions.Email);
        if (existingAdmin is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminOptions.Email,
            Email = adminOptions.Email,
            FirstName = adminOptions.FirstName,
            LastName = adminOptions.LastName,
            EmailConfirmed = true,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(admin, adminOptions.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to seed initial admin user: {Errors}", errors);
            return;
        }

        await userManager.AddToRoleAsync(admin, Roles.SuperAdmin);
        logger.LogInformation("Seeded initial SUPER_ADMIN user {Email}.", adminOptions.Email);
    }
}
