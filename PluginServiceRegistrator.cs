using System.Net.Http;
using Jellyfin.Plugin.PosterDB.Api;
using Jellyfin.Plugin.PosterDB.Providers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PosterDB;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddMemoryCache();
        
        serviceCollection.AddSingleton<PlaywrightScraper>();
        
        serviceCollection.AddSingleton<PosterDBClient>();
        
        serviceCollection.AddSingleton<PosterDBImageProvider>();
    }
}
