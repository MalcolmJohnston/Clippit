﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;

namespace Clippit
{
    public class PtMainDocumentPart : XElement
    {
        private WmlDocument ParentWmlDocument;

        public PtWordprocessingCommentsPart WordprocessingCommentsPart
        {
            get
            {
                using var ms = new MemoryStream(ParentWmlDocument.DocumentByteArray);
                using var wDoc = WordprocessingDocument.Open(ms, false);
                var commentsPart = wDoc.MainDocumentPart.WordprocessingCommentsPart;
                if (commentsPart == null)
                    return null;
                var partElement = commentsPart.GetXDocument().Root;
                var childNodes = partElement.Nodes().ToList();
                foreach (var item in childNodes)
                    item.Remove();
                return new PtWordprocessingCommentsPart(this.ParentWmlDocument, commentsPart.Uri, partElement.Name, partElement.Attributes(), childNodes);
            }
        }

        public PtMainDocumentPart(WmlDocument wmlDocument, Uri uri, XName name, params object[] values)
            : base(name, values)
        {
            ParentWmlDocument = wmlDocument;
            this.Add(
                new XAttribute(PtOpenXml.Uri, uri),
                new XAttribute(XNamespace.Xmlns + "pt", PtOpenXml.pt)
            );
        }
    }

    public class PtWordprocessingCommentsPart : XElement
    {
        private WmlDocument ParentWmlDocument;

        public PtWordprocessingCommentsPart(WmlDocument wmlDocument, Uri uri, XName name, params object[] values)
            : base(name, values)
        {
            ParentWmlDocument = wmlDocument;
            this.Add(
                new XAttribute(PtOpenXml.Uri, uri),
                new XAttribute(XNamespace.Xmlns + "pt", PtOpenXml.pt)
            );
        }
    }

    public partial class WmlDocument
    {
        public PtMainDocumentPart MainDocumentPart
        {
            get
            {
                using var ms = new MemoryStream(this.DocumentByteArray);
                using var wDoc = WordprocessingDocument.Open(ms, false);
                var partElement = wDoc.MainDocumentPart.GetXDocument().Root;
                var childNodes = partElement.Nodes().ToList();
                foreach (var item in childNodes)
                    item.Remove();
                return new PtMainDocumentPart(this, wDoc.MainDocumentPart.Uri, partElement.Name, partElement.Attributes(), childNodes);
            }
        }

        public WmlDocument(WmlDocument other, params XElement[] replacementParts)
            : base(other)
        {
            using var streamDoc = new OpenXmlMemoryStreamDocument(this);
            using (var package = streamDoc.GetPackage())
            {
                foreach (var replacementPart in replacementParts)
                {
                    var uriAttribute = replacementPart.Attribute(PtOpenXml.Uri);
                    if (uriAttribute == null)
                        throw new OpenXmlPowerToolsException("Replacement part does not contain a Uri as an attribute");
                    var uri = uriAttribute.Value;
                    var part = package.GetParts().FirstOrDefault(p => p.Uri.ToString() == uri);
                    using var partStream = part.GetStream(FileMode.Create, FileAccess.Write);
                    using var partXmlWriter = XmlWriter.Create(partStream);
                    replacementPart.Save(partXmlWriter);
                }
            }
            this.DocumentByteArray = streamDoc.GetModifiedDocument().DocumentByteArray;
        }
    }
}
