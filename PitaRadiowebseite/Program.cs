using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using PitaRadiowebseite.Data;
using PitaRadiowebseite.Models;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Railway Port
    var portStr = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{portStr}");

    // Upload limit
    builder.Services.Configure<FormOptions>(o =>
    {
        o.MultipartBodyLengthLimit = 200 * 1024 * 1024;
    });

    // Services
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // DB (mit Fallback)
    var cs = builder.Configuration.GetConnectionString("Default") ?? "Data Source=mini_radio.db";
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(cs));

    var app = builder.Build();

    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.UseSwagger();
    app.UseSwaggerUI();

    // >>> DEINE MapGet/MapPost ROUTES HIER <<<

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("FATAL STARTUP ERROR");
    Console.WriteLine($"Type: {ex.GetType().FullName}");
    Console.WriteLine($"Message: {ex.Message}");

    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner Type: {ex.InnerException.GetType().FullName}");
        Console.WriteLine($"Inner Message: {ex.InnerException.Message}");
    }

    throw;
}