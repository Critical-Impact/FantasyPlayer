namespace FantasyPlayer.Mock;

using Autofac;
using DalaMock.Core.Mocks;
using DalaMock.Core.Windows;
using DalaMock.Shared.Interfaces;
using Dalamud.Plugin;

public class MockPlugin : Plugin
{
    public MockPlugin(IDalamudPluginInterface pluginInterface) : base(pluginInterface)
    {
    }

    public override void ConfigureContainer(ContainerBuilder containerBuilder)
    {
        base.ConfigureContainer(containerBuilder);
        containerBuilder.RegisterType<MockWindowSystem>().SingleInstance();
        containerBuilder.RegisterType<MockFont>().As<IFont>();
    }
}