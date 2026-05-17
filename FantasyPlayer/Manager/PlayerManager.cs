using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Plugin;
using FantasyPlayer.Extensions;
using FantasyPlayer.Interface;
using FantasyPlayer.Interfaces;
using FantasyPlayer.Provider;
using FantasyPlayer.Provider.Common;

namespace FantasyPlayer.Manager
{
    using System.Threading;
    using Config;
    using Dalamud.Game.Config;
    using Dalamud.Plugin.Services;
    using Microsoft.Extensions.Hosting;

    public class PlayerManager : BackgroundService
    {
        private readonly IPluginLog pluginLog;
        private readonly Configuration configuration;
        private readonly IFramework framework;
        private readonly IGameConfig gameConfig;

        private bool _wasPlaying = false;
        private uint _savedBgmVolume = 100;
        public IPlayerProvider? CurrentPlayerProvider;

        public bool ProvidersLoading
        {
            get
            {
                return PlayerProviders.Any(c => !c.Initialized);
            }
        }

        public List<IPlayerProvider> PlayerProviders { get; }

        public PlayerManager(IPluginLog pluginLog, Configuration configuration, IEnumerable<IPlayerProvider> playerProviders, 
            IFramework framework, IGameConfig gameConfig, ICommandManager command)
        {
            this.pluginLog = pluginLog;
            this.configuration = configuration;
            this.PlayerProviders = playerProviders.ToList();
            this.framework = framework;
            this.gameConfig = gameConfig;
            SetupDefaultProvider();
        }

        public void SetupDefaultProvider()
        {
            var defaultProvider =
                PlayerProviders.FirstOrDefault(c => c.Key == this.configuration.PlayerSettings.DefaultProvider);
            if (defaultProvider != null)
            {
                CurrentPlayerProvider = defaultProvider;
            }
        }

        private Task<IPlayerProvider> InitializeProvider(Type type, IPlayerProvider playerProvider)
        {
            return playerProvider.Initialize();
        }

        private void Update(IFramework framework)
        {
            foreach (var playerProvider in PlayerProviders)
                playerProvider.Update();
            var isPlaying = CurrentPlayerProvider != null && CurrentPlayerProvider.PlayerState.IsPlaying;

            if (isPlaying && !_wasPlaying)
            {
                gameConfig.TryGet(SystemConfigOption.SoundBgm, out _savedBgmVolume);
                gameConfig.Set(SystemConfigOption.SoundBgm, 0u);
            }
            else if (!isPlaying && _wasPlaying)
            {
                gameConfig.Set(SystemConfigOption.SoundBgm, _savedBgmVolume);
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            this.framework.Update += Update;
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            this.framework.Update -= Update;
            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var uninitializedProvider = PlayerProviders.FirstOrDefault(c => !c.Initialized);
                if (uninitializedProvider != null)
                {
                    await uninitializedProvider.Initialize();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}