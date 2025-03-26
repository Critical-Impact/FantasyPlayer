using System.IO;

namespace FantasyPlayer.Mock
{
    using DalaMock.Core.DI;
    using DalaMock.Core.Mocks;

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