using PPSwitcher.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace PPSwitcher.TrayApp.Services
{
	////
	//  Based on: http://stackoverflow.com/questions/48935/how-can-i-register-a-global-hot-key-to-say-ctrlshiftletter-using-wpf-and-ne
	////
	[Flags]
	public enum KeyModifier
	{
		None = 0x0000,
		Alt = 0x0001,
		Ctrl = 0x0002,
		NoRepeat = 0x4000,
		Shift = 0x0004,
		Win = 0x0008
	}

	public class HotKey(Key k, KeyModifier keyModifiers)
	{
		public Key Key { get; private set; } = k;
		public KeyModifier KeyModifiers { get; private set; } = keyModifiers;
		public event Action? HotKeyFired;

		public int VirtualKeyCode => KeyInterop.VirtualKeyFromKey(Key);
		public int Id => VirtualKeyCode + ((int)KeyModifiers* 0x10000);

		public void Fire()
		{
			HotKeyFired?.Invoke();
		}
	}

	public partial class HotKeyService : IDisposable
	{
		private const int WM_HOTKEY = 0x0312;
		private const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

		private readonly Dictionary<int, HotKey> dictHotKeyToCalBackProc = [];
		private bool isDisposed = false;

		public HotKeyService()
		{
			ComponentDispatcher.ThreadFilterMessage += ComponentDispatcherThreadFilterMessage;
		}
		public bool Register(HotKey hotKey)
		{
			if (dictHotKeyToCalBackProc.TryGetValue(hotKey.Id, out HotKey? key)) { Unregister(key); }

			var success = RegisterHotKey(IntPtr.Zero, hotKey.Id, (uint)hotKey.KeyModifiers, (uint)hotKey.VirtualKeyCode);
			if (!success)
			{
				if (Marshal.GetLastWin32Error() == ERROR_HOTKEY_ALREADY_REGISTERED) { return false; }
				else { throw new PPSwitcherWrappersException($"Register() failed|{Marshal.GetLastWin32Error()}"); }
			}

			dictHotKeyToCalBackProc.Add(hotKey.Id, hotKey);
			return true;
		}
		/*
		public bool Register(HotKey hotkey)
        {
            if(_dictHotKeyToCalBackProc.ContainsKey(hotkey.Id)) { Unregister(_dictHotKeyToCalBackProc[hotkey.Id]); }

            var success = RegisterHotKey(IntPtr.Zero, hotkey.Id, (UInt32)hotkey.KeyModifiers, (UInt32)hotkey.VirtualKeyCode);
            if (!success)
            {
                //ERROR_HOTKEY_ALREADY_REGISTERED
                if (Marshal.GetLastWin32Error() == 1409) { return false; }
                else { throw new PowerSwitcherWrappersException($"RegisterHotKey() failed|{Marshal.GetLastWin32Error()}"); }
            }

            _dictHotKeyToCalBackProc.Add(hotkey.Id, hotkey);
            return true;
        }
		*/
		public void Unregister(HotKey hotKey)
		{
			//if (!dictHotKeyToCalBackProc.ContainsKey(hotKey.Id)) { throw new InvalidOperationException($"Trying to unregister not-registred Hotkey {hotKey.Id}"); }

			var success = UnregisterHotKey(IntPtr.Zero, hotKey.Id); 
			if (!success) { throw new PPSwitcherWrappersException($"Unregister() failed|{Marshal.GetLastWin32Error()}"); }

			dictHotKeyToCalBackProc.Remove(hotKey.Id);		  
		}

		private void ComponentDispatcherThreadFilterMessage(ref MSG msg, ref bool handled)
		{
			if (handled || msg.message != WM_HOTKEY) { return; }

			if (dictHotKeyToCalBackProc.TryGetValue((int)msg.wParam, out HotKey? hotKey))
			{
				hotKey?.Fire();
				handled = true;
			}
		}
		public void Dispose()
		{
			Dispose(true);
			//GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing && !isDisposed)
			{
				foreach (var hotKey in dictHotKeyToCalBackProc.Values.ToList())
				{
					Unregister(hotKey);
				}
				isDisposed = true;
			}
		}

		#region DLL imports
		[LibraryImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vlc);
		[LibraryImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool UnregisterHotKey(IntPtr hWnd, int id);
		#endregion
	}
}
