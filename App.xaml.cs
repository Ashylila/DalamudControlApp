using System.Net.WebSockets;
using System;

namespace DalamudControlApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override void OnStart()
	{
		base.OnStart();
		
#if ANDROID
		BackgroundServiceHelper.StopWebSocketService();
		
#endif
	}
	
	protected override void OnSleep()
	{
		base.OnSleep();
#if ANDROID
		BackgroundServiceHelper.StartWebSocketService();
#endif
	}
	
	protected override void OnResume()
	{
		base.OnResume();
#if ANDROID
		BackgroundServiceHelper.StopWebSocketService();
#endif

	}
	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

}