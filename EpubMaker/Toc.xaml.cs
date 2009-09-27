using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace EpubMaker
{
	/// <summary>
	/// Interaction logic for Toc.xaml
	/// </summary>
	public partial class Toc : Page, IStep
	{
		private BookInfo bookInfo;

		public Toc()
		{
			InitializeComponent();
		}

		public event StateEventHandler StateChanged;
		public void Init(BookInfo bookInfo)
		{
			this.bookInfo = bookInfo;
		}

		public void Wrapup(BookInfo bookInfo)
		{
		}

		public bool CanProceed
		{
			get { return true; }
		}

		private void chkToc_Click(object sender, RoutedEventArgs e)
		{
			GenerateToc();
		}

		private void ddlLevels_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			GenerateToc();
		}

		private void GenerateToc()
		{
			if (tvToc == null)
				return;

			tvToc.Items.Clear();

			if (!chkToc.IsChecked.Value)
				return;

			var info = bookInfo.Files[0] as HtmlFileInfo;
			var xhtml = info.Document;
			var html = xhtml.DocumentElement;
			var nsmgr = info.Nsmgr;

			var headers = html.SelectNodes("//ns:h1", nsmgr);
			foreach (XmlNode header in headers)
			{
				tvToc.Items.Add(new TreeViewItem() {Header = header.InnerText});
			}
		}
	}
}
