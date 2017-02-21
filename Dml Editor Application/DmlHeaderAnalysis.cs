#if false
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WileyBlack.Dml;
using WileyBlack.Dml.Dom;
using System.IO;

namespace Dml_Editor
{
    public class DmlHeaderAnalysis
    {
        DmlHeader Header;

        public DmlHeaderAnalysis(DmlHeader Header, IResourceResolution Resolution, ParsingOptions Options = null)
        {
            this.Header = Header;
            Analyze(null, Header, Resolution, Options);
        }
        
        public class DefinitionInfo
        {
            public DmlDefinition OriginalDefinition;
            public List<DmlTranslationRenumber> Changes = new List<DmlTranslationRenumber>();
        }

        /// <summary>
        /// DefinitionMap contains a mapping between resolved absolute DML names and their source node in the
        /// header or referenced translation document.  Absolute DML names can be written with double-colons
        /// between container and child names in order to represent a name within a local context.  The
        /// name can be made further unique by appending the type in a uniform manner.  The exact way of
        /// writing the absolute DML name is irrelevant as long as we use a consistent function.
        /// </summary>
        Dictionary<string, DefinitionInfo> DefinitionMap;

        string GetDmlNameAsString(DmlName Definition)
        {
            if (Definition.NodeType == NodeTypes.Container) return Definition.XmlName;
            if (Definition.NodeType != NodeTypes.Primitive) throw new NotSupportedException();
            string TypeStr = ((int)Definition.PrimitiveType).ToString();
            switch (Definition.PrimitiveType)
            {
                case PrimitiveTypes.Array: TypeStr = TypeStr + "[" + ((int)Definition.ArrayType).ToString() + "]"; break;
                case PrimitiveTypes.Matrix: TypeStr = TypeStr + "[" + ((int)Definition.ArrayType).ToString() + "]"; break;
                case PrimitiveTypes.Extension: TypeStr = TypeStr + "[" + Definition.TypeId.ToString() + "]"; break;
            }
            return Definition.XmlName + "[" + TypeStr + "]";
        }

        string GetAbsoluteDmlScopeName(DmlNode Entity)
        {
            if (Entity.Container == null) return "::";
            else
            {                
                if (Entity.Container.Association.LocalTranslation != null)
                {
                    if (Entity.Container.Association.LocalTranslation.ParentTranslation != null)
                        return GetAbsoluteDmlScopeName(Entity.Container) + Entity.Container.Name + "::";
                    else
                        return "::" + Entity.Container.Name + "::";                 // Found global level in translation tree.
                }
                else
                {
                    // This is the case where Entity does not exist within an immediate local translation.  Thus, the parent
                    // gets no mention in the absolute Dml Name because it exists as a container only, not a translation context.  We
                    // must still climb the container tree in case something higher level has a local translation.
                    return GetAbsoluteDmlScopeName(Entity.Container);
                }
            }
        }

        string GetAbsoluteDmlName(DmlNode Entity)
        {
            return GetAbsoluteDmlScopeName(Entity) + GetDmlNameAsString(Entity.Association.DMLName);
        }

        void Analyze(DmlTranslation ParentTranslation, DmlFragment Fragment, IResourceResolution References, ParsingOptions Options = null)
        {
            DomResolvedTranslation ret = new DomResolvedTranslation();
            ret.Translation = new DmlTranslation(ParentTranslation);
            if (ParentTranslation == null)
            {
                ret.Translation.Add(DmlTranslation.DML3);         // A top-level translation should build on DML3.
            }
            ret.PrimitiveSets = new List<PrimitiveSet>();
            ret.XmlRoot = new DmlContainer();
            AppendAnalyze(ret, "::", Fragment, References, Options);
            //ReducePrimitiveSets(ret.PrimitiveSets);
            //return ret;
        }

