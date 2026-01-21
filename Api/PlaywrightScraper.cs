using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.PosterDB.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Jellyfin.Plugin.PosterDB.Api;

public class PlaywrightScraper : IAsyncDisposable
{
    private readonly ILogger<PlaywrightScraper> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _browserLock = new(1, 1);
    private bool _isInitialized;
    private const string BaseUrl = "https://theposterdb.com";

    public PlaywrightScraper(ILogger<PlaywrightScraper> logger)
    {
        _logger = logger;
    }

    private async Task EnsureBrowserInitializedAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized && _browser != null)
            return;

        await _browserLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized && _browser != null)
                return;

            _logger.LogInformation("Initializing Playwright browser...");

            // Create Playwright instance
            _playwright = await Playwright.CreateAsync();

            // Launch headless Chromium
            _browser = await _playwright.Chromium.LaunchAsync(new()
            {
                Headless = true,
                Args = new[] { "--disable-blink-features=AutomationControlled" }
            });

            _isInitialized = true;
            _logger.LogInformation("Playwright browser initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Playwright browser");
            throw;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    public async Task<List<PosterResult>> SearchByTitleAsync(
        string title,
        int? year,
        string mediaType,
        CancellationToken cancellationToken)
    {
        await EnsureBrowserInitializedAsync(cancellationToken);

        try
        {
            // Build search term
            var searchTerm = year.HasValue ? $"{title} {year}" : title;
            var section = mediaType == "show" ? "shows" : "movies";
            var searchUrl = $"{BaseUrl}/search?term={Uri.EscapeDataString(searchTerm)}&section={section}";

            _logger.LogInformation("Searching ThePosterDB: {SearchUrl}", searchUrl);

            // Fetch search page
            var searchHtml = await FetchPageAsync(searchUrl, cancellationToken);

            // Extract set links from search results
            var setIds = ExtractSetIds(searchHtml);

            if (setIds.Count == 0)
            {
                _logger.LogWarning("No sets found for search: {Title}", title);
                return new List<PosterResult>();
            }

            // Scrape first matching set
            var firstSetId = setIds.First();
            _logger.LogInformation("Found set {SetId}, fetching posters...", firstSetId);
            
            return await ScrapeSetAsync(firstSetId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching ThePosterDB for: {Title}", title);
            return new List<PosterResult>();
        }
    }

    public async Task<List<PosterResult>> ScrapeSetAsync(int setId, CancellationToken cancellationToken)
    {
        await EnsureBrowserInitializedAsync(cancellationToken);

        try
        {
            var setUrl = $"{BaseUrl}/set/{setId}";
            _logger.LogInformation("Fetching set: {SetUrl}", setUrl);

            var html = await FetchPageAsync(setUrl, cancellationToken);

            return ParseSetPage(html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping set {SetId}", setId);
            return new List<PosterResult>();
        }
    }

    private async Task<string> FetchPageAsync(string url, CancellationToken cancellationToken)
    {
        if (_browser == null)
            throw new InvalidOperationException("Browser not initialized");

        var page = await _browser.NewPageAsync();
        try
        {
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
            
            // Wait for content to load
            await page.WaitForSelectorAsync("script", new() { Timeout = 5000 });
            await Task.Delay(1000, cancellationToken); // Extra wait for JS execution

            return await page.ContentAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private List<int> ExtractSetIds(string html)
    {
        var setIds = new List<int>();
        var pattern = new Regex(@"/set/(\d+)", RegexOptions.IgnoreCase);
        var matches = pattern.Matches(html);

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out var setId))
            {
                if (!setIds.Contains(setId))
                {
                    setIds.Add(setId);
                }
            }
        }

        return setIds;
    }

    private List<PosterResult> ParseSetPage(string html)
    {
        var results = new List<PosterResult>();

        try
        {
            // Match poster IDs from data-poster-id attributes
            var posterIdPattern = new Regex(@"data-poster-id=""(\d+)""", RegexOptions.IgnoreCase);
            var posterIdMatches = posterIdPattern.Matches(html);

            // Match poster titles
            var titlePattern = new Regex(@"<p class=""p-0 mb-1 text-break"">([^<]+)</p>", RegexOptions.IgnoreCase);
            var titleMatches = titlePattern.Matches(html);

            // Match media types
            var typePattern = new Regex(@"data-toggle=""tooltip""[^>]*title=""(Movie|Show|Collection)""", RegexOptions.IgnoreCase);
            var typeMatches = typePattern.Matches(html);

            var posterCount = Math.Min(posterIdMatches.Count, titleMatches.Count);

            for (int i = 0; i < posterCount; i++)
            {
                var posterId = posterIdMatches[i].Groups[1].Value;
                var titleText = titleMatches[i].Groups[1].Value.Trim();
                var mediaType = i < typeMatches.Count ? typeMatches[i].Groups[1].Value : "Movie";

                // Build API URL for high-res poster
                var posterUrl = $"{BaseUrl}/api/assets/{posterId}";

                // Parse title and year
                var (title, year) = ParseTitleString(titleText);

                results.Add(new PosterResult
                {
                    Id = posterId,
                    Title = title,
                    ThumbnailUrl = posterUrl,
                    FullUrl = posterUrl,
                    Uploader = "ThePosterDB",
                    Width = 1000,
                    Height = 1500,
                    IsTextless = false,
                    Language = "en",
                    Likes = 0
                });
            }

            _logger.LogInformation("Parsed {Count} posters from set page", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing set page HTML");
        }

        return results;
    }

    private (string title, int? year) ParseTitleString(string input)
    {
        // Extract year from (YYYY) pattern
        var yearMatch = Regex.Match(input, @"\((\d{4})\)");
        if (yearMatch.Success)
        {
            var year = int.Parse(yearMatch.Groups[1].Value);
            var title = input.Substring(0, yearMatch.Index).Trim();
            return (title, year);
        }

        return (input, null);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
            _browser = null;
        }

        if (_playwright != null)
        {
            _playwright.Dispose();
            _playwright = null;
        }

        _browserLock.Dispose();
        _isInitialized = false;

        GC.SuppressFinalize(this);
    }
}
