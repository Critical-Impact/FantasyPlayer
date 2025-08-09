using Microsoft.Extensions.Logging;

namespace FantasyPlayer.Interface.Window;

using DalaMock.Host.Mediator;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;

public abstract class UpdatingWindow : WindowMediatorSubscriberBase
{
    protected UpdatingWindow(ILogger<UpdatingWindow> logger, MediatorService mediatorService, string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(logger, mediatorService, name, flags, forceMainWindow)
    {

    }

    public abstract void Update();
}