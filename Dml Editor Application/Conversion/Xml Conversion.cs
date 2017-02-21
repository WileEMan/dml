using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using Microsoft.Win32;
using WileyBlack.Dml;
using WileyBlack.Dml.Dom;

namespace Dml_Editor.Conversion
{
    public class XmlToDml
    {
        DmlDocument ret = new DmlDocument();
        List<DmlString> TextNodes = new List<DmlString>();

        public XmlToDml()
        {
        }

        public static DmlDocument ImportXml(string Path, IResourceResolution Resources, List<IDmlReaderExtension> Extensions = null)
        {
            using (FileStream fs = new FileStream(Path, FileMode.Open, FileAccess.Read))
                return ImportXml(fs, Resources, Extensions);
        }

        public static DmlDocument ImportXml(Stream stream, IResourceResolution Resources, List<IDmlReaderExtension> Extensions = null)
        {
            XmlToDml info = new XmlToDml();

            // Setup an XmlParserContext so that we can pre-define the DML namespace...            
            NameTable nt = new NameTable();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(nt);
            nsmgr.AddNamespace("DML", DmlTranslation.DML3.urn);            
            XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.None);

            // Set XmlReaderSettings so that multiple top-level elements are allowed (permits DML:Header)...
            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ConformanceLevel = ConformanceLevel.Fragment;   // Permit multiple top-level elements (for DML:Header).
            bool GotHeader = false;
            bool GotTopLevel = false;

            using (XmlReader reader = XmlReader.Create(stream, xrs, context))
            {
                try
                {
                    while (reader.Read())
                    {
                        Association assoc;
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                assoc = info.GetAssociation(reader.Name, info.ret);
                                if (assoc == null) info.ret.Name = reader.Name;
                                else info.ret.Association = assoc;
                                if (assoc == DmlTranslation.DML3.Header)
                                {
                                    info.ret.Header.LoadFromXml(reader);
                                    info.OnHeaderParsed(Resources, Extensions);
                                    if (GotHeader) throw new Exception("Duplicate DML:Header detected.");
                                    GotHeader = true;
                                }
                                else
                                {
                                    if (GotHeader == false && reader.Name == "DML:Translation")
                                    {
                                        // No header given, but it looks like a translation document.  Let's make the assumption.
                                        info.ret.GlobalTranslation = DmlTranslation.DML3.Clone();
                                        info.ret.GlobalTranslation.Add(DmlTranslation.TSL2);
                                        info.ret.Header.FromTranslation(info.ret.GlobalTranslation);
                                        assoc = info.ret.GlobalTranslation[TSL2Translation.idDMLTranslation];
                                        info.ret.Association = assoc;
                                    }
                                    if (GotTopLevel) throw new Exception("Multiple top-level containers are not allowed.");
                                    GotTopLevel = true;
                                    info.ImportXmlContainer(reader, info.ret);
                                }
                                break;
                            case XmlNodeType.XmlDeclaration:
                            case XmlNodeType.ProcessingInstruction:
                                break;
                            case XmlNodeType.Comment:
                                // Check if the <DML:Header> directive is embedded in the XML comment...
                                if (reader.Value is string)
                                {
                                    string CommentValue = (string)reader.Value.Trim();
                                    if (CommentValue.Contains("<DML:Header"))
                                    {
                                        if (GotHeader) throw new Exception("Duplicate DML:Header detected.");
                                        GotHeader = true;
                                        int iAt = CommentValue.IndexOf("<DML:Header");
                                        string HeaderStr = CommentValue.Substring(iAt);
                                        info.ParseHeaderFromXmlString(info.ret, HeaderStr);
                                        info.OnHeaderParsed(Resources, Extensions);
                                    }
                                }
                                break;
                            case XmlNodeType.Whitespace:
                            case XmlNodeType.SignificantWhitespace:
                                break;
                            case XmlNodeType.Text:
                                throw new FormatException("Text node cannot be outside top-level XML element.");
                            default:
                                throw new FormatException("Unrecognized XML structure outside top-level.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message +
                        "\nwhile reading top-level xml",
                        ex);
                }
            }

            info.Optimize();
            return info.ret;
        }

