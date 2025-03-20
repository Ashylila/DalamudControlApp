using System.Collections.ObjectModel;
using System.Collections.Generic;
using DalamudControlApp.Data.Models;

public static class ChatService
{
    public static ObservableCollection<string> ChatMessages { get; } = new();
    public static List<ChatMessage> _chatMessages = new();  
}