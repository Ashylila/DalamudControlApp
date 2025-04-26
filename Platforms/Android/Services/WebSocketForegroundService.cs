using Android.App;
using Android.Content;
using Android.OS;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DalamudControlApp.Data.Models;
using DalamudControlApp.Data.Enums;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;

namespace DalamudControlApp.Platforms.Android.Services;

[Service(Exported = true)]
public class WebSocketForegroundService : Service
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource _cts = new();
    private readonly List<ChatMessage> _cachedMessages = new();
    private const string CacheFileName = "chat_messages_cache.json";

    public static WebSocketForegroundService? Instance { get; private set; }

    public override void OnCreate()
    {
        base.OnCreate();
        Instance = this; // <---- Add this
        LoadCachedMessages();
        StartForegroundServiceWithNotification();
        _ = StartWebSocketLoopAsync();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _cts.Cancel();
        _socket?.Dispose();
        Instance = null; // <---- Clean up
    }

    private void StartForegroundServiceWithNotification()
    {
        const string channelId = "dalamudcontrolapp_ws_channel";

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(channelId, "DalamudControl WebSocket", NotificationImportance.Default)
            {
                Description = "Listening for in-game messages"
            };
            var manager = (NotificationManager)GetSystemService(NotificationService)!;
            manager.CreateNotificationChannel(channel);
        }

        var notification = new Notification.Builder(this, channelId)
            .SetContentTitle("DalamudControlApp")
            .SetContentText("Listening for in-game messages...")
            .SetSmallIcon(Resource.Drawable.notification_icon_background)
            .SetOngoing(true)
            .Build();

        StartForeground(1, notification);
    }

    private async Task StartWebSocketLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            _socket?.Dispose();
            _socket = new ClientWebSocket();
            try
            {
                await _socket.ConnectAsync(new Uri("ws://65.38.98.16:5000/ws"), _cts.Token);

                var buffer = new byte[1024];
                while (_socket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cts.Token);
                        break;
                    }
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var command = JsonSerializer.Deserialize<WebSocketMessage<object>>(message);
                    HandleIncomingMessage(command);
                }
            }
            catch (Exception)
            {
                // You can log the exception if needed
                await Task.Delay(5000); // wait before trying to reconnect
            }
        }
    }

    private void HandleIncomingMessage(WebSocketMessage<object> command)
    {
        switch (command.Type)
        {
            case WebSocketActionType.ChatMessage:
                ProcessChatMessage(command);
                break;
            case WebSocketActionType.SendChatMessage:
                // Do something with the chat message
                break;
            case WebSocketActionType.InvalidCommandUsage:
                break;
            case WebSocketActionType.CommandResponse:
                break;
            default:
                break;
        }
    }

    private void ProcessChatMessage(WebSocketMessage<object> command)
    {
        if (command.Data is JsonElement data)
        {
            var chatMessage = JsonSerializer.Deserialize<ChatMessage>(data.GetRawText());
            if (chatMessage == null)
                return;

            lock (_cachedMessages)
            {
                _cachedMessages.Add(chatMessage);
                SaveCachedMessages(); // Save to file immediately after adding
            }
            
            if (!(chatMessage.Type == XivChatType.TellIncoming)) return;
            var notification = new Notification.Builder(this, "dalamudcontrolapp_ws_channel")
                .SetContentTitle("New In-Game Message")
                .SetSmallIcon(Resource.Drawable.notification_icon_background)
                .SetContentText($"[{chatMessage.Type}] {chatMessage.Sender}: {chatMessage.Message}")
                .Build();

            var notificationManager = (NotificationManager)GetSystemService(NotificationService)!;
            notificationManager.Notify(2, notification);
        }
    }

    // 🧠 Expose a method to fetch all cached messages (for when the app opens)
    public List<ChatMessage> GetCachedMessages()
    {
        lock (_cachedMessages)
        {
            return new List<ChatMessage>(_cachedMessages);
        }
    }

    public override IBinder OnBind(Intent intent)
    {
        return new WebSocketForegroundServiceBinder(this);
    }
    

    // ✨ NEW: Save and Load cached messages to file
    private void SaveCachedMessages()
    {
        try
        {
            string path = Path.Combine(ApplicationContext.FilesDir!.AbsolutePath, CacheFileName);
            var json = JsonSerializer.Serialize(_cachedMessages);
            File.WriteAllText(path, json);
        }
        catch
        {
            // handle write errors silently for now
        }
    }

    private void LoadCachedMessages()
    {
        try
        {
            string path = Path.Combine(ApplicationContext.FilesDir!.AbsolutePath, CacheFileName);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var messages = JsonSerializer.Deserialize<List<ChatMessage>>(json);
                if (messages != null)
                {
                    _cachedMessages.Clear();
                    _cachedMessages.AddRange(messages);
                }
            }
        }
        catch
        {
            // handle read errors silently for now
        }
    }
}
public class WebSocketForegroundServiceBinder : Binder
{
    private readonly WebSocketForegroundService _service;

    public WebSocketForegroundServiceBinder(WebSocketForegroundService service)
    {
        _service = service;
    }

    public WebSocketForegroundService GetService() => _service;
}


