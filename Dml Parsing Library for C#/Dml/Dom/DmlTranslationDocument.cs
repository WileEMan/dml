/***
    Copyright (c) 2012 by Wiley Black
    All rights reserved.

    This source code is made available under a custom license that I’m calling Open/Reference.  It is similar 
    to a LGPL with the following major exceptions:
        o	Binary-only uses do not require acknowledgment or credit (it is appreciated, however).
        o	Tivoization is permitted as long as it does not violate other rights herein.
        o	There are limitations on distribution of modified source code designed to protect the original 
            author and to encourage changes to be routed through the original author.

    Redistribution and use in binary forms, with or without modification, are permitted provided that the 
    following conditions are met:
        o   Agreement is accepted that omitting acknowledgment or copyright mention cannot be construed as a 
            valid claim to the rights or ownership protected herein.

    Public redistribution in source form with modification is not permitted.  

    Distribution and use in source form with modification within an organization and its immediate identified 
    partners are permitted provided the following conditions are met.  Distribution and use in source form with 
    modification is permitted between recognized private partners provided the following conditions are met.  
    Public or private redistribution and use in source form without modification is permitted provided the following 
    conditions are met.    
        o   Redistributions of source code, with or without modification, must retain the above copyright notice, 
            this license, and the following disclaimer.
        o   Modified source code must contain at least a one line prefix or postfix to this notice stating that it is 
            a derived work and providing an Internet resource where the original may be obtained.  For example, 
            “Derived from the original…” and a URL is sufficient.

    Integration in larger works is considered a “use” and not a modification as long as no changes to the source were 
    made.  Binary forms include compiled forms such as object representation (including that suitable for static linkage) 
    and intermediate language representations.

    Relicensing (such as bundling) of modified source and any binary forms are permitted but requires equal or more 
    restrictive license terms and may not reassign ownership or rights of this source code.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
    INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
    DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
    SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
    SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
    WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE 
    USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.  
***/

using System;
using System.Data;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using WileyBlack.Dml;

namespace WileyBlack.Dml.Dom
{
    public class DomResolvedTranslation : ResolvedTranslation
    {
        /// <summary>
        /// XmlRoot is provided by DOM translation as long as all resources provide the information.  If
        /// a IResourceResolution provides a ResolvedTranslation object instead of a DomResolvedTranslation
        /// object, then XmlRoot will become null.  If correctly resolved, XmlRoot will provide a DmlContainer,
        /// even if empty.
        /// </summary>
        public DmlContainer XmlRoot;
    }

    public class DomPrimitiveSet : PrimitiveSet
    {
        public DmlFragment Configuration;

        public DomPrimitiveSet() { }
        public DomPrimitiveSet(string Set, string Codec = null, string CodecURI = null) : base(Set, Codec, CodecURI) { }
    }

    /// <summary>
    /// DomAssociation is a replacement for the Association class that is used when DOM is used
    /// to parse a header in detailed mode.  DomAssociation provides an identically functional
    /// Association object, but retains addition information that can be useful to programmatic
    /// header and translation document construction, analysis, or manipulation.
    /// </summary>
    public class DomAssociation : Association
    {
        public DmlDefinition OriginalDefinition;
        public List<DmlTranslationRenumber> Changes = new List<DmlTranslationRenumber>();

        #region "Constructors for associations in translation documents"

        public DomAssociation(uint DMLID, string XmlName, NodeTypes NodeType) : base(DMLID, XmlName, NodeType) { }        
        public DomAssociation(uint DMLID, string XmlName, PrimitiveTypes PrimitiveType) : base(DMLID, XmlName, PrimitiveType) { }
        public DomAssociation(uint DMLID, string XmlName, PrimitiveTypes PrimitiveType, ArrayTypes ArrayType) : base(DMLID, XmlName, PrimitiveType, ArrayType) { }
        public DomAssociation(uint DMLID, string XmlName, DmlTranslation LocalTranslation) : base(DMLID, XmlName, LocalTranslation) { }        
        public DomAssociation(uint DMLID, DmlName Name, DmlTranslation LocalTranslation) : base(DMLID, Name, LocalTranslation) { }
        public DomAssociation(uint DMLID, DmlName Name) : base(DMLID, Name) { }

        #endregion

        public override Association Clone()
        {
            DomAssociation ret = new DomAssociation(DMLID, DMLName.Clone(), LocalTranslation);
            ret.OriginalDefinition = OriginalDefinition;
            foreach (DmlTranslationRenumber rn in Changes) ret.Changes.Add(rn);
            return ret;
        }
    }

    /// <summary>
    /// ITranslationLanguage provides an interface to common functionality found in DmlHeader and
    /// DmlTranslationDocument.  Since both classes operate on the same translation language, there
    /// is common tools for accessing, parsing, analyzing, and translating these DML objects.
    /// </summary>
    public interface ITranslationLanguage
    {
        ResolvedTranslation ToTranslation(IResourceResolution References = null, ParsingOptions Options = null);
    }

