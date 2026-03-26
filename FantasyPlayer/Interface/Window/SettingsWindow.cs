using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;
using AllaganLib.Shared.Extensions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using FantasyPlayer.Interfaces;
using FantasyPlayer.Manager;
using Microsoft.Extensions.Logging;
using OtterGui.Widgets;

namespace FantasyPlayer.Interface.Window
{
    using Config;
    using DalaMock.Host.Mediator;
    using Dalamud.Interface.Windowing;
    using Dalamud.Plugin.Services;
    using FantasyPlayer.Mediator;
    using Serilog;

    public class SettingsWindow : UpdatingWindow
    {
        private readonly Configuration _configuration;
        private readonly ConfigurationManager _configurationManager;
        private readonly CommandManagerFp commandManagerFp;
        private readonly IUiBuilder _uiBuilder;

        public SettingsWindow(ILogger<SettingsWindow> logger, MediatorService mediatorService, Configuration configuration, ConfigurationManager configurationManager, CommandManagerFp commandManagerFp, IUiBuilder uiBuilder) : base(logger, mediatorService, "Fantasy Player - Configuration", ImGuiWindowFlags.NoScrollbar)
        {
            _configuration = configuration;
            _configurationManager = configurationManager;
            this.commandManagerFp = commandManagerFp;
            _uiBuilder = uiBuilder;
            MediatorService.Subscribe<ConfigurationUpdatedMessage>(this, ConfigurationUpdated );
            this.Size = new Vector2(800, 800);
            this.SizeCondition = ImGuiCond.FirstUseEver;
            this.SizeConstraints = new WindowSizeConstraints()
            {
                MinimumSize = new Vector2(600, 400)
            };
        }

        public override void OnClose()
        {
            _configuration.ConfigShown = false;
            base.OnClose();
        }

        private void ConfigurationUpdated(ConfigurationUpdatedMessage obj)
        {
            if (_configuration.ConfigShown && !IsOpen)
            {
                IsOpen = true;
            }
            if (!_configuration.ConfigShown && IsOpen)
            {
                IsOpen = false;
            }
        }

        public override bool DrawConditions()
        {
            if (!_configuration.ConfigShown)
            {
                return false;
            }

            return base.DrawConditions();
        }

        public override void Draw()
        {
            MainWindow();
        }

        public override void Update()
        {

        }

        public void HelpMarker(string helpText, FontAwesomeIcon icon, Vector4? color = null)
        {
            using var col = new ImRaii.Color();

            if (color.HasValue)
            {
                col.Push(ImGuiCol.TextDisabled, color.Value);
            }

            ImGui.SameLine();

            using (ImRaii.PushFont(_uiBuilder.FontIcon))
            {
                ImGui.TextDisabled(icon.ToIconString());
            }

            if (ImGui.IsItemHovered())
            {
                using (ImRaii.Tooltip())
                {
                    using (ImRaii.TextWrapPos(ImGui.GetFontSize() * 35.0f))
                    {
                        ImGui.Text(helpText);
                    }
                }
            }
        }

        private string? spotifyClientId = null;

        private void MainWindow()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextWrapped("Due to recent changes in how spotify provides access to it's API you will now need to provide your own client ID to continue to use Fantasy Player. Please see the Spotify Settings section for more details.");
            ImGui.PopStyleColor();
            ImGui.NewLine();
            ImGui.PushStyleColor(ImGuiCol.Text, InterfaceUtils.DarkenColor);
            ImGui.Text($"Type '{commandManagerFp.Command} help' to display chat commands!");
            ImGui.PopStyleColor();

            if (ImGui.CollapsingHeader("Fantasy Player"))
            {
                var displayChatMessages = _configuration.DisplayChatMessages;
                if (ImGui.Checkbox("Display chat messages", ref displayChatMessages))
                {
                    _configuration.DisplayChatMessages = displayChatMessages;
                }
                if (Widget.DrawChatTypeSelector("Chat message output channel",
                        "To which chat channel should the fantasy player messages be echo'd?",
                        _configuration.PlayerSettings.ChatType,
                        type => { _configuration.PlayerSettings.ChatType = type; }))
                {
                }
            }

