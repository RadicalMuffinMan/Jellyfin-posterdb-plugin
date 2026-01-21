using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.PosterDB.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PosterDB.Api;

public class PosterDBClient : IAsyncDisposable
{
    private readonly ILogger<PosterDBClient> _logger;
    private readonly IMemoryCache _cache;
    private readonly PlaywrightScraper _scraper;
    private const int CacheDurationMinutes = 60;

    public PosterDBClient(
        ILogger<PosterDBClient> logger,
        IMemoryCache cache,
        PlaywrightScraper scraper)
    {
        _logger = logger;
        _cache = cache;
        _scraper = scraper;
    }

    public async Task<SearchResponse> SearchByTitleAsync(string title, CancellationToken cancellationToken)
    {
        var cacheKey = $"title_{title}";
        if (_cache.TryGetValue<SearchResponse>(cacheKey, out var cached))
        {
            _logger.LogDebug("Returning cached results for: {Title}", title);
            return cached!;
        }

        try
        {
            _logger.LogInformation("Searching ThePosterDB for: {Title}", title);
            
            // Parse title and year if present (e.g., "Inception (2010)")
            var (cleanTitle, year) = ParseTitleAndYear(title);
            
            // Use Playwright scraper directly
            var posters = await _scraper.SearchByTitleAsync(cleanTitle, year, "movie", cancellationToken);
            
            var result = new SearchResponse
            {
                Results = posters,
                TotalResults = posters.Count,
                Success = true
            };
            
            if (result.Success && result.Results.Count > 0)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheDurationMinutes));
            }
            
            _logger.LogInformation("Retrieved {Count} posters for '{Title}'", 
                result.Results.Count, title);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching ThePosterDB for title: {Title}", title);
            return new SearchResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    private (string title, int? year) ParseTitleAndYear(string input)
    {
        // Try to extract year from patterns like "Title (2020)"
        var match = System.Text.RegularExpressions.Regex.Match(input, @"^(.+?)\s*\((\d{4})\)$");
        if (match.Success)
        {
            return (match.Groups[1].Value.Trim(), int.Parse(match.Groups[2].Value));
        }
        
        return (input.Trim(), null);
    }

    public async ValueTask DisposeAsync()
    {
        if (_scraper != null)
        {
            await _scraper.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
