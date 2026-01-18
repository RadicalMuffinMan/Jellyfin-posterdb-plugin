using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PosterDB.Configuration;

/// <summary>
/// Plugin configuration for PosterDB.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        // Set default values
        ApiKey = string.Empty;
        PreferTextless = false;
        EnableForMovies = true;
        EnableForShows = true;
        EnableForSeasons = true;
        Priority = 10;
    }

    /// <summary>
    /// Gets or sets the ThePosterDB API key.
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to prefer textless posters.
    /// </summary>
    public bool PreferTextless { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider is enabled for movies.
    /// </summary>
    public bool EnableForMovies { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider is enabled for TV shows.
    /// </summary>
    public bool EnableForShows { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider is enabled for seasons.
    /// </summary>
    public bool EnableForSeasons { get; set; }

    /// <summary>
    /// Gets or sets the provider priority (lower numbers = higher priority).
    /// </summary>
    public int Priority { get; set; }
}
