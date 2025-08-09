using Microsoft.Extensions.Logging;

namespace FantasyPlayer.Interface.Window;

using System.Linq;
using Config;
using DalaMock.Host.Mediator;
using Dalamud.Plugin.Services;
using FantasyPlayer.Mediator;
using Dalamud.Bindings.ImGui;
using Manager;
using Provider.Common;
using Serilog;

public class DebugWindow : UpdatingWindow
{
    private readonly PlayerManager playerManager;
    private readonly Configuration configuration;
    private readonly IClientState clientState;

    public DebugWindow(ILogger<DebugWindow> logger, MediatorService mediatorService, PlayerManager playerManager, Configuration configuration, IClientState clientState) : base(logger, mediatorService, "Fantasy Player - Debug")
    {
        this.playerManager = playerManager;
        this.configuration = configuration;
        this.clientState = clientState;
        MediatorService.Subscribe<ConfigurationUpdatedMessage>(this, ConfigurationUpdated );
    }

    private void ConfigurationUpdated(ConfigurationUpdatedMessage obj)
    {
        if (playerManager.CurrentPlayerProvider != null &&
            !playerManager.ProvidersLoading &&
            configuration.PlayerSettings.DebugWindowOpen)
        {
            if (!IsOpen)
            {
                IsOpen = true;
            }
        }
        else
        {
            if (IsOpen)
            {
                IsOpen = false;
            }
        }
    }

    public override void OnClose()
    {
        configuration.PlayerSettings.DebugWindowOpen = false;
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
        foreach (var provider in playerManager.PlayerProviders
                     .Where(provider => provider.PlayerState.ServiceName != null))
        {
            var playerState = provider.PlayerState;
            var providerText = playerState.ServiceName;

            if (playerState.ServiceName == playerManager.CurrentPlayerProvider?.PlayerState.ServiceName)
                providerText += " (Current)";

            if (!ImGui.CollapsingHeader(providerText)) continue;
            ImGui.Text("RequiresLogin: " + playerState.RequiresLogin);
            ImGui.Text("IsLoggedIn: " + playerState.IsLoggedIn);
            ImGui.Text("IsAuthenticating: " + playerState.IsAuthenticating);
            ImGui.Text("RepeatState: " + playerState.RepeatState);
            ImGui.Text("ShuffleState: " + playerState.ShuffleState);
            ImGui.Text("IsPlaying: " + playerState.IsPlaying);
            ImGui.Text("ProgressMs: " + playerState.ProgressMs);

            if (ImGui.CollapsingHeader(providerText + ": CurrentlyPlaying"))
                RenderTrackStructDebug(playerState.CurrentlyPlaying);

            if (playerState.ServiceName == playerManager.CurrentPlayerProvider?.PlayerState.ServiceName) continue;
            if (ImGui.Button($"Set {playerState.ServiceName} as current provider"))
            {
                playerManager.CurrentPlayerProvider = provider;
            }
        }
    }

    private void RenderTrackStructDebug(TrackStruct track)
    {
        ImGui.Text("Id: " + track.Id);
        ImGui.Text("Name: " + track.Name);
        ImGui.Text("DurationMs: " + track.DurationMs);

        if (track.Artists != null)
            ImGui.Text("Artists: " + string.Join(", ", track.Artists));

        if (track.Album.Name != null)
            ImGui.Text("Album.Name: " + track.Album.Name);
    }


    public override void Update()
    {

    }
}