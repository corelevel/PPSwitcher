using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PPSwitcher.Wrappers
{
	public partial class Win32PowSchemasWrapper
	{
		private const int ERROR_NO_MORE_ITEMS = 259;
		private const uint ACCESS_SCHEME = 16;
		public static Guid GetActiveScheme()
		{
			IntPtr guidPtr = IntPtr.Zero;

			try
			{
				var errCode = PowerGetActiveScheme(IntPtr.Zero, out guidPtr);

				if (errCode != 0) { throw new PPSwitcherWrappersException($"GetActiveScheme() failed with code {errCode}"); }
				if (guidPtr == IntPtr.Zero) { throw new PPSwitcherWrappersException("GetActiveScheme() returned null pointer for GUID"); }

				Guid? activeScheme = (Guid?)Marshal.PtrToStructure(guidPtr, typeof(Guid));
				return activeScheme == null
					? throw new PPSwitcherWrappersException("GetActiveScheme() unable to marshall GUID")
					: (Guid)activeScheme;
			}
			finally
			{
				if (guidPtr != IntPtr.Zero) { LocalFree(guidPtr); }
			}
		}

		public static void SetActiveScheme(Guid guid)
		{
			var errCode = PowerSetActiveScheme(IntPtr.Zero, ref guid);
			if (errCode != 0) { throw new PPSwitcherWrappersException($"SetActiveScheme() failed with code {errCode}"); }
		}

		public static string GetSchemeName(Guid guid)
		{
			IntPtr bufferPtr = IntPtr.Zero;
			uint bufferSize = 0;

			try
			{
				var errCode = PowerReadFriendlyName(IntPtr.Zero, ref guid, IntPtr.Zero, IntPtr.Zero, bufferPtr, ref bufferSize);
				if (errCode != 0) { throw new PPSwitcherWrappersException($"GetSchemeName() failed when getting buffer size with code {errCode}"); }

				if (bufferSize <= 0) { return string.Empty; }
				bufferPtr = Marshal.AllocHGlobal((int)bufferSize);

				errCode = PowerReadFriendlyName(IntPtr.Zero, ref guid, IntPtr.Zero, IntPtr.Zero, bufferPtr, ref bufferSize);
				if (errCode != 0) { throw new PPSwitcherWrappersException($"GetSchemeName() failed when getting buffer pointer with code {errCode}"); }

				string? name = Marshal.PtrToStringUni(bufferPtr);
				return name ?? throw new PPSwitcherWrappersException("GetSchemeName() unable to marshall string");
			}
			finally
			{
				if (bufferPtr != IntPtr.Zero) { Marshal.FreeHGlobal(bufferPtr); }
			}
		}

		public static List<PowerScheme> GetExistingSchemas()
		{
			var activeSchemeGuid = GetActiveScheme();
			var schemas = GetExistingSchemasGuid().Select(guid => new PowerScheme(GetSchemeName(guid), guid)).ToList();

			var activeScheme = schemas.FirstOrDefault(s => s.Guid == activeSchemeGuid);
			if (activeScheme != null) activeScheme.IsActive = true;

			return schemas;
		}

		private static IEnumerable<Guid> GetExistingSchemasGuid()
		{
			var schemeGuid = Guid.Empty;

			uint sizeSchemeGuid = (uint)Marshal.SizeOf(typeof(Guid));
			uint schemeIndex = 0;

			while (true)
			{
				uint errCode = PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ACCESS_SCHEME, schemeIndex, ref schemeGuid, ref sizeSchemeGuid);
				if (errCode == ERROR_NO_MORE_ITEMS) { yield break; }
				if (errCode != 0) { throw new PPSwitcherWrappersException($"GetExistingSchemasGuid() failed when getting buffer pointer with code {errCode}"); }

				yield return schemeGuid;
				schemeIndex++;
			}
		}

		#region DLL imports
		[LibraryImport("kernel32.dll", SetLastError = true)]
		private static partial IntPtr LocalFree(IntPtr hMem);
		[LibraryImport("powrprof.dll", EntryPoint = "PowerSetActiveScheme")]
		private static partial uint PowerSetActiveScheme(IntPtr UserPowerKey, ref Guid ActivePolicyGuid);
		[LibraryImport("powrprof.dll", EntryPoint = "PowerGetActiveScheme")]
		private static partial uint PowerGetActiveScheme(IntPtr UserPowerKey, out IntPtr ActivePolicyGuid);
		[LibraryImport("powrprof.dll", EntryPoint = "PowerReadFriendlyName")]
		private static partial uint PowerReadFriendlyName(IntPtr RootPowerKey, ref Guid SchemeGuid, IntPtr SubGroupOfPowerSettingsGuid, IntPtr PowerSettingGuid, IntPtr BufferPtr, ref uint BufferSize);
		[LibraryImport("PowrProf.dll")]
		public static partial uint PowerEnumerate(IntPtr RootPowerKey, IntPtr SchemeGuid, IntPtr SubGroupOfPowerSettingGuid, uint AcessFlags, uint Index, ref Guid Buffer, ref uint BufferSize);
		#endregion
	}

	public class DefaultPowSchemasWrapper
	{
		public static PowerScheme GetBalancedSchema()
		{
			return new("Balanced", new Guid("381b4222-f694-41f0-9685-ff5bb260df2e"));
		}
		public static PowerScheme GetHighPerformanceSchema()
		{
			return new("High performance", new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"));
		}
		public static PowerScheme GetPowerSaverSchema()
		{
			return new("Power saver", new Guid("a1841308-3541-4fab-bc81-f71556f20b4a"));
		}

		public static List<PowerScheme> GetDefaultSchemas()
		{
			return
			[
				GetHighPerformanceSchema(),
				GetBalancedSchema(),
				GetPowerSaverSchema()
			];
		}
	}
}
