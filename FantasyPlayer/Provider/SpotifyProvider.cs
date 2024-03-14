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
    public class SpotifyProvider : IPlayerProvider
    {
        public PlayerStateStruct PlayerState { get; set; }
        
        private IPlugin _plugin;
        private SpotifyState _spotifyState;
        private string _lastId;
        
        private CancellationTokenSource _startCts;
        private CancellationTokenSource _loginCts;

        public async Task<IPlayerProvider> Initialize(IPlugin plugin)
        {
            _plugin = plugin;
            PlayerState = new PlayerStateStruct
            {
                ServiceName = "Spotify",
                RequiresLogin = true
            };

            _spotifyState = new SpotifyState(Constants.SpotifyLoginUri, Constants.SpotifyClientId,
                Constants.SpotifyLoginPort, Constants.SpotifyPlayerRefreshTime);

            _spotifyState.OnLoggedIn += OnLoggedIn;
            _spotifyState.OnPlayerStateUpdate += OnPlayerStateUpdate;
            
            if (_plugin.Configuration.SpotifySettings.TokenResponse == null) return this;
            _spotifyState.TokenResponse = _plugin.Configuration.SpotifySettings.TokenResponse;
            await _spotifyState.RequestToken();
            _startCts = new CancellationTokenSource();
            await Task.Run(() => _spotifyState.Start(_startCts.Token), _startCts.Token);
            return this;
        }

        private void OnPlayerStateUpdate(CurrentlyPlayingContext currentlyPlaying, FullTrack playbackItem)
        {
            if (playbackItem.Id != _lastId)
                _plugin.DisplaySongTitle(playbackItem.Name);
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

            _plugin.Configuration.SpotifySettings.TokenResponse = tokenResponse;

            if (_spotifyState.IsPremiumUser)
                _plugin.Configuration.SpotifySettings.LimitedAccess = false;

            if (!_spotifyState.IsPremiumUser)
            {
                if (!_plugin.Configuration.SpotifySettings.LimitedAccess
                ) //Do a check to not spam the user, I don't want to force it down their throats. (fuck marketing)
                    Service.ChatGui.PrintError(
                        "Uh-oh, it looks like you're not premium on Spotify. Some features in Fantasy Player have been disabled.");

                _plugin.Configuration.SpotifySettings.LimitedAccess = true;

                //Change configs
                if (_plugin.Configuration.PlayerSettings.CompactPlayer)
                    _plugin.Configuration.PlayerSettings.CompactPlayer = false;
                if (!_plugin.Configuration.PlayerSettings.NoButtons)
                    _plugin.Configuration.PlayerSettings.NoButtons = true;
            }

            _plugin.ConfigurationManager.Save();
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
    }
}