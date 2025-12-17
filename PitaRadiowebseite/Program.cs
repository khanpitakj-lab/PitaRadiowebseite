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

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // DB (SQLite)
    var cs = builder.Configuration.GetConnectionString("Default")
             ?? "Data Source=mini_radio.db";

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(cs));

    var app = builder.Build();

    // 👉 DB automatisch erstellen (wichtig für Railway!)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.UseSwagger();
    app.UseSwaggerUI();

    // =========================================================
    // API ROUTES
    // =========================================================

    // Genres
    app.MapGet("/api/genres", async (AppDbContext db) =>
    {
        var genres = await db.Tracks
            .Where(t => t.Genre != null && t.Genre != "")
            .Select(t => t.Genre)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();

        return Results.Ok(genres);
    });

    // Tracks (optional ?genre=...)
    app.MapGet("/api/tracks", async (AppDbContext db, string? genre) =>
    {
        var q = db.Tracks.AsQueryable();

        if (!string.IsNullOrWhiteSpace(genre))
            q = q.Where(t => t.Genre == genre);

        var tracks = await q
            .OrderByDescending(t => t.CreatedUtc)
            .Select(t => new
            {
                id = t.Id,
                title = t.Title,
                artist = t.Artist,
                genre = t.Genre,
                url = t.Url,
                cover_url = t.CoverUrl,
                artist_url = t.ArtistUrl
            })
            .ToListAsync();

        return Results.Ok(tracks);
    });

    // Charts pro Genre
    app.MapGet("/api/charts", async (AppDbContext db, string genre) =>
    {
        if (string.IsNullOrWhiteSpace(genre))
            return Results.BadRequest("genre required");

        var rows = await (
            from t in db.Tracks
            join s in db.TrackStats on t.Id equals s.TrackId into stats
            from s in stats.DefaultIfEmpty()
            where t.Genre == genre
            orderby (s != null ? s.ClapCount : 0) descending,
                   (s != null ? s.PlayCount : 0) descending
            select new
            {
                title = t.Title,
                artist = t.Artist,
                cover_url = t.CoverUrl,
                clap_count = s != null ? s.ClapCount : 0,
                play_count = s != null ? s.PlayCount : 0,
                play_seconds = s != null ? s.PlaySeconds : 0
            }
        ).ToListAsync();

        return Results.Ok(rows);
    });

    // Clap speichern
    app.MapPost("/api/stats/clap", async (AppDbContext db, ClapRequest req) =>
    {
        if (req.TrackId <= 0)
            return Results.BadRequest();

        var stat = await db.TrackStats.FindAsync(req.TrackId);

        if (stat == null)
        {
            stat = new TrackStat
            {
                TrackId = req.TrackId,
                ClapCount = 1
            };
            db.TrackStats.Add(stat);
        }
        else
        {
            stat.ClapCount++;
            stat.UpdatedUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Results.Ok();
    });

    // Playtime speichern (optional)
    app.MapPost("/api/stats/play", async (AppDbContext db, PlayRequest req) =>
    {
        if (req.TrackId <= 0 || req.Seconds <= 0)
            return Results.Ok();

        var stat = await db.TrackStats.FindAsync(req.TrackId);

        if (stat == null)
        {
            stat = new TrackStat
            {
                TrackId = req.TrackId,
                PlayCount = 1,
                PlaySeconds = req.Seconds
            };
            db.TrackStats.Add(stat);
        }
        else
        {
            stat.PlayCount++;
            stat.PlaySeconds += req.Seconds;
            stat.UpdatedUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Results.Ok();
    });

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("FATAL STARTUP ERROR");
    Console.WriteLine(ex);
    throw;
}

// =========================================================
// DTOs
// =========================================================
public record ClapRequest(int TrackId);
public record PlayRequest(int TrackId, int Seconds);