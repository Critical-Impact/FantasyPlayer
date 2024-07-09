using FantasyPlayer.Interface.Window;
using FantasyPlayer.Interfaces;

namespace FantasyPlayer.Interface
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using DalaMock.Host.Factories;
    using DalaMock.Shared;
    using DalaMock.Shared.Interfaces;
    using Dalamud.Interface;
    using Dalamud.Interface.Windowing;
    using Dalamud.Plugin;
    using Dalamud.Plugin.Services;
    using Microsoft.Extensions.Hosting;

    public class InterfaceController : IHostedService, IDisposable
    {
        private readonly IUiBuilder uiBuilder;
        private readonly PlayerWindow playerWindow;
        private readonly SettingsWindow settingsWindow;
        private readonly IFramework framework;
        private IWindowSystem windowSystem;
        
        public InterfaceController(IUiBuilder uiBuilder, IEnumerable<Dalamud.Interface.Windowing.Window> windows, IFramework framework, WindowSystemFactory windowSystemFactory)
        {
            this.uiBuilder = uiBuilder;
            this.framework = framework;
            this.windowSystem = windowSystemFactory.Create("FantasyPlayer");
            foreach (var window in windows)
            {
                this.windowSystem.AddWindow(window);
            }
            this.framework.Update += FrameworkOnUpdate;
        }

        private void FrameworkOnUpdate(IFramework framework1)
        {
            foreach (var window in windowSystem.Windows)
            {
                if (window is UpdatingWindow updatingWindow)
                {
                    updatingWindow.Update();
                }
            }
        }

        public void Draw()
        {
            this.windowSystem.Draw();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.uiBuilder.Draw += Draw;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.uiBuilder.Draw -= Draw;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            this.framework.Update -= FrameworkOnUpdate;
        }
    }
}