using Jellyfin.Plugin.Jellythek.Helpers;
using Jellyfin.Plugin.Jellythek.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Jellythek;

/// <summary>
/// Registriert Plugin-Services im Dependency Injection Container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Helper Services registrieren
        serviceCollection.AddSingleton<RssFeedParser>();
        serviceCollection.AddSingleton<StrmFileHelper>();
        
        // Scheduled Task registrieren
        serviceCollection.AddSingleton<IScheduledTask, MediathekSyncTask>();
    }
}