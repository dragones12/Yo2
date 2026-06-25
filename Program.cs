using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using CBN_Online.Data;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// 1. CONFIGURACIÓN DE SERVICIOS
// =============================================

// Agregar DbContext para SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Agregar Controladores con Vistas
builder.Services.AddControllersWithViews();

// =============================================
// CONFIGURACIÓN DE SESSION (AGREGAR ESTO)
// =============================================
builder.Services.AddDistributedMemoryCache(); // Cache en memoria para sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de expiración
    options.Cookie.HttpOnly = true; // Seguridad
    options.Cookie.IsEssential = true; // Necesario para GDPR
});

// Configurar Autenticación por Cookies
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.AccessDeniedPath = "/Cuenta/Denegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Configurar Autorización
builder.Services.AddAuthorization();

// =============================================
// 2. CONSTRUCCIÓN DE LA APLICACIÓN
// =============================================

var app = builder.Build();

// =============================================
// 3. CONFIGURACIÓN DEL PIPELINE HTTP
// =============================================

// Manejo de errores en desarrollo
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Redirección HTTPS, Archivos estáticos y Enrutamiento
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// =============================================
// SESSION DEBE IR ANTES DE AUTHENTICATION
// =============================================
app.UseSession(); // <-- AGREGAR ESTO

// Autenticación y Autorización (IMPORTANTE: en este orden)
app.UseAuthentication();
app.UseAuthorization();

// Rutas por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// =============================================
// 4. APLICAR MIGRACIONES AUTOMÁTICAMENTE (OPCIONAL)
// =============================================

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Aplica las migraciones pendientes automáticamente
        dbContext.Database.Migrate();
        Console.WriteLine("✅ Migraciones aplicadas correctamente");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al aplicar migraciones: {ex.Message}");
    }
}

// =============================================
// 5. EJECUTAR LA APLICACIÓN
// =============================================

app.Run();