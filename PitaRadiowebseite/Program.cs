using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using PitaRadiowebseite.Data;
using PitaRadiowebseite.Models;

var builder = WebApplication.CreateBuilder(args);

// Upload limit
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 200 * 1024 * 1024;
});

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default"))
);

var app = builder.Build();

// Static files
app.UseDefaultFiles();
app.UseStaticFiles();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// API routes ...
// (deine MapGet/MapPost bleiben genau wie sie sind)

app.Run();