    /// <summary>
    /// The DmlTranslationDocument can be used to read a DML translation document obeying the 
    /// translation v2 specification.  Call the static LoadTranslation() method to load and
    /// interpret a translation document to a DmlTranslation object.  Call Load() to read the 
    /// translation in its DOM form.  Once loaded, the ToTranslation() method can still be used 
    /// to interpret the translation document's DML into an in-memory DmlTranslation object.  
    /// 
    /// The DmlTranslationDocument can also be used to write a DML translation document, although
    /// it is usually a more elegant solution ton construct such a document by-hand.  To write a 
    /// DML translation document use the FromTranslation() method to populate the translation 
    /// information and call the Save() method.
    /// </summary>
    public class DmlTranslationDocument : DmlDocument, ITranslationLanguage
    {
        #region "Initialization & Control"

        public DmlTranslationDocument()
        {
            Association = TSL2Translation.DMLTranslationAssociation;
        }

        public static ResolvedTranslation LoadTranslation(string TranslationURI, IResourceResolution Resources, 
            ParsingOptions Options = null)
        {
            return LoadTranslation(TranslationURI, null, Resources, Options);
        }

        public static ResolvedTranslation LoadTranslation(string TranslationURI, string TranslationURN, IResourceResolution Resources, 
            ParsingOptions Options = null)
        {
            if (TranslationURI == DmlInternalData.urnBuiltinDML)
            {
                DomResolvedTranslation ret = new DomResolvedTranslation();
                ret.Translation = DmlTranslation.DML3.Clone();
                ret.PrimitiveSets = new List<PrimitiveSet>();
                ret.XmlRoot = null; 
                return ret;
            }
            else if (TranslationURI == WileyBlack.Dml.EC.EC2Translation.urn)
            {
                DomResolvedTranslation ret = new DomResolvedTranslation();
                ret.Translation = DmlTranslation.EC2.Clone();
                ret.PrimitiveSets = new List<PrimitiveSet>();
                ret.XmlRoot = null; 
                return ret;
            }
            else if (TranslationURI == TSL2Translation.urn)
            {
                DomResolvedTranslation ret = new DomResolvedTranslation();
                ret.Translation = DmlTranslation.TSL2.Clone();
                ret.PrimitiveSets = new List<PrimitiveSet>();
                ret.XmlRoot = null; 
                return ret;
            }
            else if (Resources == null) throw new Exception("Unable to retrieve DML Translation.");

            DmlTranslationDocument doc = new DmlTranslationDocument();
            bool IsXml;
            IDisposable Resource = Resources.Resolve(TranslationURI, out IsXml);            
            if (Resource is Stream)
            {
                try
                {
                    Stream Source = (Stream)Resource;
                    if (IsXml)
                        doc.LoadFromXml(Source);
                    else
                    {
                        using (DmlReader Reader = DmlReader.Create(Source))
                        {
                            if (Options != null) Reader.Options = Options;
                            doc.Load(Reader, Resources);
                        }
                    }
                    return doc.ToTranslation(Resources, Options);
                }
                finally
                {
                    Resource.Dispose();
                }
            }
            else if (Resource is ResolvedTranslation) return (ResolvedTranslation)Resource;
            else throw new NotSupportedException("Invalid resource object type received from IResourceResolution.");
        }

        #endregion

        #region "Properties"

        /// <summary>
        /// Retrieves or sets the Version value for this translation document.  This is the version of the
        /// translation language being used, not the DML version.  If a DML document does not contain this 
        /// attribute, then the default value is assumed.
        /// </summary>
        public uint Version
        {
            get
            {
                return Attributes.GetUInt(DmlTranslation.DML3.Version.DMLID, TSL2Translation.TSLVersion);
            }
            set
            {
                SetAttribute(DmlTranslation.DML3.Version.DMLID, value);
            }
        }

        /// <summary>
        /// Retrieves or sets the ReadVersion value for this translation document.  This is the version of the
        /// translation language being used, not the DML version.  If a DML document does not contain this 
        /// attribute, then the Version value is used.
        /// </summary>
        public uint ReadVersion
        {
            get
            {
                return Attributes.GetUInt(DmlTranslation.DML3.ReadVersion.DMLID, Version);
            }
            set
            {
                SetAttribute(DmlTranslation.DML3.ReadVersion.DMLID, value);
            }
        }

        #endregion

        #region "Xml Conversion"

