using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using FantasyPlayer.Provider.Common;
using Microsoft.Extensions.Logging;

namespace FantasyPlayer.Ipc
{
    using Manager;
    using Microsoft.Extensions.Hosting;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes FantasyPlayer state and controls via Dalamud IPC.
    /// Consumer prefix: "FantasyPlayer."
    ///
    /// Read gates (Func):
    ///   GetTitle          → string   Current song title, empty if nothing loaded
    ///   GetArtist         → string   Current song artist(s)
    ///   GetAlbum          → string   Current song album
    ///   GetDuration       → float    Total duration in seconds
    ///   GetPosition       → float    Current playback position in seconds
    ///   GetPlaybackState  → int      0=stopped, 1=playing
    ///   GetShuffle        → bool     Shuffle enabled
    ///   GetRepeatMode     → int      0=off, 1=all, 2=one
    ///
    /// Control gates (Action):
    ///   Play              ()          Resume or start playback
    ///   Pause             ()          Pause playback
    ///   Next              ()          Skip to next song
    ///   Previous          ()          Skip to previous song
    ///   ToggleShuffle     ()          Toggle shuffle
    ///   ToggleRepeat      ()          Cycle repeat mode
    ///   SetVolume         (int)       Set volume 0-100
    ///
    /// Events (SendMessage):
    ///   Initialized           → bool                              Fires once on startup
    ///   PlaybackStateChanged  → bool                              Fires when play/pause state changes; payload = IsPlaying
    ///   TrackChanged          → (string?, string, string[], string, int)
    ///                                                             Fires when the current track changes;
    ///                                                             payload = (Id, Title, Artists, Album, DurationMs)
    /// </summary>
    public sealed class IpcService : IHostedService, IDisposable
    {
        private readonly IDalamudPluginInterface _pluginInterface;
        private readonly PlayerManager _playerManager;
        private readonly IFramework _framework;
        private readonly ILogger<IpcService> _logger;

        private ICallGateProvider<string>? _getTitle;
        private ICallGateProvider<string>? _getArtist;
        private ICallGateProvider<string>? _getAlbum;
        private ICallGateProvider<float>? _getDuration;
        private ICallGateProvider<float>? _getPosition;
        private ICallGateProvider<int>? _getPlaybackState;
        private ICallGateProvider<bool>? _getShuffle;
        private ICallGateProvider<int>? _getRepeatMode;
        private ICallGateProvider<object?>? _play;
        private ICallGateProvider<object?>? _pause;
        private ICallGateProvider<object?>? _next;
        private ICallGateProvider<object?>? _previous;
        private ICallGateProvider<object?>? _toggleShuffle;
        private ICallGateProvider<object?>? _toggleRepeat;
        private ICallGateProvider<int, object?>? _setVolume;

        private ICallGateProvider<bool, bool>? _initialized;
        private ICallGateProvider<bool, bool>? _playbackStateChanged;
        private ICallGateProvider<(string?, string, string[], string, int), bool>? _trackChanged;

        private bool? _lastIsPlaying;
        private string? _lastTrackId;

        public IpcService(IDalamudPluginInterface pluginInterface, PlayerManager playerManager, IFramework framework, ILogger<IpcService> logger)
        {
            this._pluginInterface = pluginInterface;
            this._playerManager = playerManager;
            this._framework = framework;
            this._logger = logger;
        }

        private static (string?, string, string[], string, int) PackTrack(TrackStruct t) => (t.Id, t.Name, t.Artists, t.Album.Name, t.DurationMs);

