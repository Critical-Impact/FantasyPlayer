using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using FantasyPlayer.Config;
using FantasyPlayer.Interfaces;
using FantasyPlayer.Manager;
using FantasyPlayer.Provider;
using FantasyPlayer.Provider.Common;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.Logging;
using OtterGui;

namespace FantasyPlayer.Interface.Window
{
    using DalaMock.Host.Mediator;
    using DalaMock.Shared.Interfaces;
    using Dalamud.Interface.Windowing;
    using Dalamud.Plugin.Services;
    using FantasyPlayer.Mediator;
    using Serilog;

    public class PlayerWindow : UpdatingWindow
    {
        private readonly IUiBuilder uiBuilder;
        private readonly IFont font;
        private readonly PlayerManager _playerManager;
        private readonly Configuration configuration;
        private readonly ConfigurationManager _configurationManager;
        private readonly ICondition condition;
        private readonly IClientState clientState;

        private DateTime? _lastUpdated;
        private DateTime? _lastPaused;
        private TimeSpan _difference;
        private int _progressMs;
        private string _lastId;
        private bool _lastBoundByDuty;

        private readonly Vector2 _playerWindowSize = new Vector2(401 * ImGui.GetIO().FontGlobalScale,
            89 * ImGui.GetIO().FontGlobalScale);

        private readonly Vector2 _windowSizeNoButtons = new Vector2(401 * ImGui.GetIO().FontGlobalScale,
            62 * ImGui.GetIO().FontGlobalScale);

        private readonly Vector2 _windowSizeCompact = new Vector2(179 * ImGui.GetIO().FontGlobalScale,
            39 * ImGui.GetIO().FontGlobalScale);


        public PlayerWindow(ILogger<PlayerWindow> logger, IUiBuilder uiBuilder, IFont font, MediatorService mediatorService, PlayerManager playerManager, Configuration configuration, ConfigurationManager configurationManager, ICondition condition, IClientState clientState) : base(logger, mediatorService, "Fantasy Player - Player")
        {
            this.uiBuilder = uiBuilder;
            this.font = font;
            _playerManager = playerManager;
            this.configuration = configuration;
            _configurationManager = configurationManager;
            this.condition = condition;
            this.clientState = clientState;
            SetDefaultWindowSize();
            MediatorService.Subscribe<ConfigurationUpdatedMessage>(this, ConfigurationUpdated );
            this.uiBuilder.OpenMainUi += UiBuilderOnOpenMainUi;
        }

        private void UiBuilderOnOpenMainUi()
        {
            configuration.PlayerSettings.PlayerWindowShown = true;
        }

        private void ConfigurationUpdated(ConfigurationUpdatedMessage obj)
        {
            if (configuration.PlayerSettings.PlayerWindowShown && !IsOpen)
            {
                IsOpen = true;
            }
            else if(!configuration.PlayerSettings.PlayerWindowShown && IsOpen)
            {
                IsOpen = false;
            }

            var lockFlags = (configuration.PlayerSettings.PlayerLocked)
                ? ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize
                : ImGuiWindowFlags.None;

            var clickThroughFlags = (configuration.PlayerSettings.DisableInput)
                ? ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoResize
                : ImGuiWindowFlags.None;

            var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | lockFlags |
                        clickThroughFlags;
            if (Flags != flags)
            {
                Flags = flags;
            }

            SetDefaultWindowSize();
        }

        public override void OnClose()
        {
            configuration.PlayerSettings.PlayerWindowShown = false;
            base.OnClose();
        }

        public override bool DrawConditions()
        {
            if (configuration.PlayerSettings.OnlyOpenWhenLoggedIn &&
                clientState.LocalContentId == 0)
            {
                return false;
            }

            return base.DrawConditions();
        }