        /// <summary>
        /// LoadArbitraryFromXml() is used when parsing DML:Include-Primitives directives that
        /// contain arbitrary xml elements for configuration.  The Xml is converted to Dml
        /// nodes.
        /// </summary>        
        internal static void LoadArbitraryFromXml(DmlContainer Into, XmlReader Reader)
        {
            while (Reader.Read())
            {
                switch (Reader.NodeType)
                {
                    case XmlNodeType.EndElement: return;

                    case XmlNodeType.Element:
                        {
                            Association Assoc = new Association(Reader.Name, NodeTypes.Container);
                            DmlContainer Container = Into.Document.CreateContainer(Assoc, Into);

                            for (int ii = 0; ii < Reader.AttributeCount; ii++)
                            {
                                Reader.MoveToAttribute(ii);
                                DmlPrimitive Attr = new DmlString(Into.Document, Reader.Value);
                                Attr.Name = Reader.Name;
                                Into.Attributes.Add(Attr);
                            }

                            if (!Reader.IsEmptyElement) LoadArbitraryFromXml(Container, Reader);

                            // Check if we just parsed a text node.  In DML, that would convert to a string primitive element.
                            if (Container.Attributes.Count == 0)
                            {
                                bool AllText = false;
                                StringBuilder sb = new StringBuilder();
                                for (int ii = 0; ii < Container.Children.Count; ii++)
                                {
                                    if (Container.Children[ii] is DmlString) { AllText = true; sb.Append(((DmlString)Container.Children[ii]).Value as string); }
                                    else { AllText = false; break; }
                                }
                                if (AllText)
                                {
                                    DmlString Replace = new DmlString(Into.Document, sb.ToString());
                                    Replace.Name = Assoc.DMLName.XmlName;
                                    Into.Children.Add(Replace);
                                }
                                else Into.Children.Add(Container);
                            }
                            else Into.Children.Add(Container);
                            continue;
                        }                    
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Text:
                        {
                            DmlString Text = new DmlString(Into.Document, Reader.Value);
                            Text.Name = "";
                            Into.Children.Add(Text);
                            continue;
                        }
                }
            }
        }

        internal static void LoadTSLFromXml(DmlContainer Into, XmlReader Reader)
        {
            while (Reader.Read())
            {
                if (Reader.NodeType == XmlNodeType.EndElement) return;
                if (Reader.NodeType != XmlNodeType.Element) continue;

                switch (Reader.Name.ToLower())
                {
                    case "node":
                        {
                            DmlNodeDefinition NewAssoc = new DmlNodeDefinition(Into);
                            Into.AddAttribute(TSL2Translation.idDMLID, UInt32.Parse(Reader.GetAttribute("id")));
                            Into.AddAttribute(TSL2Translation.idName, Reader.GetAttribute("name"));
                            Into.AddAttribute(TSL2Translation.idType, Reader.GetAttribute("type"));
                            Into.Children.Add(NewAssoc);
                            continue;
                        }

                    case "container":
                        {
                            DmlContainerDefinition NewAssoc = new DmlContainerDefinition(Into);
                            Into.AddAttribute(TSL2Translation.idDMLID, UInt32.Parse(Reader.GetAttribute("id")));
                            Into.AddAttribute(TSL2Translation.idName, Reader.GetAttribute("name"));
                            LoadTSLFromXml(NewAssoc, Reader);
                            Into.Children.Add(NewAssoc);
                            continue;
                        }

                    case "renumber":
                        {
                            DmlTranslationRenumber NewDirective = new DmlTranslationRenumber(Into);
                            NewDirective.FromDMLID = UInt32.Parse(Reader.GetAttribute("id"));
                            NewDirective.ToDMLID = UInt32.Parse(Reader.GetAttribute("new-id"));
                            Into.Children.Add(NewDirective);
                            continue;
                        }

                    case "dml:include-translation":
                    case "include-translation":
                        {                            
                            string NewTranslationURI = Reader.GetAttribute("URI");
                            string NewTranslationURN = Reader.GetAttribute("URN");                               
                            DmlIncludeTranslation NewInclude = new DmlIncludeTranslation(Into, NewTranslationURI, NewTranslationURN);
                            Into.Children.Add(NewInclude);
                            continue;
                        }

                    case "dml:include-primitives":
                    case "include-primitives":
                        {                            
                            string NewPrimitives = Reader.GetAttribute("Set");
                            if (NewPrimitives != null)
                            {
                                PrimitiveSet ps = new PrimitiveSet(NewPrimitives, Reader.GetAttribute("Codec"), Reader.GetAttribute("CodecURI"));
                                DmlIncludePrimitives Include = new DmlIncludePrimitives(Into, ps);
                                if (!Reader.IsEmptyElement) LoadArbitraryFromXml(Include, Reader);
                                Into.Children.Add(Include);
                            }

                            continue;                        
                        }

                    default:
                        throw new FormatException("Unrecognized directive in XML-based DML Translation document");
                }
            }
        }

