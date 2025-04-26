#if ANDROID
using Android.Content;
using DalamudControlApp.Platforms.Android.Services;
#endif

namespace DalamudControlApp;

public static class BackgroundServiceHelper
{
#if ANDROID
    public static void StartWebSocketService()
    {
        var context = Android.App.Application.Context;
        var intent = new Intent(context, typeof(WebSocketForegroundService));
        context.StartForegroundService(intent);
    }

    public static void StopWebSocketService()
    {
        var context = Android.App.Application.Context;
        var intent = new Intent(context, typeof(WebSocketForegroundService));
        context.StopService(intent);
    }
#endif
}