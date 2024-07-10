namespace FantasyPlayer.Manager;

using System;
using System.Threading;
using System.Threading.Tasks;
using Config;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Hosting;

public class CommandsService : IHostedService
{
    private readonly CommandManagerFp commandManager;
    private readonly Configuration configuration;
    private readonly PlayerManager playerManager;
    private readonly ChatMessageService chatMessageService;

    public CommandsService(CommandManagerFp commandManager, Configuration configuration, PlayerManager playerManager, ChatMessageService chatMessageService)
    {
        this.commandManager = commandManager;
        this.configuration = configuration;
        this.playerManager = playerManager;
        this.chatMessageService = chatMessageService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        commandManager.Commands.Add("config",
            (OptionType.Boolean, new string[] {"settings"}, "Toggles config display.", OnConfigCommand));

        commandManager.Commands.Add("shuffle",
            (OptionType.Boolean, new string[] { }, "Toggle shuffle.", OnShuffleCommand));
        commandManager.Commands.Add("next",
            (OptionType.None, new string[] {"skip"}, "Skip to the next track.", OnNextCommand));
        commandManager.Commands.Add("back",
            (OptionType.None, new string[] {"previous"}, "Go back a track.", OnBackCommand));
        commandManager.Commands.Add("pause",
            (OptionType.None, new string[] {"stop"}, "Pause playback.", OnPauseCommand));
        commandManager.Commands.Add("play",
            (OptionType.None, new string[] { }, "Continue playback.", OnPlayCommand));
        commandManager.Commands.Add("volume",
            (OptionType.Int, new string[] { }, "Set playback volume.", OnVolumeCommand));
        commandManager.Commands.Add("relogin",
            (OptionType.None, new string[] {"reauth"}, "Re-opens the login window and lets you login again",
                OnReLoginCommand));

        commandManager.Commands.Add("display",
            (OptionType.Boolean, new string[] { }, "Toggle player display.", OnDisplayCommand));

        return Task.CompletedTask;
    }

	private void OnReLoginCommand(bool boolValue, int intValue, CallbackResponse response)
    {
        var playerProvider = playerManager.CurrentPlayerProvider;
        if (playerProvider != null)
        {
            var playerState = playerProvider.PlayerState;
            playerState.IsLoggedIn = false;
            playerProvider.PlayerState = playerState;
            playerProvider.ReAuth();
        }
        else
        {
            chatMessageService.DisplayNoProviderMessage();
        }
    }

    private void OnDisplayCommand(bool boolValue, int intValue, CallbackResponse response)
    {
        configuration.PlayerSettings.PlayerWindowShown = response switch
        {
            CallbackResponse.SetValue => boolValue,
            CallbackResponse.ToggleValue => !configuration.PlayerSettings.PlayerWindowShown,
            _ => configuration.PlayerSettings.PlayerWindowShown
        };
    }

    private void OnVolumeCommand(bool boolValue, int intValue, CallbackResponse response)
    {
        var playerProvider = playerManager.CurrentPlayerProvider;
        if (playerProvider != null)
        {
            chatMessageService.DisplayMessage($"Set volume to: {intValue}");
            playerProvider.SetVolume(intValue);
        }
        else
        {
            chatMessageService.DisplayNoProviderMessage();
        }


    }

    private void OnShuffleCommand(bool boolValue, int intValue, CallbackResponse response)
    {
        var playerProvider = playerManager.CurrentPlayerProvider;
        if (playerProvider != null)
        {
            switch (response)
            {
                case CallbackResponse.SetValue:
                {
                    if (boolValue)
                        chatMessageService.DisplayMessage("Turned on shuffle.");

                    if (!boolValue)
                        chatMessageService.DisplayMessage("Turned off shuffle.");

                    playerProvider.SetShuffle(boolValue);
                    break;
                }
                case CallbackResponse.ToggleValue:
                {
                    if (!playerProvider.PlayerState.ShuffleState)
                        chatMessageService.DisplayMessage("Turned on shuffle.");

                    if (playerProvider.PlayerState.ShuffleState)
                        chatMessageService.DisplayMessage("Turned off shuffle.");

                    playerProvider.SetShuffle(!playerProvider.PlayerState
                        .ShuffleState);
                    break;
                }
                case CallbackResponse.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(response), response, null);
            }
        }
    }

    private void OnNextCommand(bool boolValue, int intValue, CallbackResponse response)
    {
        var playerProvider = playerManager.CurrentPlayerProvider;
        if (playerProvider == null) return;
        chatMessageService.DisplayMessage("Skipping to next track.");
        playerProvider.SetSkip(true);
    }

    private void OnBackCommand(bool boolValue, int intValue, CallbackResponse response)
    {
        var playerProvider = playerManager.CurrentPlayerProvider;
        if (playerProvider == null) return;
        chatMessageService.DisplayMessage("Going back a track.");
        playerProvider.SetSkip(false);
    }

    private void OnPlayCommand(bool boolValue, int intValue, CallbackResponse response)
    {
        var playerProvider = playerManager.CurrentPlayerProvider;
        if (playerProvider == null) return;

        if (playerProvider.PlayerState.CurrentlyPlaying.Id != null)
        {
            var displayInfo = playerProvider.PlayerState.CurrentlyPlaying.Name;
            chatMessageService.DisplaySongTitle(displayInfo);
        }

        playerProvider.SetPauseOrPlay(true);
    }

    private void OnPauseCommand(bool boolValue, int intValue, CallbackResponse response)
    {
        var playerProvider = playerManager.CurrentPlayerProvider;
        if (playerProvider == null) return;

        chatMessageService.DisplayMessage("Paused playback.");
        playerProvider.SetPauseOrPlay(false);
    }

    public void OnConfigCommand(bool boolValue, int intValue, CallbackResponse response)
    {
        if (response == CallbackResponse.SetValue)
            configuration.ConfigShown = boolValue;

        if (response == CallbackResponse.ToggleValue)
            configuration.ConfigShown = !configuration.ConfigShown;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}