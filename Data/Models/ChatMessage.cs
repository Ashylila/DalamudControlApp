
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
}