            if (ImGui.CollapsingHeader("Spotify Settings"))
            {
                if (this.spotifyClientId == null)
                {
                    this.spotifyClientId = _configuration.SpotifySettings.SpotifyClientId;
                }
                if (ImGui.InputText("Spotify Client ID",
                        ref this.spotifyClientId))
                {

                }

                ImGui.SameLine();

                if (ImGui.Button("Save") && !string.IsNullOrEmpty(this.spotifyClientId))
                {
                    _configuration.SpotifySettings.SpotifyClientId = this.spotifyClientId;
                }

                if (ImGui.Button("Open Instructions"))
                {
                    "https://github.com/Critical-Impact/FantasyPlayer/blob/main/SETUP.md".OpenBrowser();
                }
            }

            if (!_configuration.SpotifySettings.LimitedAccess)
            {
                if (ImGui.CollapsingHeader("Auto-play Settings"))
                {
                    var playInDuty = _configuration.AutoPlaySettings.PlayInDuty;
                    if (ImGui.Checkbox("Auto play when entering Duty",
                            ref playInDuty))
                    {
                        _configuration.AutoPlaySettings.PlayInDuty = playInDuty;
                    }
                }
            }

            if (ImGui.CollapsingHeader("Player Settings"))
            {
                if (_configuration.SpotifySettings.LimitedAccess)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, InterfaceUtils.DarkenColor);
                    ImGui.Text("You're not premium on Spotify. Some settings have been hidden.");
                    ImGui.PopStyleColor();
                }

                ImGui.Separator();

                var onlyWhenLoggedIn = _configuration.PlayerSettings.OnlyOpenWhenLoggedIn;
                if (ImGui.Checkbox("Only open when logged in",
                        ref onlyWhenLoggedIn))
                {
                    _configuration.PlayerSettings.OnlyOpenWhenLoggedIn = onlyWhenLoggedIn;
                }

                ImGui.Separator();

                if (!_configuration.SpotifySettings.LimitedAccess)
                {
                    var compactPlayer = _configuration.PlayerSettings.CompactPlayer;
                    if (ImGui.Checkbox("Compact mode", ref compactPlayer))
                    {
                        if (_configuration.PlayerSettings.NoButtons)
                            _configuration.PlayerSettings.NoButtons = false;
                        _configuration.PlayerSettings.CompactPlayer = compactPlayer;
                    }

                    var noButtons = _configuration.PlayerSettings.NoButtons;
                    if (ImGui.Checkbox("Hide buttons", ref noButtons))
                    {
                        if (_configuration.PlayerSettings.CompactPlayer)
                            _configuration.PlayerSettings.CompactPlayer = false;
                        _configuration.PlayerSettings.NoButtons = noButtons;
                    }
                }

                ImGui.Separator();

                var playerWindowShown = _configuration.PlayerSettings.PlayerWindowShown;
                if (ImGui.Checkbox("Player shown", ref playerWindowShown))
                {
                    _configuration.PlayerSettings.PlayerWindowShown = playerWindowShown;
                }

                var playerLocked = _configuration.PlayerSettings.PlayerLocked;
                if (ImGui.Checkbox("Player locked", ref playerLocked))
                {
                    _configuration.PlayerSettings.PlayerLocked = playerLocked;
                }

                var disableInput = _configuration.PlayerSettings.DisableInput;
                if (ImGui.Checkbox("Player input disabled", ref disableInput))
                {
                    _configuration.PlayerSettings.DisableInput = disableInput;
                }

                var timeElapsed = _configuration.PlayerSettings.ShowTimeElapsed;
                if (ImGui.Checkbox("Show time elapsed",
                        ref timeElapsed))
                {
                    _configuration.PlayerSettings.ShowTimeElapsed = timeElapsed;
                }

