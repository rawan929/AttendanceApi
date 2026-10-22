using Microsoft.AspNetCore.Identity;

namespace AttendanceApi.Data;

// بيشتغل مرة عند بداية التطبيق: بيعمل الأدوار الثلاثة وحساب الأدمن الأول لو مش موجودين
public static class DbSeeder
{
    public static readonly string[] Roles = { "Admin", "Doctor", "Student" };

    public static async Task SeedAsync(IServiceProvider sp)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
        var config = sp.GetRequiredService<IConfiguration>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var email = config["Seed:AdminEmail"]!;
        var password = config["Seed:AdminPassword"]!;

        if (await userManager.FindByEmailAsync(email) is null)
        {
            var admin = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
                throw new Exception("Failed to seed admin: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
