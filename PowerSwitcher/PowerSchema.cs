using Petrroll.Helpers;
using System;
using System.ComponentModel;

namespace PowerSwitcher
{
    public enum PowerPlugStatus { Online, Offline }

    public interface IPowerSchema : INotifyPropertyChanged
    {
        string Name { get; }
        Guid Guid { get; }
        bool IsActive { get; }
    }

    public class PowerSchema : ObservableObject, IPowerSchema
    {
        public Guid Guid { get; }
        private string name = null!;
        bool isActive;

        public string Name
        {
            get => name;
            set
            {
                ArgumentNullException.ThrowIfNull(value, nameof(value));
                if (name == value)
                {
                    return;
                }
                name = value;
                RaisePropertyChangedEvent(nameof(Name));
            }
        }

        public bool IsActive
        {
            get => isActive;
            set
            {
                if (isActive == value)
                {
                    return;
                }
                isActive = value;
                RaisePropertyChangedEvent(nameof(IsActive));
            }
        }

        public PowerSchema(string name, Guid guid) : this(name, guid, false) {}

        public PowerSchema(string name, Guid guid, bool isActive)
        {
            Name = name;
            Guid = guid;
            IsActive = isActive;
        }

        public override string ToString()
        {
            return $"{Name}:{IsActive}";
        }
    }
}