                ImGui.Separator();

                var transparency = _configuration.PlayerSettings.Transparency;
                if (ImGui.SliderFloat("Player alpha", ref transparency, 0f,
                        1f))
                {
                    _configuration.PlayerSettings.Transparency = transparency;
                }

                var accentColor = _configuration.PlayerSettings.AccentColor;
                if (ImGui.ColorEdit4("Player color", ref accentColor))
                {
                    _configuration.PlayerSettings.AccentColor = accentColor;
                }

                ImGui.NewLine();
                if (ImGui.Button("Revert"))
                {
                    _configuration.PlayerSettings.AccentColor = InterfaceUtils.FantasyPlayerColor;
                    _configuration.PlayerSettings.FirstRunNone = true;
                }
                ImGui.SameLine();
                ImGui.Text("Revert to default size");

                ImGui.Separator();
                if (ImGui.Button("Compact mode"))
                {
                    _configuration.PlayerSettings.FirstRunCompactPlayer = true;
                }

                ImGui.SameLine();
                if (ImGui.Button("Hide buttons"))
                {
                    _configuration.PlayerSettings.FirstRunSetNoButtons = true;
                }

                ImGui.SameLine();
                if (ImGui.Button("Full"))
                {
                    _configuration.PlayerSettings.FirstRunNone = true;
                }



                ImGui.Separator();

                var debugWindowOpen = _configuration.PlayerSettings.DebugWindowOpen;
                if (ImGui.Checkbox("Show debug window", ref debugWindowOpen))
                {
                    _configuration.PlayerSettings.DebugWindowOpen = debugWindowOpen;
                }
            }

            if (ImGui.CollapsingHeader("Lyrics Settings"))
            {
                var enableLyrics = _configuration.LyricsSettings.EnableLyrics;
                if (ImGui.Checkbox("Enable Lyrics", ref enableLyrics))
                {
                    _configuration.LyricsSettings.EnableLyrics = enableLyrics;
                }

                var displayModeIndex = (int)_configuration.LyricsSettings.DisplayMode;
                string[] displayModes = { "Chat", "Flytext" };
                if (ImGui.Combo("Lyric Display Mode", ref displayModeIndex, displayModes, displayModes.Length))
                {
                    _configuration.LyricsSettings.DisplayMode = (Config.LyricDisplayMode)displayModeIndex;
                }

                if (_configuration.LyricsSettings.DisplayMode == Config.LyricDisplayMode.Chat)
                {
                    if (Widget.DrawChatTypeSelector("Lyrics chat output channel",
                            "To which chat channel should the lyrics be printed?",
                            _configuration.LyricsSettings.ChatType,
                            type => { _configuration.LyricsSettings.ChatType = type; }))
                    {
                    }
                }
                else
                {
                    var lyricColor = AbgrToVector4(_configuration.LyricsSettings.FlyTextColor);
                    if (ImGui.ColorEdit4("Flytext Color", ref lyricColor))
                    {
                        _configuration.LyricsSettings.FlyTextColor = Vector4ToAbgr(lyricColor);
                    }
                }
            }

            ImGui.Separator();
        }

        private static Vector4 AbgrToVector4(uint abgr)
        {
            var r = (abgr & 0xFF) / 255f;
            var g = ((abgr >> 8) & 0xFF) / 255f;
            var b = ((abgr >> 16) & 0xFF) / 255f;
            var a = ((abgr >> 24) & 0xFF) / 255f;
            return new Vector4(r, g, b, a);
        }

        private static uint Vector4ToAbgr(Vector4 color)
        {
            var r = (uint)Math.Clamp(color.X * 255f, 0f, 255f);
            var g = (uint)Math.Clamp(color.Y * 255f, 0f, 255f);
            var b = (uint)Math.Clamp(color.Z * 255f, 0f, 255f);
            var a = (uint)Math.Clamp(color.W * 255f, 0f, 255f);
            return (a << 24) | (b << 16) | (g << 8) | r;
        }
    }
}