namespace FantasyPlayer.Manager
{
    using Config;
    using Dalamud.Game.Config;
    using Dalamud.Plugin.Services;
    using Microsoft.Extensions.Hosting;
    using System.Threading;
    using System.Threading.Tasks;

    public class BgmService : IHostedService
    {
        private readonly IGameConfig gameConfig;
        private readonly Configuration config;
        private readonly IFramework framework;
        private readonly PlayerManager playerManager;
        private readonly IPluginLog log;

        private bool _isMuted = false;
        private uint _savedBgmVolume = 100;

        public BgmService(IGameConfig gameConfig, Configuration config, IFramework framework, PlayerManager playerManager, IPluginLog log)
        {
            this.gameConfig = gameConfig;
            this.config = config;
            this.framework = framework;
            this.playerManager = playerManager;
            this.log = log;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var provider in playerManager.PlayerProviders)
                provider.PlaybackStateChanged += OnPlaybackStateChanged;
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var provider in playerManager.PlayerProviders)
                provider.PlaybackStateChanged -= OnPlaybackStateChanged;
            try
            {
                if (_isMuted)
                {
                    gameConfig.Set(SystemConfigOption.SoundBgm, _savedBgmVolume);
                }
            }
            catch 
            {
                log.Error("Failed to restore BGM volume on shutdown.");
            }
            return Task.CompletedTask;
        }
        private void OnPlaybackStateChanged(bool isPlaying)
        {
            if (!config.PlayerSettings.MuteBgmOnPlayback)
                return;
            framework.RunOnFrameworkThread(() =>
            {
                if (isPlaying)
                {
                    gameConfig.TryGet(SystemConfigOption.SoundBgm, out _savedBgmVolume);
                    gameConfig.Set(SystemConfigOption.SoundBgm, 0u);
                }
                else
                {
                    gameConfig.Set(SystemConfigOption.SoundBgm, _savedBgmVolume);
                }
                _isMuted = isPlaying;
            });
        }
    }
}
