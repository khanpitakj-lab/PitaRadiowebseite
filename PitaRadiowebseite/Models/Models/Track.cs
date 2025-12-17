using System;
using System.ComponentModel.DataAnnotations;

namespace PitaRadiowebseite.Models;

public class Track
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(200)]
    public string Artist { get; set; } = "";

    [MaxLength(100)]
    public string Genre { get; set; } = "Unsorted";

    [MaxLength(500)]
    public string Url { get; set; } = "";

    // 🆕 Cover-Bild URL
    [MaxLength(500)]
    public string CoverUrl { get; set; } = "";

    // 🆕 Künstler-/Band-Webseite (Lupe)
    [MaxLength(500)]
    public string ArtistUrl { get; set; } = "";

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}