using Petrroll.Helpers;
using System;
using System.Collections.ObjectModel;

namespace PPSwitcher.TrayApp.ViewModels
{
	public class MainWindowViewModelDesign : ObservableObject
	{
		public MainWindowViewModelDesign()
		{
			Schemas = new ObservableCollection<IPowerScheme>()
			{
				new PowerScheme("Balanced (recommended)", Guid.Empty),
				new PowerScheme("Power saver", Guid.Empty),
				new PowerScheme("Časovače vypnuty (prezentace)", Guid.Empty),
				new PowerScheme("Lorem Ipsum is simply dummy text of the printing and typesetting industry.", Guid.Empty)
			};
			Schemas.Add(ActiveSchema);
		}

		public IPowerScheme ActiveSchema { get; set; } = new PowerScheme("High performance", Guid.Empty, true);
		public ObservableCollection<IPowerScheme> Schemas { get; set; }
	}
}
