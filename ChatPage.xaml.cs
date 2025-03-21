
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using DalamudControlApp.Data.Models;
using DalamudControlApp.Util;

namespace DalamudControlApp
{
    public partial class ChatPage : ContentPage
    {
        public ObservableCollection<string> ChatMessages { get; } = new();

        public ChatPage()
        {
            InitializeComponent();
            ChatMessages = ChatService.ChatMessages;
            BindingContext = this;
        }

    }
}