        private static DmlDocument ImportXmlRoot(Stream stream, IResourceResolution Resources)
        {
            // Similar to ImportXml(), but only permits a single top-level element.  This variation
            // is used when importing the DML:Header during ImportXml().

            XmlToDml info = new XmlToDml();

            // Setup an XmlParserContext so that we can pre-define the DML namespace...            
            NameTable nt = new NameTable();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(nt);
            nsmgr.AddNamespace("DML", DmlTranslation.DML3.urn);
            XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.None);

            // Set XmlReaderSettings so that multiple top-level elements are allowed (permits DML:Header)...
            XmlReaderSettings xrs = new XmlReaderSettings();            

            using (XmlReader reader = XmlReader.Create(stream, xrs, context))
            {
                try
                {
                    while (reader.Read())
                    {
                        Association assoc;
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                assoc = info.GetAssociation(reader.Name, info.ret);
                                if (assoc == null) info.ret.Name = reader.Name;
                                else info.ret.Association = assoc;
                                info.ImportXmlContainer(reader, info.ret);
                                break;
                            case XmlNodeType.XmlDeclaration:
                            case XmlNodeType.ProcessingInstruction:
                                break;
                            case XmlNodeType.Comment:
                                break;
                            case XmlNodeType.Whitespace:
                            case XmlNodeType.SignificantWhitespace:
                                break;
                            case XmlNodeType.Text:
                                throw new FormatException("Text node cannot be outside top-level XML element.");
                            default:
                                throw new FormatException("Unrecognized XML structure outside top-level.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message +
                        "\nwhile reading top-level xml",
                        ex);
                }
            }

            info.Optimize();
            return info.ret;
        }

        void OnHeaderParsed(IResourceResolution Resources, List<IDmlReaderExtension> Extensions = null)
        {            
            // Identify or parse translation if necessary/possible          
            ParsingOptions po = null;
            if (Extensions != null)
            {                
                po = new ParsingOptions();
                foreach (IDmlReaderExtension ext in Extensions) po.AddExtension(ext);
            }
            DomResolvedTranslation rt = ret.Header.ToTranslation(Resources, po) as DomResolvedTranslation;
            if (rt == null) throw new FormatException("Expected DOM resolved translation.");
            ret.ResolvedHeader.Translation = rt.Translation;
            ret.ResolvedHeader.PrimitiveSets = rt.PrimitiveSets;
            ret.GlobalTranslation = rt.Translation;
        }

        DmlPrimitive TryImportPrimitive(string Name, string Value, DmlContainer Context)
        {
            Association assoc = GetAssociation(Name, Context);
            bool SuccessGoingSpecific = false;
            if (assoc != null)
            {
                switch (assoc.DMLName.PrimitiveType)
                {
                    case PrimitiveTypes.Boolean:
                        {
                            DmlBool attr = new DmlBool(Context.Document);
                            attr.Association = assoc;
                            bool bVal;
                            SuccessGoingSpecific = bool.TryParse(Value, out bVal);
                            if (SuccessGoingSpecific)
                            {
                                attr.Value = bVal;
                                return attr;
                            }
                            return null;
                        }

                    case PrimitiveTypes.DateTime:
                        {
                            DmlDateTime attr = new DmlDateTime(Context.Document);
                            attr.Association = assoc;
                            DateTime Val;
                            SuccessGoingSpecific = DateTime.TryParse(Value, out Val);
                            if (SuccessGoingSpecific)
                            {
                                attr.Value = Val;
                                return attr;
                            }
                            return null;
                        }

                    case PrimitiveTypes.Double:
                        {
                            DmlDouble attr = new DmlDouble(Context.Document);
                            attr.Association = assoc;
                            double Val;
                            SuccessGoingSpecific = double.TryParse(Value, out Val);
                            if (SuccessGoingSpecific)
                            {
                                attr.Value = Val;
                                return attr;
                            }
                            return null;
                        }

                    case PrimitiveTypes.Single:
                        {
                            DmlSingle attr = new DmlSingle(Context.Document);
                            attr.Association = assoc;
                            float Val;
                            SuccessGoingSpecific = float.TryParse(Value, out Val);
                            if (SuccessGoingSpecific)
                            {
                                attr.Value = Val;
                                return attr;
                            }
                            return null;
                        }

                    case PrimitiveTypes.Int:
                        {
                            DmlInt attr = new DmlInt(Context.Document);
                            attr.Association = assoc;
                            long Val;
                            SuccessGoingSpecific = long.TryParse(Value, out Val);
                            if (SuccessGoingSpecific)
                            {
                                attr.Value = Val;
                                return attr;
                            }
                            return null;
                        }

                    case PrimitiveTypes.UInt:
                        {
                            DmlUInt attr = new DmlUInt(Context.Document);
                            attr.Association = assoc;
                            ulong Val;
                            SuccessGoingSpecific = ulong.TryParse(Value, out Val);
                            if (SuccessGoingSpecific)
                            {
                                attr.Value = Val;
                                return attr;
                            }
                            return null;
                        }

                    case PrimitiveTypes.String:
                        {
                            DmlString attr = new DmlString(Context.Document);
                            attr.Association = assoc;
                            attr.Value = Value;
                            return attr;
                        }
                }
            }
            return null;
        }

