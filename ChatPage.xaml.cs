using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using DalamudControlApp.Data.Enums;
using DalamudControlApp.Data.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using DalamudControlApp.Util;

#if ANDROID
using DalamudControlApp.Platforms.Android;
using DalamudControlApp.Platforms.Android.Services;
#endif

namespace DalamudControlApp
{
    public partial class ChatPage : ContentPage
    {
        public static ChatPage? Instance { get; private set; }
        public ObservableRangeCollection<ChatMessage> ChatMessages { get; private set; } = new();
        public ObservableRangeCollection<ChatMessage> FilteredChatMessages { get; private set; } = new();
        private HashSet<string> DefaultChatTypes { get; } = new()
        {
            "Say", "Party", "Shout", "FreeCompany", "Ls1", "Ls2", "Ls3", "Ls4", "Ls5", "Ls6", "Ls7", "Ls8",
            "CrossLinkShell1", "CrossLinkShell2", "CrossLinkShell3"
        };

        public ObservableRangeCollection<string> ChatTypes { get; } = new()
        {
            "Say", "Party", "Shout", "FreeCompany", "Ls1", "Ls2", "Ls3", "Ls4", "Ls5", "Ls6", "Ls7", "Ls8",
            "CrossLinkShell1", "CrossLinkShell2", "CrossLinkShell3"
        };

        public ObservableRangeCollection<string> FilterChatTypes { get; } = new()
        {
            "All", "Say", "Party", "Shout", "FreeCompany", "Ls1", "Ls2", "Ls3", "Ls4", "Ls5", "Ls6", "Ls7", "Ls8",
            "CrossLinkShell1", "CrossLinkShell2", "CrossLinkShell3", "TellIncoming", "TellOutgoing"
        };

        private string selectedChatType;
        public string SelectedChatType
        {
            get => selectedChatType;
            set
            {
                if (selectedChatType != value)
                {
                    selectedChatType = value;
                    OnPropertyChanged();
                }
            }
        }

        private string selectedFilterType = "All";
        public string SelectedFilterType
        {
            get => selectedFilterType;
            set
            {
                if (selectedFilterType != value)
                {
                    selectedFilterType = value;
                    OnPropertyChanged();
                    UpdateFilteredChatMessages();
                }
            }
        }

        private string outgoingMessage;
        public string OutgoingMessage
        {
            get => outgoingMessage;
            set
            {
                if (outgoingMessage != value)
                {
                    outgoingMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoadingMessages { get; set; } = false;
        private bool _isScrolling = false;
        private bool _isScrollingTimerRunning = false;

        public ChatPage()
        {
            InitializeComponent();
            BindingContext = this;

            ChatMessages = ChatService.ChatMessages;
            SelectedChatType = ChatTypes.First();
            SelectedFilterType = "All";

            ChatMessages.CollectionChanged += ChatMessages_CollectionChanged;
            Instance = this;
        }
        

private const int MaxMessages = 300; 

private bool isUpdating = false;  

private DateTime lastMessageTime = DateTime.MinValue;

private async void ChatMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
{
    if (isUpdating) return;

    isUpdating = true;

    try
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count == 1)
        {
            var msg = (ChatMessage)e.NewItems[0];

            
            if (DateTime.Now - lastMessageTime < TimeSpan.FromMilliseconds(50))
            {
                return;
            }

            lastMessageTime = DateTime.Now;

            
            if (!FilteredChatMessages.Contains(msg))
            {
                if (msg.Type == XivChatType.TellIncoming && !ChatTypes.Contains(msg.Sender))
                    ChatTypes.Add(msg.Sender);

                if (SelectedFilterType == "All" || msg.Type.ToString() == SelectedFilterType)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        FilteredChatMessages.Add(msg);
                        // Limit the number of messages to MaxMessages
                        if (FilteredChatMessages.Count > MaxMessages)
                        {
                            FilteredChatMessages.RemoveAt(0);
                        }
                    });
                    if(!_isScrolling)
                        ScrollToBottom();
                }
            }
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(UpdateFilteredChatMessages);
        }
    }
    finally
    {
        isUpdating = false;
    }
}

