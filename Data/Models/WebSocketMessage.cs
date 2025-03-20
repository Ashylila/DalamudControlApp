using DalamudControlApp.Data.Enums;

namespace DalamudControlApp.Data.Models;
#nullable disable
public class WebSocketMessage<T>
{
    public WebSocketActionType Type { get; set; }
    public T Data { get; set; }

}