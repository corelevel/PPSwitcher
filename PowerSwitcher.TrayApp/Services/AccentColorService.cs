using System;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace PowerSwitcher.TrayApp.Services
{
    ////
    //  Code heavily inspired by https://github.com/File-New-Project/EarTrumpet/blob/master/EarTrumpet/Services/AccentColorService.cs
    ////
    public static partial class AccentColorService
    {
        static partial class Interop
        {
            // Thanks, Quppa! -RR

            [LibraryImport("uxtheme.dll", EntryPoint = "#94")]
            internal static partial int GetImmersiveColorSetCount();

            [LibraryImport("uxtheme.dll", EntryPoint = "#95")]
            internal static partial uint GetImmersiveColorFromColorSetEx(uint dwImmersiveColorSet, uint dwImmersiveColorType, [MarshalAs(UnmanagedType.Bool)] bool bIgnoreHighContrast, uint dwHighContrastCacheMode);

            [LibraryImport("uxtheme.dll", EntryPoint = "#96", StringMarshalling = StringMarshalling.Utf16)]
            internal static partial uint GetImmersiveColorTypeFromName(string name);

            [DllImport("uxtheme.dll", EntryPoint = "#98", CharSet = CharSet.Unicode)]
            internal static extern uint GetImmersiveUserColorSetPreference(bool bForceCheckRegistry, bool bSkipCheckOnFail);

            [DllImport("uxtheme.dll", EntryPoint = "#100", CharSet = CharSet.Unicode)]
            internal static extern IntPtr GetImmersiveColorNamedTypeByIndex(uint dwIndex);
        }

        public static Color GetColorByTypeName(string name)
        {
            var colorSet = Interop.GetImmersiveUserColorSetPreference(false, false);
            var colorType = Interop.GetImmersiveColorTypeFromName(name);

            var rawColor = Interop.GetImmersiveColorFromColorSetEx(colorSet, colorType, false, 0);

            return FromABGR(rawColor);
        }

        public static Color FromABGR(uint abgrValue)
        {
            var colorBytes = new byte[4];
            colorBytes[0] = (byte)((0xFF000000 & abgrValue) >> 24);	// A
            colorBytes[1] = (byte)((0x00FF0000 & abgrValue) >> 16);	// B
            colorBytes[2] = (byte)((0x0000FF00 & abgrValue) >> 8);	// G
            colorBytes[3] = (byte)(0x000000FF & abgrValue);			// R

            return Color.FromArgb(colorBytes[0], colorBytes[3], colorBytes[2], colorBytes[1]);
        }
    }
}
