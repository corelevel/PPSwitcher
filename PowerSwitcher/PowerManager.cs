using Petrroll.Helpers;
using PowerSwitcher.Wrappers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Versioning;


namespace PowerSwitcher
{
    public interface IPowerManager : INotifyPropertyChanged, IDisposable
    {
        ObservableCollection<IPowerSchema> Schemas { get; }
        IPowerSchema CurrentSchema { get; }

        PowerPlugStatus CurrentPowerStatus { get; }

        void UpdateSchemas();

        void SetPowerSchema(IPowerSchema schema);
        void SetPowerSchema(Guid guid);
    }

    [SupportedOSPlatform("windows")]
    public class PowerManager : ObservableObject, IPowerManager
    {
        readonly BatteryInfoWrapper batteryWrapper;

        public ObservableCollection<IPowerSchema> Schemas{ get; private set; }
        public IPowerSchema CurrentSchema { get; private set; }

        public PowerPlugStatus CurrentPowerStatus { get; private set; }

        public PowerManager()
        {
            batteryWrapper = new BatteryInfoWrapper(PowerChangedEvent);

            Schemas = [];

            PowerChangedEvent(BatteryInfoWrapper.GetCurrentChargingStatus());
            UpdateSchemas();
        }

        public void UpdateSchemas()
        {
            var currSchemaGuid = Win32PowSchemasWrapper.GetActiveGuid();
            var newSchemas = Win32PowSchemasWrapper.GetCurrentSchemas();

            //Add and update new / changed schemas
            foreach (var newSchema in newSchemas)
            {
                var originalSchema = Schemas.FirstOrDefault(sch => sch.Guid == newSchema.Guid);
                if (originalSchema == null) { InsertNewSchema(newSchemas, newSchema); originalSchema = newSchema; }
               
                if (newSchema.Guid == currSchemaGuid && originalSchema?.IsActive != true)
                { SetNewCurrSchema(originalSchema); }
                
                if (originalSchema?.Name != newSchema.Name)
                { ((PowerSchema)originalSchema).Name = newSchema.Name; }
            }

            if(!Schemas.Any(sch => currSchemaGuid == sch.Guid))
            {
                NoSchemaIsActive();
            }

            //remove old schemas
            var schemasToBeRemoved = new List<IPowerSchema>();
            foreach (var oldSchema in Schemas)
            {
                if (newSchemas.FirstOrDefault(sch => sch.Guid == oldSchema.Guid) == null)
                { schemasToBeRemoved.Add(oldSchema); }
            }
            schemasToBeRemoved.ForEach(sch => Schemas.Remove(sch));
        }

        private void NoSchemaIsActive()
        {
            var oldActive = Schemas.FirstOrDefault(sch => sch.IsActive);
            if (oldActive != null)
            {
                ((PowerSchema)oldActive).IsActive = false;

                CurrentSchema = null;
                RaisePropertyChangedEvent(nameof(CurrentSchema));
            }
        }

        private void InsertNewSchema(List<PowerSchema> newSchemas, PowerSchema newSchema)
        {
            var insertToIndex = Math.Min(newSchemas.IndexOf(newSchema), Schemas.Count);
            Schemas.Insert(insertToIndex, newSchema);
        }

        private void SetNewCurrSchema(IPowerSchema newActiveSchema)
        {
            var oldActiveSchema = Schemas.FirstOrDefault(sch => sch.IsActive);

            ((PowerSchema)newActiveSchema).IsActive = true;
            CurrentSchema = newActiveSchema;
            RaisePropertyChangedEvent(nameof(CurrentSchema));

            //can cause change change of curr power schema: http://stackoverflow.com/questions/42703092/remove-selection-when-selected-item-gets-deleted-from-listbox
            if (oldActiveSchema != null) { ((PowerSchema)oldActiveSchema).IsActive = false; }
        }

        public void SetPowerSchema(IPowerSchema schema)
        {
            SetPowerSchema(schema.Guid);
        }

        public void SetPowerSchema(Guid guid)
        {
            try
            {
                Win32PowSchemasWrapper.SetActiveGuid(guid);
            }
            catch (PowerSwitcherWrappersException) { }
            UpdateSchemas();
        }

        private void PowerChangedEvent(PowerPlugStatus newStatus)
        {
            if(newStatus == CurrentPowerStatus) { return; }

            CurrentPowerStatus = newStatus;
            RaisePropertyChangedEvent(nameof(CurrentPowerStatus));
        }

        #region IDisposable Support
        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) { return; }
            if (disposing)
            {
                batteryWrapper.Dispose();
            }

            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);

            //No destructor so isn't required (yet)            
            // GC.SuppressFinalize(this); 
        }

        #endregion

    }

}



