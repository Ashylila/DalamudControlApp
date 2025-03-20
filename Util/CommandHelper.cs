using DalamudControlApp.Data.Enums;
using DalamudControlApp.Data.Models;
using System.Text;

using System.Text.Json;
namespace DalamudControlApp.Util;
public static class CommandHelper
{
    public static byte[] CreateCommand<T>(T data, WebSocketActionType type)
{
    var websocketCommand = new WebSocketMessage<T>
    {
        Type = type,
        Data = data
    };

    string json = JsonSerializer.Serialize(websocketCommand);
    return Encoding.UTF8.GetBytes(json);
}

}