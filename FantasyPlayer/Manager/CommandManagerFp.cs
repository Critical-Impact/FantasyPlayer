using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin;
using FantasyPlayer.Interface;
using FantasyPlayer.Interfaces;

namespace FantasyPlayer.Manager
{
    using System.Threading;
    using System.Threading.Tasks;
    using Dalamud.Game.Command;
    using Dalamud.Plugin.Services;
    using Microsoft.Extensions.Hosting;

    public enum OptionType
    {
        None,
        Boolean,
        Int
    }

    public enum CallbackResponse //TODO: better name needed...?
    {
        None,
        SetValue,
        ToggleValue
    }

    public class CommandManagerFp : IHostedService
    {
        private readonly ICommandManager commandManager;
        private readonly IChatGui chatGui;
        public readonly string Command = "/pfp";

        public Dictionary<string, (OptionType type, string[] aliases, string helpString,
            Action<bool, int, CallbackResponse> commandCallback)> Commands { get; } =
            new Dictionary<string, (OptionType type, string[] aliases, string helpString,
                Action<bool, int, CallbackResponse>
                commandCallback)>();

        public CommandManagerFp(ICommandManager commandManager, IChatGui chatGui)
        {
            this.commandManager = commandManager;
            this.chatGui = chatGui;
            Commands.Add("help", (OptionType.None, new string[] { "" }, "Show command help.", PrintHelp));
        }

        public void ParseCommand(string argsString)
        {
            var args = argsString.ToLower().Split(' ');

            if (args.Length == 0)
                PrintHelp(false, 0, CallbackResponse.None);

            if (args.Length < 1)
                return;

            foreach (var command in Commands)
            {
                if (command.Key != args[0] && !command.Value.aliases.Contains(args[0]))
                    continue;

                var cmd = command.Value;

                if (cmd.type == OptionType.None)
                {
                    cmd.commandCallback.Invoke(false, 0, CallbackResponse.None);
                    return;
                }

                if (cmd.type == OptionType.Boolean)
                {
                    if (args.Length < 2)
                        cmd.commandCallback.Invoke(false, 0, CallbackResponse.ToggleValue);

                    if (args[1] == "toggle")
                        cmd.commandCallback.Invoke(false, 0, CallbackResponse.ToggleValue);

                    if (args[1] == "off")
                        cmd.commandCallback.Invoke(false, 0, CallbackResponse.SetValue);

                    if (args[1] == "on")
                        cmd.commandCallback.Invoke(true, 0, CallbackResponse.SetValue);

                    return;
                }

                if (cmd.type == OptionType.Int)
                {
                    if (args.Length < 2)
                    {
                        chatGui.PrintError($"You need to provide a number for the '{command.Key}' command!");
                        return;
                    }

                    if (!int.TryParse(args[1], out var value)) return;
                    cmd.commandCallback.Invoke(false, value, CallbackResponse.SetValue);
                    return;
                }
            }
            
            chatGui.PrintError("That command wasn't found. For a list of commands please type: '/pfp help'");
        }

        public void PrintHelp(bool boolValue, int intValue, CallbackResponse response)
        {
            var helpMessage = "";
            helpMessage += "Fantasy Player Command Help:\n";

            foreach (var command in Commands)
            {
                var commandName = command.Key;
                if (command.Value.aliases != null)
                    commandName =
                        command.Value.aliases.Aggregate(commandName, (current, alias) => current + ("/" + alias));

                if (command.Value.helpString != null) //? If helpString is null this means it should not show up!
                    helpMessage += $"{commandName} - {command.Value.helpString} (ex: '{command.Key}{GetCommandExample(command.Value.type)}')\n";
            }
            
            helpMessage = helpMessage.Remove(helpMessage.Length - 1);
            chatGui.Print(helpMessage);
        }

        public string GetCommandExample(OptionType optionType)
        {
            switch (optionType)
            {
                case OptionType.Boolean:
                    return $" on/off/toggle";
                case OptionType.Int:
                    return $" 50";
                case OptionType.None:
                    return "";
                default:
                    return "";
            }
        }

        private void OnCommand(string command, string arguments)
        {
            ParseCommand(arguments);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            commandManager.AddHandler(Command, new CommandInfo(OnCommand)
            {
                HelpMessage = "Run commands for Fantasy Player"
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            commandManager.RemoveHandler(Command);
            return Task.CompletedTask;
        }
    }
}