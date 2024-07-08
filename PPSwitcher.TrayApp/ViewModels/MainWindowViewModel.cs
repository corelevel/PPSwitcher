using Petrroll.Helpers;
using PPSwitcher.TrayApp.Configuration;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.Versioning;

namespace PPSwitcher.TrayApp.ViewModels
{
	[SupportedOSPlatform("windows")]
	public class MainWindowViewModel : ObservableObject
	{
		private readonly IPowerManager pwrManager = null!;
		private readonly ConfigurationInstance<PPSwitcherSettings> config = null!;

		public INotifyCollectionChanged Schemas { get; private set; } = null!;
		public IPowerScheme ActiveSchema
		{
			get { return pwrManager.CurrentSchema; }
			set { if (value != null && !value.IsActive) { pwrManager.SetPowerScheme(value); } }
		}

		public MainWindowViewModel()
		{
			if (System.Windows.Application.Current is not App currApp) { return; }

			pwrManager = currApp.PowerManager;
			config = currApp.Configuration;

			pwrManager.PropertyChanged += PwrManager_PropertyChanged;
			config.Data.PropertyChanged += SettingsData_PropertyChanged;

			Schemas = pwrManager.Schemas.WhereObservableSwitchable<ObservableCollection<IPowerScheme>, IPowerScheme>
				(
				sch => Wrappers.DefaultPowSchemasWrapper.GetDefaultSchemas().Exists(item => item.Guid == sch.Guid) || sch.IsActive,
				config.Data.ShowOnlyDefaultSchemas
				);
		}

		private void SettingsData_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(PPSwitcherSettings.ShowOnlyDefaultSchemas))
			{
				UpdateOnlyDefaultSchemasSetting();
			}
		}

		private void UpdateOnlyDefaultSchemasSetting()
		{
			(Schemas as ObservableCollectionWhereSwitchableShim<ObservableCollection<IPowerScheme>, IPowerScheme>).FilterOn = config.Data.ShowOnlyDefaultSchemas;
		}

		private void PwrManager_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IPowerManager.CurrentSchema))
			{
				RaisePropertyChangedEvent(nameof(ActiveSchema));
			}
		}

		public void SetGuidAsActive(Guid guid)
		{
			pwrManager.SetPowerScheme(guid);
		}

		public void Refresh()
		{
			pwrManager.UpdateSchemas();
		}
	}
}
