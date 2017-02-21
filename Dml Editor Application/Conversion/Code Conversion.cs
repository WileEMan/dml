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
    public class TranslationConversionBase
    {
        protected static void WriteIndent(StringBuilder sb, int indent)
        {
            for (int ii = 0; ii < indent; ii++) sb.Append("\t");
        }

        protected static string XmlNameToCodeName(string XmlName)
        {
            /*
            string ret = XmlName;
            for (; ; )
            {
                int index = ret.IndexOf('-');
                if (index < 0)
                    index = ret.IndexOf('_');
                if (index < 0)
                    return ret;

                if (index == 0)
                {
                    if (ret.Length == 1) return "";
                    ret = ret.Substring(1);
                }
                else if (index == ret.Length - 1)
                {
                    if (ret.Length == 1) return "";
                    ret = ret.Substring(0, index);
                }
                else ret = ret.Substring(0, index) + ret.Substring(index + 1, ret.Length - (index + 1));
            }
             */
            return XmlName.Replace('-', '_');
        }
    }

    public class DmlTranslationToCSharpClass : TranslationConversionBase
    {
        public static string Convert(DmlDocument doc, IResourceResolution References, string ClassName)
        {
            DmlTranslationDocument nsdoc = new DmlTranslationDocument();
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    ms.Position = 0;
                    nsdoc.Load(ms);
                }
                return Convert(nsdoc, References, ClassName);
            }
            catch (Exception ex)
            {
                throw new Exception("Document is not a valid Dml Translation: " + ex.Message);
            }
        }

        public static string Convert(DmlTranslationDocument doc, IResourceResolution References, string ClassName)
        {
            ResolvedTranslation Resolved = doc.ToTranslation(References, null);

            StringBuilder sb = new StringBuilder();
            StringBuilder sbConstructor = new StringBuilder();
            
            sb.AppendLine("public class " + ClassName + " : DmlTranslation");
            sb.AppendLine("{");
            if (doc.Attributes["DML:URN"] != null)            
            {
                sb.AppendLine("\tpublic string urn = \"" + doc.Attributes["DML:URN"].Value + "\";");
            }
            sb.AppendLine("\tpublic PrimitiveSet[] RequiredPrimitiveSets = new PrimitiveSet[] {");
            WritePrimitiveSets(2, sb, Resolved.PrimitiveSets);
            sb.AppendLine("\t\t};");
            sb.AppendLine();            
            WriteAssociations(1, sb, sbConstructor, doc);
            sb.AppendLine("");
            sb.AppendLine("\tpublic " + ClassName + "()");
            sb.AppendLine("\t\t: base()");
            sb.AppendLine("\t{");
            sb.Append(sbConstructor.ToString());
            sb.AppendLine("\t\tAdd(new Association[] {");
            WriteTranslationList(3, sb, doc);
            sb.AppendLine("\t\t});");
            sb.AppendLine("\t}");
            sb.AppendLine("");
            sb.AppendLine("\tpublic static " + ClassName + " Translation = new " + ClassName + "();");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void WritePrimitiveSets(int Indent, StringBuilder sb, List<PrimitiveSet> PrimitiveSets)
        {
            for (int ii=0; ii < PrimitiveSets.Count; ii++)
            {
                PrimitiveSet ps = PrimitiveSets[ii];
                bool Last = (ii + 1 >= PrimitiveSets.Count);

                WriteIndent(sb, Indent);
                if (string.IsNullOrEmpty(ps.Codec))
                    sb.AppendLine("new PrimitiveSet(\"" + ps.Set + "\")" + (Last ? "" : ","));
                else
                    sb.AppendLine("new PrimitiveSet(\"" + ps.Set + "\", \"" + ps.Codec + "\")" + (Last ? "" : ","));
            }
        }

        private static void WriteTranslationList(int Indent, StringBuilder sb, DmlContainer Container)
        {
            bool First = true;
            for (int ii=0; ii < Container.Children.Count; ii++)
            {
                if (Container.Children[ii] is DmlDefinition)
                {
                    DmlDefinition Assoc = (DmlDefinition)Container.Children[ii];

                    if (First) First = false; else sb.AppendLine(",");
                    WriteIndent(sb, Indent);
                    sb.Append(XmlNameToCodeName(Assoc.DefineName));
                }
            }
            sb.AppendLine();
        }

        private static void WriteAssociations(int Indent, StringBuilder sb, StringBuilder sbConstructor, DmlContainer Container)
        {
            for (int ii=0; ii < Container.Children.Count; ii++)
            {
                if (Container.Children[ii] is DmlDefinition)
                {
                    DmlDefinition Assoc = (DmlDefinition)Container.Children[ii];
                    WriteAssociation(Indent, sb, Assoc);
                }
                else if (Container.Children[ii] is DmlIncludeTranslation)
                {
                    DmlIncludeTranslation Include = (DmlIncludeTranslation)Container.Children[ii];                    
                    string URI = Include.Attributes.GetString(DmlTranslation.TSL2.URI.DMLID, "");
                    if (sbConstructor == null)
                        throw new Exception("Unable to generate code for translation inclusion '" + URI + "' below top-level of new translation.");
                    if (URI.Equals(WileyBlack.Dml.EC.EC2Translation.urn, StringComparison.OrdinalIgnoreCase))
                        sbConstructor.AppendLine("\t\tAdd(EC2);\t// Include base translation.");
                    else if (URI.Equals(WileyBlack.Dml.TSL2Translation.urn, StringComparison.OrdinalIgnoreCase))
                        sbConstructor.AppendLine("\t\tAdd(TSL2);\t// Include base translation.");
                    else
                        sbConstructor.AppendLine("\t\tAdd(TODO(\"" + URI + "\"));\t// Include base translation.");
                }
            }
        }

        private static void WriteAssociation(int Indent, StringBuilder sb, DmlDefinition Assoc)
        {
            WriteIndent(sb, Indent);
            if (Assoc is DmlNodeDefinition)
            {
                DmlNodeDefinition NodeAssoc = (DmlNodeDefinition)Assoc;
                PrimitiveTypes PrimType;
                ArrayTypes ArrType;
                NodeAssoc.GetDefineType(out PrimType, out ArrType);
                if (PrimType == PrimitiveTypes.Array || PrimType == PrimitiveTypes.Matrix)
                    sb.AppendLine("public Association " + XmlNameToCodeName(Assoc.DefineName) + " = new Association(" + Assoc.DefineID + ", \""
                        + Assoc.DefineName + "\", PrimitiveTypes." + PrimType.ToString() + ", ArrayTypes." + ArrType.ToString() + ");");
                else
                    sb.AppendLine("public Association " + XmlNameToCodeName(Assoc.DefineName) + " = new Association(" + Assoc.DefineID + ", \""
                        + Assoc.DefineName + "\", PrimitiveTypes." + PrimType.ToString() + ");");
            }
            else if (Assoc is DmlContainerDefinition)
            {
                DmlContainerDefinition CAssoc = (DmlContainerDefinition)Assoc;
                if (CAssoc.Children.Count == 0)
                    sb.AppendLine("public Association " + XmlNameToCodeName(Assoc.DefineName) + " = new Association(" + Assoc.DefineID + ", \""
                        + Assoc.DefineName + "\", NodeTypes.Container);");
                else
                {
                    sb.AppendLine();
                    WriteIndent(sb, Indent);
                    sb.AppendLine("public class type" + XmlNameToCodeName(CAssoc.DefineName) + " : Association {");
                    WriteIndent(sb, Indent + 1);                    
                    sb.AppendLine("public type" + XmlNameToCodeName(CAssoc.DefineName) + "() : base(" + Assoc.DefineID
                        + ", \"" + Assoc.DefineName + "\", null) {");
                    WriteIndent(sb, Indent + 2);                    
                    sb.AppendLine("LocalTranslation = DmlTranslation.CreateFrom(new Association[] {");
                    WriteTranslationList(Indent + 3, sb, CAssoc);
                    WriteIndent(sb, Indent + 2);
                    sb.AppendLine("});");
                    WriteIndent(sb, Indent + 1);
                    sb.AppendLine("}");
                    sb.AppendLine();
                    WriteAssociations(Indent + 1, sb, null, CAssoc);
                    WriteIndent(sb, Indent);
                    sb.AppendLine("}");
                    WriteIndent(sb, Indent);
                    sb.AppendLine("public type" + XmlNameToCodeName(CAssoc.DefineName) + " " + XmlNameToCodeName(CAssoc.DefineName) + " = new type"
                        + XmlNameToCodeName(CAssoc.DefineName) + "();");
                    sb.AppendLine();
                }
            }            
        }        
    }

    public class DmlTranslationToCPPClass : TranslationConversionBase
    {
        public static string Convert(DmlDocument doc, IResourceResolution References, string ClassName)
        {
            DmlTranslationDocument nsdoc = new DmlTranslationDocument();
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    ms.Position = 0;
                    nsdoc.Load(ms);
                }
                return Convert(nsdoc, References, ClassName);
            }
            catch (Exception ex)
            {
                throw new Exception("Document is not a valid Dml Translation: " + ex.Message);
            }
        }

        private static string Convert(DmlTranslationDocument doc, IResourceResolution References, string ClassName)
        {
            ResolvedTranslation Resolved = doc.ToTranslation(References, null);

            StringBuilder sbC = new StringBuilder();
            sbC.AppendLine("// Source File (.cpp)");
            sbC.AppendLine("// This code was automatically generated by the Dml Editor.");
            sbC.AppendLine("#include \"" + ClassName + ".h\"");
            sbC.AppendLine();

            StringBuilder sbH = new StringBuilder();
            sbH.AppendLine("// Header File (.h)");
            sbH.AppendLine("// This code was automatically generated by the Dml Editor.");
            sbH.AppendLine();
            sbH.AppendLine("#include \"Dml.h\"");
            sbH.AppendLine("using namespace wb::dml;");
            sbH.AppendLine();
            sbH.AppendLine("class " + ClassName + " : public Translation");
            sbH.AppendLine("{");
            sbH.AppendLine("public:");
            if (doc.Attributes["DML:URN"] != null)
            {
                sbH.AppendLine("\tstatic const char* urn;");
                sbH.AppendLine();
                sbC.AppendLine("/*static*/ const char* " + ClassName + "::urn = \"" + doc.Attributes["DML:URN"].Value + "\";");
                sbC.AppendLine();
            }
            if (Resolved.PrimitiveSets.Count != 0)
            {
                sbH.AppendLine("\tenum { NumOfRequiredPrimitiveSets = " + Resolved.PrimitiveSets.Count + " };");
                sbH.AppendLine("\tstatic PrimitiveSet RequiredPrimitiveSets[" + Resolved.PrimitiveSets.Count + "];");
                sbH.AppendLine();
                sbC.AppendLine("/*static*/ PrimitiveSet " + ClassName + "::RequiredPrimitiveSets[] = {");

                for (int ii = 0; ii < Resolved.PrimitiveSets.Count; ii++)
                {
                    PrimitiveSet ps = Resolved.PrimitiveSets[ii];
                    bool Last = (ii + 1 >= Resolved.PrimitiveSets.Count);
                    
                    if (string.IsNullOrEmpty(ps.Codec))
                        sbC.AppendLine("\tPrimitiveSet(\"" + ps.Set + "\")" + (Last ? "" : ","));
                    else
                        sbC.AppendLine("\tPrimitiveSet(\"" + ps.Set + "\", \"" + ps.Codec + "\", \"" + ps.CodecURI + "\")" + (Last ? "" : ","));
                }
                sbC.AppendLine("};");
                sbC.AppendLine();
            }            

            StringBuilder sbH1 = new StringBuilder();
            StringBuilder sbH2 = new StringBuilder();
            StringBuilder sbConstructor = new StringBuilder();
            for (int ii = 0; ii < doc.Children.Count; ii++)
            {
                if (doc.Children[ii] is DmlDefinition)
                {
                    WriteAssociation(1, ClassName, sbC, sbH1, sbH2, (DmlDefinition)doc.Children[ii]);
                    WriteIndent(sbConstructor, 2);
                    sbConstructor.AppendLine("Add(" + XmlNameToCodeName(((DmlDefinition)doc.Children[ii]).DefineName) + ");");
                }
                else if (doc.Children[ii] is DmlIncludeTranslation)
                {
                    DmlIncludeTranslation Include = (DmlIncludeTranslation)doc.Children[ii];
                    string URI = Include.Attributes.GetString(DmlTranslation.TSL2.URI.DMLID, "");
                    WriteIndent(sbConstructor, 2);
                    if (URI.Equals(WileyBlack.Dml.EC.EC2Translation.urn, StringComparison.OrdinalIgnoreCase))
                        sbConstructor.AppendLine("Add(EC2);\t// Include base translation.");
                    else if (URI.Equals(WileyBlack.Dml.TSL2Translation.urn, StringComparison.OrdinalIgnoreCase))
                        sbConstructor.AppendLine("Add(TSL2);\t// Include base translation.");
                    else
                        sbConstructor.AppendLine("Add(TODO(\"" + URI + "\"));\t// Include translation dependency.");
                }
            }
            
            sbH.Append(sbH1);
            sbH.AppendLine();
            sbH.Append(sbH2);
            sbH.AppendLine();
            WriteIndent(sbH, 1); sbH.AppendLine(ClassName + "()");
            WriteIndent(sbH, 1); sbH.AppendLine("{");
            sbH.Append(sbConstructor.ToString());
            WriteIndent(sbH, 1); sbH.AppendLine("}");            
            sbH.AppendLine("};");            

            StringBuilder sb = new StringBuilder();
            sb.Append(sbH.ToString());
            sb.AppendLine();
            sb.Append(sbC.ToString());
            return sb.ToString();
        }

        private static void WriteAssociation(int IndentH, string Namespace, StringBuilder sbC, StringBuilder sbH1, StringBuilder sbH2, DmlDefinition Assoc)
        {
            // sbH1 contains type declarations (new classes).
            // sbH2 contains association declarations (static variables).
            // sbC gains the static definitions.

            if (Assoc is DmlNodeDefinition)
            {
                DmlNodeDefinition NodeAssoc = (DmlNodeDefinition)Assoc;
                PrimitiveTypes PrimType;
                ArrayTypes ArrType;
                NodeAssoc.GetDefineType(out PrimType, out ArrType);
                WriteIndent(sbH2, IndentH);
                sbH2.AppendLine("static Association " + XmlNameToCodeName(Assoc.DefineName) + ";");
                if (PrimType == PrimitiveTypes.Array || PrimType == PrimitiveTypes.Matrix)
                    sbC.AppendLine("Association " + Namespace + "::" + XmlNameToCodeName(Assoc.DefineName) + "(" + Assoc.DefineID + ", \""
                        + Assoc.DefineName + "\", PrimitiveTypes::" + PrimType.ToString() + ", ArrayTypes::" + ArrType.ToString() + ");");
                else
                    sbC.AppendLine("Association " + Namespace + "::" + XmlNameToCodeName(Assoc.DefineName) + "(" + Assoc.DefineID + ", \""
                        + Assoc.DefineName + "\", PrimitiveTypes::" + PrimType.ToString() + ");");
            }
            else if (Assoc is DmlContainerDefinition)
            {
                DmlContainerDefinition CAssoc = (DmlContainerDefinition)Assoc;
                if (CAssoc.Children.Count == 0)
                {
                    WriteIndent(sbH2, IndentH);
                    sbH2.AppendLine("static Association " + XmlNameToCodeName(Assoc.DefineName) + ";");

                    sbC.AppendLine("Association " + Namespace + "::" + XmlNameToCodeName(Assoc.DefineName) + "(" + Assoc.DefineID + ", \""
                        + Assoc.DefineName + "\", NodeTypes::Container);");
                }
                else
                {
                    // There is a local translation here.  We'll need to define a nested class for that local translation.

                    string CodeName = XmlNameToCodeName(Assoc.DefineName);                    
                    
                    WriteIndent(sbH1, IndentH);
                    sbH1.AppendLine("class " + CodeName + " : public Association {");
                    WriteIndent(sbH1, IndentH);
                    sbH1.AppendLine("public:");

                    StringBuilder localH1 = new StringBuilder();
                    StringBuilder localH2 = new StringBuilder();                    
                    for (int ii = 0; ii < CAssoc.Children.Count; ii++)
                    {
                        if (CAssoc.Children[ii] is DmlDefinition)
                        {
                            DmlDefinition ChildAssoc = (DmlDefinition)CAssoc.Children[ii];
                            WriteAssociation(IndentH + 1, Namespace + "::" + CodeName, sbC, localH1, localH2, ChildAssoc);
                        }
                    }
                    
                    sbH1.Append(localH1);
                    if (localH1.Length > 0) sbH1.AppendLine();
                    sbH1.Append(localH2);               // We merge the child H2 into H1 here.
                    sbH1.AppendLine();
                    WriteIndent(sbH1, IndentH+1);
                    sbH1.AppendLine(CodeName + "() : Association(" + Assoc.DefineID + ", \"" + Assoc.DefineName + "\", NodeTypes::Container) {");
                    WriteIndent(sbH1, IndentH+2);
                    sbH1.AppendLine("pLocalTranslation = new Translation();");
                    for (int ii = 0; ii < CAssoc.Children.Count; ii++)
                    {
                        if (CAssoc.Children[ii] is DmlDefinition)
                        {
                            DmlDefinition ChildAssoc = (DmlDefinition)CAssoc.Children[ii];
                            WriteIndent(sbH1, IndentH+2);
                            sbH1.AppendLine("pLocalTranslation->Add(" + XmlNameToCodeName(ChildAssoc.DefineName) + ");");
                        }
                        else if (CAssoc.Children[ii] is DmlIncludeTranslation)
                        {
                            DmlIncludeTranslation Include = (DmlIncludeTranslation)CAssoc.Children[ii];
                            string URI = Include.Attributes.GetString(DmlTranslation.TSL2.URI.DMLID, "");
                            WriteIndent(sbH1, IndentH + 2);
                            if (URI.Equals(WileyBlack.Dml.EC.EC2Translation.urn, StringComparison.OrdinalIgnoreCase))
                                sbH1.AppendLine("Add(EC2);\t// Include base translation.");
                            else if (URI.Equals(WileyBlack.Dml.TSL2Translation.urn, StringComparison.OrdinalIgnoreCase))
                                sbH1.AppendLine("Add(TSL2);\t// Include base translation.");
                            else
                                sbH1.AppendLine("Add(TODO(\"" + URI + "\"));\t// Include translation dependency.");
                        }
                    }
                    WriteIndent(sbH1, IndentH+1);
                    sbH1.AppendLine("}");
                    WriteIndent(sbH1, IndentH);
                    sbH1.AppendLine("};");

                    WriteIndent(sbH2, IndentH);
                    sbH2.AppendLine("static " + CodeName + " " + CodeName + ";");

                    // We prefix the keyword class to clarify for C++ that the next token is a type and not the object of the same name.
                    sbC.AppendLine("class " + Namespace + "::" + CodeName + " " + Namespace + "::" + CodeName + ";");                    
                }
            }
        }                
    }
}
