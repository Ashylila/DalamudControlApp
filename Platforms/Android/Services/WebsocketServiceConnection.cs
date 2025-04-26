using Android.Content;
using Android.OS;
using DalamudControlApp.Platforms.Android.Services;

namespace DalamudControlApp.Platforms.Android
{
    public class WebSocketServiceConnection : Java.Lang.Object, IServiceConnection
    {
        public WebSocketForegroundService? BoundService { get; private set; }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            if (service is WebSocketForegroundServiceBinder binder)
            {
                BoundService = binder.GetService();
            }
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            BoundService = null;
        }
    }
}