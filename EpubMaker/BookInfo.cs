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
        public string Id;

        public XmlElement ContentNode;
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
		private XmlNamespaceManager contentNsmgr;
		private XmlDocument content;
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

		public XmlDocument Content
		{
			get { return content; }
			set
			{
				content = value;
				contentNsmgr = new XmlNamespaceManager(content.NameTable);
				contentNsmgr.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
				contentNsmgr.AddNamespace("opf", "http://www.idpf.org/2007/opf");
			}
		}

		public string DestinationName
		{
			get { return destinationName; }
			set
			{
				destinationName = value;
				var body = (XmlElement) content.SelectSingleNode("//opf:item[@id='body']", contentNsmgr);
				body.SetAttribute("href", destinationName);
			}
		}

		public string NcxFileName
		{
			get { return ncxFileName; }
			set
			{
				ncxFileName = value;
				var ncx = (XmlElement)content.SelectSingleNode("//opf:item[@id='ncx']", contentNsmgr);
				ncx.SetAttribute("href", destinationName);
			}
		}

		public void AddFile(FileInfo fileInfo)
		{
			Files.Add(fileInfo);
            fileInfo.Id = string.Format("file{0}", ++fileCounter);

			var manifestNode = content.SelectSingleNode("//opf:manifest", contentNsmgr);
			var imageItem = (XmlElement) content.CreateNode(XmlNodeType.Element, "item", contentNsmgr.LookupNamespace("opf"));
			manifestNode.AppendChild(imageItem);
			imageItem.SetAttribute("href", fileInfo.NewPath);
			imageItem.SetAttribute("id", fileInfo.Id);
			imageItem.SetAttribute("media-type", fileInfo.MediaType);

            fileInfo.ContentNode = imageItem;
		}

        public void AddSpineEntry(FileInfo fileInfo)
        {
            var spine = content.SelectSingleNode("//opf:spine", contentNsmgr);
            var spineEntry = (XmlElement)content.CreateNode(XmlNodeType.Element, "itemref", contentNsmgr.LookupNamespace("opf"));
            spine.AppendChild(spineEntry);

            spineEntry.SetAttribute("idref", fileInfo.Id);
        }

		public List<FileInfo> GetFilesByMime(string mediaType)
		{
			return Files.FindAll(x => x.MediaType == mediaType);
		}

		#region Auxiliary functions

		private void SetMetadata(string attribute, string value)
		{
			var parentNode = content.SelectSingleNode("//opf:metadata", contentNsmgr);
			var node = parentNode.SelectSingleNode("//" + attribute, contentNsmgr);
			if (node == null)
			{
				node = content.CreateNode(XmlNodeType.Element, attribute, contentNsmgr.LookupNamespace("dc"));
				parentNode.AppendChild(node);
			}

			var textNode = node.SelectSingleNode("child::text()");
			if (textNode != null)
			{
				node.RemoveChild(textNode);
			}
			node.AppendChild(content.CreateTextNode(value));
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