        public override void Draw()
        {
            if (configuration.PlayerSettings.OnlyOpenWhenLoggedIn &&
                clientState.LocalContentId == 0)
            {
            }
            else if (_playerManager.CurrentPlayerProvider == null &&
                     configuration.PlayerSettings.PlayerWindowShown)
            {
                DrawWelcome();
            }
            else if (_playerManager.CurrentPlayerProvider != null &&
                     !_playerManager.ProvidersLoading &&
                     _playerManager.CurrentPlayerProvider.PlayerState.RequiresLogin &&
                     configuration.PlayerSettings.PlayerWindowShown &&
                     !_playerManager.CurrentPlayerProvider.PlayerState.IsLoggedIn)
            {
                DrawLogin();
            }
            else if (_playerManager.CurrentPlayerProvider != null &&
                     !_playerManager.ProvidersLoading &&
                     _playerManager.CurrentPlayerProvider.PlayerState.IsLoggedIn &&
                     configuration.PlayerSettings.PlayerWindowShown)
            {
                DrawMain(_playerManager.CurrentPlayerProvider.PlayerState, _playerManager.CurrentPlayerProvider);
                CheckClientState();
            }
            else
            {
                ImGui.Text("Loading, please wait...");
            }
        }

        private void CheckClientState()
        {
            if (_playerManager.CurrentPlayerProvider == null)
            {
                return;
            }

            var isBoundByDuty = condition[ConditionFlag.BoundByDuty];
            if (configuration.AutoPlaySettings.PlayInDuty && isBoundByDuty &&
                !_playerManager.CurrentPlayerProvider.PlayerState.IsPlaying)
            {
                if (_lastBoundByDuty == false)
                {
                    _lastBoundByDuty = true;
                    _playerManager.CurrentPlayerProvider.SetPauseOrPlay(true);
                }
            }

            _lastBoundByDuty = isBoundByDuty;
        }

        public void DrawWelcome()
        {
            if (_playerManager.ProvidersLoading)
            {
                InterfaceUtils.TextCentered($"The music providers are still being loaded.");
                return;
            }

            InterfaceUtils.TextCentered("Please select your default provider.");
            foreach (var provider in _playerManager.PlayerProviders)
            {
                ImGui.SameLine();
                if (ImGui.Button(provider.Name.Replace("Provider", "")))
                {
                    _playerManager.CurrentPlayerProvider = provider;
                    configuration.PlayerSettings.DefaultProvider = provider.Key;
                }
            }
        }

        public void DrawLogin()
        {
            if (_playerManager.ProvidersLoading)
            {
                return;
            }

            var playerProvider = _playerManager.CurrentPlayerProvider;
            if (playerProvider == null)
            {
                return;
            }

            if (!playerProvider.PlayerState.IsAuthenticating)
            {
                InterfaceUtils.TextCentered($"Please login to {playerProvider.PlayerState.ServiceName} to start.");
                if (InterfaceUtils.ButtonCentered("Login"))
                    playerProvider.StartAuth();
            }
            else
            {
                InterfaceUtils.TextCentered("Waiting for a response to login... Please check your browser.");
                if (InterfaceUtils.ButtonCentered("Re-open Url"))
                    playerProvider.RetryAuth();
            }
        }

        public override void Update()
        {


        }

        private void SetDefaultWindowSize()
        {
            if (configuration.PlayerSettings.FirstRunNone)
            {
                Size =_playerWindowSize;
                configuration.PlayerSettings.FirstRunNone = false;
            }

            if (configuration.PlayerSettings.CompactPlayer && configuration.PlayerSettings.FirstRunCompactPlayer)
            {
                Size =_windowSizeCompact;
                configuration.PlayerSettings.FirstRunCompactPlayer = false;
            }

            if (configuration.PlayerSettings.NoButtons && configuration.PlayerSettings.FirstRunSetNoButtons)
            {
                Size =_windowSizeNoButtons;
                configuration.PlayerSettings.FirstRunSetNoButtons = false;
            }

            if (configuration.SpotifySettings.LimitedAccess && configuration.PlayerSettings.FirstRunCompactPlayer)
            {
                Size =_windowSizeNoButtons;
                configuration.PlayerSettings.FirstRunCompactPlayer = false;
            }
        }