        public void LoadFromXml(Stream Stream)
        {
            // Setup an XmlParserContext so that we can pre-define the DML namespace...            
            NameTable nt = new NameTable();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(nt);
            nsmgr.AddNamespace("DML", "urn:dml:dml3");
            XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.None);

            // Set XmlReaderSettings so that multiple top-level elements are allowed (permits an optional DML:Header)...
            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ConformanceLevel = ConformanceLevel.Fragment;   // Permit multiple top-level elements (for DML:Header).

            using (XmlReader Reader = XmlReader.Create(Stream, xrs, context))
            {
                // Find translation document top-level container            
                for (; ; )
                {
                    if (!Reader.Read()) throw new FormatException("Expected DML:Translation element.");
                    if (Reader.NodeType != XmlNodeType.Element) continue;
                    if (Reader.Name == "DML:Translation" || Reader.Name == "Translation")
                    {
                        if (Reader.IsEmptyElement) return;              // No content in the Translation.

                        // Parse translation document body
                        LoadTSLFromXml(this, Reader);
                        return;
                    }
                }
            }
        }

        #endregion

        #region "Reader interpretation support"

        public override DmlContainer CreateContainer(Association assoc, DmlFragment Context)
        {
            DmlContainer ret;
            switch (assoc.DMLID)
            {
                case TSL2Translation.idDMLIncludeTranslation: ret = new DmlIncludeTranslation(); break;
                case TSL2Translation.idDMLIncludePrimitives: ret = new DmlIncludePrimitives(); break;
                case TSL2Translation.idNode: ret = new DmlNodeDefinition(); break;
                case TSL2Translation.idContainer: ret = new DmlContainerDefinition(); break;
                case TSL2Translation.idRenumber: ret = new DmlTranslationRenumber(); break;
                default: return base.CreateContainer(assoc, Context);
            }
            ret.Document = this;
            ret.Container = Context;
            return ret;
        }

        #endregion

        #region "ToTranslation() interpreter"

        /// <summary>
        /// ReducePrimitiveSets() is called internally to resolve multiple instance of the same 
        /// primitive set with the last entry.  For example, consider a DML Header that includes 
        /// a DML Translation document utilizing the array primitive set but omitting a codec
        /// specification.  The DML Header specifies the necessary codec in a second
        /// &lt;DML:Include Primitives=... /&gt; directive.  This can result in two
        /// instances of the array codec being referenced, but the last one takes
        /// precedence.  ReducePrimitiveSets() will reduce the set to only those that have
        /// final precedence.
        /// </summary>
        /// <param name="PrimitiveSets"></param>
        internal static void ReducePrimitiveSets(List<PrimitiveSet> PrimitiveSets)
        {
            for (int ii = 0; ii < PrimitiveSets.Count; )
            {
                bool Removed = false;
                for (int jj = ii + 1; jj < PrimitiveSets.Count; jj++)
                {
                    if (PrimitiveSets[ii].Set.ToLower() == PrimitiveSets[jj].Set.ToLower())
                    {
                        PrimitiveSets.RemoveAt(ii);
                        Removed = true;
                        break;
                    }
                }
                if (!Removed) ii++;
            }
        }

        /// <summary>
        /// Call the ToTranslation() method to interpret the content of the DmlTranslationDocument into
        /// a DmlTranslation.  An IResourceResolution object may be provided to retrieve dependencies,
        /// or null can be provided.  An exception is thrown if any inclusions are encountered and
        /// cannot be resolved.
        /// </summary>
        /// <returns>The DmlTranslation represented by this DML Translation document.</returns>
        public ResolvedTranslation ToTranslation(IResourceResolution References = null, ParsingOptions Options = null)
        {
            if (ID != TSL2Translation.idDMLTranslation)
                throw CreateDmlException("Not a DML Translation document.");

            if (Attributes.GetUInt(DML3Translation.idDMLReadVersion,
                    Attributes.GetUInt(DML3Translation.idDMLVersion, TSL2Translation.TSLReadVersion)) != TSL2Translation.TSLReadVersion)
                throw CreateDmlException("DML Translation version is not supported by this reader.");
                        
            DomResolvedTranslation ret = new DomResolvedTranslation();
            ret.Translation = new DmlTranslation();            
            ret.PrimitiveSets = new List<PrimitiveSet>();
            ret.XmlRoot = new DmlContainer();
            AppendFragmentToTranslation(ret, this, References, Options);
            ReducePrimitiveSets(ret.PrimitiveSets);
            return ret;
        }

