using Dalamud.Interface.Windowing;

namespace FantasyPlayer.Mock;

using Autofac;
using DalaMock.Core.Mocks;
using DalaMock.Core.Windows;
using DalaMock.Shared.Interfaces;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

public class MockPlugin : Plugin
{
    public MockPlugin(IDalamudPluginInterface pluginInterface) : base(pluginInterface)
    {
    }

    public override void ConfigureContainer(ContainerBuilder containerBuilder)
    {
        base.ConfigureContainer(containerBuilder);
        containerBuilder.RegisterType<MockWindowSystem>().As<IWindowSystem>();
        containerBuilder.RegisterType<MockFont>().As<IFont>();
    }
}