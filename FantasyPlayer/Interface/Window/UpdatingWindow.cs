namespace FantasyPlayer.Interface.Window;

using DalaMock.Host.Mediator;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Serilog;

public abstract class UpdatingWindow : WindowMediatorSubscriberBase
{
    protected UpdatingWindow(IPluginLog logger, MediatorService mediatorService, string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(logger, mediatorService, name, flags, forceMainWindow)
    {

    }

    public abstract void Update();
}