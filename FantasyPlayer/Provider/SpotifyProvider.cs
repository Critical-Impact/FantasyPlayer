using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FantasyPlayer.Interface;
using FantasyPlayer.Interfaces;
using FantasyPlayer.Provider.Common;
using FantasyPlayer.Spotify;
using SpotifyAPI.Web;

namespace FantasyPlayer.Provider
{
    using Config;
    using Dalamud.Plugin.Services;
    using Manager;

    public class SpotifyProvider : IPlayerProvider
    {
        private readonly ConfigurationManager configurationManager;
        private readonly Configuration configuration;
        private readonly ChatMessageService chatMessageService;
        private readonly IChatGui chatGui;

        public SpotifyProvider(ConfigurationManager configurationManager, Configuration configuration, ChatMessageService chatMessageService, IChatGui chatGui)
        {
            this.configurationManager = configurationManager;
            this.configuration = configuration;
            this.chatMessageService = chatMessageService;
            this.chatGui = chatGui;
        }
        public PlayerStateStruct PlayerState { get; set; }
        
        private SpotifyState _spotifyState;
        private string _lastId;
        
        private CancellationTokenSource _startCts;
        private CancellationTokenSource _loginCts;
        private bool initialized;

        public async Task<IPlayerProvider> Initialize()
        {
            PlayerState = new PlayerStateStruct
            {
                ServiceName = "Spotify",
                RequiresLogin = true
            };

            _spotifyState = new SpotifyState(Constants.SpotifyLoginUri, Constants.SpotifyClientId, Constants.SpotifyLoginPort, Constants.SpotifyPlayerRefreshTime);

            _spotifyState.OnLoggedIn += OnLoggedIn;
            _spotifyState.OnPlayerStateUpdate += OnPlayerStateUpdate;

            if (configuration.SpotifySettings.TokenResponse == null)
            {
                initialized = true;
                return this;
            }
            _spotifyState.TokenResponse = configuration.SpotifySettings.TokenResponse;
            await _spotifyState.RequestToken();
            _startCts = new CancellationTokenSource();
            await Task.Run(() => _spotifyState.Start(_startCts.Token), _startCts.Token);
            initialized = true;
            return this;
        }

        public string Key => "spotify";

        public string Name => "Spotify";

        public bool Initialized => initialized;

        private void OnPlayerStateUpdate(CurrentlyPlayingContext currentlyPlaying, FullTrack playbackItem)
        {
            if (playbackItem.Id != _lastId)
                chatMessageService.DisplaySongTitle(playbackItem.Name);
            _lastId = playbackItem.Id;


            var playerStateStruct = PlayerState;
            playerStateStruct.ProgressMs = currentlyPlaying.ProgressMs;
            playerStateStruct.IsPlaying = currentlyPlaying.IsPlaying;
            playerStateStruct.RepeatState = currentlyPlaying.RepeatState;
            playerStateStruct.ShuffleState = currentlyPlaying.ShuffleState;
            
            playerStateStruct.CurrentlyPlaying = new TrackStruct
            {
                Id = playbackItem.Id,
                Name = playbackItem.Name,
                Artists = playbackItem.Artists.Select(artist => artist.Name).ToArray(),
                DurationMs = playbackItem.DurationMs,
                Album = new AlbumStruct
                {
                    Name = playbackItem.Album.Name
                }
            };
            
            PlayerState = playerStateStruct;
        }

        private void OnLoggedIn(PrivateUser privateUser, PKCETokenResponse tokenResponse)
        {
            var playerStateStruct = PlayerState;
            playerStateStruct.IsLoggedIn = true;
            PlayerState = playerStateStruct;

            configuration.SpotifySettings.TokenResponse = tokenResponse;

            if (_spotifyState.IsPremiumUser)
                configuration.SpotifySettings.LimitedAccess = false;

            if (!_spotifyState.IsPremiumUser)
            {
                if (!configuration.SpotifySettings.LimitedAccess
                ) //Do a check to not spam the user, I don't want to force it down their throats. (fuck marketing)
                    chatGui.PrintError(
                        "Uh-oh, it looks like you're not premium on Spotify. Some features in Fantasy Player have been disabled.");

                configuration.SpotifySettings.LimitedAccess = true;

                //Change configs
                if (configuration.PlayerSettings.CompactPlayer)
                    configuration.PlayerSettings.CompactPlayer = false;
                if (!configuration.PlayerSettings.NoButtons)
                    configuration.PlayerSettings.NoButtons = true;
            }
        }

        public void Update()
        {
        }

        public void ReAuth()
        {
            //StartAuth();
        }

        public void Dispose()
        {
            if (_startCts != null)
            {
                _startCts.Cancel();
                _startCts.Dispose();
            }

            if (_loginCts != null)
            {
                _loginCts.Cancel();
                _loginCts.Dispose();
            }

            _spotifyState.OnLoggedIn -= OnLoggedIn;
            _spotifyState.OnPlayerStateUpdate -= OnPlayerStateUpdate;
            _spotifyState.Dispose();
        }

        public void StartAuth()
        {
            _loginCts = new CancellationTokenSource();
            Task.Run(() => _spotifyState.StartAuth(_loginCts.Token), _loginCts.Token);
            var playerStateStruct = PlayerState;
            playerStateStruct.IsAuthenticating = true;
            PlayerState = playerStateStruct;
        }

        public void RetryAuth()
        {
            _spotifyState.RetryLogin();
        }

        public void SwapRepeatState()
        {
            if (_spotifyState.CurrentlyPlaying != null)
                _spotifyState.SwapRepeatState();
        }

        public void SetPauseOrPlay(bool play)
        {
            if (_spotifyState.CurrentlyPlaying != null)
                _spotifyState.PauseOrPlay(play);
        }

        public void SetSkip(bool forward)
        {
            if (_spotifyState.CurrentlyPlaying != null)
                _spotifyState.Skip(forward);
        }

        public void SetShuffle(bool value)
        {
            if (_spotifyState.CurrentlyPlaying != null)
                _spotifyState.Shuffle(value);
        }

        public void SetVolume(int volume)
        {
            if (_spotifyState.CurrentlyPlaying != null)
                _spotifyState.SetVolume(volume);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Initialize();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}