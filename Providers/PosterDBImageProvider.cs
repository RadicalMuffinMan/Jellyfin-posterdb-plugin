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
        var apiKey = Plugin.Instance?.Configuration.ApiKey;
        var results = new List<RemoteImageInfo>();

        try
        {
            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
            var tvdbId = item.GetProviderId(MetadataProvider.Tvdb);
            var imdbId = item.GetProviderId(MetadataProvider.Imdb);

            Models.SearchResponse? response = null;

            if (!string.IsNullOrEmpty(tmdbId))
            {
                response = await _posterDBClient.SearchByTmdbIdAsync(tmdbId, apiKey, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(tvdbId))
            {
                response = await _posterDBClient.SearchByTvdbIdAsync(tvdbId, apiKey, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(imdbId))
            {
                response = await _posterDBClient.SearchByImdbIdAsync(imdbId, apiKey, cancellationToken);
            }
            else
            {
                response = await _posterDBClient.SearchByTitleAsync(item.Name, apiKey, cancellationToken);
            }

            if (response?.Success == true && response.Results != null)
            {
                foreach (var poster in response.Results)
                {
                    var imageType = DetermineImageType(poster);
                    
                    results.Add(new RemoteImageInfo
                    {
                        ProviderName = Name,
                        Url = poster.FullUrl,
                        ThumbnailUrl = poster.ThumbnailUrl,
                        Type = imageType,
                        Width = poster.Width,
                        Height = poster.Height,
                        Language = poster.Language,
                        CommunityRating = poster.Likes
                    });
                }
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
