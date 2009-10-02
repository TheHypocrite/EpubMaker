using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Xml;

namespace EpubMaker
{
	/// <summary>
	/// Interaction logic for Metadata.xaml
	/// </summary>
	public partial class Metadata : Page, IStep
	{
		public Metadata()
		{
			InitializeComponent();
		}

		public event StateEventHandler StateChanged;
		public void Init(BookInfo bookInfo)
		{
			var info = bookInfo.Files[0] as HtmlFileInfo;
			var xhtml = info.Document;
			var html = xhtml.DocumentElement;
			var nsmgr = info.Nsmgr;

			// File name
			var fileName = System.IO.Path.GetFileName(bookInfo.Source);
			var match = Regex.Match(fileName, "([^-]*) - (.*)");
			if (match.Success)
			{
				bookInfo.Author = match.Groups[1].Value;
				bookInfo.Title = match.Groups[2].Value;
			}

			var head = (XmlElement) xhtml.SelectSingleNode("//ns:head", nsmgr);
            if (head != null)
            {
                // Document title
                var title = (XmlElement)head.SelectSingleNode("//ns:title", nsmgr);
                if (title != null && !string.IsNullOrEmpty(title.InnerText))
                {
                    bookInfo.Title = title.InnerText;
                }

                // DC metadata
                var link = (XmlElement)xhtml.SelectSingleNode("//ns:link[starts-with(translate(@href,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'http://purl.org/dc/elements/')]", nsmgr);
                if (link != null)
                {
                    var prefix = link.GetAttribute("rel").Remove(0, 7).ToLower();
                    title = (XmlElement)head.SelectSingleNode(string.Format("//ns:meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{0}.title']", prefix), nsmgr);
                    if (title != null)
                    {
                        bookInfo.Title = title.GetAttribute("content");
                    }
                    var author = (XmlElement)head.SelectSingleNode(string.Format("//ns:meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{0}.creator']", prefix), nsmgr);
                    if (author != null)
                    {
                        bookInfo.Author = author.GetAttribute("content");
                    }
                }
            }

			txtAuthor.Text = bookInfo.Author;
			txtTitle.Text = bookInfo.Title;
		}

		public void Wrapup(BookInfo bookInfo)
		{
			if (!string.IsNullOrEmpty(txtAuthor.Text))
			{
				bookInfo.Author = txtAuthor.Text;
			}
			if (!string.IsNullOrEmpty(txtTitle.Text))
			{
				bookInfo.Title = txtTitle.Text;
			}
		}

		public bool CanProceed
		{
			get { return true; }
		}

		private void TextboxGotFocus(object sender, RoutedEventArgs e)
		{
			((TextBox)sender).SelectAll();
		}
	}
}
