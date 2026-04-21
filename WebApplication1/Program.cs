using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Uniflow;
using Uniflow.Data;
using Uniflow.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurare Baza de Date (SQL Server)
// Se presupune ca ai connection string-ul in appsettings.json sub numele "DefaultConnection"
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configurare Identity (Useri si Roluri)
// Important: Adaugam .AddRoles<IdentityRole>() pentru SCUM-9 (Meniu Dinamic)
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

// Servicii pentru gamificare
builder.Services.AddScoped<Uniflow.Services.GamificationService>();

// Servicii pentru dashboard
builder.Services.AddScoped<Uniflow.Services.DashboardService>();

// Servicii pentru notificări
builder.Services.AddScoped<Uniflow.Services.NotificationService>();

// Servicii pentru insigne (Badges)
builder.Services.AddScoped<Uniflow.Services.BadgeService>();

// Servicii pentru vouchere
builder.Services.AddScoped<Uniflow.Services.VoucherService>();

// Configurare Email
builder.Services.Configure<Uniflow.Services.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, Uniflow.Services.EmailSender>();

var app = builder.Build();

// Configurare pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 3. Activare Autentificare si Autorizare
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "A ap?rut o eroare la crearea bazelor de date/rolurilor.");
    }
}
app.Run();
