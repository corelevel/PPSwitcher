﻿using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;

////
//  Code copied from https://github.com/File-New-Project/EarTrumpet/blob/master/EarTrumpet/Services/TaskbarService.cs
////
namespace PPSwitcher.TrayApp.Services
{
	[SupportedOSPlatform("windows")]
	public sealed class TaskbarService
	{
		private const string TASK_BAR_CLASS_NAME = "Shell_TrayWnd";

		public static TaskbarState GetWinTaskbarState()
		{
			APPBARDATA ABD = new();
			TaskbarState retState = new();
			var hwnd = User32.FindWindow(TASK_BAR_CLASS_NAME, null);

			ABD.cbSize = Marshal.SizeOf(ABD);
			ABD.uEdge = 0;
			ABD.hWnd = hwnd;
			ABD.lParam = 1;

			User32.GetWindowRect(hwnd, out RECT scaledTaskbarRect);

			var taskbarNonDPIAwareSize = Shell32.SHAppBarMessage((int)ABMsg.ABM_GETTASKBARPOS, ref ABD);

			var scalingAmount = (double)(scaledTaskbarRect.bottom - scaledTaskbarRect.top) / (ABD.rc.bottom - ABD.rc.top);

			retState.TaskbarSize = default;
			retState.TaskbarSize.top = (int)(ABD.rc.top * scalingAmount);
			retState.TaskbarSize.bottom = (int)(ABD.rc.bottom * scalingAmount);
			retState.TaskbarSize.left = (int)(ABD.rc.left * scalingAmount);
			retState.TaskbarSize.right = (int)(ABD.rc.right * scalingAmount);

			var screen = Screen.AllScreens.FirstOrDefault(x => x.Bounds.Contains(
							new Rectangle(
								retState.TaskbarSize.left,
								retState.TaskbarSize.top,
								retState.TaskbarSize.right - retState.TaskbarSize.left,
								retState.TaskbarSize.bottom - retState.TaskbarSize.top)
						 ));

			retState.TaskbarPosition = TaskbarPosition.Bottom;

			if (screen != null)
			{
				if (retState.TaskbarSize.bottom == screen.Bounds.Bottom && retState.TaskbarSize.top == screen.Bounds.Top)
				{
					retState.TaskbarPosition = (retState.TaskbarSize.left == screen.Bounds.Left) ? TaskbarPosition.Left : TaskbarPosition.Right;
				}
				if (retState.TaskbarSize.right == screen.Bounds.Right && retState.TaskbarSize.left == screen.Bounds.Left)
				{
					retState.TaskbarPosition = (retState.TaskbarSize.top == screen.Bounds.Top) ? TaskbarPosition.Top : TaskbarPosition.Bottom;
				}
			}

			return retState;
		}
	}

	public static partial class User32
	{
		[LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
		public static partial IntPtr FindWindow(string lpClassName, string? lpWindowName);
		[LibraryImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static partial bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}

	public static partial class Shell32
	{
		[LibraryImport("shell32.dll")]
		public static partial IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct APPBARDATA
	{
		public int cbSize; // initialize this field using: Marshal.SizeOf(typeof(APPBARDATA));
		public IntPtr hWnd;
		public uint uCallbackMessage;
		public uint uEdge;
		public RECT rc;
		public int lParam;
	}

	public enum ABMsg
	{
		ABM_NEW = 0,
		ABM_REMOVE,
		ABM_QUERYPOS,
		ABM_SETPOS,
		ABM_GETSTATE,
		ABM_GETTASKBARPOS,
		ABM_ACTIVATE,
		ABM_GETAUTOHIDEBAR,
		ABM_SETAUTOHIDEBAR,
		ABM_WINDOWPOSCHANGED,
		ABM_SETSTATE
	}

	public enum ABEdge
	{
		ABE_LEFT = 0,
		ABE_TOP = 1,
		ABE_RIGHT = 2,
		ABE_BOTTOM = 3
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct TaskbarState
	{
		public TaskbarPosition TaskbarPosition;
		public RECT TaskbarSize;
	}

	public enum TaskbarPosition
	{
		Top,
		Left,
		Right,
		Bottom
	}
}
