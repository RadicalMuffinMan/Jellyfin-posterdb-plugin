using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.PosterDB.Configuration;
using Jellyfin.Plugin.PosterDB.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PosterDB.Api;

public class PosterDBClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PosterDBClient> _logger;
    private readonly IMemoryCache _cache;
    private const string BaseUrl = "https://theposterdb.com/api";
    private const int CacheDurationMinutes = 60;

    public PosterDBClient(
        IHttpClientFactory httpClientFactory,
        ILogger<PosterDBClient> logger,
        IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cache = cache;
    }

    public async Task<SearchResponse> SearchByTmdbIdAsync(string tmdbId, string? apiKey, CancellationToken cancellationToken)
    {
        var cacheKey = $"tmdb_{tmdbId}";
        if (_cache.TryGetValue<SearchResponse>(cacheKey, out var cached))
        {
            return cached!;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }

            var response = await client.GetAsync(
                $"{BaseUrl}/posters/tmdb/{tmdbId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TPDb API returned {StatusCode} for TMDB ID {TmdbId}", 
                    response.StatusCode, tmdbId);
                return new SearchResponse 
                { 
                    Success = false, 
                    ErrorMessage = $"API returned {response.StatusCode}"
                };
            }

            var result = await ParseResponseAsync(response, cancellationToken);
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheDurationMinutes));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching TPDb by TMDB ID {TmdbId}", tmdbId);
            return new SearchResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    public async Task<SearchResponse> SearchByTvdbIdAsync(string tvdbId, string? apiKey, CancellationToken cancellationToken)
    {
        var cacheKey = $"tvdb_{tvdbId}";
        if (_cache.TryGetValue<SearchResponse>(cacheKey, out var cached))
        {
            return cached!;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }

            var response = await client.GetAsync(
                $"{BaseUrl}/posters/tvdb/{tvdbId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new SearchResponse 
                { 
                    Success = false, 
                    ErrorMessage = $"API returned {response.StatusCode}"
                };
            }

            var result = await ParseResponseAsync(response, cancellationToken);
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheDurationMinutes));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching TPDb by TVDB ID {TvdbId}", tvdbId);
            return new SearchResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    public async Task<SearchResponse> SearchByImdbIdAsync(string imdbId, string? apiKey, CancellationToken cancellationToken)
    {
        var cacheKey = $"imdb_{imdbId}";
        if (_cache.TryGetValue<SearchResponse>(cacheKey, out var cached))
        {
            return cached!;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }

            var response = await client.GetAsync(
                $"{BaseUrl}/posters/imdb/{imdbId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new SearchResponse 
                { 
                    Success = false, 
                    ErrorMessage = $"API returned {response.StatusCode}"
                };
            }

            var result = await ParseResponseAsync(response, cancellationToken);
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheDurationMinutes));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching TPDb by IMDB ID {ImdbId}", imdbId);
            return new SearchResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    public async Task<SearchResponse> SearchByTitleAsync(string title, string? apiKey, CancellationToken cancellationToken)
    {
        var cacheKey = $"title_{title}";
        if (_cache.TryGetValue<SearchResponse>(cacheKey, out var cached))
        {
            return cached!;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }

            var encodedTitle = Uri.EscapeDataString(title);
            var response = await client.GetAsync(
                $"{BaseUrl}/search?query={encodedTitle}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new SearchResponse 
                { 
                    Success = false, 
                    ErrorMessage = $"API returned {response.StatusCode}"
                };
            }

            var result = await ParseResponseAsync(response, cancellationToken);
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheDurationMinutes));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching TPDb by title {Title}", title);
            return new SearchResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    private async Task<SearchResponse> ParseResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var data = JsonSerializer.Deserialize<JsonElement>(content, options);
            
            var results = new List<PosterResult>();
            
            if (data.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataArray.EnumerateArray())
                {
                    results.Add(new PosterResult
                    {
                        Id = GetStringProperty(item, "id"),
                        Title = GetStringProperty(item, "title"),
                        ThumbnailUrl = GetStringProperty(item, "thumbnail_url"),
                        FullUrl = GetStringProperty(item, "url"),
                        Uploader = GetStringProperty(item, "uploader"),
                        Width = GetIntProperty(item, "width"),
                        Height = GetIntProperty(item, "height"),
                        IsTextless = GetBoolProperty(item, "textless"),
                        Language = GetStringProperty(item, "language") ?? "en",
                        Likes = GetIntProperty(item, "likes")
                    });
                }
            }

            return new SearchResponse
            {
                Results = results,
                TotalResults = results.Count,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing TPDb response");
            return new SearchResponse
            {
                Success = false,
                ErrorMessage = "Failed to parse response"
            };
        }
    }

    private static string GetStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() ?? string.Empty
            : string.Empty;
    }

    private static int GetIntProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetInt32()
            : 0;
    }

    private static bool GetBoolProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && 
               (prop.ValueKind == JsonValueKind.True || 
                (prop.ValueKind == JsonValueKind.Number && prop.GetInt32() == 1));
    }
}
