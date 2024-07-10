using Petrroll.Helpers;
using PPSwitcher.TrayApp.Configuration;
using PPSwitcher.TrayApp.Resources;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace PPSwitcher.TrayApp
{
	[SupportedOSPlatform("windows")]
	public class TrayApp
	{
		#region PrivateObjects
		readonly NotifyIcon trayIcon;
		public event Action? ShowFlyout;
		readonly IPowerManager pwrManager;
		readonly ConfigurationInstance<PPSwitcherSettings> configuration;
		#endregion

		#region Contructor
		public TrayApp(IPowerManager powerManager, ConfigurationInstance<PPSwitcherSettings> config)
		{
			pwrManager = powerManager;
			pwrManager.PropertyChanged += PwrManager_PropertyChanged;

			configuration = config;

			trayIcon = new NotifyIcon();
			trayIcon.MouseClick += TrayIcon_MouseClick;

			trayIcon.Icon = new System.Drawing.Icon(System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/PPSwitcher.TrayApp;component/Tray.ico")).Stream, SystemInformation.SmallIconSize);
			trayIcon.Text = string.Concat(AppStrings.AppName);
			trayIcon.Visible = true;

			if (System.Windows.Application.Current is App a)
			{
				if (a.MainWindow is MainWindow w)
					ShowFlyout += w.ToggleWindowVisibility;
				else
					throw new Exception();
			}
			else
				throw new Exception();
			//ShowFlyout += (((App)System.Windows.Application.Current).MainWindow as MainWindow).ToggleWindowVisibility;

			//Run automatic on-off-AC change at boot
			PowerStatusChanged();
		}

		public void CreateAltMenu()
		{
			var contextMenuRoot = new ContextMenuStrip();
			contextMenuRoot.Opened += ContextMenu_Popup;

			trayIcon.ContextMenuStrip = contextMenuRoot;

			var contextMenuRootItems = contextMenuRoot.Items;
			contextMenuRootItems.Add("-");

			var contextMenuSettings = contextMenuRootItems.Add(AppStrings.Settings);
			contextMenuSettings.Name = "settings";

			var settingsOnACItem = contextMenuRootItems.Add(AppStrings.SchemaToSwitchOnAc);
			settingsOnACItem.Name = "settingsOnAC";

			var settingsOffACItem = contextMenuRootItems.Add(AppStrings.SchemaToSwitchOffAc);
			settingsOffACItem.Name = "settingsOffAC";

			var automaticSwitchItem = contextMenuRootItems.Add(AppStrings.AutomaticOnOffACSwitch);
			if (configuration.Data.AutomaticOnACSwitch) automaticSwitchItem.Select();
			automaticSwitchItem.Click += AutomaticSwitchItem_Click;

			var automaticHideItem = contextMenuRootItems.Add(AppStrings.HideFlyoutAfterSchemaChangeSwitch);
			if (configuration.Data.AutomaticFlyoutHideAfterClick) automaticHideItem.Select();
			automaticHideItem.Click += AutomaticHideItem_Click;

			var onlyDefaultSchemasItem = contextMenuRootItems.Add(AppStrings.ShowOnlyDefaultSchemas);
			if (configuration.Data.ShowOnlyDefaultSchemas) onlyDefaultSchemasItem.Select();
			onlyDefaultSchemasItem.Click += OnlyDefaultSchemas_Click;

			var enableShortcutsToggleItem = contextMenuRootItems.Add($"{AppStrings.ToggleOnShowrtcutSwitch} ({configuration.Data.ShowOnShortcutKeyModifier} + {configuration.Data.ShowOnShortcutKey})");
			if (System.Windows.Application.Current is App a)
				enableShortcutsToggleItem.Enabled = !a.HotKeyFailed;
			if (configuration.Data.ShowOnShortcutSwitch) enableShortcutsToggleItem.Select();
			enableShortcutsToggleItem.Click += EnableShortcutsToggleItem_Click;

			var asm = Assembly.GetEntryAssembly();
			if (asm != null)
			{
				var aboutItem = contextMenuRootItems.Add($"{AppStrings.About} ({asm.GetName().Version})");
				aboutItem.Click += About_Click;
			}

			var exitItem = contextMenuRootItems.Add(AppStrings.Exit);
			exitItem.Click += Exit_Click;
		}

		#endregion

		#region FlyoutRelated
		void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ShowFlyout?.Invoke();
			}
		}

		#endregion

		#region SettingsTogglesRegion
		private void EnableShortcutsToggleItem_Click(object? sender, EventArgs e)
		{
			if (sender != null)
			{
				ToolStripMenuItem enableShortcutsToggleItem = (ToolStripMenuItem)sender;

				configuration.Data.ShowOnShortcutSwitch = !configuration.Data.ShowOnShortcutSwitch;
				enableShortcutsToggleItem.Checked = configuration.Data.ShowOnShortcutSwitch;
				if (System.Windows.Application.Current is App a)
					enableShortcutsToggleItem.Enabled = !a.HotKeyFailed;

//				enableShortcutsToggleItem.Enabled = !(System.Windows.Application.Current as App).HotKeyFailed;

				configuration.Save();
			}
		}

		private void AutomaticHideItem_Click(object? sender, EventArgs e)
		{
			if (sender != null)
			{
				ToolStripMenuItem automaticHideItem = (ToolStripMenuItem)sender;

				configuration.Data.AutomaticFlyoutHideAfterClick = !configuration.Data.AutomaticFlyoutHideAfterClick;
				automaticHideItem.Checked = configuration.Data.AutomaticFlyoutHideAfterClick;

				configuration.Save();
			}
		}
		private void OnlyDefaultSchemas_Click(object? sender, EventArgs e)
		{
			if (sender != null)
			{
				ToolStripMenuItem onlyDefaultSchemasItem = (ToolStripMenuItem)sender;

				configuration.Data.ShowOnlyDefaultSchemas = !configuration.Data.ShowOnlyDefaultSchemas;
				onlyDefaultSchemasItem.Checked = configuration.Data.ShowOnlyDefaultSchemas;

				configuration.Save();
			}
		}
		private void AutomaticSwitchItem_Click(object? sender, EventArgs e)
		{
			if (sender != null)
			{
				ToolStripMenuItem automaticSwitchItem = (ToolStripMenuItem)sender;

				configuration.Data.AutomaticOnACSwitch = !configuration.Data.AutomaticOnACSwitch;
				automaticSwitchItem.Checked = configuration.Data.AutomaticOnACSwitch;

				if (configuration.Data.AutomaticOnACSwitch) { PowerStatusChanged(); }

				configuration.Save();
			}
		}
		#endregion

		#region AutomaticOnACSwitchRelated
		private void PwrManager_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IPowerManager.CurrentPowerStatus)) { PowerStatusChanged(); }
		}

		private void PowerStatusChanged()
		{
			if(!configuration.Data.AutomaticOnACSwitch) { return; }

			var currentPowerPlugStatus = pwrManager.CurrentPowerStatus;
			Guid schemaGuidToSwitch = default;

			switch (currentPowerPlugStatus)
			{
				case PowerLineStatus.Online:
					schemaGuidToSwitch = configuration.Data.AutomaticPlanGuidOnAC;
					break;
				case PowerLineStatus.Offline:
					schemaGuidToSwitch = configuration.Data.AutomaticPlanGuidOffAC;
					break;
				default:
					break;
			}

//			IPowerScheme schemaToSwitchTo = pwrManager.Schemas.FirstOrDefault(sch => sch.Guid == schemaGuidToSwitch);
//			if(schemaToSwitchTo == null) { return; }

//			pwrManager.SetPowerScheme(schemaToSwitchTo);
		}

		#endregion

		#region ContextMenuItemRelatedStuff

		private void ContextMenu_Popup(object? sender, EventArgs e)
		{
			ClearPowerSchemasInTray();

			//pwrManager.UpdateSchemas();
			foreach (var powerSchema in pwrManager.Schemas)
			{
				UpdateTrayMenuWithPowerSchema(powerSchema);
			}
		}

		private void UpdateTrayMenuWithPowerSchema(IPowerScheme powerSchema)
		{
			/*
			var newItemMain = GetNewPowerSchemaItem(
				powerSchema,
				(s, ea) => SwitchToPowerSchema(powerSchema),
				powerSchema.IsActive
				);
			_trayIcon.ContextMenuStrip.Items.Add(0, newItemMain);

			var newItemSettingsOffAC = GetNewPowerSchemaItem(
				powerSchema,
				(s, ea) => SetPowerSchemaAsOffAC(powerSchema),
				(powerSchema.Guid == configuration.Data.AutomaticPlanGuidOffAC)
				);
			_trayIcon.ContextMenuStrip.Items["settings"].MenuItems["settingsOffAC"].MenuItems.Add(0, newItemSettingsOffAC);

			var newItemSettingsOnAC = GetNewPowerSchemaItem(
				powerSchema,
				(s, ea) => SetPowerSchemaAsOnAC(powerSchema),
				(powerSchema.Guid == configuration.Data.AutomaticPlanGuidOnAC)
				);

			_trayIcon.ContextMenu.MenuItems["settings"].MenuItems["settingsOnAC"].MenuItems.Add(0, newItemSettingsOnAC);
			*/
		}

		private void ClearPowerSchemasInTray()
		{
			/*
			for (int i = trayIcon.ContextMenuStrip.Items.Count - 1; i >= 0; i--)
			{
				var item = trayIcon.ContextMenuStrip.Items[i];
				if (item.Name.StartsWith("pwrScheme", StringComparison.Ordinal))
				{
					trayIcon.ContextMenuStrip.Items.Remove(item);
				}
			}
			*/
			//_trayIcon.ContextMenuStrip.Items["settings"].Items["settingsOffAC"].MenuItems.Clear();
			//_trayIcon.ContextMenuStrip.Items["settings"].Items["settingsOnAC"].MenuItems.Clear();
		}

		private static ToolStripMenuItem GetNewPowerSchemaItem(IPowerScheme powerSchema, EventHandler clickedHandler, bool isChecked)
		{
			var newItemMain = new ToolStripMenuItem(powerSchema.Name)
			{
				Name = $"pwrScheme{powerSchema.Guid}",
				Checked = isChecked
			};
			newItemMain.Click += clickedHandler;

			return newItemMain;
		}

		#endregion

		#region OnSchemaClickMethods
		private void SetPowerSchemaAsOffAC(IPowerScheme powerSchema)
		{
			configuration.Data.AutomaticPlanGuidOffAC = powerSchema.Guid;
			configuration.Save();
		}

		private void SetPowerSchemaAsOnAC(IPowerScheme powerSchema)
		{
			configuration.Data.AutomaticPlanGuidOnAC = powerSchema.Guid;
			configuration.Save();
		}

		private void SwitchToPowerSchema(IPowerScheme powerSchema)
		{
			pwrManager.SetPowerScheme(powerSchema);
		}
		#endregion

		#region OtherItemsClicked

		void About_Click(object? sender, EventArgs e)
		{
			Process.Start(AppStrings.AboutAppURL);
		}

		private void IconLicenceItem_Click(object? sender, EventArgs e)
		{
			Process.Start(AppStrings.IconLicenceURL);
		}

		void Exit_Click(object? sender, EventArgs e)
		{
			trayIcon.Visible = false;
			trayIcon.Dispose();

			pwrManager.Dispose();

			System.Windows.Application.Current.Shutdown();
		}
		#endregion

	}
}
