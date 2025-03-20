using DalamudControlApp.Data.Enums;
using DalamudControlApp.Data.Models;
using System.Text;

using System.Text.Json;
namespace DalamudControlApp.Util;
public static class CommandHelper
{
    public static byte[] createCommand (string command, WebSocketActionType type)
    {
        var websocketcommand = new WebSocketMessage
        {
            Type = type,
            Data = command
        };
        byte[] messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize<WebSocketMessage>(websocketcommand));
        return messageBytes;
    }

}