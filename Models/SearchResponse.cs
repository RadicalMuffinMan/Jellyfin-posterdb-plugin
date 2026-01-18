using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.PosterDB.Models;

public class SearchResponse
{
    public List<PosterResult> Results { get; set; } = new();
    public int TotalResults { get; set; }
    public string Query { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
