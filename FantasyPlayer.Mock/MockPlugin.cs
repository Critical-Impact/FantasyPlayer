namespace FantasyPlayer.Mock;

using Autofac;
using DalaMock.Core.Mocks;
using DalaMock.Core.Windows;
using DalaMock.Shared.Interfaces;
using Dalamud.Plugin;

public class MockPlugin : Plugin
{
    public MockPlugin(MockReplacementContainer replacementContainer, IDalamudPluginInterface pluginInterface) : base(pluginInterface)
    {
        ReplacementContainer = replacementContainer;
    }

    public override IReplacementContainer ReplacementContainer { get; }
}