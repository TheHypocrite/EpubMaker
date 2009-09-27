using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace EpubMaker
{
	/// <summary>
	/// Interaction logic for Style.xaml
	/// </summary>
	public partial class Style : Page, IStep
	{
		public Style()
		{
			InitializeComponent();
		}

		public event StateEventHandler StateChanged;
		public void Init(BookInfo bookInfo)
		{
		}

		public void Wrapup(BookInfo bookInfo)
		{
			if (txtStyle.Text.Length == 0)
				return;

			// TODO: Style
			//XmlDocument document = bookInfo.Document;
			//var nsmgr = new XmlNamespaceManager(document.NameTable);
			//XmlElement documentElement = document.DocumentElement;
			//nsmgr.AddNamespace("ns", documentElement.NamespaceURI);
			//var headNode = document.SelectSingleNode("//ns:head", nsmgr);

			//var styleNode = (XmlElement) document.CreateNode(XmlNodeType.Element, "style", documentElement.NamespaceURI);
			//headNode.AppendChild(styleNode);
			//styleNode.SetAttribute("type", "text/css");
			//styleNode.AppendChild(document.CreateTextNode(txtStyle.Text));
		}

		public bool CanProceed
		{
			get { return true; }
		}
	}
}
