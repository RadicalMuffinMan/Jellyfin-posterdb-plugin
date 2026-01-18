using System;

namespace Jellyfin.Plugin.PosterDB.Models;

public class PosterResult
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string FullUrl { get; set; } = string.Empty;
    public string Uploader { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsTextless { get; set; }
    public string Language { get; set; } = "en";
    public DateTime UploadDate { get; set; }
    public int Likes { get; set; }
}
