using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PowerSwitcher.Wrappers
{
    public partial class Win32PowSchemasWrapper
    {
        public static Guid GetActiveGuid()
        {
            IntPtr guidPtr = IntPtr.Zero;

            try
            {
                var errCode = PowerGetActiveScheme(IntPtr.Zero, out guidPtr);

                if (errCode != 0) { throw new PowerSwitcherWrappersException($"GetActiveGuid() failed with code {errCode}"); }
                if (guidPtr == IntPtr.Zero) { throw new PowerSwitcherWrappersException("GetActiveGuid() returned null pointer for GUID"); }

                return (Guid)Marshal.PtrToStructure(guidPtr, typeof(Guid));
            }
            finally
            {
                if (guidPtr != IntPtr.Zero) { LocalFree(guidPtr); }
            }
        }

        public static void SetActiveGuid(Guid guid)
        {
            var errCode = PowerSetActiveScheme(IntPtr.Zero, ref guid);
            if (errCode != 0) { throw new PowerSwitcherWrappersException($"SetActiveGuid() failed with code {errCode}"); }
        }

        public static string GetPowerPlanName(Guid guid)
        {
            IntPtr bufferPointer = IntPtr.Zero;
            uint bufferSize = 0;

            try
            {
                var errCode = PowerReadFriendlyName(IntPtr.Zero, ref guid, IntPtr.Zero, IntPtr.Zero, bufferPointer, ref bufferSize);
                if (errCode != 0) { throw new PowerSwitcherWrappersException($"GetPowerPlanName() failed when getting buffer size with code {errCode}"); }

                if (bufferSize <= 0) { return string.Empty; }
                bufferPointer = Marshal.AllocHGlobal((int)bufferSize);

                errCode = PowerReadFriendlyName(IntPtr.Zero, ref guid, IntPtr.Zero, IntPtr.Zero, bufferPointer, ref bufferSize);
                if (errCode != 0) { throw new PowerSwitcherWrappersException($"GetPowerPlanName() failed when getting buffer pointer with code {errCode}"); }

                return Marshal.PtrToStringUni(bufferPointer);
            }
            finally
            {
                if (bufferPointer != IntPtr.Zero) { Marshal.FreeHGlobal(bufferPointer); }
            }
        }

        private const int ERROR_NO_MORE_ITEMS = 259;
        public static List<PowerSchema> GetCurrentSchemas()
        {
            return GetAllPowerSchemaGuids().Select(guid => new PowerSchema(GetPowerPlanName(guid), guid)).ToList();
        }

        private static IEnumerable<Guid> GetAllPowerSchemaGuids()
        {
            var schemeGuid = Guid.Empty;

            uint sizeSchemeGuid = (uint)Marshal.SizeOf(typeof(Guid));
            uint schemeIndex = 0;

            while (true)
            {
                uint errCode = PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)AccessFlags.ACCESS_SCHEME, schemeIndex, ref schemeGuid, ref sizeSchemeGuid);
                if (errCode == ERROR_NO_MORE_ITEMS) { yield break; }
                if (errCode != 0) { throw new PowerSwitcherWrappersException($"GetPowerSchemeGUIDs() failed when getting buffer pointer with code {errCode}"); }

                yield return schemeGuid;
                schemeIndex++;
            }
        }

        #region EnumerationEnums
        public enum AccessFlags : uint
        {
            ACCESS_SCHEME = 16,
            ACCESS_SUBGROUP = 17,
            ACCESS_INDIVIDUAL_SETTING = 18
        }
        #endregion


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
        public static List<PowerSchema> GetCurrentSchemas()
        {
            var schemas = new List<PowerSchema>
            {
                new("Maximum performance", new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c")),
                new("Balanced", new Guid("381b4222-f694-41f0-9685-ff5bb260df2e")),
                new("Power saver", new Guid("a1841308-3541-4fab-bc81-f71556f20b4a"))
            };

            return schemas;
        }
    }
}