        void AppendAnalyze(ResolvedTranslation Into, string CurrentName, DmlFragment Fragment, IResourceResolution References, ParsingOptions Options = null)
        {
            uint DmlId;
            foreach (DmlNode NDirective in Fragment.Children)
            {
                DmlContainer Directive = NDirective as DmlContainer;
                if (Directive == null) continue;

                switch (Directive.ID)
                {
                    case TSL2Translation.idDMLInclude:
                        {
                            if (Directive.Attributes.ContainsID(TSL2Translation.idDMLTranslationURI))
                            {
                                string IncludeURI = Directive.Attributes.GetString(TSL2Translation.idDMLTranslationURI, "");
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
                                            AppendAnalyze(Into, CurrentName, doc, References, Options);
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
                            else if (Directive.Attributes.ContainsID(TSL2Translation.idDMLPrimitives))
                            {
                                PrimitiveSet ps = new PrimitiveSet();
                                ps.Primitives = Directive.Attributes.GetString(TSL2Translation.idDMLPrimitives, null);
                                ps.Codec = Directive.Attributes.GetString(TSL2Translation.idDMLCodec, null);
                                ps.Configuration = Directive.Attributes.GetU8Array(TSL2Translation.idDMLConfiguration, null);
                                ps.CodecURI = Directive.Attributes.GetString(TSL2Translation.idDMLCodecURI, null);
                                Into.PrimitiveSets.Add(ps);
                            }
                            else throw new DmlException("DML:Include directive missing required TranslationURI or Primitives attribute.");
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
                            uint NewDmlId = Directive.Attributes.GetUInt(TSL2Translation.idNewID, 0);
                            if (NewDmlId == 0) throw new DmlException("Required attribute new-id not found.");
                            if (Into.Translation.Contains(NewDmlId)) throw new DmlException("Renumber directive's new-id value is already in use.");
                            Into.Translation.Renumber(DmlId, NewDmlId);

                            Association UpdatedAssociation;
                            if (!Into.Translation.TryGet(NewDmlId, out UpdatedAssociation)) throw new NotSupportedException();
                            string AbsName = CurrentName + GetDmlNameAsString(UpdatedAssociation.DMLName);
                            DefinitionInfo di;
                            if (!DefinitionMap.TryGetValue(AbsName, out di)) throw new NotSupportedException("Existing association not found during analysis of renumber directive.");
                            di.Changes.Add(Directive as DmlTranslationRenumber);
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
                                AppendAnalyze(Local, CurrentName + Name + "::", Directive, References, Options);
                                Association NewAssociation = new Association(DmlId, new DmlName(Name, NodeTypes.Container), Local.Translation);
                                Into.Translation.Add(NewAssociation);
                                Into.PrimitiveSets.AddRange(Local.PrimitiveSets);
                                if (Local.XmlRoot == null)
                                {
                                    if (Into is DomResolvedTranslation) ((DomResolvedTranslation)Into).XmlRoot = null;          // Mark as invalid due to lack of dependency XmlRoot data.
                                }
                                else
                                {
                                    if (Into is DomResolvedTranslation) ((DomResolvedTranslation)Into).XmlRoot.Merge(Local.XmlRoot);
                                }

                                DefinitionInfo di = new DefinitionInfo();
                                di.OriginalDefinition = Directive as DmlDefinition;     // The Directive also contains a reference to the document, so that we can tell if it was the DmlHeader or a secondary translation document.
                                DefinitionMap.Add(CurrentName + GetDmlNameAsString(NewAssociation.DMLName), di);
                            }
                            continue;
                        }
                        else
                        {
                            Association NewAssociation = new Association(DmlId, new DmlName(Name, NodeTypes.Container), null);
                            Into.Translation.Add(NewAssociation);

                            DefinitionInfo di = new DefinitionInfo();
                            di.OriginalDefinition = Directive as DmlDefinition;
                            DefinitionMap.Add(CurrentName + GetDmlNameAsString(NewAssociation.DMLName), di);
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
                            Association NewAssociation = new Association(DmlId, new DmlName(Name, PrimitiveType, ArrayType));
                            Into.Translation.Add(NewAssociation);

                            DefinitionInfo di = new DefinitionInfo();
                            di.OriginalDefinition = Directive as DmlDefinition;     // The Directive also contains a reference to the document, so that we can tell if it was the DmlHeader or a secondary translation document.
                            DefinitionMap.Add(CurrentName + GetDmlNameAsString(NewAssociation.DMLName), di);
                            continue;
                        }
                    default: throw new DmlException("Unrecognized element in DML translation document.");
                }
            }
        }        

        /// <summary>
        /// FindSpecificDefinition() searches the analyzed DmlHeader and referenced documents in order to identify the translation
        /// entr(ies) defining the DocumentEntry.  This version locates an association at the specified location.  For example,
        /// if the node is identified as ::A::B::C, where A and B are nested translations, then this is the only definition
        /// that will be retrieved.
        /// </summary>
        /// <param name="DocumentEntry">The node to find a header or translation definition for.</param>
        public DefinitionInfo FindSpecificDefinition(DmlNode DocumentEntry)
        {
            return DefinitionMap[GetAbsoluteDmlName(DocumentEntry)];
        }

        /// <summary>
        /// FindAnyDefinition() searches the analyzed DmlHeader and referenced documents in order to identify the translation
        /// entr(ies) defining the DocumentEntry.  This version explores up the translation tree to locate any valid definition
        /// for the DocumentEntry.  For example, if the node is identified as ::A::B::C, where A and B are nested translations,
        /// FindAnyDefinition() will locate a definition for ::A::B::C, ::A::C, or ::C.  In this example, ::A::B would be the
        /// Container parameter and C would be the Specific parameter.
        /// </summary>
        /// <param name="DocumentEntry">The container level to start the search from.</param>
        /// <param name="Specific">The specific definition to be found.</param>
        /// <param name="PreferGlobal">If true, the search begins with global definitions.  If false, the search tries the most
        /// localized contexts first.</param>
        public DefinitionInfo FindAnyDefinition(DmlFragment Container, DmlName Specific, bool PreferGlobal = true)
        {
            if (Container == null)
            {
                DefinitionInfo di;
                if (DefinitionMap.TryGetValue("::" + GetDmlNameAsString(Specific), out di)) return di;
                return null;
            }

            if (PreferGlobal)
            {
                DefinitionInfo di = FindAnyDefinition(Container.Container, Specific, true);
                if (di != null) return di;
                if (DefinitionMap.TryGetValue(GetAbsoluteDmlName(Container) + "::" + GetDmlNameAsString(Specific), out di)) return di;
                return null;                                
            }
            else
            {
                DefinitionInfo di;
                if (DefinitionMap.TryGetValue(GetAbsoluteDmlName(Container) + "::" + GetDmlNameAsString(Specific), out di)) return di;
                di = FindAnyDefinition(Container.Container, Specific, false);
                return di;
            }
        }
    }
}
#endif
