using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Microsoft.Win32;
using Path=System.IO.Path;

namespace EpubMaker
{
	/// <summary>
	/// Interaction logic for Source.xaml
	/// </summary>
	public partial class Source : Page, IStep
	{
		public Source()
		{
			InitializeComponent();
		}

		public string SourceFile
		{
			get;
			private set;
		}

		public string RealSourceFile { get; private set; }

		public string DestinationFile { get; private set; }

		private bool sourceReady;

		private void OnChooseSource(object sender, RoutedEventArgs e)
		{
			var openDialog = new OpenFileDialog()
			                 	{DefaultExt = ".html", Filter = "HTML documents (*.htm, *.html)|*.htm;*.html|All files (*.*)|*.*"};
			bool? result = openDialog.ShowDialog();
			sourceReady = false;
			txtSource.Text = string.Empty;

			if (!result.Value)
				return;

			txtSource.Text = RealSourceFile = SourceFile = openDialog.FileName;
			DestinationFile = Path.GetDirectoryName(SourceFile) + "\\" + Path.GetFileNameWithoutExtension(SourceFile) + ".epub";
			txtDestination.Text = DestinationFile;

			if (StateChanged != null)
			{
				StateChanged(this, new StateChangeArgs { Description = "Ready", State = States.Ready});
			}
		}

		private void OnChooseDestination(object sender, RoutedEventArgs e)
		{
			var saveDialog = new SaveFileDialog
			                 	{
			                 		DefaultExt = ".epub",
			                 		Filter = "EPUB documents (*.epub)|All files (*.*)"
			                 	};
			bool? result = saveDialog.ShowDialog();
			if (result.Value)
			{
				DestinationFile = saveDialog.FileName;
			}

			if (CanProceed && StateChanged != null)
			{
				StateChanged(this, new StateChangeArgs() { Description = "Ready", State = States.Ready});
			}
		}

		public event StateEventHandler StateChanged;

		public void Init(BookInfo bookInfo)
		{
			// Do nothing
		}

		public void Wrapup(BookInfo bookInfo)
		{
			Mouse.OverrideCursor = Cursors.Wait;

			bookInfo.SourceType = GetSourceType(SourceFile);

			switch (bookInfo.SourceType)
			{
				case SourceType.Html:
					LoadHtml(bookInfo);
					break;
				case SourceType.Opf:
					LoadOpf(bookInfo);
					break;
			}

			Mouse.OverrideCursor = null;

			if (CanProceed && StateChanged != null)
			{
				StateChanged(this, new StateChangeArgs { Description = "Ready", State = States.Ready });
			}
		}

		private void LoadOpf(BookInfo bookInfo)
		{
			bookInfo.Toc = new XmlDocument();
			bookInfo.Toc.Load(SourceFile);

			throw new NotImplementedException();
		}

		private void LoadHtml(BookInfo bookInfo)
		{
			var document = new XmlDocument();
	
			try
			{
				if (StateChanged != null)
				{
					StateChanged(this, new StateChangeArgs() { Description = "Loading document..." });
				}
				document.Load(SourceFile);
				if (document.DocumentElement.NamespaceURI != "http://www.w3.org/1999/xhtml")
				{
					throw new Exception("Invalid XHTML");
				}

				sourceReady = true;
				txtSource.Text = SourceFile;
			}
			catch (Exception)
			{
				// It's not a valid XHTML file. Try converting it via html-tidy
				string convertedFile = Path.GetTempFileName();

				if (StateChanged != null)
				{
					StateChanged(this, new StateChangeArgs() { Description = "Converting to XHTML..." });
				}
				var p = Process.Start(new ProcessStartInfo()
				                      	{
				                      		FileName = "tidy",
				                      		Arguments =
				                      			string.Format("-o \"{0}\" -w 0 -c -asxml --doctype strict --anchor-as-name no --drop-proprietary-attributes yes --enclose-text yes -ascii -q \"{1}\"", convertedFile, SourceFile),
				                      		WindowStyle = ProcessWindowStyle.Hidden
				                      	});
				p.WaitForExit();

				if (p.ExitCode < 2)
				{
					if (StateChanged != null)
					{
						StateChanged(this, new StateChangeArgs() { Description = "Loading XHTML..." });
					}

					txtSource.Text = SourceFile;
					RealSourceFile = convertedFile;
					document.Load(convertedFile);
					File.Delete(convertedFile);
					sourceReady = true;
				}
				else
				{
					if (StateChanged != null)
					{
						StateChanged(this, new StateChangeArgs() { Description = "XHTML conversion failed" });
					}

					MessageBox.Show("XHTML conversion failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			bookInfo.Toc = new XmlDocument();
			bookInfo.Toc.LoadXml(
				@"<?xml version='1.0' ?>
<package version='2.0' xmlns='http://www.idpf.org/2007/opf' unique-identifier='BookId'>
<metadata xmlns:dc='http://purl.org/dc/elements/1.1/' xmlns:opf='http://www.idpf.org/2007/opf'>
	<dc:identifier id='BookId'></dc:identifier>
	<dc:language>en</dc:language>
	<dc:type>Text</dc:type>
</metadata>
<manifest>
	<item id='body' href='' media-type='application/xhtml+xml'/>
	<item id='ncx' href='' media-type='application/x-dtbncx+xml'/>
</manifest>
<spine toc='ncx'>
	<itemref idref='body'/>
</spine>
</package>");

			bookInfo.Source = SourceFile;
			bookInfo.RealSource = RealSourceFile;
			bookInfo.Destination = DestinationFile;

			bookInfo.AddFile(new HtmlFileInfo {Document = document, FilePath = SourceFile});
		}

		private SourceType GetSourceType(string file)
		{
			var normFile = file.ToLower();
			if (Regex.IsMatch(normFile, "\\.html?$"))
			{
				return SourceType.Html;
			}
			if (Regex.IsMatch(normFile, "\\.opf$"))
			{
				return SourceType.Opf;
			}

			throw new ArgumentException(string.Format("Wrong source file type: {0}", file));
		}

		public bool CanProceed
		{
			get { return sourceReady && !string.IsNullOrEmpty(DestinationFile); }
		}
	}
}