        internal static void AppendFragmentToTranslation(ResolvedTranslation Into, DmlFragment Fragment, 
            IResourceResolution References = null, ParsingOptions Options = null)
        {
            uint DmlId;
            foreach (DmlNode NDirective in Fragment.Children)
            {
                DmlContainer Directive = NDirective as DmlContainer;
                if (Directive == null) continue;

                switch (Directive.ID)
                {
                    case TSL2Translation.idDMLIncludeTranslation:
                        {
                            if (Directive.Attributes.ContainsID(TSL2Translation.idDML_URI))
                            {
                                string IncludeURI = Directive.Attributes.GetString(TSL2Translation.idDML_URI, "");                                
                                try
                                {
                                    bool IsXml;
                                    using (IDisposable Resource = References.Resolve(IncludeURI, out IsXml))
                                    {
                                        if (Resource is Stream)
                                        {
                                            Stream ResolvedSource = (Stream)Resource;
                                            DmlTranslationDocument doc = new DmlTranslationDocument();
                                            if (IsXml)
                                                doc.LoadFromXml(ResolvedSource);
                                            else
                                            {
                                                using (DmlReader Reader = DmlReader.Create(ResolvedSource))
                                                {
                                                    if (Options != null) Reader.Options = Options;
                                                    doc.Load(Reader, References);
                                                }
                                            }
                                            AppendFragmentToTranslation(Into, doc, References, Options);
                                        }
                                        else if (Resource is DomResolvedTranslation)
                                        {
                                            DomResolvedTranslation Resolved = (DomResolvedTranslation)Resource;
                                            Into.Translation.Add(Resolved.Translation);
                                            Into.PrimitiveSets.AddRange(Resolved.PrimitiveSets);
                                            if (Into is DomResolvedTranslation) ((DomResolvedTranslation)Into).XmlRoot.Merge(Resolved.XmlRoot);
                                        }
                                        else if (Resource is ResolvedTranslation)
                                        {
                                            ResolvedTranslation Resolved = (ResolvedTranslation)Resource;
                                            Into.Translation.Add(Resolved.Translation);
                                            Into.PrimitiveSets.AddRange(Resolved.PrimitiveSets);
                                            if (Into is DomResolvedTranslation) ((DomResolvedTranslation)Into).XmlRoot = null;      // Mark the XmlRoot as invalid due to missing dependency resource.
                                        }
                                        else throw new ArgumentException("Invalid object type received from resource resolution.");
                                    }
                                }
                                catch (DmlException de)
                                {
                                    throw new DmlException(de.Message + "\nWhile resolving translation inclusion '" + IncludeURI + "'.", de);
                                }
                            }
                            else throw new DmlException("DML:Include-Translation directive missing required URI attribute.");
                            continue;
                        }

                    case TSL2Translation.idDMLIncludePrimitives:
                        {
                            if (Directive.Attributes.ContainsID(TSL2Translation.idDMLSet))
                            {
                                DomPrimitiveSet ps = new DomPrimitiveSet();
                                ps.Set = Directive.Attributes.GetString(TSL2Translation.idDMLSet, null);
                                ps.Codec = Directive.Attributes.GetString(TSL2Translation.idDMLCodec, null);                                
                                ps.CodecURI = Directive.Attributes.GetString(TSL2Translation.idDMLCodecURI, null);
                                ps.Configuration = new DmlFragment();
                                foreach (DmlNode Node in Directive.Children) ps.Configuration.Children.Add(Node.Clone(null));
                                Into.PrimitiveSets.Add(ps);
                            }
                            else throw new DmlException("DML:Include-Primitives directive missing required Set attribute.");
                            continue;
                        }

                    case TSL2Translation.idXMLRoot:
                        {
                            if (Into is DomResolvedTranslation)
                            {
                                DomResolvedTranslation DomInto = (DomResolvedTranslation)Into;
                                DomInto.XmlRoot.Merge(Directive);
                            }
                            continue;
                        }

                    case TSL2Translation.idRenumber:
                        {
                            DmlId = Directive.Attributes.GetUInt(TSL2Translation.idDMLID, 0);
                            if (DmlId == 0) throw new DmlException("Required attribute id not found.");
                            Association Existing;
                            if (!Into.Translation.TryGet(DmlId, out Existing)) throw new DmlException("Attempt to renumber a definition that was not found in current translation.");
                            uint NewDmlId = Directive.Attributes.GetUInt(TSL2Translation.idNewID, 0);
                            if (NewDmlId == 0) throw new DmlException("Required attribute new-id not found.");
                            if (Into.Translation.Contains(NewDmlId)) throw new DmlException("Renumber directive's new-id value is already in use.");
                            Into.Translation.Renumber(DmlId, NewDmlId);
                            if (Existing is DomAssociation) ((DomAssociation)Existing).Changes.Add(Directive as DmlTranslationRenumber);                            
                            continue;
                        }
                    case TSL2Translation.idContainer: break;
                    case TSL2Translation.idNode: break;
                    default: throw new DmlException("Unrecognized element in DML translation document.");
                }

                DmlId = Directive.Attributes.GetUInt(TSL2Translation.idDMLID, 0);
                if (DmlId == 0) throw new DmlException("Required attribute id not found for translation definition.");
                string Name = Directive.Attributes.GetString(TSL2Translation.idName, null);
                if (Name == null) throw new DmlException("Required attribute name not found for translation definition.");

                switch (Directive.ID)
                {
                    case TSL2Translation.idContainer:
                        if (Directive.Children.Count > 0)
                        {
                            using (DomResolvedTranslation Local = new DomResolvedTranslation())
                            {
                                Local.Translation = new DmlTranslation(Into.Translation);
                                if (Into.Translation == null)
                                    Local.Translation.Add(DmlTranslation.DML3);         // All top-level translations should merge with DML3.
                                Local.PrimitiveSets = new List<PrimitiveSet>();
                                AppendFragmentToTranslation(Local, Directive, References, Options);
                                DomAssociation NewAssociation = new DomAssociation(DmlId, new DmlName(Name, NodeTypes.Container), Local.Translation);
                                Into.Translation.Add(NewAssociation);
                                NewAssociation.OriginalDefinition = Directive as DmlDefinition;
                                Into.PrimitiveSets.AddRange(Local.PrimitiveSets);
                                if (Local.XmlRoot == null)
                                {
                                    if (Into is DomResolvedTranslation) ((DomResolvedTranslation)Into).XmlRoot = null;          // Mark as invalid due to lack of dependency XmlRoot data.
                                }
                                else
                                {
                                    if (Into is DomResolvedTranslation) ((DomResolvedTranslation)Into).XmlRoot.Merge(Local.XmlRoot);
                                }
                            }
                            continue;
                        }
                        else
                        {
                            DomAssociation NewAssociation = new DomAssociation(DmlId, new DmlName(Name, NodeTypes.Container), null);
                            Into.Translation.Add(NewAssociation);
                            NewAssociation.OriginalDefinition = Directive as DmlDefinition;
                            continue;
                        }
                    case TSL2Translation.idNode:
                        {
                            string Type = Directive.Attributes.GetString(TSL2Translation.idType, null);
                            if (Type == null) throw new DmlException("Required attribute type not found for translation definition '" + Name + "'.");
                            PrimitiveTypes PrimitiveType; ArrayTypes ArrayType;
                            if (!DmlInternalData.StringToPrimitiveType(Type, out PrimitiveType, out ArrayType))
                            {
                                uint TypeId = 0; IDmlReaderExtension Extension = null;
                                if (Options != null && Options.Extensions != null)
                                {
                                    foreach (IDmlReaderExtension Ext in Options.Extensions)
                                    {
                                        TypeId = Ext.Identify(Type);
                                        if (TypeId != 0) { Extension = Ext; break; }
                                    }
                                }
                                if (TypeId != 0)
                                {
                                    Into.Translation.Add(new Association(DmlId, new DmlName(Name, Extension, TypeId)));
                                    continue;
                                }
                                else throw new DmlException("Unrecognized node type for translation definition.");
                            }
                            DomAssociation NewAssociation = new DomAssociation(DmlId, new DmlName(Name, PrimitiveType, ArrayType));
                            Into.Translation.Add(NewAssociation);
                            NewAssociation.OriginalDefinition = Directive as DmlDefinition;
                            continue;
                        }
                    default: throw new DmlException("Unrecognized element in DML translation document.");
                }
            }
        }

