using System.Net.WebSockets;
using System.Text;
using DalamudControlApp.Data.Models;

namespace DalamudControlApp
{
    public partial class MainPage : ContentPage
    {
        public ClientWebSocket _webSocket;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void ConnectButton_Clicked(object sender, EventArgs e)
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

        private async void SendCommand_Clicked(object sender, EventArgs e)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                await DisplayAlert("Error", "Not connected to WebSocket server!", "OK");
                return;
            }

            string command = CommandEntry.Text?.Trim();
            if (string.IsNullOrEmpty(command)) return;

            byte[] messageBytes = Encoding.UTF8.GetBytes(command);
            await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
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
                    var message = System.Text.Json.JsonSerializer.Deserialize<ChatMessage>(json);
                    if (message == null)
                    {
                        throw new Exception("Failed to deserialize message.");
                    }
                                    MainThread.BeginInvokeOnMainThread(() =>
                {
                    ChatLog.Text += $"[{message.Timestamp}] [{message.Type}] {message.Sender}: {message.Message}" + "\n";
                });
                }catch(Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to deserialize message: {ex.Message}", "OK");
                    continue;
                }

            }
        }
    }
}
