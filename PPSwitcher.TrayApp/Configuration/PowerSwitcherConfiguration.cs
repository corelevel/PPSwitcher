using Petrroll.Helpers;
using PPSwitcher.TrayApp.Services;
using System;
using System.Windows.Input;
using PPSwitcher.Wrappers;

namespace PPSwitcher.TrayApp.Configuration
{
	[Serializable]
	public class PPSwitcherSettings : ObservableObject
	{
		bool showOnShortcutSwitch = false;
		bool showOnlyDefaultSchemas = false;

		//I know that everything should be observable but it's not neccessary so let's leave it as it is for now
		public bool AutomaticFlyoutHideAfterClick { get; set; } = true;
		public bool AutomaticOnACSwitch { get; set; } = false;
		public Guid AutomaticPlanGuidOnAC { get; set; } = DefaultPowSchemasWrapper.GetBalancedSchema().Guid;
		public Guid AutomaticPlanGuidOffAC { get; set; } = DefaultPowSchemasWrapper.GetPowerSaverSchema().Guid;

		//TODO: Fix so that it can be changed during runtime
		public Key ShowOnShortcutKey { get; set; } = Key.L;
		public KeyModifier ShowOnShortcutKeyModifier { get; set; } = KeyModifier.Shift | KeyModifier.Win;
		public bool ShowOnShortcutSwitch { get { return showOnShortcutSwitch; } set { showOnShortcutSwitch = value; RaisePropertyChangedEvent(nameof(ShowOnShortcutSwitch)); } }
		public bool ShowOnlyDefaultSchemas { get { return showOnlyDefaultSchemas; } set { showOnlyDefaultSchemas = value; RaisePropertyChangedEvent(nameof(ShowOnlyDefaultSchemas)); } }

	}
}
