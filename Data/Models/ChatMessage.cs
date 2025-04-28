
using System;
using DalamudControlApp.Data.Enums;
#nullable disable

namespace DalamudControlApp.Data.Models;
public class ChatMessage
{
    public string Sender { get; set; }
    public string Message { get; set; }
    public XivChatType Type { get; set; }
    public DateTime Timestamp { get; set; }
    
    public Color TypeColor => GetColorForType(Type.ToString());
    public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

    private static Color GetColorForType(string chatType)
    {
        return chatType switch
        {
            "FreeCompany" => Colors.LightBlue,
            "Party" => Colors.Blue,

            "Ls1" => Colors.Lime,
            "Ls2" => Colors.Lime,
            "Ls3" => Colors.Lime,
            "Ls4" => Colors.Lime,
            "Ls5" => Colors.Lime,
            "Ls6" => Colors.Lime,
            "Ls7" => Colors.Lime,
            "Ls8" => Colors.Lime,

            "CrossLinkShell1" => Colors.Purple,
            "CrossLinkShell2" => Colors.Purple,
            "CrossLinkShell3" => Colors.Purple,

            "TellIncoming" => Colors.Magenta,
            "TellOutgoing" => Colors.Magenta,

            "Say" => Colors.LightGray,
            "Shout" => Colors.Orange,

            _ => Colors.LightGray,
        };
    }
}