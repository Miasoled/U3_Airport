using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using U3_Examen_Airport.Data;
using U3_Examen_Airport.Services;

var builder = WebApplication.CreateBuilder(args);

// Obtener la cadena de conexión
var connectionString =
    builder.Configuration.GetConnectionString("AirportConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'AirportConnection' en appsettings.json.");
}

// Contexto Database First para las tablas originales de Airport
builder.Services.AddDbContext<AirportContext>(options =>
    options.UseNpgsql(connectionString));

// Contexto para Identity y las tablas propias del examen
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpClient<IPayPalService, PayPalService>();

// Configuración de Identity
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;

        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Servicios MVC
builder.Services.AddControllersWithViews();

// Necesario para las páginas de Identity:
// Login, Register, Logout, AccessDenied, etc.
builder.Services.AddRazorPages();

var app = builder.Build();

// Manejo de errores
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Redirección HTTPS
app.UseHttpsRedirection();

// Archivos estáticos
app.UseStaticFiles();

// Enrutamiento
app.UseRouting();

// Identity: autenticación antes de autorización
app.UseAuthentication();
app.UseAuthorization();

// Ruta predeterminada MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Rutas de las páginas Razor de Identity
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