        private void OnFrameworkUpdate(IFramework f)
        {
            var provider = _playerManager.CurrentPlayerProvider;
            if (provider == null) return;
            var state = provider.PlayerState;

            if (_lastIsPlaying != state.IsPlaying)
            {
                _lastIsPlaying = state.IsPlaying;
                _playbackStateChanged?.SendMessage(state.IsPlaying);
            }

            if (_lastTrackId != state.CurrentlyPlaying.Id)
            {
                _lastTrackId = state.CurrentlyPlaying.Id;
                _trackChanged?.SendMessage(PackTrack(state.CurrentlyPlaying));
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting FantasyPlayer IPC service");

            _getTitle = _pluginInterface.GetIpcProvider<string>("FantasyPlayer.GetTitle");
            _getTitle.RegisterFunc(() => _playerManager.CurrentPlayerProvider?.PlayerState.CurrentlyPlaying.Name ?? string.Empty);

            _getArtist = _pluginInterface.GetIpcProvider<string>("FantasyPlayer.GetArtist");
            _getArtist.RegisterFunc(() =>
            {
                var artists = _playerManager.CurrentPlayerProvider?.PlayerState.CurrentlyPlaying.Artists;
                return artists != null ? string.Join(", ", artists) : string.Empty;
            });

            _getAlbum = _pluginInterface.GetIpcProvider<string>("FantasyPlayer.GetAlbum");
            _getAlbum.RegisterFunc(() => _playerManager.CurrentPlayerProvider?.PlayerState.CurrentlyPlaying.Album.Name ?? string.Empty);

            _getDuration = _pluginInterface.GetIpcProvider<float>("FantasyPlayer.GetDuration");
            _getDuration.RegisterFunc(() => (_playerManager.CurrentPlayerProvider?.PlayerState.CurrentlyPlaying.DurationMs ?? 0) / 1000f);

            _getPosition = _pluginInterface.GetIpcProvider<float>("FantasyPlayer.GetPosition");
            _getPosition.RegisterFunc(() => (_playerManager.CurrentPlayerProvider?.PlayerState.ProgressMs ?? 0) / 1000f);

            _getPlaybackState = _pluginInterface.GetIpcProvider<int>("FantasyPlayer.GetPlaybackState");
            _getPlaybackState.RegisterFunc(() => _playerManager.CurrentPlayerProvider?.PlayerState.IsPlaying == true ? 1 : 0);

            _getShuffle = _pluginInterface.GetIpcProvider<bool>("FantasyPlayer.GetShuffle");
            _getShuffle.RegisterFunc(() => _playerManager.CurrentPlayerProvider?.PlayerState.ShuffleState ?? false);

            _getRepeatMode = _pluginInterface.GetIpcProvider<int>("FantasyPlayer.GetRepeatMode");
            _getRepeatMode.RegisterFunc(() =>
            {
                var state = _playerManager.CurrentPlayerProvider?.PlayerState.RepeatState ?? string.Empty;
                return state switch
                {
                    "track" => 2,
                    "context" => 1,
                    _ => 0
                };
            });

            _play = _pluginInterface.GetIpcProvider<object?>("FantasyPlayer.Play");
            _play.RegisterAction(() => _playerManager.CurrentPlayerProvider?.SetPauseOrPlay(true));

            _pause = _pluginInterface.GetIpcProvider<object?>("FantasyPlayer.Pause");
            _pause.RegisterAction(() => _playerManager.CurrentPlayerProvider?.SetPauseOrPlay(false));

            _next = _pluginInterface.GetIpcProvider<object?>("FantasyPlayer.Next");
            _next.RegisterAction(() => _playerManager.CurrentPlayerProvider?.SetSkip(true));

            _previous = _pluginInterface.GetIpcProvider<object?>("FantasyPlayer.Previous");
            _previous.RegisterAction(() => _playerManager.CurrentPlayerProvider?.SetSkip(false));

            _toggleShuffle = _pluginInterface.GetIpcProvider<object?>("FantasyPlayer.ToggleShuffle");
            _toggleShuffle.RegisterAction(() =>
            {
                var provider = _playerManager.CurrentPlayerProvider;
                if (provider != null)
                {
                    provider.SetShuffle(!provider.PlayerState.ShuffleState);
                }
            });

            _toggleRepeat = _pluginInterface.GetIpcProvider<object?>("FantasyPlayer.ToggleRepeat");
            _toggleRepeat.RegisterAction(() => _playerManager.CurrentPlayerProvider?.SwapRepeatState());

            _setVolume = _pluginInterface.GetIpcProvider<int, object?>("FantasyPlayer.SetVolume");
            _setVolume.RegisterAction(vol => _playerManager.CurrentPlayerProvider?.SetVolume(Math.Clamp(vol, 0, 100)));

            _playbackStateChanged = _pluginInterface.GetIpcProvider<bool, bool>("FantasyPlayer.PlaybackStateChanged");
            _trackChanged = _pluginInterface.GetIpcProvider<(string?, string, string[], string, int), bool>("FantasyPlayer.TrackChanged");
            _initialized = _pluginInterface.GetIpcProvider<bool, bool>("FantasyPlayer.Initialized");

            _framework.Update += OnFrameworkUpdate;

            _initialized.SendMessage(true);

            _logger.LogDebug("FantasyPlayer IPC service started");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Stopping FantasyPlayer IPC service");
            Dispose();
            _logger.LogDebug("FantasyPlayer IPC service stopped");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _framework.Update -= OnFrameworkUpdate;

            _getTitle?.UnregisterFunc();
            _getArtist?.UnregisterFunc();
            _getAlbum?.UnregisterFunc();
            _getDuration?.UnregisterFunc();
            _getPosition?.UnregisterFunc();
            _getPlaybackState?.UnregisterFunc();
            _getShuffle?.UnregisterFunc();
            _getRepeatMode?.UnregisterFunc();
            _play?.UnregisterAction();
            _pause?.UnregisterAction();
            _next?.UnregisterAction();
            _previous?.UnregisterAction();
            _toggleShuffle?.UnregisterAction();
            _toggleRepeat?.UnregisterAction();
            _setVolume?.UnregisterAction();
            _playbackStateChanged?.UnregisterFunc();
            _trackChanged?.UnregisterFunc();
            _initialized?.UnregisterFunc();
        }
    }
}