        void ImportXmlContainer(XmlReader reader, DmlContainer container)
        {
            try
            {
                // Read attributes...
                for (int ii = 0; ii < reader.AttributeCount; ii++)
                {
                    reader.MoveToAttribute(ii);
                    
                    // Filter out XML-Specific attributes that should not be carried into DML...
                    if (reader.Prefix.ToLower() == "xmlns") continue;

                    DmlPrimitive Prim = TryImportPrimitive(reader.Name, reader.Value, container);
                    if (Prim != null)
                        container.Attributes.Add(Prim);
                    else                    
                    {
                        DmlString Text = new DmlString(container.Document);
                        Text.Name = reader.Name;
                        Text.Value = reader.Value;
                        container.Attributes.Add(Text);
                    }
                }
                reader.MoveToElement();
                if (reader.IsEmptyElement) return;

                // Read children (including elements)...
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            {
                                DmlContainer child = new DmlContainer(container.Document);
                                Association assoc = GetAssociation(reader.Name, container);
                                if (assoc != null) child.Association = assoc;
                                else child.Name = reader.Name;
                                ImportXmlContainer(reader, child);
                                container.Children.Add(child);
                                break;
                            }
                        case XmlNodeType.SignificantWhitespace:
                            {
                                DmlString Text = new DmlString(container.Document);
                                Text.Name = "Text";
                                Text.Value = reader.Value;
                                TextNodes.Add(Text);
                                container.Children.Add(Text);
                                break;
                            }
                        case XmlNodeType.CDATA:
                        case XmlNodeType.Text:
                            {
                                // Text nodes can experience significant alteration
                                // in the optimization pass.
                                DmlString Text = new DmlString(container.Document);
                                Text.Name = "Text";
                                Text.Value = reader.Value;
                                TextNodes.Add(Text);
                                container.Children.Add(Text);
                                break;
                            }
                        case XmlNodeType.XmlDeclaration:
                        case XmlNodeType.ProcessingInstruction:
                            break;
                        case XmlNodeType.Comment:
                            {
                                DmlComment node = new DmlComment(container.Document);
                                node.Text = reader.Value;
                                ret.Children.Add(node);
                                break;
                            }
                        case XmlNodeType.EndElement:
                            return;
                        case XmlNodeType.Attribute:
                            {
                                DmlString Text = new DmlString(container.Document);
                                Text.Name = reader.Name;
                                Text.Value = reader.Value;
                                container.Attributes.Add(Text);
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message +
                    "\nwhile reading xml <" + container.Name + ">",
                    ex);
            }
        }

