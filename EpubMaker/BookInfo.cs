using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace EpubMaker
{
	public enum SourceType
	{
		Unknown,
		Html,
		Opf
	} ;

	public class FileInfo
	{
		public string FilePath;
		public string NewPath;
		public string MediaType { get; protected set; }
	}

	public class HtmlFileInfo : FileInfo
	{
		public HtmlFileInfo()
		{
			MediaType = "application/xhtml+xml";
		}

		private XmlDocument document;
		public XmlDocument Document
		{
			get { return document; }
			set
			{
				document = value;
				var rootElement = document.DocumentElement;

				Nsmgr = new XmlNamespaceManager(document.NameTable);
				Nsmgr.AddNamespace("ns", rootElement.NamespaceURI);

				// Invalid XHTML
				var hrs = rootElement.SelectNodes("//ns:hr", Nsmgr);
				foreach (XmlNode hr in hrs)
				{
					((XmlElement)hr).RemoveAttribute("width");
				}

				// Linebreaks
				var lineBreak = rootElement.SelectSingleNode("//ns:br", Nsmgr);
				while (lineBreak != null)
				{
					var brContainer = lineBreak.ParentNode;
					int brCount = 0;
					foreach (var childNode in brContainer.ChildNodes)
					{
						var elt = childNode as XmlElement;
						if (elt != null && elt.LocalName == "br")
						{
							brCount++;
						}
					}
					if (brCount == brContainer.ChildNodes.Count)
					{
						var prevPara = brContainer.PreviousSibling as XmlElement;
						if (prevPara != null && prevPara.LocalName == "p")
						{
							prevPara.SetAttribute("class", prevPara.GetAttribute("class") + " br");
							brContainer.ParentNode.RemoveChild(brContainer);
						}
					}

					lineBreak = brContainer.SelectSingleNode("following::ns:br", Nsmgr);
				}
			}
		}

		public XmlNamespaceManager Nsmgr;
	}

	public class BookInfo
	{
		public readonly List<FileInfo> Files = new List<FileInfo>();
		private XmlNamespaceManager tocNsmgr;
		private XmlDocument toc;
		private string destinationName;
		private string ncxFileName;
		private int fileCounter;

		public string Source { get; set; }
		public string RealSource { get; set;  }
		public string Destination { get; set; }

		public SourceType SourceType = SourceType.Unknown;

		private string author;
		public string Author
		{
			get { return author; }
			set
			{
				author = value;
				SetMetadata("dc:creator", author);
			}
		}

		private string title;
		public string Title
		{
			get { return title; }
			set
			{
				title = value;
				SetMetadata("dc:title", title);
			}
		}

		public XmlDocument Toc
		{
			get { return toc; }
			set
			{
				toc = value;
				tocNsmgr = new XmlNamespaceManager(toc.NameTable);
				tocNsmgr.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
				tocNsmgr.AddNamespace("opf", "http://www.idpf.org/2007/opf");
			}
		}

		public string DestinationName
		{
			get { return destinationName; }
			set
			{
				destinationName = value;
				var body = (XmlElement) toc.SelectSingleNode("//opf:item[@id='body']", tocNsmgr);
				body.SetAttribute("href", destinationName);
			}
		}

		public string NcxFileName
		{
			get { return ncxFileName; }
			set
			{
				ncxFileName = value;
				var ncx = (XmlElement)toc.SelectSingleNode("//opf:item[@id='ncx']", tocNsmgr);
				ncx.SetAttribute("href", destinationName);
			}
		}

		public void AddFile(FileInfo fileInfo)
		{
			Files.Add(fileInfo);

			var manifestNode = toc.SelectSingleNode("//opf:manifest", tocNsmgr);
			var imageItem = (XmlElement) toc.CreateNode(XmlNodeType.Element, "item", tocNsmgr.LookupNamespace("opf"));
			manifestNode.AppendChild(imageItem);
			imageItem.SetAttribute("href", fileInfo.FilePath);
			imageItem.SetAttribute("id", string.Format("file{0}", ++fileCounter));
			imageItem.SetAttribute("media-type", fileInfo.MediaType);
		}

		public List<FileInfo> GetFilesByMime(string mediaType)
		{
			return Files.FindAll(x => x.MediaType == mediaType);
		}

		#region Auxiliary functions

		private void SetMetadata(string attribute, string value)
		{
			var parentNode = toc.SelectSingleNode("//opf:metadata", tocNsmgr);
			var node = parentNode.SelectSingleNode("//" + attribute, tocNsmgr);
			if (node == null)
			{
				node = toc.CreateNode(XmlNodeType.Element, attribute, tocNsmgr.LookupNamespace("dc"));
				parentNode.AppendChild(node);
			}

			var textNode = node.SelectSingleNode("child::text()");
			if (textNode != null)
			{
				node.RemoveChild(textNode);
			}
			node.AppendChild(toc.CreateTextNode(value));
		}

		#endregion

		public void CopyImage(string startPath, string endPath)
		{
			foreach (var file in Files)
			{
				var document = file as HtmlFileInfo;
				if (document == null)
					continue;

				var nodeList = document.Document.SelectNodes("//ns:img", document.Nsmgr);
				// TODO:
			}
		}
	}
}
