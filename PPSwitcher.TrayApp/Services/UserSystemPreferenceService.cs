using Microsoft.Win32;
using System.Runtime.Versioning;

namespace PPSwitcher.TrayApp.Services
{
	////
	//  Code heavily inspired by https://github.com/File-New-Project/EarTrumpet/blob/master/EarTrumpet/Services/UserSystemPreferencesService.cs
	////

	[SupportedOSPlatform("windows")]
	public static class UserSystemPreferencesService
	{
		private static object? GetRegistryKeyValue(string key)
		{
			using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
			var subKey = baseKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
			return subKey?.GetValue(key, 0);
		}
		public static bool IsTransparencyEnabled
		{
			get
			{
				var value = GetRegistryKeyValue("EnableTransparency");
				return value != null && (int)value > 0;
			}
		}

		public static bool UseAccentColor
		{
			get
			{
				var value = GetRegistryKeyValue("ColorPrevalence");
				return value != null && (int)value > 0;
			}
		}
	}
}
