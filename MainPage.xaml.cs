using System.Net.WebSockets;
using System.Text;
using DalamudControlApp.Data.Models;
using DalamudControlApp.Data.Enums;
using DalamudControlApp.Util;
using System.Collections.ObjectModel;

namespace DalamudControlApp
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<string> Log {get;} = new();
        public ClientWebSocket _webSocket;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            Log.Add("Welcome to Dalamud Control App!");
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await DisplayAlert("Info", "Already connected.", "OK");
                return;
            }

            _webSocket = new ClientWebSocket();
            try
            {
                await _webSocket.ConnectAsync(new Uri("ws://localhost:5000"), CancellationToken.None);
                await DisplayAlert("Success", "Connected to WebSocket server!", "OK");
                _ = ReceiveMessages();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to connect: {ex.Message}", "OK");
            }
        }

        private async void OnSendCommandClicked(object sender, EventArgs e)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                await DisplayAlert("Error", "Not connected to WebSocket server!", "OK");
                return;
            }

            string command = CommandEntry.Text?.Trim();
            if (string.IsNullOrEmpty(command)) return;
            
            await _webSocket.SendAsync(new ArraySegment<byte>(CommandHelper.createCommand(command, WebSocketActionType.Command)), WebSocketMessageType.Text, true, CancellationToken.None);
            CommandEntry.Text = "";
        }
        
        private async Task ReceiveMessages()
        {
            var buffer = new byte[1024];

            while (_webSocket?.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

                try
                {
                    var command = System.Text.Json.JsonSerializer.Deserialize<WebSocketMessage>(json);
                    if (command == null)
                    {
                        await DisplayAlert("Error", "Failed to deserialize message.", "OK");
                        return;
                    }
                    ProcessCommand(command);
                                   
                }catch(Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to deserialize message: {ex.Message}", "OK");
                    continue;
                }

            }
        }
        private void ProcessCommand(WebSocketMessage command)
        {
            switch (command.Type)
            {
                case WebSocketActionType.ChatMessageReceived:
                    // Do something with the chat message
                    break;
                case WebSocketActionType.SendChatMessage:
                    // Do something with the chat message
                    break;
                case WebSocketActionType.InvalidCommandUsage:
                    Log.Add(command.Data);
                    break;
                case WebSocketActionType.CommandResponse:
                    Log.Add(command.Data);
                    break;
                default:
                    break;
            }
        }
    }
}
