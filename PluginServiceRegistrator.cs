using Jellyfin.Plugin.PosterDB.Api;
using Jellyfin.Plugin.PosterDB.Providers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.PosterDB;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient();
        serviceCollection.AddMemoryCache();
        serviceCollection.AddSingleton<PosterDBClient>();
        serviceCollection.AddSingleton<PosterDBImageProvider>();
    }
}
