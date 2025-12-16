using Microsoft.EntityFrameworkCore;
using PitaRadiowebseite.Models;

namespace PitaRadiowebseite.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Track> Tracks => Set<Track>();
}