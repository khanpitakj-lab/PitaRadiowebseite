using Microsoft.EntityFrameworkCore;
using PitaRadiowebseite.Models;

namespace PitaRadiowebseite.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Track> Tracks => Set<Track>();

    // 🆕 Statistik pro Track
    public DbSet<TrackStat> TrackStats => Set<TrackStat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TrackStat: 1 Datensatz pro Track
        modelBuilder.Entity<TrackStat>()
            .HasKey(x => x.TrackId);
    }
}