        /// <summary>
        /// Returns an association matching the given name from xml, if
        /// one can be found with certainty.  If multiple matches are
        /// possible for the name, null is returned and inline identification
        /// can be used instead.
        /// </summary>
        /// <param name="Name">Name from xml to match in namespace tree.</param>
        /// <param name="Container">Context where possible association applies.</param>
        /// <returns>The matching association if one was definitively found.  Null
        /// otherwise.</returns>
        Association GetAssociation(string Name, DmlContainer Context)
        {
            // First, locate the deepest local translation, if any...
            if (Context == null) return null;
            DmlTranslation TryTSL = Context.Association.LocalTranslation;
            while (TryTSL == null)
            {
                Context = Context.Container as DmlContainer;
                if (Context == null) TryTSL = ret.GlobalTranslation;
                else TryTSL = Context.Association.LocalTranslation;
            }

            for (; ; )
            {
                // Next, check the translation for possible matches and count
                // them.  There is a subtly here - Dml allows multiple
                // uses of the same Name+Type pair, but Xml does not define
                // type.  As is the most common case, when only one use of 
                // the Name is defined, this is simple.  When there are
                // multiple definitions for Name, we have a problem - we
                // can't distinguish so we use inline identification instead.

                int Matches = 0;
                foreach (Association Defn in TryTSL)
                {
                    if (Defn.DMLName.XmlName.ToLowerInvariant() == Name.ToLowerInvariant()) Matches++;
                }
                if (Matches > 1) return null;
                if (Matches == 1)
                {
                    foreach (Association Defn in TryTSL)
                    {
                        if (Defn.DMLName.XmlName.ToLowerInvariant() == Name.ToLowerInvariant()) return Defn;
                    }
                    throw new Exception();
                }

                // No matches - before we quit, climb the translation tree...
                TryTSL = TryTSL.ParentTranslation;
                if (TryTSL == null) return null;
            }
        }

        bool IsAdjacent(DmlNode a, DmlNode b)
        {
            if (a.Container != b.Container) return false;
            DmlFragment Container = a.Container;
            int iA = Container.Children.IndexOf(a);
            int iB = Container.Children.IndexOf(b);
            if (iA < 0 || iB < 0) return false;
            return (iA == iB - 1);
        }

