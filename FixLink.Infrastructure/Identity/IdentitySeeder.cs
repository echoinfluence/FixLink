using Microsoft.AspNetCore.Identity;
using FixLink.Infrastructure.Identity;

namespace FixLink.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        const string adminRole = "Admin";

        // Create Admin role if it doesn't exist
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            var role = new IdentityRole<Guid>(adminRole);

            var roleResult = await roleManager.CreateAsync(role);

            if (!roleResult.Succeeded)
            {
                throw new Exception(
                    $"Failed to create Admin role: " +
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        // Admin account details
        const string adminEmail = "admin@fixlink.co.za";
        const string adminPassword = "FixLinkAdmin123!";

        var existingAdmin =
            await userManager.FindByEmailAsync(adminEmail);

        if (existingAdmin == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,

                FirstName = "FixLink",
                LastName = "Administrator",

                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result =
                await userManager.CreateAsync(admin, adminPassword);

            if (!result.Succeeded)
            {
                throw new Exception(
                    $"Failed to create admin account: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(admin, adminRole);
        }
    }
}