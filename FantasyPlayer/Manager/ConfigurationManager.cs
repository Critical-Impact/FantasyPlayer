using Dalamud.Plugin;
using FantasyPlayer.Config;
using FantasyPlayer.Interfaces;

namespace FantasyPlayer.Manager;

using System;
using System.Threading;
using System.Threading.Tasks;
using DalaMock.Host.Mediator;
using Dalamud.Plugin.Services;
using FantasyPlayer.Mediator;
using Microsoft.Extensions.Hosting;

public class ConfigurationManager : IConfigurationManager, IHostedService

{
    private IDalamudPluginInterface _pluginInterface;
    private readonly IFramework framework;
    private readonly MediatorService mediatorService;
    private DateTime? nextFrameworkSave;

    public ConfigurationManager(IDalamudPluginInterface pluginInterface, IFramework framework, MediatorService mediatorService)
    {
        _pluginInterface = pluginInterface;
        this.framework = framework;
        this.mediatorService = mediatorService;
    }

    public Configuration Config { get; set; }
    public string ConfigurationFile
    {
        get => _pluginInterface.ConfigFile.FullName;
        set { } //No setting the configuration file
    }

    public void Load()
    {
        var pluginConfig = (Configuration)_pluginInterface.GetPluginConfig();
        Config = pluginConfig ?? new Configuration();
    }

    public void Save()
    {
        Config.MarkClean();
        _pluginInterface.SavePluginConfig(Config);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _pluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
        framework.Update += FrameworkOnUpdate;
        return Task.CompletedTask;
    }


    public void OpenConfig()
    {
        Config.ConfigShown = true;
    }

    private void FrameworkOnUpdate(IFramework _)
    {
        if (Config.IsDirty && (nextFrameworkSave == null || nextFrameworkSave <= DateTime.Now))
        {
            Save();
            mediatorService.Publish(new ConfigurationUpdatedMessage());
            nextFrameworkSave = DateTime.Now.AddMilliseconds(10);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _pluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
        framework.Update -= FrameworkOnUpdate;
        Save();
        return Task.CompletedTask;
    }
}