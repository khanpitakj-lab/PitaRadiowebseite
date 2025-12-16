using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using PitaRadiowebseite.Data;
using PitaRadiowebseite.Models;

var builder = WebApplication.CreateBuilder(args);

// =========================
// UPLOAD LIMIT (z.B. 200 MB)
// =========================
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 200 * 1024 * 1024; // 200 MB
});

builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 200 * 1024 * 1024; // 200 MB
});

// =========================
// SERVICES
// =========================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default"))
);

var app = builder.Build();

// =========================
// MIDDLEWARE
// =========================
app.UseDefaultFiles();   // index.html automatisch laden
app.UseStaticFiles();    // wwwroot ausliefern (HTML, CSS, JS, MP3)

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// =========================
// API ENDPOINTS
// =========================

// Genres abrufen
app.MapGet("/api/genres", async (AppDbContext db) =>
{
    var genres = await db.Tracks
        .Select(t => t.Genre)
        .Distinct()
        .OrderBy(g => g)
        .ToListAsync();

    return Results.Ok(genres);
});

// Alle Tracks oder nach Genre (?genre=Pop)
app.MapGet("/api/tracks", async (string? genre, AppDbContext db) =>
{
    var query = db.Tracks.AsQueryable();

    if (!string.IsNullOrWhiteSpace(genre))
        query = query.Where(t => t.Genre == genre);

    var tracks = await query
        .OrderBy(t => t.Id)
        .ToListAsync();

    return Results.Ok(tracks);
});

// Upload (multipart/form-data: file + title + artist + genre)
app.MapPost("/api/upload", async (
    HttpRequest request,
    AppDbContext db,
    IWebHostEnvironment env) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("FormData erwartet.");

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");

    if (file == null || file.Length == 0)
        return Results.BadRequest("Keine Datei hochgeladen.");

    var title = form["title"].ToString().Trim();
    var artist = form["artist"].ToString().Trim();
    var genre = form["genre"].ToString().Trim();

    if (string.IsNullOrWhiteSpace(genre))
        genre = "Unsorted";

    // sichere Namen (Ordner/Dateiname)
    static string Safe(string input) =>
        string.Concat((input ?? "").Where(c =>
            char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' '))
        .Trim();

    var safeGenre = Safe(genre);
    if (string.IsNullOrWhiteSpace(safeGenre))
        safeGenre = "Unsorted";

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext != ".mp3" && ext != ".wav" && ext != ".m4a" && ext != ".ogg")
        return Results.BadRequest("Nur mp3 / wav / m4a / ogg erlaubt.");

    var baseName = Safe(Path.GetFileNameWithoutExtension(file.FileName));
    if (string.IsNullOrWhiteSpace(baseName))
        baseName = "track";

    var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{baseName}{ext}";

    // Ziel: wwwroot/audio/<Genre>/
    var audioDir = Path.Combine(env.WebRootPath ?? "wwwroot", "audio", safeGenre);
    Directory.CreateDirectory(audioDir);

    var fullPath = Path.Combine(audioDir, fileName);
    await using (var stream = File.Create(fullPath))
        await file.CopyToAsync(stream);

    var url = $"/audio/{Uri.EscapeDataString(safeGenre)}/{Uri.EscapeDataString(fileName)}";

    var track = new Track
    {
        Title = string.IsNullOrWhiteSpace(title) ? baseName : title,
        Artist = artist,
        Genre = safeGenre,
        Url = url,
        CreatedUtc = DateTime.UtcNow
    };

    db.Tracks.Add(track);
    await db.SaveChangesAsync();

    return Results.Ok(track);
});

// =========================
// START
// =========================
app.Run();