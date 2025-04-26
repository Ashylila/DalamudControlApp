using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using DalamudControlApp.Data.Models;
using DalamudControlApp.Util;
#if ANDROID
using DalamudControlApp.Platforms.Android;
using DalamudControlApp.Platforms.Android.Services;
using Android.Content;
using Android.App;
#endif

namespace DalamudControlApp
{
    public partial class ChatPage : ContentPage
    {
        public ObservableCollection<ChatMessage> ChatMessages { get; } = new();

        public ChatPage()
        {
            InitializeComponent();
            ChatMessages = ChatService.ChatMessages;
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
#if ANDROID
            await LoadCachedChatMessagesAsync();
#endif
        }

#if ANDROID
        private async Task LoadCachedChatMessagesAsync()
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(WebSocketForegroundService));
            var serviceConnection = new WebSocketServiceConnection();

            context.BindService(intent, serviceConnection, Bind.AutoCreate);

            await Task.Delay(500); // Give time for the service to connect

            if (serviceConnection.BoundService != null)
            {
                var cachedMessages = serviceConnection.BoundService.GetCachedMessages();

                ChatService.ChatMessages.Clear();
                foreach (var chatMessage in cachedMessages)
                {
                    ChatService.ChatMessages.Add(chatMessage);
                }
            }

            context.UnbindService(serviceConnection);
        }
#endif
    }
}