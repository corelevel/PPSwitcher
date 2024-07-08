namespace PPSwitcher.Wrappers
{
	public class PPSwitcherWrappersException : System.Exception
	{
		public PPSwitcherWrappersException() { }
		public PPSwitcherWrappersException(string message) : base(message) { }
		public PPSwitcherWrappersException(string message, System.Exception inner) : base(message, inner) { }
	}
}
