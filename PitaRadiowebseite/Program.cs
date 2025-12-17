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

    // =========================================================
    // API ROUTES
    // =========================================================

    // Genres (distinct aus Tracks)
    app.MapGet("/api/genres", async (AppDbContext db) =>
    {
        var genres = await db.Tracks
            .Where(t => t.Genre != null && t.Genre != "")
            .Select(t => t.Genre!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();

        return Results.Ok(genres);
    });

    // Tracks (optional: ?genre=Rock)
    app.MapGet("/api/tracks", async (AppDbContext db, string? genre) =>
    {
        var q = db.Tracks.AsQueryable();

        if (!string.IsNullOrWhiteSpace(genre))
            q = q.Where(t => t.Genre == genre);

        // OPTIONAL: Wenn du TrackStat hast, joinen wir sie mit rein
        // Damit das Frontend "minimal bevorzugen" (clapCount) sofort hat.
        var tracks = await q
            .OrderByDescending(t => t.CreatedUtc)
            .Select(t => new
            {
                id = t.Id,
                title = t.Title,
                artist = t.Artist,
                genre = t.Genre,
                url = t.Url,

                // falls du diese Felder später ergänzt:
                cover_url = (t as dynamic).CoverUrl,   // klappt nur wenn Feld existiert
                artist_url = (t as dynamic).ArtistUrl  // klappt nur wenn Feld existiert
            })
            .ToListAsync();

        return Results.Ok(tracks);
    });

    // Charts pro Genre (sortiert nach ClapCount, dann PlayCount)
    app.MapGet("/api/charts", async (AppDbContext db, string genre) =>
    {
        if (string.IsNullOrWhiteSpace(genre))
            return Results.BadRequest("genre is required");

        // Wenn du TrackStats Tabelle noch nicht hast, bekommst du hier leere Liste.
        // Wir setzen unten gleich TrackStat als Model + DbSet voraus.
        var rows = await (from t in db.Tracks
                          join s in db.TrackStats on t.Id equals s.TrackId into stats
                          from s in stats.DefaultIfEmpty()
                          where t.Genre == genre
                          orderby (s != null ? s.ClapCount : 0) descending,
                                 (s != null ? s.PlayCount : 0) descending,
                                 t.Title
                          select new
                          {
                              title = t.Title,
                              artist = t.Artist,
                              genre = t.Genre,
                              // falls vorhanden:
                              cover_url = (t as dynamic).CoverUrl,
                              artist_url = (t as dynamic).ArtistUrl,

                              clap_count = s != null ? s.ClapCount : 0,
                              play_count = s != null ? s.PlayCount : 0,
                              play_seconds = s != null ? s.PlaySeconds : 0
                          })
            .ToListAsync();

        return Results.Ok(rows);
    });

    // Clap speichern (1 pro Track pro Browser/Tag ist im Frontend via localStorage begrenzt)
    app.MapPost("/api/stats/clap", async (AppDbContext db, ClapRequest req) =>
    {
        if (req.TrackId <= 0) return Results.BadRequest("trackId required");

        var stat = await db.TrackStats.SingleOrDefaultAsync(x => x.TrackId == req.TrackId);
        if (stat == null)
        {
            stat = new TrackStat
            {
                TrackId = req.TrackId,
                ClapCount = 1,
                PlayCount = 0,
                PlaySeconds = 0,
                UpdatedUtc = DateTime.UtcNow
            };
            db.TrackStats.Add(stat);
        }
        else
        {
            stat.ClapCount += 1;
            stat.UpdatedUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { ok = true });
    });

    // Playtime speichern (optional, falls du später willst)
    app.MapPost("/api/stats/play", async (AppDbContext db, PlayRequest req) =>
    {
        if (req.TrackId <= 0) return Results.BadRequest("trackId required");

        var addSeconds = Math.Max(0, req.Seconds);
        if (addSeconds == 0) return Results.Ok(new { ok = true });

        var stat = await db.TrackStats.SingleOrDefaultAsync(x => x.TrackId == req.TrackId);
        if (stat == null)
        {
            stat = new TrackStat
            {
                TrackId = req.TrackId,
                ClapCount = 0,
                PlayCount = 1,
                PlaySeconds = addSeconds,
                UpdatedUtc = DateTime.UtcNow
            };
            db.TrackStats.Add(stat);
        }
        else
        {
            stat.PlayCount += 1;
            stat.PlaySeconds += addSeconds;
            stat.UpdatedUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { ok = true });
    });

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

// =========================================================
// Request DTOs (Minimal API)
// =========================================================
public record ClapRequest(int TrackId);
public record PlayRequest(int TrackId, int Seconds);