        #endregion

        #region "FromTranslation() translation"

        /// <summary>
        /// <para>Call the FromTranslation() method to translate a DmlTranslation object into the DML representation
        /// provided by DmlTranslationDocument.  Any existing document content is replaced by the translation 
        /// representation.</para>
        /// <para>Note that the representation provided by the FromTranslation() method is machine-generated and
        /// may not be the most clear representation of a DML translation.  In particular, any references that
        /// were already resolved are not replaced by &lt;Include&gt; directives that can significantly
        /// simplify a DML translation document.</para>        
        /// </summary>
        /// <param name="TSL">The DmlTranslation from which to generate this DmlTranslationDocument's content.</param>
        /// <param name="PrimitiveSets">An optional list of primitive sets to be required by this 
        /// DmlTranslationDocument.</param>
        /// <param name="XmlRoot">An optional XmlRoot container.  The XmlRoot container is attached at the top-level
        /// of a Dml document if the document is converted to Xml.  It can contain attributes such as Xml namespace
        /// references.</param>
        public void FromTranslation(DmlTranslation TSL, List<DomPrimitiveSet> PrimitiveSets = null, DmlContainer XmlRoot = null)
        {
            Children.Clear();

            foreach (Association Assoc in TSL)
            {
                switch (Assoc.DMLName.NodeType)
                {
                    case NodeTypes.Container: Children.Add(new DmlContainerDefinition(this, Assoc)); continue;                    
                    case NodeTypes.Primitive: Children.Add(new DmlNodeDefinition(this, Assoc)); continue;
                    default: throw CreateDmlException("Unsupported association in translation document model.");
                }
            }

            if (XmlRoot != null)
            {
                if (XmlRoot.ID != TSL2Translation.idXMLRoot) throw new ArgumentException("XmlRoot container must have DMLID of XMLRoot.");
                Children.Add(XmlRoot);
            }

            if (PrimitiveSets != null)
            {
                foreach (DomPrimitiveSet ps in PrimitiveSets)
                {
                    DmlIncludePrimitives Include = new DmlIncludePrimitives(this, ps);
                    if (ps.Configuration != null)
                        foreach (DmlNode ConfigChild in ps.Configuration.Children) Include.Children.Add(ConfigChild.Clone(this));
                    Children.Add(Include);
                }
            }
        }

