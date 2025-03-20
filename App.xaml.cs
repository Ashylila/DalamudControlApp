using System.Net.WebSockets;
namespace DalamudControlApp;

public partial class App : Application
{
	private readonly ClientWebSocket _server;
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

}