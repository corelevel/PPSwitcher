using Petrroll.Helpers;
using System;
using System.ComponentModel;

namespace PPSwitcher
{
	public interface IPowerScheme : INotifyPropertyChanged
	{
		string Name { get; }
		Guid Guid { get; }
		bool IsActive { get; set; }
	}
	public class PowerScheme : ObservableObject, IPowerScheme
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
		public PowerScheme(string name, Guid guid) : this(name, guid, false) {}
		public PowerScheme(string name, Guid guid, bool isActive)
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
	public class PowerSchemaUnknown : ObservableObject, IPowerScheme
	{
		public Guid Guid { get; } = Guid.Empty;
		public string Name { get; } = "unknown";
		public bool IsActive { get; set; } = false;

		public override string ToString()
		{
			return $"{Name}:{IsActive}";
		}
	}
}