        #endregion
    }

    public class DmlIncludeTranslation : DmlContainer
    {
        public DmlIncludeTranslation()
        {
            Association = TSL2Translation.TSL2.IncludeTranslation.Clone();
        }

        public DmlIncludeTranslation(DmlFragment Container) : base(Container) 
        {
            Association = DmlTranslation.TSL2.IncludeTranslation.Clone();
        }

        public DmlIncludeTranslation(DmlFragment Container, string TranslationURI, string TranslationURN = null)
            : base(Container)
        {
            Association = DmlTranslation.TSL2.IncludeTranslation.Clone();
            AddAttribute(DmlTranslation.TSL2.URI.DMLID, TranslationURI);
            if (TranslationURN != null) AddAttribute(DmlTranslation.TSL2.URN.DMLID, TranslationURN);
        }

        public string URI
        {
            get
            {
                DmlPrimitive Attr = Attributes.GetByID(DmlTranslation.TSL2.URI.DMLID);
                if (Attr == null) return null;
                return Attr.Value as string;
            }
        }

        public string URN
        {
            get
            {
                DmlPrimitive Attr = Attributes.GetByID(DmlTranslation.TSL2.URN.DMLID);
                if (Attr == null) return null;
                return Attr.Value as string;
            }
        }
    }

    public class DmlIncludePrimitives : DmlContainer
    {
        public DmlIncludePrimitives()
        {
            Association = TSL2Translation.TSL2.IncludePrimitives.Clone();
        }

        public DmlIncludePrimitives(DmlFragment Container) : base(Container) 
        {
            Association = DmlTranslation.TSL2.IncludePrimitives.Clone();
        }        

        public DmlIncludePrimitives(DmlFragment Container, PrimitiveSet PS)
            : base(Container)
        {
            Association = DmlTranslation.TSL2.IncludePrimitives.Clone();
            AddAttribute(DmlTranslation.TSL2.Set.DMLID, PS.Set);
            if (PS.Codec != null && PS.Codec.Length > 0) AddAttribute(DmlTranslation.TSL2.Codec.DMLID, PS.Codec);
            if (PS.CodecURI != null && PS.CodecURI.Length > 0) AddAttribute(DmlTranslation.TSL2.CodecURI.DMLID, PS.CodecURI);            
        }

        public string Set
        {
            get
            {
                DmlPrimitive Attr = Attributes.GetByID(DmlTranslation.TSL2.Set.DMLID);
                if (Attr == null) return null;
                return Attr.Value as string;
            }
        }

        public string Codec
        {
            get
            {
                DmlPrimitive Attr = Attributes.GetByID(DmlTranslation.TSL2.Codec.DMLID);
                if (Attr == null) return null;
                return Attr.Value as string;
            }
        }

        public string CodecURI
        {
            get
            {
                DmlPrimitive Attr = Attributes.GetByID(DmlTranslation.TSL2.CodecURI.DMLID);
                if (Attr == null) return null;
                return Attr.Value as string;
            }
        }
    }

    public abstract class DmlDefinition : DmlContainer
    {
        public DmlDefinition() { }
        public DmlDefinition(DmlFragment Container) : base(Container) { }
        public DmlDefinition(DmlDocument Document) : base(Document) { }

        public uint DefineID
        {
            get { return Attributes.GetUInt(TSL2Translation.idDMLID, 0); }
            set { SetAttribute(TSL2Translation.idDMLID, value); }
        }

        public string DefineName
        {
            get { return Attributes.GetString(TSL2Translation.idName, ""); }
            set { SetAttribute(TSL2Translation.idName, value); }
        }
    }

    public class DmlNodeDefinition : DmlDefinition
    {
        public DmlNodeDefinition()
        {
            Association = TSL2Translation.NodeAssociation;
        }

        public DmlNodeDefinition(DmlFragment Container) : base(Container)
        {
            Association = TSL2Translation.NodeAssociation;
        }

        public DmlNodeDefinition(DmlDocument Document) : base(Document)
        {
            Association = TSL2Translation.NodeAssociation;
        }

        public DmlNodeDefinition(DmlFragment Container, Association Defn) : base(Container)
        {
            Association = TSL2Translation.NodeAssociation;
            AddAttribute(TSL2Translation.idDMLID, Defn.DMLID);
            AddAttribute(TSL2Translation.idName, Defn.DMLName.XmlName);
            AddAttribute(TSL2Translation.idType, DmlInternalData.PrimitiveTypeToString(Defn.DMLName.PrimitiveType, Defn.DMLName.ArrayType));            
        }

        public DmlNodeDefinition(DmlFragment Container, uint DMLID, string XMLName, PrimitiveTypes PrimitiveType)
            : this(Container, DMLID, XMLName, PrimitiveType, ArrayTypes.Unknown) { }

        public DmlNodeDefinition(DmlFragment Container, uint DMLID, string XMLName, PrimitiveTypes PrimitiveType, ArrayTypes ArrayType)
            : base(Container)
        {
            Association = TSL2Translation.NodeAssociation;
            AddAttribute(TSL2Translation.idDMLID, DMLID);
            AddAttribute(TSL2Translation.idName, XMLName);
            AddAttribute(TSL2Translation.idType, DmlInternalData.PrimitiveTypeToString(PrimitiveType, ArrayType));            
        }

        public void GetDefineType(out PrimitiveTypes PrimitiveType, out ArrayTypes ArrayType)
        {
            DmlInternalData.StringToPrimitiveType(Attributes.GetString(TSL2Translation.idType, "unknown"), out PrimitiveType, out ArrayType);
        }

        public void SetDefineType(PrimitiveTypes PrimitiveType) { SetDefineType(PrimitiveType, ArrayTypes.Unknown); }

        public void SetDefineType(PrimitiveTypes PrimitiveType, ArrayTypes ArrayType)
        {
            SetAttribute(TSL2Translation.idType, DmlInternalData.PrimitiveTypeToString(PrimitiveType, ArrayType));
        }
    }
    
    public class DmlContainerDefinition : DmlDefinition
    {
        public DmlContainerDefinition()
        {
            Association = TSL2Translation.ContainerAssociation;
        }

        public DmlContainerDefinition(DmlFragment Container) : base(Container) 
        {
            Association = TSL2Translation.ContainerAssociation;
        }

        public DmlContainerDefinition(DmlDocument Document)
            : base(Document)
        {
            Association = TSL2Translation.ContainerAssociation;
        }

        public DmlContainerDefinition(DmlFragment Container, Association Defn)
            : this(Container, Defn.DMLID, Defn.DMLName.XmlName, Defn.LocalTranslation)
        {
        }

        public DmlContainerDefinition(DmlFragment Container, uint DMLID, string XMLName)
            : this(Container, DMLID, XMLName, null) { }

        public DmlContainerDefinition(DmlFragment Container, uint DMLID, string XMLName, DmlTranslation LocalTranslation)
            : base(Container)
        {
            Association = TSL2Translation.ContainerAssociation;
            AddAttribute(TSL2Translation.idDMLID, DMLID);
            AddAttribute(TSL2Translation.idName, XMLName);

            if (LocalTranslation != null)
            {
                foreach (Association Assoc in LocalTranslation)
                {
                    switch (Assoc.DMLName.NodeType)
                    {
                        case NodeTypes.Container: Children.Add(new DmlContainerDefinition(this, Assoc)); continue;
                        case NodeTypes.Primitive: Children.Add(new DmlNodeDefinition(this, Assoc)); continue;
                        default: throw CreateDmlException("Unsupported association in translation document model.");
                    }
                }
            }
        }
    }

    public class DmlTranslationRenumber : DmlContainer
    {
        public DmlTranslationRenumber()
        {
            Association = TSL2Translation.RenumberAssociation;
        }

        public DmlTranslationRenumber(DmlFragment Container)
            : base(Container)
        {
            Association = TSL2Translation.RenumberAssociation;
        }

        public DmlTranslationRenumber(DmlFragment Container, uint FromDMLID, uint ToDMLID)
            : base(Container)
        {
            Association = TSL2Translation.RenumberAssociation;
            AddAttribute(TSL2Translation.idDMLID, FromDMLID);
            AddAttribute(TSL2Translation.idNewID, ToDMLID);
        }

        public uint FromDMLID
        {
            get { return Attributes.GetUInt(TSL2Translation.idDMLID, 0); }
            set { SetAttribute(TSL2Translation.idDMLID, value); }
        }

        public uint ToDMLID
        {
            get { return Attributes.GetUInt(TSL2Translation.idNewID, 0); }
            set { SetAttribute(TSL2Translation.idNewID, value); }
        }
    }
}
