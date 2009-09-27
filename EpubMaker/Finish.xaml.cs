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
using Path=System.IO.Path;

namespace EpubMaker
{
	/// <summary>
	/// Interaction logic for Finish.xaml
	/// </summary>
	public partial class Finish : Page, IStep
	{
		public Finish()
		{
			InitializeComponent();
		}

		public event StateEventHandler StateChanged;
		public void Init(BookInfo bookInfo)
		{
			Cursor = Cursors.Wait;

			File.Delete(bookInfo.Destination);

			var prevDir = Directory.GetCurrentDirectory();

			var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempDir);
			Directory.SetCurrentDirectory(tempDir);

			// mimetype
			var file = new StreamWriter("mimetype");
			file.Write("application/epub+zip");
			file.Close();

			var startInfo = new ProcessStartInfo()
			           	{
			           		FileName = "zip",
			           		Arguments = string.Format("-q0X \"{0}\" mimetype", bookInfo.Destination),
			           		WindowStyle = ProcessWindowStyle.Hidden
			           	};
			var p = Process.Start(startInfo);
			if (p != null)
			{
				p.WaitForExit();
			}

			// meta-inf
			try
			{
				Directory.Delete("META-INF", true);
			}
			catch (Exception) { }
			Directory.CreateDirectory("META-INF");
			var writer = new StreamWriter("META-INF/container.xml", false, Encoding.UTF8);
			writer.WriteLine(
@"<?xml version='1.0' ?>
<container version='1.0' xmlns='urn:oasis:names:tc:opendocument:xmlns:container'>
	<rootfiles>
		<rootfile full-path='OEBPS/content.opf' media-type='application/oebps-package+xml'/>
	</rootfiles>
</container>");
			writer.Close();

			var sourceBareName = Path.GetFileNameWithoutExtension(bookInfo.Source);
			var sourceName = Path.GetFileName(bookInfo.Source);
			var sourcePath = Path.GetDirectoryName(bookInfo.Source);

			// oebps
			try
			{
				Directory.Delete("OEBPS", true);
			}
			catch (Exception) { }
			Directory.CreateDirectory("OEBPS");

			var uniqueIdentifier = "ISBN...";

			// toc
			var ncxFileName = "toc.ncx";
			var xmlWriter = new XmlTextWriter("OEBPS/" + ncxFileName, Encoding.UTF8) {Formatting = Formatting.Indented};
			var tocDoc = new XmlDocument();
			tocDoc.LoadXml(
@"<?xml version='1.0'?>
<ncx xmlns='http://www.daisy.org/z3986/2005/ncx/' version='2005-1'>
   <head>
      <meta name='dtb:uid' content='" + uniqueIdentifier + @"'/>
      <meta name='dtb:depth' content='1'/>
      <meta name='dtb:totalPageCount' content='0'/>
      <meta name='dtb:maxPageNumber' content='0'/>
   </head>

   <docTitle>
      <text>" + bookInfo.Title + @"</text>
   </docTitle>

   <navMap>
      <navPoint id='navPoint-1' playOrder='1'>
         <navLabel>
            <text>" + bookInfo.Title + @"</text>
         </navLabel>
         <content src='" + sourceBareName + @".html'/>
      </navPoint>
	</navMap>
</ncx>");
			tocDoc.WriteContentTo(xmlWriter);
			xmlWriter.Close();

			string destinationName = sourceBareName + ".html";
			bookInfo.DestinationName = destinationName;
			bookInfo.NcxFileName = ncxFileName;

			// content
			var contentDoc = bookInfo.Toc;

			// Metadata
			var nsmgr = new XmlNamespaceManager(contentDoc.NameTable);
			nsmgr.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
			nsmgr.AddNamespace("opf", "http://www.idpf.org/2007/opf");

			var manifestNode = contentDoc.SelectSingleNode("//opf:manifest", nsmgr);

			// Images
			Directory.CreateDirectory("OEBPS/img");
			var images = bookInfo.Files.FindAll(x => Regex.IsMatch(x.MediaType, "image/"));
			foreach (var image in images)
			{
				bookInfo.CopyImage(image.FilePath, "OEBPS/img");
			}

			// TODO: CSS
			//Directory.CreateDirectory("OEBPS/css");
			//const string cssLocation = "css/style.css";
			//const string cssMediaType = "text/css";

			//var style = new StreamWriter("OEBPS/" + cssLocation);
			//style.WriteLine(".br { margin-bottom: 2em; } ");

			//nodeList = rootElement.SelectNodes("//ns:style", docnsmgr);
			//if (nodeList.Count > 0)
			//{
			//    foreach (XmlNode node in nodeList)
			//    {
			//        if (node.Attributes["type"].InnerXml == cssMediaType)
			//        {
			//            style.WriteLine(node.InnerText);
			//        }
			//        node.ParentNode.RemoveChild(node);
			//    }

			//    var headNode = rootElement.SelectSingleNode("//ns:head", docnsmgr);
			//    var linkNode = (XmlElement) bookInfo.Document.CreateNode(XmlNodeType.Element, "link", docnsmgr.LookupNamespace("ns"));
			//    headNode.AppendChild(linkNode);
			//    linkNode.SetAttribute("rel", "stylesheet");
			//    linkNode.SetAttribute("type", cssMediaType);
			//    linkNode.SetAttribute("href", cssLocation);

			//    var styleNode = (XmlElement) contentDoc.CreateNode(XmlNodeType.Element, "item", nsmgr.LookupNamespace("opf"));
			//    manifestNode.AppendChild(styleNode);
			//    styleNode.SetAttribute("href", cssLocation);
			//    styleNode.SetAttribute("id", "css");
			//    styleNode.SetAttribute("media-type", cssMediaType);
			//}
			//style.Close();

			// content.opf
			xmlWriter = new XmlTextWriter("OEBPS/content.opf", Encoding.UTF8) { Formatting = Formatting.Indented };
			contentDoc.WriteContentTo(xmlWriter);
			xmlWriter.Close();

			// Finally, the document
			xmlWriter = new XmlTextWriter("OEBPS/" + destinationName, Encoding.UTF8)
			            	{
			            		Formatting = Formatting.Indented,
			            		IndentChar = '\t',
			            		Indentation = 1
			            	};
			bookInfo.Document.WriteContentTo(xmlWriter);
			xmlWriter.Close();

			startInfo.Arguments = string.Format("-r \"{0}\" META-INF OEBPS", bookInfo.Destination);
			p = Process.Start(startInfo);
			if (p != null)
			{
				p.WaitForExit();
			}
			Directory.SetCurrentDirectory(prevDir);

			Directory.Delete(tempDir, true);

			Cursor = Cursors.Arrow;
		}

		public void Wrapup(BookInfo bookInfo)
		{
			if (chkOpenDoc.IsChecked.GetValueOrDefault())
			{
				Process.Start(bookInfo.Destination);
			}

			if (chkOpenFolder.IsChecked.GetValueOrDefault())
			{
				Process.Start(Path.GetDirectoryName(bookInfo.Destination));
			}
		}

		public bool CanProceed
		{
			get { return true; }
		}
	}
}
