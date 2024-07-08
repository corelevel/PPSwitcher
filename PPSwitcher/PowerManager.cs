using Petrroll.Helpers;
using PPSwitcher.Wrappers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace PPSwitcher
{
	public interface IPowerManager : INotifyPropertyChanged, IDisposable
	{
		ObservableCollection<IPowerScheme> Schemas { get; }
		IPowerScheme CurrentSchema { get; }
		PowerLineStatus CurrentPowerStatus { get; }

		void UpdateSchemas();
		void SetPowerScheme(IPowerScheme schema);
		void SetPowerScheme(Guid guid);
	}

	[SupportedOSPlatform("windows")]
	public sealed class PowerManager : ObservableObject, IPowerManager
	{
		readonly BatteryInfoWrapper batteryWrapper = null!;
		private bool isDisposed = false;

		public ObservableCollection<IPowerScheme> Schemas { get; private set; } = [];
		public IPowerScheme CurrentSchema { get; private set; } = new PowerSchemaUnknown();
		public PowerLineStatus CurrentPowerStatus { get; private set; }

		public PowerManager()
		{
			batteryWrapper = new BatteryInfoWrapper(PowerChangedEvent);
			PowerChangedEvent(BatteryInfoWrapper.GetCurrentPowerStatus());
			InitializeSchemas();
		}
		private void InitializeSchemas()
		{
			var existingSchemas = Win32PowSchemasWrapper.GetExistingSchemas();
			bool activeFound = false;

			// Add existing schemas
			foreach (var scheme in existingSchemas)
			{
				Schemas.Add(scheme);

				if (scheme.IsActive)
				{
					activeFound = true;
					CurrentSchema = scheme;
					RaisePropertyChangedEvent(nameof(CurrentSchema));
				}
			}

			if (!activeFound)
			{
				CurrentSchema = new PowerSchemaUnknown();
			}
		}
		public void UpdateSchemas()
		{
			var activeSchemeUid = Win32PowSchemasWrapper.GetActiveScheme();
			bool activeFound = false;

			foreach (var scheme in Schemas)
			{
				if (scheme.Guid == activeSchemeUid && scheme.IsActive == false)
				{
					activeFound = true;
					scheme.IsActive = true;
					CurrentSchema = scheme;
					RaisePropertyChangedEvent(nameof(CurrentSchema));
				}
				else if (scheme.Guid == activeSchemeUid && scheme.IsActive)
				{
					activeFound = true;
				}
				else if (scheme.IsActive)
				{
					scheme.IsActive = false;
				}
			}

			if (!activeFound)
			{
				CurrentSchema = new PowerSchemaUnknown();
			}
		}
		public void SetPowerScheme(IPowerScheme schema)
		{
			SetPowerScheme(schema.Guid);
		}
		public void SetPowerScheme(Guid guid)
		{
			Win32PowSchemasWrapper.SetActiveScheme(guid);
		}
		private void PowerChangedEvent(PowerLineStatus newStatus)
		{
			if(newStatus == CurrentPowerStatus) { return; }

			CurrentPowerStatus = newStatus;
			RaisePropertyChangedEvent(nameof(CurrentPowerStatus));
		}
		void Dispose(bool disposing)
		{
			if (disposing && !isDisposed)
			{
				batteryWrapper.Dispose();
				isDisposed = true;
			}
		}
		public void Dispose()
		{
			Dispose(true);
		}
	}

}



