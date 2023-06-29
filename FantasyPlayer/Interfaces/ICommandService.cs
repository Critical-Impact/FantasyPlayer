using Dalamud.Game.Command;

namespace FantasyPlayer.Interfaces;

public interface ICommandService
{
    public bool AddHandler(string command, CommandInfo info);
    public bool RemoveHandler(string command);

    public bool ProcessCommand(string content);
}