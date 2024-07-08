using System;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace PPSwitcher.Wrappers
{
	[SupportedOSPlatform("windows")]
	sealed class BatteryInfoWrapper : IDisposable
	{
		private readonly Microsoft.Win32.PowerModeChangedEventHandler powerChangedDelegate = null!;
		private bool isDisposed = false;

		public BatteryInfoWrapper(Action<PowerLineStatus> powerStatusChangedFunc)
		{
			powerChangedDelegate = (sender, e) => { powerStatusChangedFunc(GetCurrentPowerStatus()); };
			Microsoft.Win32.SystemEvents.PowerModeChanged += powerChangedDelegate;
		}

		public static PowerLineStatus GetCurrentPowerStatus()
		{
			return SystemInformation.PowerStatus.PowerLineStatus;
		}

		void Dispose(bool disposing)
		{
			if (disposing && !isDisposed)
			{
				if (powerChangedDelegate != null)
				{
					Microsoft.Win32.SystemEvents.PowerModeChanged -= powerChangedDelegate;
				}
				isDisposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
	}
}