        private void DrawMain(PlayerStateStruct playerState, IPlayerProvider currentProvider)
        {
            BgAlpha = configuration.PlayerSettings.Transparency;

            if (Size != null)
            {
                Size = null;
            }

            //////////////// Right click popup ////////////////

            if (ImGui.BeginPopupContextWindow("RightClick"))
            {
                if (_playerManager.PlayerProviders.Count > 1)
                {
                    if (ImGui.BeginMenu("Switch provider"))
                    {
                        foreach (var provider in _playerManager.PlayerProviders)
                        {
                            if (provider == _playerManager.CurrentPlayerProvider) continue;
                            if (ImGui.MenuItem(provider.Name))
                            {
                                _playerManager.CurrentPlayerProvider = provider;
                                configuration.PlayerSettings.DefaultProvider = provider.Key;
                            }
                        }
                        ImGui.EndMenu();
                    }

                    ImGui.Separator();
                }

                if (!configuration.SpotifySettings.LimitedAccess)
                {
                    var compactPlayer = configuration.PlayerSettings.CompactPlayer;
                    if (ImGui.MenuItem("Compact mode", ref compactPlayer))
                    {
                        if (configuration.PlayerSettings.NoButtons)
                            configuration.PlayerSettings.NoButtons = false;
                        configuration.PlayerSettings.CompactPlayer = compactPlayer;
                    }

                    var noButtons = configuration.PlayerSettings.NoButtons;
                    if (ImGui.MenuItem("Hide Buttons", ref noButtons))
                    {
                        if (configuration.PlayerSettings.CompactPlayer)
                            configuration.PlayerSettings.CompactPlayer = false;
                        configuration.PlayerSettings.NoButtons = noButtons;
                    }

                    ImGui.Separator();
                }

                var playerSettingsPlayerLocked = configuration.PlayerSettings.PlayerLocked;
                if (ImGui.MenuItem("Lock player", ref playerSettingsPlayerLocked))
                {
                    configuration.PlayerSettings.PlayerLocked = playerSettingsPlayerLocked;
                }

                var playerWindowShown = configuration.PlayerSettings.PlayerWindowShown;
                if (ImGui.MenuItem("Show player", ref playerWindowShown))
                {
                    configuration.PlayerSettings.PlayerWindowShown = playerWindowShown;
                }

                var configShown = configuration.ConfigShown;
                if (ImGui.MenuItem("Show config", ref configShown))
                {
                    configuration.ConfigShown = configShown;
                }

                ImGui.EndPopup();
            }

            //////////////// Window Basics ////////////////

            if (playerState.CurrentlyPlaying.Id == null)
            {
                InterfaceUtils.TextCentered($"Nothing is playing on {playerState.ServiceName}.");
                return;
            }

            {
                //////////////// Window Setup ////////////////

                ImGui.PushStyleColor(ImGuiCol.Button, InterfaceUtils.TransparentColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, InterfaceUtils.TransparentColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, InterfaceUtils.DarkenButtonColor);

                var track = playerState.CurrentlyPlaying;



                if (_progressMs != playerState.ProgressMs)
                {
                    _lastUpdated = DateTime.Now;
                    _progressMs = playerState.ProgressMs;
                }
                if (!playerState.IsPlaying)
                {
                    _lastUpdated = null;
                }
                float percent;
                TimeSpan actualProgress;
                TimeSpan songTotal;

                if (_lastUpdated != null)
                {
                    actualProgress = (DateTime.Now - _lastUpdated.Value).Add(TimeSpan.FromMilliseconds(playerState.ProgressMs));
                    songTotal = TimeSpan.FromMilliseconds(track.DurationMs);
                    if (actualProgress >= songTotal)
                    {
                        actualProgress = songTotal;
                    }
                    percent = (float)((double) actualProgress.Ticks / songTotal.Ticks * 100);
                }
                else
                {
                    actualProgress = TimeSpan.FromMilliseconds(playerState.ProgressMs);
                    songTotal = TimeSpan.FromMilliseconds(track.DurationMs);
                    if (actualProgress >= songTotal)
                    {
                        actualProgress = songTotal;
                    }
                    percent = (float)((double) actualProgress.Ticks / songTotal.Ticks * 100);
                }

                var artists = track.Artists.Aggregate("", (current, artist) => current + (artist + ", "));

                if (!configuration.PlayerSettings.NoButtons)
                {
                    //////////////// Play and Pause ////////////////

                    var stateIcon = (playerState.IsPlaying)
                        ? FontAwesomeIcon.Pause.ToIconString()
                        : FontAwesomeIcon.Play.ToIconString();

                    ImGui.PushFont(font.IconFont);

                    if (ImGui.Button(FontAwesomeIcon.Backward.ToIconString()))
                        currentProvider.SetSkip(false);

                    if (InterfaceUtils.ButtonCentered(stateIcon))
                        currentProvider.SetPauseOrPlay(!playerState.IsPlaying);

                    //////////////// Shuffle and Repeat ////////////////

                    ImGui.SameLine(ImGui.GetWindowSize().X / 2 +
                                   (ImGui.GetFontSize() + ImGui.CalcTextSize(FontAwesomeIcon.Random.ToIconString()).X));

                    if (playerState.ShuffleState)
                        ImGui.PushStyleColor(ImGuiCol.Text, configuration.PlayerSettings.AccentColor);

                    if (ImGui.Button(FontAwesomeIcon.Random.ToIconString()))
                        currentProvider.SetShuffle(!playerState.ShuffleState);

                    if (playerState.ShuffleState)
                        ImGui.PopStyleColor();

                    if (playerState.RepeatState != "off")
                        ImGui.PushStyleColor(ImGuiCol.Text, configuration.PlayerSettings.AccentColor);

                    var buttonIcon = FontAwesomeIcon.Retweet.ToIconString();

                    if (playerState.RepeatState == "track")
                        buttonIcon = FontAwesomeIcon.Music.ToIconString();

                    ImGui.SameLine(ImGui.GetWindowSize().X / 2 -
                                   (ImGui.GetFontSize() + ImGui.CalcTextSize(buttonIcon).X +
                                    ImGui.CalcTextSize(FontAwesomeIcon.Random.ToIconString()).X));

                    if (ImGui.Button(buttonIcon))
                        currentProvider.SwapRepeatState();

                    if (playerState.RepeatState != "off")
                        ImGui.PopStyleColor();

                    ImGui.SameLine(ImGui.GetWindowSize().X -
                                   (ImGui.GetFontSize() +
                                    ImGui.CalcTextSize(FontAwesomeIcon.Forward.ToIconString()).X));
                    if (ImGui.Button(FontAwesomeIcon.Forward.ToIconString()))
                        currentProvider.SetSkip(true);

                    ImGui.PopFont();
                }

                if (!configuration.PlayerSettings.CompactPlayer)
                {
                    if (configuration.PlayerSettings.ShowTimeElapsed)
                    {
                        ImGuiUtil.Center(actualProgress.ToString("mm\\:ss", CultureInfo.InvariantCulture) + " / " +
                                         songTotal.ToString("mm\\:ss", CultureInfo.InvariantCulture) +
                                         (playerState.IsPlaying ? "" : " - Paused"));
                    }
                    //////////////// Progress Bar ////////////////

                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, configuration.PlayerSettings.AccentColor);
                    ImGui.ProgressBar(percent / 100f, new Vector2(-1, 2f));
                    ImGui.PopStyleColor();


                    Vector2 imageSize = new Vector2(100 * ImGui.GetIO().FontGlobalScale,
                        100 * ImGui.GetIO().FontGlobalScale);

                    //////////////// Text ////////////////

                    InterfaceUtils.TextCentered(track.Name);

                    ImGui.PushStyleColor(ImGuiCol.Text, InterfaceUtils.DarkenColor);

                    ImGui.Spacing();
                    InterfaceUtils.TextCentered(artists.Remove(artists.Length - 2));


                    ImGui.PopStyleColor();
                }

                ImGui.PopStyleColor(3);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.uiBuilder.OpenMainUi -= UiBuilderOnOpenMainUi;
            }

            base.Dispose(disposing);
        }
    }
}