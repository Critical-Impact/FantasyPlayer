using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FantasyPlayer.Config;
using FantasyPlayer.Manager;
using Microsoft.Extensions.Hosting;

namespace FantasyPlayer.Lyrics
{
    public class LyricsManager : BackgroundService
    {
        private readonly IPluginLog _pluginLog;
        private readonly Configuration _configuration;
        private readonly PlayerManager _playerManager;
        private readonly LyricsService _lyricsService;
        private readonly IChatGui _chatGui;
        private readonly IFlyTextGui _flyTextGui;
        private readonly IFramework _framework;

        private string? _lastTrackId;
        private List<(TimeSpan Time, string Text)> _currentLyrics = new();
        private int _lastLyricIndex = -1;
        private bool _isFetching;

        private int _lastProgressMs = -1;
        private DateTime _lastProgressTime = DateTime.MinValue;

        public LyricsManager(
            IPluginLog pluginLog,
            Configuration configuration,
            PlayerManager playerManager,
            LyricsService lyricsService,
            IChatGui chatGui,
            IFlyTextGui flyTextGui,
            IFramework framework)
        {
            _pluginLog = pluginLog;
            _configuration = configuration;
            _playerManager = playerManager;
            _lyricsService = lyricsService;
            _chatGui = chatGui;
            _flyTextGui = flyTextGui;
            _framework = framework;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _framework.Update += OnFrameworkUpdate;
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _framework.Update -= OnFrameworkUpdate;
            await base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (!_configuration.LyricsSettings.EnableLyrics)
                return;

            var provider = _playerManager.CurrentPlayerProvider;
            if (provider == null)
                return;

            var state = provider.PlayerState;
            if (!state.IsPlaying)
                return;

            var currentTrack = state.CurrentlyPlaying;
            var trackId = currentTrack.Id;

            if (trackId != _lastTrackId)
            {
                _lastTrackId = trackId;
                _lastLyricIndex = -1;
                _lastProgressMs = -1;
                _lastProgressTime = DateTime.MinValue;
                _currentLyrics = new List<(TimeSpan, string)>();

                if (!string.IsNullOrEmpty(trackId) && !_isFetching)
                {
                    var artist = currentTrack.Artists.Length > 0 ? currentTrack.Artists[0] : string.Empty;
                    _ = FetchLyricsAsync(artist, currentTrack.Name, currentTrack.Album.Name, currentTrack.DurationMs);
                }
            }

            if (_currentLyrics.Count == 0)
                return;

            var apiProgressMs = state.ProgressMs;
            var now = DateTime.UtcNow;

            TimeSpan currentTime;
            if (_lastProgressMs >= 0 && apiProgressMs == _lastProgressMs)
            {
                currentTime = TimeSpan.FromMilliseconds(_lastProgressMs) + (now - _lastProgressTime);
            }
            else
            {
                if (_lastProgressMs >= 0)
                {
                    var expectedMs = _lastProgressMs + (int)(now - _lastProgressTime).TotalMilliseconds;
                    if (apiProgressMs < _lastProgressMs || Math.Abs(apiProgressMs - expectedMs) > 5000)
                    {
                        _lastLyricIndex = -1;
                    }
                }

                _lastProgressMs = apiProgressMs;
                _lastProgressTime = now;
                currentTime = TimeSpan.FromMilliseconds(apiProgressMs);
            }

            for (var i = _lastLyricIndex + 1; i < _currentLyrics.Count; i++)
            {
                var lyric = _currentLyrics[i];
                if (currentTime < lyric.Time)
                    break;

                var isLastLine = i + 1 >= _currentLyrics.Count;
                var beforeNext = isLastLine || currentTime < _currentLyrics[i + 1].Time;

                if (beforeNext)
                {
                    DisplayLyric(lyric.Text);
                    _lastLyricIndex = i;
                    break;
                }
            }
        }

        private async Task FetchLyricsAsync(string artist, string title, string album, int durationMs)
        {
            _isFetching = true;
            try
            {
                var lyrics = await _lyricsService.FetchSyncedLyricsAsync(artist, title, album, durationMs);
                _currentLyrics = lyrics;
                _lastLyricIndex = -1;
            }
            catch (Exception ex)
            {
                _pluginLog.Error($"Failed to fetch lyrics: {ex.Message}");
            }
            finally
            {
                _isFetching = false;
            }
        }

        private void DisplayLyric(string text)
        {
            try
            {
                var line = $"♪ {text}";
                if (_configuration.LyricsSettings.DisplayMode == LyricDisplayMode.FlyText)
                {
                    _flyTextGui.AddFlyText(
                        FlyTextKind.Named,
                        1, 0, 0,
                        new SeString(new List<Payload> { new TextPayload(line) }),
                        SeString.Empty,
                        _configuration.LyricsSettings.FlyTextColor,
                        0, 0);
                }
                else
                {
                    _chatGui.Print(new XivChatEntry
                    {
                        Message = new SeString(new List<Payload> { new TextPayload(line) }),
                        Name = SeString.Empty,
                        Type = _configuration.LyricsSettings.ChatType,
                    });
                }
            }
            catch (Exception ex)
            {
                _pluginLog.Error($"Failed to display lyric: {ex.Message}");
            }
        }
    }
}
