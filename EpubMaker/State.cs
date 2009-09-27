using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EpubMaker
{
	public class StateChangeArgs : EventArgs
	{
		public string Description = "Working...";
		public States State = States.Working;
	}

	public enum States
	{
		Working,
		Ready
	} ;
}
