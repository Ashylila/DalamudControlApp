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
                    UpdateFilteredChatMessages(false);
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

        private void ChatMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems?.Count == 1)
            {
                var msg = (ChatMessage)e.NewItems[0];
                if (msg.Type == XivChatType.TellIncoming && !ChatTypes.Contains(msg.Sender))
                    ChatTypes.Add(msg.Sender);

                if (ChatMessages.Count > 300)
                    ChatMessages.RemoveAt(0);

                if (SelectedFilterType == "All" || msg.Type.ToString() == SelectedFilterType)
                {
                    if (FilteredChatMessages.Count > 300)
                        FilteredChatMessages.RemoveAt(0);

                    FilteredChatMessages.Add(msg);
                    ScrollToBottom();
                }
            }
            else
            {
                UpdateFilteredChatMessages(true);
            }
        }


        private void UpdateFilteredChatMessages(bool scroll)
        {
            var filtered = ChatMessages.Where(msg =>
                    SelectedFilterType == "All" || msg.Type.ToString() == SelectedFilterType)
                .TakeLast(300)
                .ToList();

            FilteredChatMessages.ReplaceRange(filtered);

            if (scroll)
                ScrollToBottom();
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
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (ChatMessagesCollectionView.ItemsSource is ICollection<ChatMessage> collection && collection.Count > 0)
                {
                    await Task.Delay(1);
                    ChatMessagesCollectionView.ScrollTo(collection.Last(), position: ScrollToPosition.End, animate: true);
                }
            });
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
            if (e.VerticalOffset == 0)
            {
#if ANDROID
                LoadMoreMessages();
#endif
            }
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
