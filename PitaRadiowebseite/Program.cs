using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using PitaRadiowebseite.Data;
using PitaRadiowebseite.Models;

// SQLite native init (Linux/Railway)
SQLitePCL.Batteries_V2.Init();

var builder = WebApplication.CreateBuilder(args);

// Railway: IMMER PORT aus ENV (nicht hart!)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Upload limit
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 200 * 1024 * 1024;
});

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SQLite mit Fallback
var cs = builder.Configuration.GetConnectionString("Default")
         ?? "Data Source=/app/mini_radio.db";

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(cs)
);

var app = builder.Build();

// Middleware
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

// 👉 HIER deine API Endpoints (MapGet / MapPost)

app.Run();