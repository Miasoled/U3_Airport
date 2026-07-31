using Microsoft.AspNetCore.Identity;

namespace U3_Examen_Airport.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager =
            serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager =
            serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

        string[] roles =
        {
            "Administrador",
            "Cliente"
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!result.Succeeded)
                {
                    var errores = string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"No se pudo crear el rol {role}: {errores}");
                }
            }
        }

        const string adminEmail = "admin@airport.com";
        const string adminPassword = "Admin123*";

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult =
                await userManager.CreateAsync(admin, adminPassword);

            if (!createResult.Succeeded)
            {
                var errores = string.Join(
                    ", ",
                    createResult.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"No se pudo crear el administrador: {errores}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Administrador"))
        {
            await userManager.AddToRoleAsync(
                admin,
                "Administrador");
        }
    }
}