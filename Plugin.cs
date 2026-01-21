using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Plugin.PosterDB.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PosterDB;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILogger<Plugin> _logger;

    public Plugin(
        IApplicationPaths applicationPaths, 
        IXmlSerializer xmlSerializer,
        ILogger<Plugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        _logger = logger;

        // Auto-install Chromium browser on first startup
        Task.Run(async () => await EnsureBrowserInstalledAsync());
    }

    public override string Name => "PosterDB";

    public override Guid Id => Guid.Parse("a4df60d5-8c0f-4b4a-9e5c-3f8a2e1b7c9d");

    public static Plugin? Instance { get; private set; }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        };
    }

    private async Task EnsureBrowserInstalledAsync()
    {
        try
        {
            if (IsBrowserInstalled())
            {
                _logger.LogInformation("Playwright Chromium browser already installed");
                return;
            }

            _logger.LogInformation("Chromium browser not found. Installing automatically...");
            _logger.LogInformation("This is a one-time download (~200MB). Please wait...");

            // Install Chromium via Playwright
            var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });

            if (exitCode == 0)
            {
                _logger.LogInformation("Chromium browser installed successfully!");
            }
            else
            {
                _logger.LogWarning(
                    "Chromium installation returned non-zero exit code: {ExitCode}. " +
                    "Browser may still work, or you may need to install manually with: playwright install chromium",
                    exitCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to auto-install Chromium browser. " +
                "You may need to install manually with: playwright install chromium");
        }
    }

    private bool IsBrowserInstalled()
    {
        try
        {
            // Check common Playwright browser cache locations
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            // Linux/Mac: ~/.cache/ms-playwright/chromium-*
            var linuxPath = Path.Combine(userProfile, ".cache", "ms-playwright");
            if (Directory.Exists(linuxPath))
            {
                var chromiumDirs = Directory.GetDirectories(linuxPath, "chromium-*");
                if (chromiumDirs.Length > 0)
                {
                    return true;
                }
            }

            // Windows: %USERPROFILE%\AppData\Local\ms-playwright\chromium-*
            var windowsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ms-playwright");
            if (Directory.Exists(windowsPath))
            {
                var chromiumDirs = Directory.GetDirectories(windowsPath, "chromium-*");
                if (chromiumDirs.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if Chromium is installed, assuming not installed");
            return false;
        }
    }
}
