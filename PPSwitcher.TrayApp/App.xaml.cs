using Petrroll.Helpers;
using PPSwitcher.TrayApp.Configuration;
using PPSwitcher.TrayApp.Services;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows;

namespace PPSwitcher.TrayApp
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>

	[SupportedOSPlatform("windows")]
	public partial class App : Application
	{
		public HotKeyService HotKeyManager { get; private set; } = null!;
		public bool HotKeyFailed { get; private set; } = false;

		public IPowerManager PowerManager { get; private set; } = null!;
		public TrayApp TrayApp { get; private set; } = null!;
		public ConfigurationInstance<PPSwitcherSettings> Configuration { get; private set; } = null!;

		private Mutex? mutex = null;

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			// To prevent multiple app instances
			if (!TryToCreateMutex())
			{
				Current.Shutdown();
				return;
			}

			var configurationManager = new ConfigurationManagerXML<PPSwitcherSettings>(Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
				"Petrroll", "PPSwitcher", "PPSwitcherSettings.xml"
				));

			Configuration = new ConfigurationInstance<PPSwitcherSettings>(configurationManager);
			MigrateSettings();

			HotKeyManager = new HotKeyService();
			PowerManager = new PowerManager();
			MainWindow = new MainWindow();
			TrayApp = new TrayApp(PowerManager, Configuration); //Has to be last because it hooks to MainWindow

			Configuration.Data.PropertyChanged += Configuration_PropertyChanged;
			if (Configuration.Data.ShowOnShortcutSwitch) { RegisterHotkeyFromConfiguration(); }

			TrayApp.CreateAltMenu();
		}
		private void MigrateSettings()
		{
			//Migration of shortcut because Creators update uses WinShift + S for screenshots
			if(Configuration.Data.ShowOnShortcutKey == System.Windows.Input.Key.S &&
				Configuration.Data.ShowOnShortcutKeyModifier == (KeyModifier.Shift | KeyModifier.Win))
			{
				Configuration.Data.ShowOnShortcutKey = System.Windows.Input.Key.L;
				Configuration.Save();
			}
		}
		private void Configuration_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if(e.PropertyName == nameof(PPSwitcherSettings.ShowOnShortcutSwitch))
			{
				if (Configuration.Data.ShowOnShortcutSwitch) { RegisterHotkeyFromConfiguration(); }
				else { UnregisterHotkeyFromConfiguration(); }
			}
		}
		private void UnregisterHotkeyFromConfiguration()
		{
			HotKeyManager.Unregister(new HotKey(Configuration.Data.ShowOnShortcutKey, Configuration.Data.ShowOnShortcutKeyModifier));
		}
		private bool RegisterHotkeyFromConfiguration()
		{
			var newHotKey = new HotKey(Configuration.Data.ShowOnShortcutKey, Configuration.Data.ShowOnShortcutKeyModifier);

			bool success = HotKeyManager.Register(newHotKey);
			if(!success) { HotKeyFailed = true; return false; }

			if (MainWindow is MainWindow w)
				newHotKey.HotKeyFired += w.ToggleWindowVisibility;

			return true;
		}
		private static string GetMutexName()
		{
			var assembly = Assembly.GetExecutingAssembly();
			return string.Format(CultureInfo.InvariantCulture, "Local\\{{{0}}}{{{1}}}", assembly.GetType().GUID, assembly.GetName().Name);
		}
		private bool TryToCreateMutex()
		{
			mutex = new Mutex(true, GetMutexName(), out bool created);
			return created;
		}
		private void ReleaseMutex()
		{
			mutex?.ReleaseMutex();
			mutex?.Close();
		}
		private void App_OnExit(object sender, ExitEventArgs e)
		{
			ReleaseMutex();
			// ???
			PowerManager?.Dispose();
			HotKeyManager?.Dispose();
		}
		/*
		~App()
		{
			DisposeMutex();
		}
		*/
	}
}
