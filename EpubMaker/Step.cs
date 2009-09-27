using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EpubMaker
{
	public delegate void StateEventHandler(IStep sender, StateChangeArgs newState);

	public interface IStep
	{
		event StateEventHandler StateChanged;

		void Init(BookInfo bookInfo);
		void Wrapup(BookInfo bookInfo);

		bool CanProceed { get; }
	}
}
