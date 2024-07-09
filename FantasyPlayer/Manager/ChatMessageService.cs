namespace FantasyPlayer.Manager;

using Config;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

public class ChatMessageService
{
    private readonly Configuration configuration;
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IChatGui chatGui;

    public ChatMessageService(Configuration configuration, IDalamudPluginInterface pluginInterface, IChatGui chatGui)
    {
        this.configuration = configuration;
        this.pluginInterface = pluginInterface;
        this.chatGui = chatGui;
    }

    public void DisplayNoProviderMessage()
    {
        var entry = new XivChatEntry()
        {
            Message = "You have no provider selected. Please configure one in Fantasy Player's configuration.",
            Name = SeString.Empty,
            Type = configuration.PlayerSettings.ChatType,
        };
        chatGui.Print(entry);
    }

    public void DisplayMessage(string message)
    {
        if (!configuration.DisplayChatMessages)
            return;

        var entry = new XivChatEntry()
        {
            Message = message,
            Name = SeString.Empty,
            Type = configuration.PlayerSettings.ChatType,
        };
        chatGui.Print(entry);
    }


    public void DisplaySongTitle(string songTitle)
    {
        if (!configuration.DisplayChatMessages)
            return;

        var message = pluginInterface.UiLanguage switch
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
            Type = configuration.PlayerSettings.ChatType,
        };
        chatGui.Print(entry);
    }
}