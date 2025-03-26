using System.IO;
using FantasyPlayer.Config;
using FantasyPlayer.Interfaces;
using Newtonsoft.Json;

namespace FantasyPlayer.Mock;

public class MockConfigurationManager : IConfigurationManager
{
    public Configuration Config { get; set; }
    public string ConfigurationFile { get; set; }
    public void Load()
    {
        FileInfo configFile = new FileInfo(ConfigurationFile);
        if (!configFile.Exists)
        {
            Config = new Configuration();
            return;
        }

        var data = File.ReadAllText(configFile.FullName);
        Config = JsonConvert.DeserializeObject<Configuration>(data, new JsonSerializerSettings()
        {
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.Objects
        }) ?? new Configuration();
    }

    public void Save()
    {
        File.WriteAllText(ConfigurationFile, JsonConvert.SerializeObject(Config, Formatting.None, new JsonSerializerSettings()
        {
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.Objects
        }));
    }
}