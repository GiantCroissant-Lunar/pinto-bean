using System.ComponentModel.DataAnnotations;

namespace ConsoleGame.Plugin.Audio;

public sealed class AudioOptions
{
    [Display(Name = "Enable LibVLC-backed audio playback")]
    public bool Enabled { get; set; } = true;

    [MaxLength(256)]
    public string? DisableReason { get; set; }
}
