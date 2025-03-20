namespace DalamudControlApp.Data.Enums;

public enum WebSocketActionType
{
    ChatMessageReceived,
    SendChatMessage,
    Command,
    InvalidCommandUsage,
    CommandResponse
}
