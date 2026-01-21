using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.PosterDB.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PosterDB.Providers;

public class PosterDBImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PosterDBClient _posterDBClient;
    private readonly ILogger<PosterDBImageProvider> _logger;

    public PosterDBImageProvider(
        IHttpClientFactory httpClientFactory,
        PosterDBClient posterDBClient,
        ILogger<PosterDBImageProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _posterDBClient = posterDBClient;
        _logger = logger;
    }

    public int Order => 1;
    public string Name => "ThePosterDB";

    public bool Supports(BaseItem item)
    {
        return item is Movie or Series or Season or Episode or BoxSet;
    }

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new[]
        {
            ImageType.Primary,
            ImageType.Backdrop,
            ImageType.Banner,
            ImageType.Thumb,
            ImageType.Logo
        };
    }

    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetImages called for item: {ItemName} (Type: {ItemType})", item.Name, item.GetType().Name);
        
        var results = new List<RemoteImageInfo>();

        try
        {
            // ThePosterDB only supports title search and external IDs (TMDB/TVDB/IMDB) are not supported
            // Always search by the item's name
            var searchTitle = item.Name;
            
            // For movies, include year if available for better matching
            if (item is Movie movie && movie.ProductionYear.HasValue)
            {
                searchTitle = $"{item.Name} ({movie.ProductionYear.Value})";
            }
            else if (item is Series series && series.ProductionYear.HasValue)
            {
                searchTitle = $"{item.Name} ({series.ProductionYear.Value})";
            }

            _logger.LogInformation("Searching ThePosterDB for: {SearchTitle}", searchTitle);
            
            var response = await _posterDBClient.SearchByTitleAsync(searchTitle, cancellationToken);

            _logger.LogInformation("Search completed. Success: {Success}, Results: {Count}", 
                response?.Success ?? false, response?.Results?.Count ?? 0);

            if (response?.Success == true && response.Results != null)
            {
                foreach (var poster in response.Results)
                {
                    var imageType = DetermineImageType(poster);
                    
                    var imageInfo = new RemoteImageInfo
                    {
                        ProviderName = Name,
                        Url = poster.FullUrl,
                        ThumbnailUrl = poster.ThumbnailUrl,
                        Type = imageType,
                        Width = poster.Width,
                        Height = poster.Height,
                        Language = poster.Language,
                        CommunityRating = poster.Likes
                    };
                    
                    _logger.LogDebug("Adding image: {Url} (Type: {Type}, Size: {Width}x{Height})", 
                        poster.FullUrl, imageType, poster.Width, poster.Height);
                    
                    results.Add(imageInfo);
                }
                
                _logger.LogInformation("Returning {Count} images for {ItemName}", results.Count, item.Name);
            }
            else if (response != null && !response.Success)
            {
                _logger.LogWarning("Search failed: {ErrorMessage}", response.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching images from ThePosterDB for {ItemName}", item.Name);
        }

        return results;
    }

    private static ImageType DetermineImageType(Models.PosterResult poster)
    {
        var ratio = poster.Width > 0 && poster.Height > 0 
            ? (double)poster.Width / poster.Height 
            : 0.67;

        if (ratio > 1.5)
        {
            return ImageType.Backdrop;
        }
        
        return ImageType.Primary;
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _httpClientFactory.CreateClient().GetAsync(url, cancellationToken);
    }
}
