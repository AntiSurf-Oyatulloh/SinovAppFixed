using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SinovApp.Models;

namespace SinovApp.Data;

public static class SeedData
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        const string adminEmail = "oyatullohmuxtorov5@gmail.com";
        const string adminPassword = "Oyatulloh5523.08@sharq";

        var user = await userManager.FindByEmailAsync(adminEmail);
        if (user == null)
        {
            var newAdmin = new ApplicationUser
            {
                UserName = "Oyatulloh",
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Oyatulloh Muxtorov"
            };

            var result = await userManager.CreateAsync(newAdmin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(newAdmin, "Admin");
        }
        else if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }
}