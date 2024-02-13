using System;
using DalaMock.Shared;
using DalaMock.Shared.Classes;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FantasyPlayer.Config;
using FantasyPlayer.Interface;
using FantasyPlayer.Interfaces;
using FantasyPlayer.Manager;

namespace FantasyPlayer
{
    public class Plugin : IDalamudPlugin, IPlugin
    {
        public string Name => "FantasyPlayer";
        public const string Command = "/pfp";

        public InterfaceController InterfaceController { get; set; }
        public DalamudPluginInterface PluginInterface { get; private set; }
        public Configuration Configuration { get; set; }
        public PlayerManager PlayerManager { get; set; }
        public IClientState ClientState { get; set; }
        public IConfigurationManager ConfigurationManager { get; set; }
        public CommandManagerFp CommandManager { get; set; }

        public Plugin(DalamudPluginInterface pluginInterface)
        {
            PluginInterface = pluginInterface;
            PluginInterface.Create<Service>();
            Service.Interface = new PluginInterfaceService(pluginInterface);
            ClientState = Service.ClientState;

            ConfigurationManager = new ConfigurationManager(PluginInterface);
            ConfigurationManager.Load();
            Configuration = ConfigurationManager.Config;

            Service.CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
            {
                HelpMessage = "Run commands for Fantasy Player"
            });

            //Setup player
            PlayerManager = new PlayerManager(this);
            
            CommandManager = new CommandManagerFp(this);

            InterfaceController = new InterfaceController(this);

            PluginInterface.UiBuilder.Draw += InterfaceController.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
        }

        private void OnCommand(string command, string arguments)
        {
            CommandManager.ParseCommand(arguments);
        }

        public void DisplayMessage(string message)
        {
            if (!Configuration.DisplayChatMessages)
                return;

            var entry = new XivChatEntry()
            {
                Message = message,
                Name = SeString.Empty,
                Type = Configuration.PlayerSettings.ChatType,
            };
            Service.ChatGui.Print(entry);
        }

        public void DisplaySongTitle(string songTitle)
        {
            if (!Configuration.DisplayChatMessages)
                return;

            var message = PluginInterface.UiLanguage switch
            {
                "ja" => new SeString(new Payload[]
                    {
                        new TextPayload($"「{songTitle}」を再生しました。"), // 「Weight of the World／Prelude Version」を再生しました。
                    }),
                "de" => new SeString(new Payload[]
                    {
                        new TextPayload($"„{songTitle}“ wird nun wiedergegeben."), // „Weight of the World (Prelude Version)“ wird nun wiedergegeben.
                    }),
                "fr" => new SeString(new Payload[]
                    {
                        new TextPayload($"Le FantasyPlayer lit désormais “{songTitle}”."), // L'orchestrion joue désormais “Weight of the World (Prelude Version)”.
                    }),
                _ => new SeString(new Payload[]
                    {
                        new EmphasisItalicPayload(true),
                        new TextPayload(songTitle), // _Weight of the World (Prelude Version)_ is now playing.
                        new EmphasisItalicPayload(false),
                        new TextPayload(" is now playing."),
                    }),
            };

            var entry = new XivChatEntry()
            {
                Message = message,
                Name = SeString.Empty,
                Type = Configuration.PlayerSettings.ChatType,
            };
            Service.ChatGui.Print(entry);
        }

        public void OpenConfig()
        {
            Configuration.ConfigShown = true;
        }

        public void Dispose()
        {
            ConfigurationManager.Save();
            Service.CommandManager.RemoveHandler(Command);
            PluginInterface.UiBuilder.Draw -= InterfaceController.Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;

            InterfaceController.Dispose();
            PlayerManager.Dispose();
        }
    }
}