        void Optimize()
        {
            try
            {
                // Step 1: Merge any adjacent text nodes.
                for (int ii = 0; ii < TextNodes.Count - 1; )
                {
                    if (IsAdjacent(TextNodes[ii], TextNodes[ii + 1]))
                    {
                        DmlFragment Container = TextNodes[ii].Container;
                        TextNodes[ii].Value = (string)TextNodes[ii].Value + (string)TextNodes[ii + 1].Value;
                        Container.Children.Remove(TextNodes[ii + 1]);
                        TextNodes.RemoveAt(ii + 1);
                    }
                    else ii++;
                }

                // Step 2: Check for any text nodes which can be promoted to their parent container,
                // converting the parent container into a string primitive element instead.                

                // Step 2B: Also, now that we have a Name:Value pair setup, check if we can convert
                // into something more specific than a string primitive - with an association.
                for (int ii = 0; ii < TextNodes.Count; )
                {
                    DmlFragment Container = TextNodes[ii].Container;
                    if (Container.Children.Count == 1)
                    {
                        // Locate index and detach Container from its parent...
                        DmlFragment UpperContainer = Container.Container;
                        if (UpperContainer == null) { ii++; continue; }
                        int iUpper = UpperContainer.Children.IndexOf(Container);
                        if (iUpper < 0) { ii++; continue; }
                        UpperContainer.Children.RemoveAt(iUpper);

                        // Build the new primitive, and attach where Container once was...
                        DmlPrimitive Prim = null;
                        if (UpperContainer is DmlContainer)
                            Prim = TryImportPrimitive(Container.Name, (string)TextNodes[ii].Value, (DmlContainer)UpperContainer);
                        if (Prim == null)
                        {
                            // Build a new string element...
                            Prim = new DmlString(UpperContainer.Document);
                            Prim.Name = Container.Name;
                            Prim.Value = TextNodes[ii].Value;
                        }

                        // Attach new string element where Container once was...
                        UpperContainer.Children.Insert(iUpper, Prim);

                        // Remove the entry from the TextNodes list.
                        TextNodes.RemoveAt(ii);
                    }
                    else ii++;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\nwhile optimizing xml->dml read.", ex);
            }
        }

        private void ParseHeaderFromXmlString(DmlDocument Doc, string XmlString)
        {
            /** First, convert the string to a stream **/
            byte[] AsStream = Encoding.UTF8.GetBytes(XmlString);            
            using (MemoryStream ms = new MemoryStream(AsStream))
            {
                ms.Position = 0;
                /** Next, convert the xml stream to a DmlDocument **/
                Doc.Header.LoadFromXml(ms);
            }
        }

        void CopyXmlElement(XmlReader from, XmlWriter to)
        {
            // Copy attributes...
            to.WriteStartElement(from.Prefix, from.LocalName, from.NamespaceURI);
            to.WriteAttributes(from, false);
            from.MoveToElement();
            if (from.IsEmptyElement) 
            {
                to.WriteEndElement();
                return;
            }

            // Copy elements...
            while (from.Read())
            {
                switch (from.NodeType)
                {
                    case XmlNodeType.Element:
                        {
                            CopyXmlElement(from, to);                                
                            break;
                        }
                    case XmlNodeType.SignificantWhitespace: break;                            

                    case XmlNodeType.CDATA:
                    case XmlNodeType.Text: to.WriteString(from.ReadString()); break;
                            
                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.ProcessingInstruction:
                        break;
                    case XmlNodeType.Comment:
                        break;
                    case XmlNodeType.EndElement:
                        to.WriteEndElement();
                        return;
                    case XmlNodeType.Attribute:
                        throw new Exception("Expected attributes to be already copied.");
                }
            }            
        }

        private DmlHeader ParseHeaderFromXmlElement(XmlReader reader)
        {
            /** Transfer just the current Xml Element into its own
             *  separate stream... **/
            DmlDocument tempDoc;
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter xw = XmlWriter.Create(ms))            
                    CopyXmlElement(reader, xw);
                ms.Position = 0;
                /** Next, convert the xml stream to a DmlDocument **/
                tempDoc = ImportXmlRoot(ms, null);
            }
            if (tempDoc.Name != "DML:Header") throw new Exception("Expected DML:Header.");
            return DmlHeader.CopyFrom(ret, tempDoc);
        }
    }

    public class DmlToXml
    {
        public static void SaveToXml(DmlDocument doc, string Path, DmlContainer XmlRoot = null)
        {
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.Indent = true;

            using (FileStream fs = new FileStream(Path, FileMode.Create, FileAccess.ReadWrite))
            using (XmlWriter writer = XmlWriter.Create(fs, xws))
            {
                writer.WriteStartDocument();

                // XML does not support 2 top-level nodes, and won't recognize the DML:Header.  We write
                // it as a comment.
                using (MemoryStream ms = new MemoryStream())
                {
                    XmlWriterSettings xwsHeader = new XmlWriterSettings();
                    xwsHeader.Indent = true;
                    xwsHeader.OmitXmlDeclaration = true;
                    using (XmlWriter hwriter = XmlWriter.Create(ms, xwsHeader))
                    {
                        hwriter.WriteStartDocument();
                        SaveToXml(hwriter, doc.Header);
                        hwriter.WriteEndDocument();
                    }
                    ms.Position = 0;
                    using (StreamReader sr = new StreamReader(ms))
                        writer.WriteComment(sr.ReadToEnd());
                }

                // Start the top-level container...
                writer.WriteStartElement(doc.Name);

                DmlContainer MergedXmlRoot = new DmlContainer();
                if (doc.ResolvedHeader is DomResolvedTranslation) MergedXmlRoot.Merge(((DomResolvedTranslation)doc.ResolvedHeader).XmlRoot);
                if (XmlRoot != null) MergedXmlRoot.Merge(XmlRoot);

                try
                {
                    foreach (DmlPrimitive Prim in MergedXmlRoot.Attributes)
                        WriteTo(writer, Prim);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message +
                        "\nwhile writing XmlRoot attributes on <" + doc.Name + ">", ex);
                }

                try
                {
                    foreach (DmlPrimitive Prim in doc.Attributes)
                        WriteTo(writer, Prim);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message +
                        "\nwhile writing <" + doc.Name + ">", ex);
                }

                try
                {                    
                    foreach (DmlNode Node in MergedXmlRoot.Children)
                    {
                        if (Node is DmlContainer) SaveToXml(writer, (DmlContainer)Node);
                        else if (Node is DmlPrimitive) WriteTo(writer, (DmlPrimitive)Node);
                        else if (Node is DmlComment) writer.WriteComment(((DmlComment)Node).Text);
                        else throw new NotSupportedException();
                    }                    
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message +
                        "\nwhile writing XmlRoot elements on <" + doc.Name + ">", ex);
                }

                try
                {
                    foreach (DmlNode Node in doc.Children)
                    {
                        if (Node is DmlContainer) SaveToXml(writer, (DmlContainer)Node);
                        else if (Node is DmlPrimitive) WriteTo(writer, (DmlPrimitive)Node);
                        else if (Node is DmlComment) writer.WriteComment(((DmlComment)Node).Text);
                        else throw new NotSupportedException();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message +
                        "\nwhile writing <" + doc.Name + ">", ex);
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }        

        static void SaveToXml(XmlWriter writer, DmlContainer Container)
        {
            WriteStartElement(writer, Container.Name);

            try
            {
                foreach (DmlPrimitive Prim in Container.Attributes)
                    WriteTo(writer, Prim);

                foreach (DmlNode Node in Container.Children)
                {
                    if (Node is DmlContainer) SaveToXml(writer, (DmlContainer)Node);
                    else if (Node is DmlPrimitive) WriteTo(writer, (DmlPrimitive)Node);
                    else if (Node is DmlComment) writer.WriteComment(((DmlComment)Node).Text);
                    else throw new NotSupportedException();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message +
                    "\nwhile writing <" + Container.Name + ">", ex);
            }

            writer.WriteEndElement();
        }

        /*
         * static void AddXmlRoot(XmlWriter writer)
        {
        }
         */

        static void WriteTo(XmlWriter writer, DmlPrimitive Primitive)
        {
            string Name = Primitive.Name;
            string Value;

            if (Primitive is DmlString) Value = ((DmlString)Primitive).Value.ToString();
            else if (Primitive is DmlInt) Value = ((DmlInt)Primitive).Value.ToString();
            else if (Primitive is DmlUInt) Value = ((DmlUInt)Primitive).Value.ToString();
            else if (Primitive is DmlBool) Value = ((DmlBool)Primitive).Value.ToString();
            else if (Primitive is DmlSingle) Value = ((DmlSingle)Primitive).Value.ToString();
            else if (Primitive is DmlDouble) Value = ((DmlDouble)Primitive).Value.ToString();
            else if (Primitive is DmlDateTime) Value = ((DmlDateTime)Primitive).Value.ToString();
            else if (Primitive is DmlArray) { WriteTo(writer, (DmlArray)Primitive); return; }
            else if (Primitive is DmlMatrix) { WriteTo(writer, (DmlMatrix)Primitive); return; }
            else throw new NotSupportedException("Unrecognized primitive type in DML document.");
            
            if (Primitive.IsAttribute) WriteAttributeString(writer, Name, Value.ToString());
            else WriteElementString(writer, Name, Value.ToString());
        }

        static void WriteAttributeString(XmlWriter writer, string Name, string Value)
        {
            int iSep = Name.IndexOf(':');
            if (iSep < 0) { writer.WriteAttributeString(Name, Value); return; }
            string prefix = Name.Substring(0, iSep);
            string localName = Name.Substring(iSep + 1);
            writer.WriteAttributeString(prefix, localName, null, Value);
        }

        static void WriteElementString(XmlWriter writer, string Name, string Value)
        {
            int iSep = Name.IndexOf(':');
            if (iSep < 0) { writer.WriteElementString(Name, Value); return; }
            string prefix = Name.Substring(0, iSep);
            string localName = Name.Substring(iSep + 1);
            writer.WriteElementString(prefix, localName, null, Value);
        }

        static void WriteStartElement(XmlWriter writer, string Name)
        {
            int iSep = Name.IndexOf(':');
            if (iSep < 0) { writer.WriteStartElement(Name); return; }
            string prefix = Name.Substring(0, iSep);
            string localName = Name.Substring(iSep + 1);
            if (prefix == "DML")
                writer.WriteStartElement(prefix, localName, "urn:dml:dml2");
            else
                writer.WriteStartElement(prefix, localName, "urn:ignore");
        }

        static void WriteTo(XmlWriter writer, DmlArray Array)
        {
            if (Array.IsAttribute) WriteAttributeString(writer, Array.Name, "[Array]");
            else WriteElementString(writer, Array.Name, "[Array]");
        }

        static void WriteTo(XmlWriter writer, DmlMatrix Matrix)
        {
            if (Matrix.IsAttribute) WriteAttributeString(writer, Matrix.Name, "[Matrix]");
            else WriteElementString(writer, Matrix.Name, "[Matrix]");
        }
    }
}