private async void UpdateFilteredChatMessages()
{
    
    await Task.Run(() =>
    {
        var filtered = new List<ChatMessage>();

        foreach (var msg in ChatMessages)
        {
            if (msg.Type == XivChatType.TellIncoming && !ChatTypes.Contains(msg.Sender))
                ChatTypes.Add(msg.Sender);

            if (SelectedFilterType == "All" || msg.Type.ToString() == SelectedFilterType)
            {
                filtered.Add(msg);
            }
        }
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            FilteredChatMessages.ReplaceRange(filtered);

            
            if (FilteredChatMessages.Count > MaxMessages)
            {
                FilteredChatMessages.RemoveRange(0, FilteredChatMessages.Count - MaxMessages);
            }
            if(!_isScrolling)
                ScrollToBottom();
        });
    });
}



        private async void OnSendButtonClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(MessageInput.Text))
            {
                var type = DefaultChatTypes.Contains(SelectedChatType) ? Enum.Parse<XivChatType>(SelectedChatType) : XivChatType.TellOutgoing;
                var newMessage = new ChatMessage
                {
                    Timestamp = DateTime.Now,
                    Type = type,
                    Sender = DefaultChatTypes.Contains(SelectedChatType) ? "You" : SelectedChatType,
                    Message = MessageInput.Text
                };

                MessageInput.Text = string.Empty;
                if (MainPage.Instance != null)
                {
                    var command = CommandHelper.CreateCommand(newMessage, WebSocketActionType.SendChatMessage);
                    await MainPage.Instance._webSocket.SendAsync(new ArraySegment<byte>(command), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
#if ANDROID
            LoadCachedChatMessagesAsync();
#endif
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
#if ANDROID
            _ = SaveChatMessagesAsync(ChatMessages.ToList());
#endif
        }

        private void ScrollToBottom()
        {
            if (!_isScrolling)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (ChatMessagesCollectionView.ItemsSource is ICollection<ChatMessage> collection && collection.Count > 0)
                    {
                        await Task.Delay(10);
                        ChatMessagesCollectionView.ScrollTo(collection.Last(), position: ScrollToPosition.End, animate: false);
                    }
                });
            }
        }




        private async void LoadMoreMessages()
        {
            if (IsLoadingMessages) return;

            IsLoadingMessages = true;

            try
            {
                var oldMessages = await GetOlderMessagesFromJsonAsync();
                if (oldMessages != null && oldMessages.Count > 0)
                {
                    oldMessages.Reverse();
                    ChatMessages.InsertRange(0, oldMessages);
                }
            }
            finally
            {
                IsLoadingMessages = false;
            }
        }

        private async Task<List<ChatMessage>> GetOlderMessagesFromJsonAsync()
        {
#if ANDROID
            try
            {
                string filePath = Path.Combine(Android.App.Application.Context.FilesDir!.AbsolutePath, "chat_messages_cache.json");

                if (File.Exists(filePath))
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    var allMessages = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new List<ChatMessage>();

                    DateTime lastLoadedMessageTimestamp = ChatMessages.Count > 0 ? ChatMessages[0].Timestamp : DateTime.MinValue;

                    var olderMessages = allMessages
                        .Where(msg => msg.Timestamp < lastLoadedMessageTimestamp)
                        .OrderByDescending(msg => msg.Timestamp)
                        .Take(20)
                        .ToList();

                    return olderMessages;
                }
                return new List<ChatMessage>();
            }
            catch
            {
                return new List<ChatMessage>();
            }
#else
            return new List<ChatMessage>();
#endif
        }

        private void ChatMessagesCollectionView_Scrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            _isScrolling = true;
            
            if (e.VerticalOffset == 0 && !IsLoadingMessages)
            {
#if ANDROID
                LoadMoreMessages();
#endif
            }

            if (_isScrollingTimerRunning)
                return;
            
            StartScrollStopTimer();
        }
        private async void StartScrollStopTimer()
        {
            _isScrollingTimerRunning = true;
            
            await Task.Delay(200);
            
            if (_isScrolling)
            {
                _isScrolling = false;

            }

            _isScrollingTimerRunning = false;
        }

#if ANDROID
        private void LoadCachedChatMessagesAsync()
        {
            try
            {
                string path = Path.Combine(Android.App.Application.Context.FilesDir!.AbsolutePath, "chat_messages_cache.json");

                HashSet<ChatMessage> existingMessages = [];
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    existingMessages = JsonSerializer.Deserialize<List<ChatMessage>>(json)?.ToHashSet() ?? [];
                }

                existingMessages = existingMessages
                    .Where(m => m.Timestamp.Date == DateTime.Today)
                    .ToHashSet();

                ChatService.ChatMessages.ReplaceRange(existingMessages);
            }
            catch { }

            ScrollToBottom();
        }

        private async Task SaveChatMessagesAsync(List<ChatMessage> messages)
        {
            try
            {
                await Task.Run(() =>
                {
                    string path = Path.Combine(Android.App.Application.Context.FilesDir!.AbsolutePath, "chat_messages_cache.json");

                    List<ChatMessage> existingMessages = [];
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        existingMessages = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? [];
                    }

                    existingMessages = existingMessages
                        .Where(m => m.Timestamp.Date == DateTime.Today)
                        .ToList();

                    existingMessages.AddRange(messages);

                    var jsonString = JsonSerializer.Serialize(existingMessages);
                    File.WriteAllText(path, jsonString);
                });
            }
            catch { }
        }
#endif
    }
}
