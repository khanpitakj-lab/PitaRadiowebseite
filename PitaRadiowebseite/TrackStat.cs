using System;

namespace PitaRadiowebseite.Models;

public class TrackStat
{
    public int TrackId { get; set; }

    public int PlayCount { get; set; }
    public int PlaySeconds { get; set; }
    public int ClapCount { get; set; }

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}