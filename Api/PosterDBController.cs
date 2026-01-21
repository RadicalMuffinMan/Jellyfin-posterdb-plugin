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

    [HttpGet("search/title")]
    public async Task<ActionResult<SearchResponse>> SearchByTitle(
        [FromQuery] [Required] string query,
        CancellationToken cancellationToken)
    {
        var result = await _client.SearchByTitleAsync(query, cancellationToken);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new
        {
            configured = true,
            version = Plugin.Instance?.Version.ToString() ?? "unknown"
        });
    }
}
