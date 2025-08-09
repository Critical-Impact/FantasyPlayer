using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FantasyPlayer.Config;
using FantasyPlayer.Interface;
using FantasyPlayer.Interfaces;
using FantasyPlayer.Manager;

namespace FantasyPlayer
{
    using Autofac;
    using DalaMock.Host;
    using DalaMock.Host.Hosting;
    using DalaMock.Host.Mediator;
    using DalaMock.Shared.Classes;
    using DalaMock.Shared.Interfaces;
    using Dalamud.Interface.Windowing;
    using Interface.Window;
    using Microsoft.Extensions.DependencyInjection;
    using Provider;
    using Provider.Common;

    public class Plugin : HostedPlugin
    {
        public Plugin(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog, IFramework framework, IClientState clientState, IChatGui chatGui, ICommandManager commandManager, ICondition condition) : base(pluginInterface, pluginLog, framework, clientState, chatGui, commandManager, condition)
        {
            CreateHost();
            Start();
        }

        public override HostedPluginOptions ConfigureOptions()
        {
            return new HostedPluginOptions()
            {
                UseMediatorService = true
            };
        }

        public override void ConfigureContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<ConfigurationManager>().SingleInstance();
            containerBuilder.Register(provider =>
            {
                var configurationManager = provider.Resolve<ConfigurationManager>();
                configurationManager.Load();
                var configuration = configurationManager.Config;
                return configuration;
            }).As<Configuration>().SingleInstance();
            containerBuilder.RegisterType<SpotifyProvider>().SingleInstance();
            containerBuilder.RegisterType<CommandManagerFp>().SingleInstance();
            containerBuilder.RegisterType<PlayerManager>().SingleInstance();
            containerBuilder.RegisterType<ChatMessageService>().SingleInstance();
            containerBuilder.RegisterType<InterfaceController>().SingleInstance();
            containerBuilder.RegisterType<SettingsWindow>().As<Window>().SingleInstance();
            containerBuilder.RegisterType<PlayerWindow>().As<Window>().SingleInstance();
            containerBuilder.RegisterType<DebugWindow>().As<Window>().SingleInstance();
            containerBuilder.RegisterType<CommandsService>().SingleInstance();
            containerBuilder.RegisterType<Font>().As<IFont>().SingleInstance();
            containerBuilder.RegisterType<SpotifyProvider>().As<IPlayerProvider>().SingleInstance();
        }

        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddHostedService(p => p.GetRequiredService<ConfigurationManager>());
            serviceCollection.AddHostedService(p => p.GetRequiredService<InterfaceController>());
            serviceCollection.AddHostedService(p => p.GetRequiredService<CommandManagerFp>());
            serviceCollection.AddHostedService(p => p.GetRequiredService<PlayerManager>());
            serviceCollection.AddHostedService(p => p.GetRequiredService<CommandsService>());
            serviceCollection.AddHostedService(p => p.GetRequiredService<MediatorService>());
        }
    }
}