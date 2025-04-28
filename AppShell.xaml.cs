namespace DalamudControlApp;

public partial class AppShell : Shell
{

	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

		this.Navigated += OnNavigated;
	}
private async void OnSettingsClicked(object sender, EventArgs e)
	{
		// Navigate to the settings page
		await Shell.Current.GoToAsync(nameof(SettingsPage));
	}
	private void OnNavigated(object? sender, ShellNavigatedEventArgs e)
	{
		var location = e.Current?.Location.ToString() ?? "";

		if (location.Contains(nameof(SettingsPage)))
		{
			SettingsToolbarItem.IsEnabled = false;
		}
		else
		{
			SettingsToolbarItem.IsEnabled = true;
		}

	}
}
