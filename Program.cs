using Microsoft.EntityFrameworkCore;
using U3_Examen_Airport.Data;

var builder = WebApplication.CreateBuilder(args);

// Obtener la cadena de conexión desde appsettings.json
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

// Contexto Code First para las tablas propias del examen
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Servicios MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Manejo de errores
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Redirección HTTPS
app.UseHttpsRedirection();

// Archivos estáticos: CSS, JavaScript, imágenes, Bootstrap
app.UseStaticFiles();

// Enrutamiento
app.UseRouting();

// Autorización
app.UseAuthorization();

// Ruta predeterminada MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();