using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.PosterDB.Models;
using MediaBrowser.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.PosterDB.Api;

[ApiController]
[Authorize(Policy = "DefaultAuthorization")]
[Route("PosterDB")]
public class PosterDBController : ControllerBase
{
    private readonly PosterDBClient _client;
    private readonly IServerApplicationHost _applicationHost;

    public PosterDBController(PosterDBClient client, IServerApplicationHost applicationHost)
    {
        _client = client;
        _applicationHost = applicationHost;
    }

    [HttpGet("search/tmdb/{tmdbId}")]
    public async Task<ActionResult<SearchResponse>> SearchByTmdbId(
        [FromRoute] [Required] string tmdbId,
        CancellationToken cancellationToken)
    {
        var apiKey = Plugin.Instance?.Configuration.ApiKey;
        var result = await _client.SearchByTmdbIdAsync(tmdbId, apiKey, cancellationToken);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("search/tvdb/{tvdbId}")]
    public async Task<ActionResult<SearchResponse>> SearchByTvdbId(
        [FromRoute] [Required] string tvdbId,
        CancellationToken cancellationToken)
    {
        var apiKey = Plugin.Instance?.Configuration.ApiKey;
        var result = await _client.SearchByTvdbIdAsync(tvdbId, apiKey, cancellationToken);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("search/imdb/{imdbId}")]
    public async Task<ActionResult<SearchResponse>> SearchByImdbId(
        [FromRoute] [Required] string imdbId,
        CancellationToken cancellationToken)
    {
        var apiKey = Plugin.Instance?.Configuration.ApiKey;
        var result = await _client.SearchByImdbIdAsync(imdbId, apiKey, cancellationToken);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("search/title")]
    public async Task<ActionResult<SearchResponse>> SearchByTitle(
        [FromQuery] [Required] string query,
        CancellationToken cancellationToken)
    {
        var apiKey = Plugin.Instance?.Configuration.ApiKey;
        var result = await _client.SearchByTitleAsync(query, apiKey, cancellationToken);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        var hasApiKey = !string.IsNullOrEmpty(Plugin.Instance?.Configuration.ApiKey);
        
        return Ok(new
        {
            configured = hasApiKey,
            version = Plugin.Instance?.Version.ToString() ?? "unknown"
        });
    }
}
