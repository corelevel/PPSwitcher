using System;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace PowerSwitcher.Wrappers
{
    [SupportedOSPlatform("windows")]
    public class BatteryInfoWrapper : IDisposable
    {
        readonly Microsoft.Win32.PowerModeChangedEventHandler powerChangedDelegate = null;

        public BatteryInfoWrapper(Action<PowerPlugStatus> powerStatusChangedFunc)
        {
            powerChangedDelegate = (sender, e) => { powerStatusChangedFunc(GetCurrentChargingStatus()); };
            Microsoft.Win32.SystemEvents.PowerModeChanged += powerChangedDelegate;
        }

        public static PowerPlugStatus GetCurrentChargingStatus()
        {
            PowerStatus pwrStatus = SystemInformation.PowerStatus;
            return (pwrStatus.PowerLineStatus == PowerLineStatus.Online) ? PowerPlugStatus.Online : PowerPlugStatus.Offline;
        }

        public static int GetChargeValue()
        {
            PowerStatus pwrStatus = SystemInformation.PowerStatus;
            return pwrStatus.BatteryLifeRemaining / 60;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) { return; }

            if (disposing)
            {
                var tmpDelegate = powerChangedDelegate;
                if (tmpDelegate == null) { return; }

                Microsoft.Win32.SystemEvents.PowerModeChanged -= tmpDelegate;
            }

            disposedValue = true;
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~BatteryInfoWrapper() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        public void Dispose()
        {
            Dispose(true);
            //GC.SuppressFinalize(this); //No destructor so isn't required yet
        }
        #endregion
    }
}
