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

/** Serialization TODO List:
 * 
 *  - The Allow() function assumes local translations are set for every container when GenerateTranslation is used, but
 *    I don't believe I'm creating those local translation anywhere.  A NullReferenceException will fire every time.
 *  - If local translations are generated for every single container, many will be empty and an optimization could be
 *    performed somewhere.
 *  - What should really happen is that when GenerateTranslation is used, a top-level DmlTranslation object is populated 
 *    during the scan.  No such DmlTranslation object exists at the moment.---- Wait, actually such a translation would
 *    be accessed as 'TopType.Association.LocalTranslation', once TopType is assigned...
 *  - Provide serialization example code.
 *  - Serialization of Bitmap objects.
*    
 */

using System;
using System.Data;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using WileyBlack.Dml;

namespace WileyBlack.Dml.Serialization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, 
        AllowMultiple = false)]
    public class DmlElementAttribute : Attribute
    {
        public string XMLName;
        public uint DMLID = uint.MaxValue;

        public DmlElementAttribute(string XMLName)
        {
            this.XMLName = XMLName;
            DMLID = uint.MaxValue;
        }

        public DmlElementAttribute(uint DMLID)
        {
            this.XMLName = null;
            this.DMLID = DMLID;
        }

        public DmlElementAttribute(uint DMLID, string XMLName)
        {
            this.XMLName = XMLName;
            this.DMLID = DMLID;
        }        
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DmlAttributeAttribute : Attribute
    {
        public string XMLName;
        public uint DMLID = uint.MaxValue;

        public DmlAttributeAttribute(string XMLName)
        {
            this.XMLName = XMLName;
            DMLID = uint.MaxValue;
        }

        public DmlAttributeAttribute(uint DMLID)
        {
            this.XMLName = null;
            this.DMLID = DMLID;
        }

        public DmlAttributeAttribute(uint DMLID, string XMLName)
        {
            this.XMLName = XMLName;
            this.DMLID = DMLID;
        }
    }

    /// <summary>
    /// The DmlIgnore attribute can be applied to a property or field to indicate that it should
    /// not be serialized or deserialized even if it is a public property or field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DmlIgnoreAttribute : Attribute
    {
        public DmlIgnoreAttribute()
        {
        }
    }

    /// <summary>
    /// The DmlOptional attribute can be applied to a property or field to indicate that the node
    /// can be omitted when the property or field is null.  The DmlOptional attribute cannot be
    /// used with dml array elements, although arrays themselves can be optional.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DmlOptionalAttribute : Attribute
    {
        public DmlOptionalAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class DmlArrayItemAttribute : Attribute
    {
        public Type AllowType;

        /// <summary>
        /// Since the DmlElementAttribute can be applied to type definitions or to fields/properties,
        /// the type definition can provide enough information to identify and instantiate the
        /// type.  The DMLID value in DmlArrayItem then is used only to override this recognition
        /// and is not required if the type definition contains a DmlElementAttribute.
        /// </summary>
        public uint DMLID = uint.MaxValue;

        /// <summary>
        /// Since the DmlElementAttribute can be applied to type definitions or to fields/properties,
        /// the type definition can provide enough information to identify and instantiate the
        /// type.  The XMLName value in DmlArrayItem then is used only to override this recognition
        /// and is not required if the type definition contains a DmlElementAttribute.
        /// </summary>
        public string XMLName;

        public DmlArrayItemAttribute(Type AllowType)
        {
            this.AllowType = AllowType;
        }
    }

    /// <summary>
    /// <para>The DmlRoot attribute can be applied to any class or struct in order to denote it as a possible
    /// root container in a Dml tree.  The DmlRoot attribute acts like the DmlElement attribute except that it
    /// can also specify a translation URI.  Classes with the DmlRoot attribute are still eligible to be utilized 
    /// as a subordinate element within DML documents, but their use will trigger the inclusion of the specified 
    /// translation URI in a generated translation document.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class DmlRootAttribute : Attribute
    {
        public string TranslationUri;
        public string DocType;
        public string XMLName;
        public uint DMLID = uint.MaxValue;

        public DmlRootAttribute(string XMLName)
        {            
            this.XMLName = XMLName;
        }

        public DmlRootAttribute(string TranslationUri, string XMLName)
        {
            this.TranslationUri = TranslationUri;
            this.XMLName = XMLName;            
        }

        public DmlRootAttribute(string TranslationUri, uint DMLID)
        {
            this.TranslationUri = TranslationUri;
            this.DMLID = DMLID;
        }

        public DmlRootAttribute(string TranslationUri, uint DMLID, string XMLName)
        {
            this.TranslationUri = TranslationUri;            
            this.DMLID = DMLID;
            this.XMLName = XMLName;
        }
    }

    /// <summary>
    /// <para>In most cases, the Dml Serialization system will automatically detect required
    /// primitive sets.  However if a custom serialization is being performed, any required
    /// primitive sets should be marked on the class in an attribute.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class DmlPrimitiveSetAttribute : Attribute
    {
        public PrimitiveSet PrimitiveSet;        

        public DmlPrimitiveSetAttribute(string Primitives)
        {
            this.PrimitiveSet = new PrimitiveSet(Primitives);            
        }

        public DmlPrimitiveSetAttribute(string Primitives, string Codec)
        {
            this.PrimitiveSet = new PrimitiveSet(Primitives);            
        }
    }

    /// <summary>
    /// The IDmlSerializable interface can be declared on a type in order to provide custom serialization logic whenever
    /// that type is encountered.  Either a DmlRoot or DmlElement attribute must be attached to any type that will be 
    /// serialized using IDmlSerializable or to the field containing the type.  The association will be merged with the 
    /// translation generated for serialization.  When deserializing, any valid encounters of the DMLID (or name, if using
    /// inline identification) for the type will lead to instantiation of an object followed by a ReadDml() call.  
    /// When serializing, WriteDml() will be called wherever the object is encountered.
    /// </summary>
    public interface IDmlSerializable
    {
        /// <summary>
        /// The ReadDml() method must reconstitute the object from the DML provided.  When ReadDml() is called,
        /// the reader will have completed the Read() call on the node matching the identification of this
        /// type.  That is, the opening node of the type will have already been read.  When this method returns, it 
        /// must have read the entire element from beginning to end, including any associated EndContainer marker.
        /// <example>
        /// void ReadDml(DmlReader reader) {
        ///     for (; ; ) {
        ///         if (!reader.Read()) throw new EndOfStreamException("Dml stream unexpectedly terminated.");
        ///         if (reader.NodeType == NodeTypes.EndContainer) return;
        ///         // We'll allow either attribute or element formats in this example, but could also check.
        ///         switch (reader.Name.ToLower())
        ///         {
        ///         case "width": Width = reader.GetAsInt(); break;
        ///         case "height": Height = reader.GetAsInt(); break;
        ///         }
        ///     }
        /// }    
        /// </example>
        /// </summary>
        /// <param name="reader">The reader providing access to the DML stream.</param>
        void ReadDml(DmlReader reader);

        /// <summary>
        /// The WriteDml() method should serialize the DML representation of the object.  When WriteDml() is called,
        /// the writer will have made the WriteStartContainer() call for the type.  WriteDml() is responsible for
        /// writing all attributes, elements, and markers for the container (including the WriteEndContainer()
        /// call.)  
        /// <example>
        /// public void WriteDml(DmlWriter writer)
        /// {    
        ///     writer.Write(DML3Translation.idDMLContentSize, PredictedSize);
        ///     writer.Write("Width", (uint)Width);
        ///     writer.Write("Height", (uint)Height);
        ///     writer.WriteEndAttributes();
        ///     
        ///     foreach (...) 
        ///     {
        ///         writer.WriteStartContainer("SubElement");
        ///         ((IDmlSerializable)[subelement]).WriteDml(writer);
        ///     }
        ///     
        ///     writer.WriteEndContainer();
        /// }
        /// </example>
        /// </summary>
        /// <param name="writer">The writer providing access to the DML stream.</param>        
        void WriteDml(DmlWriter writer);

        /// <summary>
        /// The GetContentSize() method can provide the size of the DML representation of the object.  The size should
        /// exclude the node head, but include all attributes, elements, and end markers.  The GetContentSize() method can
        /// return UInt64.MaxValue if the size cannot be predicted at this time.  GetContentSize() is used to provide
        /// the DML:ContentSize value for higher-level containers.
        /// <example>
        /// public void GetContentSize()
        /// {    
        ///     UInt64 size = 0;        
        ///     size += DmlWriter.PredictNodeHeadSize(DML3Translation.idDMLContentSize, PrimitiveTypes.UInt);
        ///     size += DmlWriter.PredictPrimitiveSize(PredictedSize);
        ///     size += DmlWriter.PredictNodeHeadSize("Width", PrimitiveTypes.UInt);
        ///     size += DmlWriter.PredictPrimitiveSize(Width);
        ///     size += DmlWriter.PredictNodeHeadSize("Height", PrimitiveTypes.UInt);
        ///     size += DmlWriter.PredictPrimitiveSize(Height);
        ///     size ++;                // EndAttributes marker is always 1 byte.        
        ///     
        ///     foreach (...) 
        ///     {
        ///         size += DmlWriter.PredictContainerHeadSize(DMLID_MySubElement);
        ///         size += ((IDmlSerializable)[subelement]).GetSize();
        ///     }
        ///
        ///     size ++;                // EndContainer marker is always 1 byte.
        ///     return size;
        /// }
        /// </example>
        /// </summary>
        UInt64 GetContentSize();
    }

    public class DmlSerializer
    {
        #region "Initialization & Options"

        public DmlSerializer(Type type)
        {
            UseOptions = new Options();
            TopType = Scan(type, null);
            Allow(null, TopType);
        }

        public DmlSerializer(Type type, Options Options)
        {
            UseOptions = Options;
            TopType = Scan(type, null);
            Allow(null, TopType);
        }

        public class Options
        {
            /// <summary>
            /// The encoding of DML structure is endian-neutral, but additional primitives can be enabled which
            /// can use either little or big-endian representation for arrays and matrices.  By default,
            /// the current machine architecture's endianness is used.
            /// </summary>
            public bool LittleEndian = BitConverter.IsLittleEndian;

            /// <summary>
            /// The GenerateTranslation option controls handling of DML associations for which no DMLID is provided.
            /// If false, inline identification is used.  If true, then DMLIDs are assigned.
            /// </summary>
            public bool GenerateTranslation = false;

            public Options() { }
            public Options(bool LittleEndian) { this.LittleEndian = LittleEndian; }
        }

        #endregion

        #region "Internal Data and Heirarchy Scan Information"

        protected Options UseOptions;

        protected bool CommonRequired = false;
        protected bool DecimalFloatRequired = false;
        protected bool ArraysRequired = false;

        protected class AttachInfo
        {
            public PropertyInfo PI;
            public FieldInfo FI;

            public AttachInfo(PropertyInfo PI) { this.PI = PI; }
            public AttachInfo(FieldInfo FI) { this.FI = FI; }
            public AttachInfo() { }

            public object[] GetCustomAttributes()
            {
                if (PI != null) return PI.GetCustomAttributes(false);
                if (FI != null) return FI.GetCustomAttributes(false);
                return null;
            }

            public string Name
            {
                get
                {
                    if (PI != null) return PI.Name;
                    if (FI != null) return FI.Name;
                    return null;
                }
            }

            public object GetValue(object obj)
            {
                if (PI != null) return PI.GetValue(obj, null);
                if (FI != null) return FI.GetValue(obj);
                return null;
            }            

            public void SetValue(object obj, object value)
            {
                if (PI != null)
                {
                    Console.Write("value type = " + value.GetType().ToString() + "\n");
                    if (PI.PropertyType.IsEnum) value = Convert.ChangeType(value, Enum.GetUnderlyingType(PI.PropertyType));
                    switch (Type.GetTypeCode(PI.PropertyType))
                    {
                        case TypeCode.UInt64: PI.SetValue(obj, Convert.ChangeType(value, typeof(UInt64)), null); break;
                        case TypeCode.UInt32: PI.SetValue(obj, Convert.ChangeType(value, typeof(UInt32)), null); break;
                        case TypeCode.UInt16: PI.SetValue(obj, Convert.ChangeType(value, typeof(UInt16)), null); break;
                        case TypeCode.Byte: PI.SetValue(obj, Convert.ChangeType(value, typeof(Byte)), null); break;
                        case TypeCode.Int64: PI.SetValue(obj, Convert.ChangeType(value, typeof(Int64)), null); break;
                        case TypeCode.Int32: PI.SetValue(obj, Convert.ChangeType(value, typeof(Int32)), null); break;
                        case TypeCode.Int16: PI.SetValue(obj, Convert.ChangeType(value, typeof(Int16)), null); break;
                        case TypeCode.SByte: PI.SetValue(obj, Convert.ChangeType(value, typeof(SByte)), null); break;
                        default: PI.SetValue(obj, value, null); break;
                    }
                }
                if (FI != null)
                {
                    if (FI.FieldType.IsEnum) value = Convert.ChangeType(value, Enum.GetUnderlyingType(FI.FieldType));
                    switch (Type.GetTypeCode(FI.FieldType))
                    {
                        case TypeCode.UInt64: FI.SetValue(obj, Convert.ChangeType(value, typeof(UInt64))); break;
                        case TypeCode.UInt32: FI.SetValue(obj, Convert.ChangeType(value, typeof(UInt32))); break;
                        case TypeCode.UInt16: FI.SetValue(obj, Convert.ChangeType(value, typeof(UInt16))); break;
                        case TypeCode.Byte: FI.SetValue(obj, Convert.ChangeType(value, typeof(Byte))); break;
                        case TypeCode.Int64: FI.SetValue(obj, Convert.ChangeType(value, typeof(Int64))); break;
                        case TypeCode.Int32: FI.SetValue(obj, Convert.ChangeType(value, typeof(Int32))); break;
                        case TypeCode.Int16: FI.SetValue(obj, Convert.ChangeType(value, typeof(Int16))); break;
                        case TypeCode.SByte: FI.SetValue(obj, Convert.ChangeType(value, typeof(SByte))); break;
                        default: FI.SetValue(obj, value); break;
                    }
                }
            }

            public string ToContextString()
            {
                if (PI != null) return "\nattached as property '" + PI.Name + "' of type '" + PI.DeclaringType.FullName + "'";
                if (FI != null) return "\nattached as field '" + FI.Name + "' of type '" + FI.DeclaringType.FullName + "'";
                return "";
            }
        }

        protected class SNode
        {
            public Type ObjectType;
            public AttachInfo AttachInfo;
            public Association Association;

            /// <summary>
            /// If Optional is true, then null values for the node are allowed.  This is configured
            /// by the DmlOptional attribute.
            /// </summary>
            public bool Optional = false;

            public override string ToString()
            {
                return GetType().Name + " (" + ObjectType.Name + " <-> " + Association.ToString() + ")";
            }
        }

        protected class SCustom : SNode
        {
            public string TranslationUri;
            public string DocType;
        }

        protected class SPrimitive : SNode
        {
            public bool AsAttribute;
        }

        protected class SObjectArray : SNode
        {
            public List<SNode> Allowed = new List<SNode>();
        }

        protected class SContainer : SNode
        {
            public List<SPrimitive> Attributes = new List<SPrimitive>();
            public List<SNode> Children = new List<SNode>();
        }

        protected class SRootContainer : SContainer
        {
            public string TranslationUri;
            public string DocType;
        }

        SNode TopType;

        #endregion        

        #region "Support Routines"

        void OnPrimitiveSetRequirement(PrimitiveSet ps)
        {
            string Codec = ps.Codec;
            if (Codec == null) Codec = "";
            switch (ps.Set.ToLowerInvariant())
            {                
                case DmlInternalData.psBase: return;
                case DmlInternalData.psCommon:                    
                    switch (Codec.ToLower())
                    {
                        case "le": CommonRequired = true; if (!UseOptions.LittleEndian) throw new Exception("Little-endian common primitive codec required, but big-endian in use."); return;
                        case "be": CommonRequired = true; if (UseOptions.LittleEndian) throw new Exception("Big-endian common primitive codec required, but little-endian in use."); return;
                        case "": CommonRequired = true; return;
                        default: throw new Exception("Common primitive set codec is not recognized by serialization.");
                    }
                case DmlInternalData.psExtPrecision: throw new NotSupportedException("Extended precision floating-point not supported by writer.");
                case DmlInternalData.psDecimalFloat: throw new NotSupportedException("Base-10 (Decimal) floating-point not supported by writer.");
                case DmlInternalData.psArrays:
                    switch (Codec.ToLower())
                    {
                        case "le": ArraysRequired = true; if (!UseOptions.LittleEndian) throw new Exception("Little-endian arrays primitive codec required, but big-endian in use."); return;
                        case "be": ArraysRequired = true; if (UseOptions.LittleEndian) throw new Exception("Big-endian arrays primitive codec required, but little-endian in use."); return;
                        case "": ArraysRequired = true; return;
                        default: throw new Exception("Arrays primitive set codec is not recognized by serialization.");
                    }
                case DmlInternalData.psDecimalArray: throw new NotSupportedException("Decimal floating-point arrays are not supported by writer.");
                default: throw new NotSupportedException("Primitive set not recognized by serialization.");
            }
        }

        #endregion

        #region "Preparation (Initial Type/Hierarchy Scanning)"

        protected virtual PrimitiveTypes GetPrimitiveType(Type type)
        {
            if (type.IsEnum) type = Enum.GetUnderlyingType(type);
            if (type.IsArray)
            {                
                Type ElementType = type.GetElementType();
                PrimitiveTypes UnitType = GetPrimitiveType(ElementType);
                if (UnitType == PrimitiveTypes.Unknown) return PrimitiveTypes.Unknown;
                if (type.GetArrayRank() == 1) return PrimitiveTypes.Array;
                if (type.GetArrayRank() == 2) return PrimitiveTypes.Matrix;
                return PrimitiveTypes.Unknown;
            }
            if (type.IsPrimitive)
            {
                if (type == typeof(Byte)
                 || type == typeof(UInt16)
                 || type == typeof(UInt32)
                 || type == typeof(UInt64)) return PrimitiveTypes.UInt;
                if (type == typeof(SByte)
                 || type == typeof(Int16)
                 || type == typeof(Int32)
                 || type == typeof(Int64)) return PrimitiveTypes.Int;
                if (type == typeof(Single)) return PrimitiveTypes.Single;
                if (type == typeof(Double)) return PrimitiveTypes.Double;
                if (type == typeof(Decimal)) return PrimitiveTypes.Decimal;
                if (type == typeof(Boolean)) return PrimitiveTypes.Boolean;
                return PrimitiveTypes.Unknown;
            }
            if (type == typeof(DateTime)) return PrimitiveTypes.DateTime;
            if (type == typeof(String)) return PrimitiveTypes.String;
            return PrimitiveTypes.Unknown;
        }

        /// <summary>
        /// Scan() provides the primary heirarchial examination of types in preparation
        /// for serialization and deserialization.  The Scan() method is recursive, with
        /// the AI argument providing the necessary connection between the type being
        /// scanned and its attachment to the type heirarchy.  The top-level scan is
        /// initiated by provided an AI of null, which leads to some special cases.
        /// </summary>
        /// <param name="type">The type heirarchy to be scanned.</param>
        /// <param name="AI">Reflection information about the connection to the
        /// heirarchy.  Null at the top-level scan.</param>        
        /// <returns>Scan information contained in an SNode type which should
        /// be attached to the scan information heirarchy.</returns>
        protected virtual SNode Scan(Type type, AttachInfo AI)
        {
            try
            {
                string XMLName = type.Name;                
                uint DMLID = uint.MaxValue;
                string TranslationUri = DmlInternalData.urnBuiltinDML;
                string DocType = null;
                List<SNode> AllowArrayItems = null;
                bool AsAttribute = false;
                bool IsOptional = false;

                // Scan attributes that are attached to the TYPE, i.e. at the class definition:
                Attribute[] attrs = Attribute.GetCustomAttributes(type);
                // Scan XML attributes first (they can be overriden by DML attributes)...
                foreach (Attribute attr in attrs)
                {
                    if (attr is XmlRootAttribute)
                    {
                        XmlRootAttribute xra = (XmlRootAttribute)attr;
                        if (xra.ElementName != null) XMLName = xra.ElementName;
                        AsAttribute = false;
                    }
                    if (attr is XmlElementAttribute)
                    {
                        XmlElementAttribute xea = (XmlElementAttribute)attr;
                        if (xea.ElementName != null) XMLName = xea.ElementName;
                        AsAttribute = false;
                    }
                    if (attr is XmlAttributeAttribute)
                    {
                        XmlAttributeAttribute xaa = (XmlAttributeAttribute)attr;
                        if (xaa.AttributeName != null) XMLName = xaa.AttributeName;
                        AsAttribute = true;
                    }
                }
                // Scan DML attributes...
                foreach (Attribute attr in attrs)
                {
                    if (attr is DmlElementAttribute)
                    {
                        DmlElementAttribute ea = (DmlElementAttribute)attr;
                        if (ea.XMLName != null) XMLName = ea.XMLName;
                        if (ea.DMLID != uint.MaxValue) DMLID = ea.DMLID;
                        AsAttribute = false;
                    }
                    if (attr is DmlRootAttribute)
                    {
                        DmlRootAttribute dt = (DmlRootAttribute)attr;
                        if (dt.XMLName != null) XMLName = dt.XMLName;
                        if (dt.DMLID != uint.MaxValue) DMLID = dt.DMLID;
                        if (dt.TranslationUri != null) TranslationUri = dt.TranslationUri;
                        if (dt.DocType != null) DocType = dt.DocType;
                        AsAttribute = false;
                    }
                    if (attr is DmlPrimitiveSetAttribute)
                    {
                        DmlPrimitiveSetAttribute psa = (DmlPrimitiveSetAttribute)attr;
                        OnPrimitiveSetRequirement(psa.PrimitiveSet);
                    }
                }

                if (AI != null && AI.Name != null) XMLName = AI.Name;

                // Scan attributes that are attached to the MEMBER (field or property):
                object[] oattrs = null;
                if (AI != null) oattrs = AI.GetCustomAttributes();
                bool Ignore = false;
                if (oattrs != null)
                {
                    // Scan XML attributes first (they can be overriden by DML attributes)...
                    foreach (Attribute attr in attrs)
                    {
                        if (attr is XmlIgnoreAttribute) Ignore = true;
                        if (attr is XmlRootAttribute)
                        {
                            XmlRootAttribute xra = (XmlRootAttribute)attr;
                            if (xra.ElementName != null) XMLName = xra.ElementName;
                            AsAttribute = false;
                        }
                        if (attr is XmlElementAttribute)
                        {
                            XmlElementAttribute xea = (XmlElementAttribute)attr;
                            if (xea.ElementName != null) XMLName = xea.ElementName;
                            AsAttribute = false;
                        }
                        if (attr is XmlAttributeAttribute)
                        {
                            XmlAttributeAttribute xaa = (XmlAttributeAttribute)attr;
                            if (xaa.AttributeName != null) XMLName = xaa.AttributeName;
                            AsAttribute = true;
                        }
                        if (attr is XmlArrayAttribute)
                        {
                            XmlArrayAttribute xaa = (XmlArrayAttribute)attr;
                            if (xaa.ElementName != null) XMLName = xaa.ElementName;
                            AsAttribute = false;
                        }
                        if (attr is XmlArrayItemAttribute)
                        {
                            if (AllowArrayItems == null) AllowArrayItems = new List<SNode>();
                            XmlArrayItemAttribute xai = (XmlArrayItemAttribute)attr;
                            if (xai.Type != null)
                            {
                                DmlArrayItemAttribute dai = new DmlArrayItemAttribute(xai.Type);
                                if (xai.ElementName != null) dai.XMLName = xai.ElementName;
                                AllowArrayItems.Add(ScanArrayElement(dai.AllowType, dai));
                            }
                        }
                    }
                    // Scan DML attributes...
                    foreach (object attr in oattrs)
                    {
                        if (attr is DmlIgnoreAttribute) return null;
                        if (attr is DmlOptionalAttribute) IsOptional = true;
                        if (attr is DmlElementAttribute)
                        {
                            DmlElementAttribute ea = (DmlElementAttribute)attr;
                            if (ea.XMLName != null) XMLName = ea.XMLName;
                            if (ea.DMLID != uint.MaxValue) DMLID = ea.DMLID;
                            AsAttribute = false;
                            Ignore = false;
                        }
                        if (attr is DmlAttributeAttribute)
                        {
                            DmlAttributeAttribute ea = (DmlAttributeAttribute)attr;
                            if (ea.XMLName != null) XMLName = ea.XMLName;
                            if (ea.DMLID != uint.MaxValue) DMLID = ea.DMLID;
                            AsAttribute = true;
                            Ignore = false;
                        }
                        if (attr is DmlArrayItemAttribute)
                        {
                            if (AllowArrayItems == null) AllowArrayItems = new List<SNode>();
                            DmlArrayItemAttribute aitem = (DmlArrayItemAttribute)attr;
                            AllowArrayItems.Add(ScanArrayElement(aitem.AllowType, aitem));
                            Ignore = false;
                        }
                        if (attr is DmlPrimitiveSetAttribute)
                        {
                            DmlPrimitiveSetAttribute psa = (DmlPrimitiveSetAttribute)attr;
                            OnPrimitiveSetRequirement(psa.PrimitiveSet);
                            Ignore = false;
                        }
                    }
                    if (Ignore) return null;
                }
                
                if (typeof(IDmlSerializable).IsAssignableFrom(type))  
                {
                    if (AsAttribute)
                        throw new NotSupportedException("Custom serializer may not be applied to DML attributes.");

                    // We have a custom serializer here...let it take over.                
                    SCustom sc = new SCustom();
                    sc.AttachInfo = AI;
                    sc.ObjectType = type;
                    sc.Association = new Association(DMLID, new DmlName(XMLName, NodeTypes.Container), null);
                    sc.TranslationUri = TranslationUri;
                    sc.DocType = DocType;
                    sc.Optional = IsOptional;
                    return sc;
                }

                /** We've rounded up all the attribute-provided information.. now let's build it. **/               

                PrimitiveTypes PType = GetPrimitiveType(type);
                if (PType != PrimitiveTypes.Unknown || type.IsEnum)
                {                    
                    SPrimitive sa = new SPrimitive();
                    sa.AttachInfo = AI;
                    sa.ObjectType = type;
                    sa.AsAttribute = AsAttribute;
                    if (PType == PrimitiveTypes.Array || PType == PrimitiveTypes.Matrix)
                    {
                        if (type != typeof(byte[])) ArraysRequired = true;
                        sa.Association = new Association(DMLID, new DmlName(XMLName, PType,
                            DmlInternalData.GetArrayUnitType(type.GetElementType())));
                    }
                    else if (PType == PrimitiveTypes.Decimal)
                    {
                        DecimalFloatRequired = true;
                        sa.Association = new Association(DMLID, new DmlName(XMLName, PType));
                    }
                    else if (DmlInternalData.IsCommonSet(PType))
                    {
                        CommonRequired = true;
                        sa.Association = new Association(DMLID, new DmlName(XMLName, PType));
                    }
                    else
                        sa.Association = new Association(DMLID, new DmlName(XMLName, PType));
                    sa.Optional = IsOptional;
                    return sa;
                }
                else if (type.IsArray || typeof(System.Collections.IList).IsAssignableFrom(type))
                {
                    // Arrays of primitive types will have been recognized by GetPrimitiveType() and already handled.  This array must
                    // be of a class or structure type, which we can serialize as elements.                

                    if (AsAttribute)
                        throw new NotSupportedException("Only arrays of primitive types can be serialized as attributes.");

                    SObjectArray NewAr = new SObjectArray();
                    NewAr.AttachInfo = AI;
                    NewAr.Association = new Association(DMLID, new DmlName(XMLName, NodeTypes.Container));
                    NewAr.ObjectType = type;
                    NewAr.Optional = IsOptional;

                    bool AlreadyListed = false;
                    Type ElementType = null;

                    if (type.IsArray) ElementType = type.GetElementType();
                    else
                    {
                        foreach (Type interfaceType in type.GetInterfaces())
                        {
                            if (interfaceType.IsGenericType &&
                                interfaceType.GetGenericTypeDefinition() == typeof(IList<>))
                            {
                                ElementType = type.GetGenericArguments()[0];                                
                                break;
                            }
                        }
                    }

                    foreach (SNode se in AllowArrayItems)
                    {
                        if (se.ObjectType == ElementType) AlreadyListed = true;
                        Allow(NewAr, se);
                    }
                    if (!AlreadyListed)
                    {
                        SNode BaseAllowed = ScanArrayElement(ElementType, null);
                        Allow(NewAr, BaseAllowed);
                        NewAr.Allowed.Add(BaseAllowed);
                    }
                    NewAr.Allowed.AddRange(AllowArrayItems);
                    return NewAr;
                }
                else if (type.IsPrimitive)
                {
                    // Recognized by a primitive by C# but not by DML.  A primitive in C# cannot have any attributes
                    // or additional information provided by the type in order to let us know how to encode it, so
                    // we cannot support the type.
                    throw new NotSupportedException("Type is not supported by serialization.");
                }
                else
                {
                    // It is not a primitive or an array, so it must be a class or struct.  We can represent this
                    // as a container.

                    if (AsAttribute)
                        throw new NotSupportedException("Classes and structures cannot be serialized as attributes.");

                    SContainer NewEl;
                    if (AI == null || (TranslationUri != null && TranslationUri != DmlInternalData.urnBuiltinDML))
                    {
                        SRootContainer NewRoot = new SRootContainer();
                        NewRoot.TranslationUri = TranslationUri;
                        NewRoot.DocType = DocType;
                        NewEl = NewRoot;
                    }
                    else
                    {
                        NewEl = new SContainer();
                    }
                    NewEl.AttachInfo = AI;
                    NewEl.ObjectType = type;
                    NewEl.Association = new Association(DMLID, new DmlName(XMLName, NodeTypes.Container));
                    NewEl.Optional = IsOptional;
                    ScanChildren(NewEl);
                    return NewEl;
                }
            }
            catch (Exception ex)
            {                
                throw new Exception(ex.Message
                    + "\nwhile performing serialization scan for type '" + type.FullName + "'" + (AI != null ? AI.ToContextString() : ""), ex);
            }
        }

        protected virtual SNode ScanArrayElement(Type ElementType, DmlArrayItemAttribute ItemAttr)
        {
            try
            {
                string XMLName = ElementType.Name;
                uint DMLID = uint.MaxValue;
                string TranslationUri = DmlInternalData.urnBuiltinDML;
                string DocType = null;

                // Scan attributes that are attached to the TYPE, i.e. at the class definition:
                Attribute[] attrs = Attribute.GetCustomAttributes(ElementType);
                // Scan XML attributes first (they can be overriden by DML attributes)...
                foreach (Attribute attr in attrs)
                {                    
                    if (attr is XmlRootAttribute)
                    {
                        XmlRootAttribute xra = (XmlRootAttribute)attr;
                        if (xra.ElementName != null) XMLName = xra.ElementName;                        
                    }
                    if (attr is XmlElementAttribute)
                    {
                        XmlElementAttribute xea = (XmlElementAttribute)attr;
                        if (xea.ElementName != null) XMLName = xea.ElementName;                        
                    }
                    if (attr is XmlAttributeAttribute)
                    {
                        XmlAttributeAttribute xaa = (XmlAttributeAttribute)attr;
                        if (xaa.AttributeName != null) XMLName = xaa.AttributeName;
                    }                    
                }
                // Scan DML attributes...
                foreach (Attribute attr in attrs)
                {
                    if (attr is DmlElementAttribute)
                    {
                        DmlElementAttribute ea = (DmlElementAttribute)attr;
                        if (ea.XMLName != null) XMLName = ea.XMLName;
                        if (ea.DMLID != uint.MaxValue) DMLID = ea.DMLID;
                    }
                    if (attr is DmlRootAttribute)
                    {
                        DmlRootAttribute dt = (DmlRootAttribute)attr;
                        if (dt.XMLName != null) XMLName = dt.XMLName;
                        if (dt.DMLID != uint.MaxValue) DMLID = dt.DMLID;
                        if (dt.TranslationUri != null) TranslationUri = dt.TranslationUri;
                        if (dt.DocType != null) DocType = dt.DocType;
                    }
                    if (attr is DmlPrimitiveSetAttribute)
                    {
                        DmlPrimitiveSetAttribute psa = (DmlPrimitiveSetAttribute)attr;
                        OnPrimitiveSetRequirement(psa.PrimitiveSet);
                    }
                }

                // Scan the attribute representing the type as it applies to the particular
                // array.  The DmlArrayItemAttribute takes precedence (if specified) over 
                // attributes attached to the type.
                if (ItemAttr != null)
                {
                    if (ItemAttr.XMLName != null) XMLName = ItemAttr.XMLName;
                    if (ItemAttr.DMLID != uint.MaxValue) DMLID = ItemAttr.DMLID;
                }
                
                if (typeof(IDmlSerializable).IsAssignableFrom(ElementType))  
                {
                    // We have a custom serializer here...let it take over.
                    SCustom sc = new SCustom();
                    sc.AttachInfo = new AttachInfo();
                    sc.ObjectType = ElementType;
                    sc.Association = new Association(DMLID, new DmlName(XMLName, NodeTypes.Container), null);
                    sc.TranslationUri = TranslationUri;
                    sc.DocType = DocType;
                    return sc;
                }

                if (ElementType.IsArray)
                {
                    /** The thing that prevents us from supporting arrays-of-arrays (jagged arrays) is a lack of means 
                     *  to specify the DmlArrayItemAttribute attribute for the inner array.  This is not an insurmountable
                     *  difficulty by any means, we could just require properly DmlElementAttribute'd types for the
                     *  inner array.  For now we just don't support it.  
                     *  
                     *  We do support multi-dimensional arrays for types as matrices, however.
                     */
                    throw new NotImplementedException("Dml serialization does not implement jagged arrays.  Provide custom serialization.");
                }
                else if (ElementType.IsPrimitive)
                {
                    // Recognized as a primitive by C# but not by DML.  A primitive in C# cannot have any attributes
                    // or additional information provided by the type in order to let us know how to encode it, so
                    // we cannot support the type.
                    throw new NotSupportedException("Type is not supported by serialization.");
                }
                else
                {
                    SContainer NewEl;
                    if (TranslationUri != null && TranslationUri != DmlInternalData.urnBuiltinDML)
                    {
                        SRootContainer NewRoot = new SRootContainer();
                        NewRoot.TranslationUri = TranslationUri;
                        NewRoot.DocType = DocType;
                        NewEl = NewRoot;
                    }
                    else
                    {
                        NewEl = new SContainer();
                    }
                    NewEl.AttachInfo = new AttachInfo();
                    NewEl.ObjectType = ElementType;
                    NewEl.Association = new Association(DMLID, new DmlName(XMLName, NodeTypes.Container));
                    ScanChildren(NewEl);
                    return NewEl;
                }
            }
            catch (Exception ex)
            {                
                throw new Exception(ex.Message
                    + "\nwhile performing serialization scan for array element type '" + ElementType.FullName + "'.", ex);
            }
        }

        protected void ScanChildren(SContainer Container)
        {
            PropertyInfo[] props = Container.ObjectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in props)
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    Attach(Container, Scan(prop.PropertyType, new AttachInfo(prop)));
                }
            }

            FieldInfo[] fields = Container.ObjectType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo fi in fields)
            {
                Attach(Container, Scan(fi.FieldType, new AttachInfo(fi)));
            }
        }

        protected void Attach(SContainer Container, SNode Entry)
        {
            if (Entry == null) return;               // Happens when DmlIgnore shows up
            if (Entry is SPrimitive)
            {
                if (((SPrimitive)Entry).AsAttribute)
                    Container.Attributes.Add(Entry as SPrimitive);
                else
                    Container.Children.Add(Entry as SPrimitive);
            }
            else if (Entry is SContainer) Container.Children.Add(Entry as SContainer);
            else if (Entry is SObjectArray) Container.Children.Add(Entry as SObjectArray);
            else if (Entry is SCustom) Container.Children.Add(Entry as SCustom);
            else throw new NotSupportedException("Pre-serialization scan failed: Cannot attach " + Entry.ToString() + " to " + Container.ToString());

            Allow(Container, Entry);
        }

        protected void Allow(SNode Container, SNode Entry)
        {
            if (Entry.Association.DMLID == uint.MaxValue)
            {
                if (UseOptions.GenerateTranslation)
                    Entry.Association.DMLID = Container.Association.LocalTranslation.Assign(Entry.Association.DMLName);
                else
                    Entry.Association.DMLID = DML3Translation.idInlineIdentification;
            }

            if (Entry.Association.DMLID != DML3Translation.idInlineIdentification
             && DmlTranslation.DML3.Contains(Entry.Association.DMLID))
                throw new NotSupportedException("Cannot redefine built-in DML identifiers.");

            if (!Entry.Association.InlineIdentification)
            {
                if (UseOptions.GenerateTranslation)                
                    Container.Association.LocalTranslation.Add(Entry.Association);
                else
                {
                    Association tmp;
                    if (!Container.Association.LocalTranslation.TryFind(Entry.Association.DMLID, out tmp))
                        throw new NotSupportedException("During serialization, GenerateTranslation is false but a required association was not found.");
                }
            }
        }

        #endregion

        #region "Serialization"

        /// <summary>
        /// The Serialize() method serializes the type as the top-level of a DML format stream.  A DML header
        /// is generated, and the object is serialized to the top-level container of the DML stream.
        /// </summary>
        /// <param name="stream">The stream to serialize the DML document to.</param>
        /// <param name="obj">The object to serialize.</param>
        public void Serialize(Stream stream, Object obj)
        {
            string Codec = UseOptions.LittleEndian ? "le" : "be";

            List<PrimitiveSet> PrimitiveSets = new List<PrimitiveSet>();
            if (CommonRequired) PrimitiveSets.Add(new PrimitiveSet(DmlInternalData.psCommon, Codec, null));
            if (DecimalFloatRequired) PrimitiveSets.Add(new PrimitiveSet(DmlInternalData.psDecimalFloat, Codec, null));
            if (ArraysRequired) PrimitiveSets.Add(new PrimitiveSet(DmlInternalData.psArrays, Codec, null));

            DmlWriter Writer = DmlWriter.Create(stream);
            foreach (PrimitiveSet ps in PrimitiveSets) Writer.AddPrimitiveSet(ps);
            if (TopType is SRootContainer) Writer.WriteHeader(((SRootContainer)TopType).TranslationUri, null, ((SRootContainer)TopType).DocType, PrimitiveSets.ToArray());
            else if (TopType is SCustom) Writer.WriteHeader(((SCustom)TopType).TranslationUri, null, ((SCustom)TopType).DocType, PrimitiveSets.ToArray());
            Serialize(Writer, obj, TopType);
        }

        /// <summary>
        /// The SerializeContent() method serializes the type as part of a larger DML sequence.  The DmlWriter must
        /// already be initialized and the DML Header must have already been written.  Optionally, the call can be
        /// made to generate a container within a larger DML context.
        /// </summary>
        /// <param name="Writer">The DmlWriter to write the object to.</param>
        /// <param name="obj">The object to be serialized.</param>
        public void SerializeContent(DmlWriter Writer, Object obj)
        {
            string Codec = UseOptions.LittleEndian ? "le" : "be";            
            if (CommonRequired) Writer.AddPrimitiveSet(DmlInternalData.psCommon, Codec);
            if (DecimalFloatRequired) Writer.AddPrimitiveSet(DmlInternalData.psDecimalFloat, Codec);
            if (ArraysRequired) Writer.AddPrimitiveSet(DmlInternalData.psArrays, Codec);
         
            Serialize(Writer, obj, TopType);
        }

        protected virtual void Serialize(DmlWriter Writer, Object obj, SNode Node)
        {            
            if (Node == null) throw new Exception();
            if (obj == null)
            {
                if (Node.Optional) return;
                throw new Exception("Required node was not provided.");
            }
            if (Node is SPrimitive) Serialize(Writer, obj, (SPrimitive)Node);
            else if (Node is SContainer || Node is SRootContainer) Serialize(Writer, obj, (SContainer)Node);
            else if (Node is SObjectArray) Serialize(Writer, obj, (SObjectArray)Node);
            else if (Node is SCustom) Serialize(Writer, obj, (SCustom)Node);
            else throw new Exception();
        }

        protected virtual void Serialize(DmlWriter Writer, Object obj, SContainer Element)
        {
            Writer.WriteStartContainer(Element.Association);            
            foreach (SPrimitive sa in Element.Attributes)
            {
                Object Value = sa.AttachInfo.GetValue(obj);
                Serialize(Writer, Value, sa);
            }
            if (Element.Children.Count > 0)
            {
                Writer.WriteEndAttributes();
                foreach (SNode se in Element.Children)
                {
                    Object Value = se.AttachInfo.GetValue(obj);
                    Serialize(Writer, Value, se);
                }
            }
            Writer.WriteEndContainer();
        }

        protected virtual void Serialize(DmlWriter Writer, Object obj, SObjectArray Ar)
        {
            Writer.WriteStartContainer(Ar.Association);
            Writer.WriteEndAttributes();
            if (obj.GetType().IsArray)
            {
                //object[] ArrayValue = (object[])Ar.AttachInfo.GetValue(obj);
                object[] ArrayValue = (object[])obj;
                foreach (object ElementValue in ArrayValue)
                {
                    bool Found = false;
                    foreach (SNode se in Ar.Allowed)
                    {
                        if (se.ObjectType == ElementValue.GetType()) { Serialize(Writer, ElementValue, se); Found = true; break; }
                    }
                    if (!Found) throw new FormatException("Array must be marked with DmlArrayItemAttribute for type, or type must be base array type.");
                }
            }
            else if (obj is System.Collections.IList)
            {
                //System.Collections.IList ArrayValue = (System.Collections.IList)Ar.AttachInfo.GetValue(obj);
                System.Collections.IList ArrayValue = (System.Collections.IList)obj;
                foreach (object ElementValue in ArrayValue)
                {
                    bool Found = false;
                    foreach (SNode se in Ar.Allowed)
                    {
                        if (se.ObjectType == ElementValue.GetType()) { Serialize(Writer, ElementValue, se); Found = true; break; }
                    }
                    if (!Found) throw new FormatException("Array must be marked with DmlArrayItemAttribute for type, or type must be base array type.");
                }
            }
            Writer.WriteEndContainer();
        }

        protected virtual void Serialize(DmlWriter Writer, Object obj, SPrimitive Attr)
        {
            if (Attr.Association.InlineIdentification)
            {
                string Name = Attr.Association.DMLName.XmlName;
                switch (Attr.Association.DMLName.PrimitiveType)
                {
                    case PrimitiveTypes.Boolean: Writer.Write(Name, (bool)obj); return;
                    case PrimitiveTypes.Int:
                        // Optimization: Is the initial casting to the matching type necessary,
                        // or can we safely cast directly to long and ulong?
                        if (obj is Int32) Writer.Write(Name, (long)(int)obj);
                        else if (obj is sbyte) Writer.Write(Name, (long)(sbyte)obj);
                        else if (obj is Int16) Writer.Write(Name, (long)(Int16)obj);
                        else if (obj is Int64) Writer.Write(Name, (long)obj);
                        else if (obj is Enum) Writer.Write(Name, (long)Convert.ChangeType(obj, typeof(long)));                        
                        else throw new NotSupportedException();
                        return;
                    case PrimitiveTypes.UInt:
                        if (obj is UInt32) Writer.Write(Name, (ulong)(uint)obj);
                        else if (obj is Byte) Writer.Write(Name, (ulong)(byte)obj);
                        else if (obj is UInt16) Writer.Write(Name, (ulong)(UInt16)obj);
                        else if (obj is UInt64) Writer.Write(Name, (ulong)obj);
                        else if (obj is Enum) Writer.Write(Name, (ulong)Convert.ChangeType(obj, typeof(ulong)));
                        else throw new NotSupportedException();
                        return;
                    case PrimitiveTypes.Single: Writer.Write(Name, (float)obj); return;
                    case PrimitiveTypes.Double: Writer.Write(Name, (double)obj); return;
                    //case PrimitiveTypes.Decimal: Writer.Write(Name, (decimal)obj); return;                
                    case PrimitiveTypes.DateTime: Writer.Write(Name, (DateTime)obj); return;
                    case PrimitiveTypes.String: if (obj == null) Writer.Write(Name, ""); else Writer.Write(Name, (string)obj); return;
                    case PrimitiveTypes.Array:
                        switch (Attr.Association.DMLName.ArrayType)
                        {
                            case ArrayTypes.U8: Writer.Write(Name, (byte[])obj); return;
                            case ArrayTypes.U16: Writer.Write(Name, (ushort[])obj); return;
                            case ArrayTypes.U32: Writer.Write(Name, (uint[])obj); return;
                            case ArrayTypes.U64: Writer.Write(Name, (ulong[])obj); return;
                            case ArrayTypes.I8: Writer.Write(Name, (sbyte[])obj); return;
                            case ArrayTypes.I16: Writer.Write(Name, (short[])obj); return;
                            case ArrayTypes.I32: Writer.Write(Name, (int[])obj); return;
                            case ArrayTypes.I64: Writer.Write(Name, (long[])obj); return;
                            case ArrayTypes.Singles: Writer.Write(Name, (float[])obj); return;
                            case ArrayTypes.Doubles: Writer.Write(Name, (double[])obj); return;
                            case ArrayTypes.Decimals: Writer.Write(Name, (decimal[])obj); return;
                            case ArrayTypes.DateTimes: Writer.Write(Name, (DateTime[])obj); return;
                            case ArrayTypes.Strings: Writer.Write(Name, (string[])obj); return;
                            default: throw new NotSupportedException();
                        }
                    case PrimitiveTypes.Matrix:     // TODO: Revisit matrices
                        throw new NotImplementedException();
                    default: throw new NotSupportedException();
                }
            }
            else
            {
                uint ID = Attr.Association.DMLID;
                switch (Attr.Association.DMLName.PrimitiveType)
                {
                    case PrimitiveTypes.Boolean: Writer.Write(ID, (bool)obj); return;
                    case PrimitiveTypes.Int:
                        // Optimization: Is the initial casting to the matching type necessary,
                        // or can we safely cast directly to long and ulong?
                        if (obj is Int32) Writer.Write(ID, (long)(int)obj);
                        else if (obj is sbyte) Writer.Write(ID, (long)(sbyte)obj);
                        else if (obj is Int16) Writer.Write(ID, (long)(Int16)obj);
                        else if (obj is Int64) Writer.Write(ID, (long)obj);
                        return;
                    case PrimitiveTypes.UInt:
                        if (obj is UInt32) Writer.Write(ID, (ulong)(uint)obj);
                        else if (obj is Byte) Writer.Write(ID, (ulong)(byte)obj);
                        else if (obj is UInt16) Writer.Write(ID, (ulong)(UInt16)obj);
                        else if (obj is UInt64) Writer.Write(ID, (ulong)obj);
                        return;
                    case PrimitiveTypes.Single: Writer.Write(ID, (float)obj); return;
                    case PrimitiveTypes.Double: Writer.Write(ID, (double)obj); return;
                    //case PrimitiveTypes.Decimal: Writer.Write(ID, (decimal)obj); return;                
                    case PrimitiveTypes.DateTime: Writer.Write(ID, (DateTime)obj); return;
                    case PrimitiveTypes.String: Writer.Write(ID, (string)obj); return;
                    case PrimitiveTypes.Array:
                        switch (Attr.Association.DMLName.ArrayType)
                        {
                            case ArrayTypes.U8: Writer.Write(ID, (byte[])obj); return;
                            case ArrayTypes.U16: Writer.Write(ID, (ushort[])obj); return;
                            case ArrayTypes.U32: Writer.Write(ID, (uint[])obj); return;
                            case ArrayTypes.U64: Writer.Write(ID, (ulong[])obj); return;
                            case ArrayTypes.I8: Writer.Write(ID, (sbyte[])obj); return;
                            case ArrayTypes.I16: Writer.Write(ID, (short[])obj); return;
                            case ArrayTypes.I32: Writer.Write(ID, (int[])obj); return;
                            case ArrayTypes.I64: Writer.Write(ID, (long[])obj); return;
                            case ArrayTypes.Singles: Writer.Write(ID, (float[])obj); return;
                            case ArrayTypes.Doubles: Writer.Write(ID, (double[])obj); return;
                            case ArrayTypes.Decimals: Writer.Write(ID, (decimal[])obj); return;
                            case ArrayTypes.DateTimes: Writer.Write(ID, (DateTime[])obj); return;
                            case ArrayTypes.Strings: Writer.Write(ID, (string[])obj); return;
                            default: throw new NotSupportedException();
                        }
                    case PrimitiveTypes.Matrix:     // TODO: Revisit matrices
                        throw new NotImplementedException();
                    default: throw new NotSupportedException();
                }
            }
        }

        protected virtual void Serialize(DmlWriter Writer, Object obj, SCustom Custom)
        {
            IDmlSerializable DmlInterface = (IDmlSerializable)obj;            
            Writer.WriteStartContainer(Custom.Association);
            DmlInterface.WriteDml(Writer);
        }

        #endregion

        #region "Deserialization"
        
        /// <summary>
        /// The Deserialize() method generates an in-memory object from the top-level element of a DML
        /// formatted stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize the object from.</param>
        /// <returns>An object generated from the DML stream's top-level content.</returns>
        public object Deserialize(Stream stream)
        {
            DmlReader Reader = DmlReader.Create(stream);
            if (TopType.Association.LocalTranslation != null)
                Reader.GlobalTranslation = TopType.Association.LocalTranslation;
            return Deserialize(Reader);
        }

        protected object Deserialize(DmlReader Reader, IResourceResolution References = null)
        {
            Reader.ParseHeader(References);
            while (Reader.Read())
            {
                if (Reader.Association == TopType.Association) { return DeserializeContent(Reader); }
                else throw CreateDmlException("DML top-level element did not match deserialization type.", Reader);
            }
            throw CreateDmlException("Document did not contain deserialization type.", Reader, new EndOfStreamException());
        }

        /// <summary>
        /// The DeserializeContent() method performs deserialization of a type embedded within a larger DML
        /// sequence.  DeserializeContent() must be called after the matching type has been identified
        /// by a Read() call on the DmlReader, but before the type has been read.  The reader must possess
        /// a global translation which includes all necessary associations for successful deserialization.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public object DeserializeContent(DmlReader Reader)
        {
            if (TopType is SContainer) return DeserializeContainer(Reader, TopType as SContainer);
            else if (TopType is SCustom) return DeserializeCustom(Reader, TopType as SCustom);
            else throw CreateDmlException("Not supported.", Reader);
        }

        protected object DeserializeContainer(DmlReader Reader, SContainer Container)
        {
            Object ret = Activator.CreateInstance(Container.ObjectType);
            while (Reader.Read())
            {
                bool Found = false;
                switch (Reader.NodeType)
                {
                    case NodeTypes.EndContainer: return ret;                    
                    case NodeTypes.Primitive:
                        if (Reader.IsAttribute)
                        {
                            foreach (SNode se in Container.Attributes)
                            {                                
                                if (se.Association != Reader.Association) continue;
                                Found = true;
                                if (se is SCustom)
                                {
                                    object obj = DeserializeCustom(Reader, (SCustom)se);
                                    se.AttachInfo.SetValue(ret, obj);
                                    break;
                                }
                                SPrimitive sp = se as SPrimitive;
                                if (sp == null) throw CreateDmlException("Expected primitive type.", Reader);
                                object ValueObj = DeserializePrimitive(Reader, sp);
                                sp.AttachInfo.SetValue(ret, ValueObj);
                                break;
                            }
                        }
                        else
                        {
                            foreach (SNode se in Container.Children)
                            {
                                if (se.Association != Reader.Association) continue;
                                Found = true;
                                if (se is SCustom)
                                {
                                    object obj = DeserializeCustom(Reader, (SCustom)se);
                                    se.AttachInfo.SetValue(ret, obj);
                                    break;
                                }
                                SPrimitive sp = se as SPrimitive;
                                if (sp == null) throw CreateDmlException("Expected primitive type.", Reader);
                                object ValueObj = DeserializePrimitive(Reader, sp);
                                sp.AttachInfo.SetValue(ret, ValueObj);
                                break;
                            }
                        }
                        if (Found) continue;
                        throw CreateDmlException("Dml deserialization failed because type (" + Container.ObjectType.Name + ") did not contain a property or field matching node '" + Reader.Name + "'.", Reader);
                    case NodeTypes.Container:
                        foreach (SNode se in Container.Children)
                        {
                            if (se.Association == Reader.Association)
                            {
                                object obj;
                                if (se is SContainer) obj = DeserializeContainer(Reader, (SContainer)se);
                                else if (se is SObjectArray) obj = DeserializeArray(Reader, (SObjectArray)se);                                
                                else if (se is SCustom) obj = DeserializeCustom(Reader, (SCustom)se);
                                else throw CreateDmlException("Type not recognized during deserialization.", Reader);
                                se.AttachInfo.SetValue(ret, obj);
                                Found = true;
                                break;
                            }
                        }
                        if (Found) continue;
                        throw CreateDmlException("Dml deserialization failed because type (" + Container.ObjectType.Name + ") did not contain a class or structure type property or field matching node '" + Reader.Name + "'.", Reader);                        
                    case NodeTypes.Comment: continue;
                    case NodeTypes.EndAttributes: continue;
                    default:
                        throw CreateDmlException("Unrecognized DML node type while deserializing.", Reader);
                }
            }
            throw CreateDmlException("Stream terminated before proper DML closure.", Reader, new EndOfStreamException());
        }

        protected object DeserializeArray(DmlReader Reader, SObjectArray ArrayInfo)
        {
            List<object> ArrayRet = new List<object>();
            while (Reader.Read())
            {
                switch (Reader.NodeType)
                {
                    case NodeTypes.EndContainer:
                        {
                            if (ArrayInfo.ObjectType.IsArray)
                                return ArrayRet.ToArray();
                            else if (typeof(System.Collections.IList).IsAssignableFrom(ArrayInfo.ObjectType))
                            {
                                Object ret = Activator.CreateInstance(ArrayInfo.ObjectType);
                                if (ret is System.Collections.IList)
                                {
                                    System.Collections.IList list = (System.Collections.IList)ret;
                                    foreach (object obj in ArrayRet) list.Add(obj);
                                }
                                return ret;
                            }
                            else throw CreateDmlException("Dml deserialization failed - unable to determine array/list configuration.", Reader);
                        }
                    case NodeTypes.Primitive:                    
                        throw CreateDmlException("Dml deserialization failed because array (" + ArrayInfo.ObjectType.Name + ") contained an "
                            + "unexpected primitive type.  Dml arrays for serialization can contain reference or value types, but not both.", Reader);
                    case NodeTypes.Container:
                        {
                            bool Found = false;
                            foreach (SNode se in ArrayInfo.Allowed)
                            {
                                if (se.Association == Reader.Association)
                                {
                                    if (se is SContainer)
                                    {
                                        object chObj = DeserializeContainer(Reader, (SContainer)se);
                                        ArrayRet.Add(chObj);
                                        Found = true;
                                        break;
                                    }
                                    else if (se is SObjectArray)
                                    {
                                        /** Arrays of arrays could be supported, but presently aren't.
                                         */
                                        throw CreateDmlException("Unsupported type.", Reader);
                                    }
                                    else if (se is SCustom)
                                    {
                                        object obj = DeserializeCustom(Reader, (SCustom)se);
                                        ArrayRet.Add(obj);
                                        Found = true;
                                        break;
                                    }
                                    else throw CreateDmlException("Unrecognized pattern.", Reader);
                                }
                            }
                            if (Found) continue;
                            throw CreateDmlException("Dml deserialization failed because type (" + ArrayInfo.ObjectType.Name + ") did not have a DmlArrayItemAttribute or type matching '" + Reader.Name + "'.", Reader);
                        }
                    case NodeTypes.Comment: continue;
                    case NodeTypes.EndAttributes: continue;
                    default:
                        throw CreateDmlException("Unrecognized DML node type while deserializing.", Reader);
                }
            }
            throw CreateDmlException("Stream terminated before DML closure.", Reader, new EndOfStreamException());
        }

        protected virtual object DeserializePrimitive(DmlReader Reader, SPrimitive Attr)
        {
            switch (Reader.PrimitiveType)
            {
                case PrimitiveTypes.Boolean: return Reader.GetBoolean();
                case PrimitiveTypes.DateTime: return Reader.GetDateTime();                
                case PrimitiveTypes.Int: return Reader.GetInt();
                case PrimitiveTypes.Single: return Reader.GetSingle();
                case PrimitiveTypes.Double: return Reader.GetDouble();
                case PrimitiveTypes.String: return Reader.GetString();
                case PrimitiveTypes.UInt: return Reader.GetUInt();                
                case PrimitiveTypes.Decimal: return Reader.GetDecimal();
                case PrimitiveTypes.Array:
                    switch (Reader.ArrayType)
                    {
                        case ArrayTypes.U8: return Reader.GetByteArray();
                        case ArrayTypes.U16: return Reader.GetUInt16Array();
                        case ArrayTypes.U24: return Reader.GetUInt24Array();
                        case ArrayTypes.U32: return Reader.GetUInt32Array();
                        case ArrayTypes.U64: return Reader.GetUInt64Array();
                        case ArrayTypes.I8: return Reader.GetSByteArray();
                        case ArrayTypes.I16: return Reader.GetInt16Array();
                        case ArrayTypes.I24: return Reader.GetInt24Array();
                        case ArrayTypes.I32: return Reader.GetInt32Array();
                        case ArrayTypes.I64: return Reader.GetInt64Array();
                        case ArrayTypes.Singles: return Reader.GetSingleArray();
                        case ArrayTypes.Doubles: return Reader.GetDoubleArray();
                        //case ArrayTypes.Decimals: return Reader.GetDecimalArray();
                        case ArrayTypes.DateTimes: return Reader.GetDateTimeArray();
                        case ArrayTypes.Strings: return Reader.GetStringArray();
                        default: throw CreateDmlException("Unrecognized array type during deserialization.", Reader);
                    }
                case PrimitiveTypes.Matrix:
                    throw CreateDmlException("Not implemented.", Reader, new NotImplementedException());
                default: throw CreateDmlException("Unrecognized primitive.", Reader);
            }
        }

        protected virtual object DeserializeCustom(DmlReader Reader, SCustom Custom)
        {
            Object ret = Activator.CreateInstance(Custom.ObjectType);
            IDmlSerializable DmlInterface = (IDmlSerializable)ret;
            DmlInterface.ReadDml(Reader);
            return ret;
        }

        protected DmlException CreateDmlException(string Message, DmlReader Reader)
        {
            throw Reader.CreateDmlException(Message);
        }

        protected DmlException CreateDmlException(string Message, DmlReader Reader, Exception innerException)
        {
            throw Reader.CreateDmlException(Message, innerException);
        }

        #endregion

        #region "Translation Document Generation"

        /// <summary>
        /// GlobalTranslation returns the DmlTranslation generated at construction.  The
        /// GenerateTranslation option should be turned on during construction in order
        /// to assign all associations a DMLID that do not have one.  The generated 
        /// translation is machine-generated and may not provide the most clear 
        /// representation of the translation.  All references in the translation are 
        /// resolved (there are no %lt;Include%gt; references) and no ancillary
        /// markup such as the InformationURI or XMLRoot.  However, the generated
        /// translation can provide a starting point for creating a formal translation
        /// document.
        /// </summary>
        public DmlTranslation GlobalTranslation
        {
            get
            {
                return TopType.Association.LocalTranslation;
            }
        }

        #endregion        
    }
}