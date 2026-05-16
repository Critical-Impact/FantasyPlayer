using System.IO;
using DalaMock.Core.Configuration;
using DalaMock.Core.Plugin;

namespace FantasyPlayer.Mock
{
    class Program
    {
        static void Main(string[] args)
        {
            var dalamudConfiguration = new MockDalamudConfiguration();
            var mockContainer = new MockContainer(dalamudConfiguration);
            var mockDalamudUi = mockContainer.GetMockUi();
            var pluginLoader = mockContainer.GetPluginLoader();
            pluginLoader.AddPlugin(typeof(MockPlugin));
            mockDalamudUi.Run();
        }
    }
}