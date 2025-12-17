using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using PitaRadiowebseite.Data;
using PitaRadiowebseite.Models;

var builder = WebApplication.CreateBuilder(args);

// =========================
// RAILWAY: fest auf 0.0.0.0:8080 hören
// (passt zum Railway Networking Target Port 8080)
// =========================
builder.WebHost.UseUrls("http://0.0.0.0:8080");

// =========================
// UPLOAD LIMIT (Form)
// =========================
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 200 * 1024 * 1024; // 200 MB
});

// =========================
// SERVICES
// =========================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =========================
// DB (SQLite) - Fallback, falls ConnectionString fehlt
// =========================
var cs = builder.Configuration.GetConnectionString("Default")
         ?? "Data Source=mini_radio.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(cs)
);

var app = builder.Build();

// =========================
// MIDDLEWARE
// =========================
app.UseDefaultFiles();
app.UseStaticFiles();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// =========================
// API ROUTES
// =========================
// >>> HIER deine MapGet/MapPost Endpoints wieder einfügen <<<

app.Run();