using System.Collections.ObjectModel;
using System.Collections.Generic;
using DalamudControlApp.Data.Models;
using DalamudControlApp.Util;

namespace DalamudControlApp.Util;
public static class ChatService
{
    public static ObservableRangeCollection<ChatMessage> ChatMessages { get; } = new();
}