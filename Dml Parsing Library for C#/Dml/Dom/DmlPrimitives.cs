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
using WileyBlack.Dml;
using WileyBlack.Dml.EC;

namespace WileyBlack.Dml.Dom
{
    public abstract class DmlPrimitive : DmlNode
    {
        public bool IsAttribute = false;

        public DmlPrimitive()
        {
            Association.DMLName.NodeType = NodeTypes.Primitive;
            Association.DMLName.PrimitiveType = PrimitiveType;
        }

        public DmlPrimitive(DmlDocument Document)
            : base(Document)
        {
            Association.DMLName.NodeType = NodeTypes.Primitive;
            Association.DMLName.PrimitiveType = PrimitiveType;
        }        

        public abstract PrimitiveTypes PrimitiveType { get; }

        protected virtual UInt64 GetNodeHeadSize()
        {
            if (InlineIdentification)
                return DmlWriter.PredictNodeHeadSize(Name, DmlInternalData.PrimitiveTypeToString(PrimitiveType, ArrayTypes.Unknown));
            else
                return DmlWriter.PredictNodeHeadSize(ID);
        }

        public abstract object Value
        {
            get;
            set;
        }

        protected virtual void Validate()
        {
            if (Association.DMLName.NodeType != NodeTypes.Primitive)
                throw new Exception("Invalid association: DOM Primitive must be a Primitive node type.");
            if (Association.DMLName.PrimitiveType != PrimitiveType)
                throw new Exception("Invalid association: DOM Primitive type does not match associated primitive type.  (XmlName='" + Association.DMLName.XmlName + "', Type=" + Association.DMLName.PrimitiveType + "/" + PrimitiveType + ")");
        }

        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlInt cp = new DmlInt(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.Value = Value;
            return cp;
        }
    }

    #region "Base Primitives"

    /** The U8 array type is also part of the base primitive set, but is 
     *  listed under the array primitives for consistency. **/

    public class DmlInt : DmlPrimitive
    {
        private Int64 m_Value;
        public override object Value
        {
            get { return m_Value; }
            set {
                if (value is Int32) m_Value = (Int64)(Int32)value;
                else if (value is Int64) m_Value = (Int64)value;
                else throw new FormatException();                
            }
        }

        public DmlInt() : base() { }
        public DmlInt(DmlDocument Document) : base(Document) { }
        public override PrimitiveTypes PrimitiveType { get { return PrimitiveTypes.Int; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Value = Reader.GetInt(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate();
            if (InlineIdentification)
                Writer.Write(Name, m_Value);
            else
                Writer.Write(DmlID, m_Value);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Value);
        }
        public override string ToString()
        {
            if (IsAttribute) return Name + "=\"" + m_Value.ToString() + "\"";
            else return "<" + Name + ">" + m_Value.ToString() + "</" + Name + ">";
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlInt cp = new DmlInt(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Value = m_Value;
            return cp;
        }
    }

    public class DmlUInt : DmlPrimitive
    {
        private UInt64 m_Value;
        public override object Value
        {
            get { return m_Value; }
            set {
                if (value is UInt32) m_Value = (UInt64)(UInt32)value;
                else if (value is UInt64) m_Value = (UInt64)value;
                else throw new FormatException();
            }
        }

        public DmlUInt() : base() { }
        public DmlUInt(DmlDocument Document) : base(Document) { }
        public override PrimitiveTypes PrimitiveType { get { return PrimitiveTypes.UInt; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Value = Reader.GetUInt(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate();
            if (InlineIdentification)
                Writer.Write(Name, m_Value);
            else
                Writer.Write(DmlID, m_Value);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Value);
        }
        public override string ToString()
        {
            if (IsAttribute) return Name + "=\"" + m_Value.ToString() + "\"";
            else return "<" + Name + ">" + m_Value.ToString() + "</" + Name + ">";
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlUInt cp = new DmlUInt(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Value = m_Value;
            return cp;
        }
    }

    public class DmlBool : DmlPrimitive
    {
        private bool m_Value;
        public override object Value
        {
            get { return m_Value; }
            set { m_Value = (bool)value; }
        }

        public DmlBool() : base() { }
        public DmlBool(DmlDocument Document) : base(Document) { }
        public override PrimitiveTypes PrimitiveType { get { return PrimitiveTypes.Boolean; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Value = Reader.GetBoolean(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate();
            if (InlineIdentification)
                Writer.Write(Name, m_Value);
            else
                Writer.Write(DmlID, m_Value);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Value);
        }
        public override string ToString()
        {
            if (IsAttribute) return Name + "=\"" + m_Value.ToString() + "\"";
            else return "<" + Name + ">" + m_Value.ToString() + "</" + Name + ">";
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlBool cp = new DmlBool(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Value = m_Value;
            return cp;
        }
    }

    public class DmlString : DmlPrimitive
    {
        private string m_Value = "";
        public override object Value
        {
            get { return m_Value; }
            set { m_Value = (string)value; }
        }

        public DmlString() : base() { }
        public DmlString(DmlDocument Document) : base(Document) { }
        public DmlString(DmlDocument Document, string m_Value) : base(Document) { this.m_Value = m_Value; }
        public override PrimitiveTypes PrimitiveType { get { return PrimitiveTypes.String; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Value = Reader.GetString(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate();
            if (InlineIdentification)
                Writer.Write(Name, m_Value);
            else
                Writer.Write(DmlID, m_Value);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Value);
        }
        private string XmlEncode(string ss)
        {
            return ss.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("\'", "&apos;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
        public override string ToString()
        {
            if (IsAttribute) return Name + "=\"" + XmlEncode(m_Value) + "\"";
            else return "<" + Name + ">" + XmlEncode(m_Value) + "</" + Name + ">";
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlString cp = new DmlString(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Value = m_Value;
            return cp;
        }
    }

    #endregion

    #region "Common Primitives"

    public class DmlSingle : DmlPrimitive
    {
        private float m_Value;
        public override object Value
        {
            get { return m_Value; }
            set { m_Value = (float)value; }
        }

        public DmlSingle() : base() { }
        public DmlSingle(DmlDocument Document) : base(Document) { }
        public override PrimitiveTypes PrimitiveType { get { return PrimitiveTypes.Single; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Value = Reader.GetSingle(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate();
            if (InlineIdentification)
                Writer.Write(Name, m_Value);
            else
                Writer.Write(DmlID, m_Value);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Value);
        }
        public override string ToString()
        {
            if (IsAttribute) return Name + "=\"" + m_Value.ToString() + "\"";
            else return "<" + Name + ">" + m_Value.ToString() + "</" + Name + ">";
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlSingle cp = new DmlSingle(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Value = m_Value;
            return cp;
        }
    }

    public class DmlDouble : DmlPrimitive
    {
        private double m_Value;
        public override object Value
        {
            get { return m_Value; }
            set { m_Value = (double)value; }
        }

        public DmlDouble() : base() { }
        public DmlDouble(DmlDocument Document) : base(Document) { }
        public override PrimitiveTypes PrimitiveType { get { return PrimitiveTypes.Double; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Value = Reader.GetDouble(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate();
            if (InlineIdentification)
                Writer.Write(Name, m_Value);
            else
                Writer.Write(DmlID, m_Value);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Value);
        }
        public override string ToString()
        {
            if (IsAttribute) return Name + "=\"" + m_Value.ToString() + "\"";
            else return "<" + Name + ">" + m_Value.ToString() + "</" + Name + ">";
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlDouble cp = new DmlDouble(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Value = m_Value;
            return cp;
        }
    }

    public class DmlDateTime : DmlPrimitive
    {
        private DateTime m_Value;
        public override object Value
        {
            get { return m_Value; }
            set { m_Value = (DateTime)value; }
        }

        public DmlDateTime() : base() { }
        public DmlDateTime(DmlDocument Document) : base(Document) { }
        public override PrimitiveTypes PrimitiveType { get { return PrimitiveTypes.DateTime; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Value = Reader.GetDateTime(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate();
            if (InlineIdentification)
                Writer.Write(Name, m_Value);
            else
                Writer.Write(DmlID, m_Value);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Value);
        }
        public override string ToString()
        {
            if (IsAttribute) return Name + "=\"" + m_Value.ToString() + "\"";
            else return "<" + Name + ">" + m_Value.ToString() + "</" + Name + ">";
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlDateTime cp = new DmlDateTime(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Value = m_Value;
            return cp;
        }
    }

    #endregion

    #region "Decimal-Float Primitives"
#if false
    public class DmlDecimal : DmlPrimitive
    {
        private decimal m_Value;
        public override object Value
        {
            get { return m_Value; }
            set { m_Value = (decimal)value; }
        }

        public DmlDecimal(DmlDocument Document) : base(Document) {  }
        public override PrimitiveTypes PrimitiveType { get { return PrimitiveTypes.Decimal; } }
        public override void LoadContent(DmlReader Reader) { m_Value = Reader.GetDecimal(); }
        public override void WriteTo(DmlWriter Writer)
        {
            if (InlineIdentification)
                Writer.Write(Name, m_Value);
            else
                Writer.Write(DmlID, m_Value);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Value);
        }
    }
#endif
    #endregion

    #region "Array Primitives"

    public abstract class DmlArray : DmlPrimitive
    {
        public DmlArray()
            : base()
        {
            Association.DMLName.ArrayType = ArrayType;
        }
        public DmlArray(DmlDocument Document)
            : base(Document)
        {
            Association.DMLName.ArrayType = ArrayType;
        }
        public override PrimitiveTypes PrimitiveType { get { return PrimitiveTypes.Array; } }
        public abstract ArrayTypes ArrayType { get; }

        public abstract void SetElement(long iElement, object element);
        public abstract object GetElement(long iElement);

        protected override UInt64 GetNodeHeadSize()
        {
            if (InlineIdentification)
                return DmlWriter.PredictNodeHeadSize(Name, DmlInternalData.PrimitiveTypeToString(PrimitiveType, ArrayType));
            else
                return DmlWriter.PredictNodeHeadSize(ID);
        }

        protected override void Validate()
        {
            if (Association.DMLName.NodeType != NodeTypes.Primitive)
                throw new Exception("Invalid association: DOM Primitive must be a Primitive node type.");
            if (Association.DMLName.PrimitiveType != PrimitiveType)
                throw new Exception("Invalid association: DOM Primitive type does not match associated primitive type.");
            if (Association.DMLName.ArrayType != ArrayType)
                throw new Exception("Invalid association: DOM Array type does not match associated array type.");
        }

        public abstract long ArrayLength { get; }
    }

    public abstract class DmlSignedIntArray : DmlArray
    {
        public DmlSignedIntArray() : base() { }
        public DmlSignedIntArray(DmlDocument Document) : base(Document) { }
    }

    public abstract class DmlUnsignedIntArray : DmlArray
    {
        public DmlUnsignedIntArray() : base() { }
        public DmlUnsignedIntArray(DmlDocument Document) : base(Document) { }
    }

    public abstract class DmlFloatingPointArray : DmlArray
    {
        public DmlFloatingPointArray() : base() { }
        public DmlFloatingPointArray(DmlDocument Document) : base(Document) { }
    }

    #region "Signed Arrays"

    public class DmlSByteArray : DmlSignedIntArray
    {
        private sbyte[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (sbyte[])value; }
        }

        public DmlSByteArray() : base() { }
        public DmlSByteArray(DmlDocument Document) : base(Document) { }
        public DmlSByteArray(DmlDocument Document, sbyte[] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.I8; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetSByteArray(); }
        public override void SetElement(long iElement, object element) { m_Values[iElement] = (sbyte)element; }
        public override object GetElement(long iElement) { return m_Values[iElement]; }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlSByteArray cp = new DmlSByteArray(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    public class DmlInt16Array : DmlSignedIntArray
    {
        private Int16[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (short[])value; }
        }

        public DmlInt16Array() : base() { }
        public DmlInt16Array(DmlDocument Document) : base(Document) { }
        public DmlInt16Array(DmlDocument Document, short[] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.I16; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetInt16Array(); }
        public override void SetElement(long iElement, object element) { m_Values[iElement] = (short)element; }
        public override object GetElement(long iElement) { return m_Values[iElement]; }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlInt16Array cp = new DmlInt16Array(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

#   if false
    public class DmlInt24Array : DmlSignedIntArray
    {
        private Int32[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (int[])value; }
        }

        public DmlInt24Array(DmlDocument Document) : base(Document) {  }
        public override ArrayTypes ArrayType { get { return ArrayTypes.I24; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { m_Values = Reader.GetInt24Array(); }
        public override void WriteTo(DmlWriter Writer) { Writer.WriteArrayAsInt24(DmlID, m_Values); }
        public override UInt64 GetEncodedSize(DmlWriter Writer) { return DmlWriter.PredictEncodedSize(DmlID, (ulong)(m_Values.LongLength * 3)); }
    }
#   endif

    public class DmlInt32Array : DmlSignedIntArray
    {
        private Int32[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (int[])value; }
        }

        public DmlInt32Array() : base() { }
        public DmlInt32Array(DmlDocument Document) : base(Document) { }
        public DmlInt32Array(DmlDocument Document, int[] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.I32; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetInt32Array(); }
        public override void SetElement(long iElement, object element) { m_Values[iElement] = (int)element; }
        public override object GetElement(long iElement) { return m_Values[iElement]; }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlInt32Array cp = new DmlInt32Array(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    public class DmlInt64Array : DmlSignedIntArray
    {
        private Int64[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (long[])value; }
        }

        public DmlInt64Array() : base() { }
        public DmlInt64Array(DmlDocument Document) : base(Document) { }
        public DmlInt64Array(DmlDocument Document, long[] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.I64; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetInt64Array(); }
        public override void SetElement(long iElement, object element) { m_Values[iElement] = (long)element; }
        public override object GetElement(long iElement) { return m_Values[iElement]; }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlInt64Array cp = new DmlInt64Array(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    #endregion
    #region "Unsigned Arrays"

    public class DmlByteArray : DmlUnsignedIntArray
    {
        private byte[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (byte[])value; }
        }

        public DmlByteArray() : base() { }
        public DmlByteArray(DmlDocument Document) : base(Document) { }
        public DmlByteArray(DmlDocument Document, byte[] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.U8; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetByteArray(); }
        public override void SetElement(long iElement, object element) { m_Values[iElement] = (byte)element; }
        public override object GetElement(long iElement) { return m_Values[iElement]; }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlByteArray cp = new DmlByteArray(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    public class DmlUInt16Array : DmlUnsignedIntArray
    {
        private UInt16[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (ushort[])value; }
        }

        public DmlUInt16Array() : base() { }
        public DmlUInt16Array(DmlDocument Document) : base(Document) { }
        public DmlUInt16Array(DmlDocument Document, ushort[] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.U16; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetUInt16Array(); }
        public override void SetElement(long iElement, object element) { m_Values[iElement] = (ushort)element; }
        public override object GetElement(long iElement) { return m_Values[iElement]; }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlUInt16Array cp = new DmlUInt16Array(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

#   if false
    public class DmlUInt24Array : DmlUnsignedIntArray
    {
        private UInt32[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (uint[])value; }
        }

        public DmlUInt24Array(DmlDocument Document) : base(Document) {  }
        public override ArrayTypes ArrayType { get { return ArrayTypes.U24; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { m_Values = Reader.GetUInt24Array(); }
        public override void WriteTo(DmlWriter Writer) { Writer.WriteArrayAsUInt24(DmlID, m_Values); }
        public override UInt64 GetEncodedSize(DmlWriter Writer) { return DmlWriter.PredictEncodedSize(DmlID, (ulong)m_Values.LongLength * 3UL); }
    }
#   endif

    public class DmlUInt32Array : DmlUnsignedIntArray
    {
        private UInt32[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (uint[])value; }
        }

        public DmlUInt32Array() : base() { }
        public DmlUInt32Array(DmlDocument Document) : base(Document) { }
        public DmlUInt32Array(DmlDocument Document, uint[] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.U32; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetUInt32Array(); }
        public override void SetElement(long iElement, object element) { m_Values[iElement] = (uint)element; }
        public override object GetElement(long iElement) { return m_Values[iElement]; }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlUInt32Array cp = new DmlUInt32Array(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    public class DmlUInt64Array : DmlUnsignedIntArray
    {
        private UInt64[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (ulong[])value; }
        }

        public DmlUInt64Array() : base() { }
        public DmlUInt64Array(DmlDocument Document) : base(Document) { }
        public DmlUInt64Array(DmlDocument Document, ulong[] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.U64; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetUInt64Array(); }
        public override void SetElement(long iElement, object element) { m_Values[iElement] = (ulong)element; }
        public override object GetElement(long iElement) { return m_Values[iElement]; }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlUInt64Array cp = new DmlUInt64Array(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    #endregion

    #region "Floating-Point Arrays"

    public class DmlSingleArray : DmlFloatingPointArray
    {
        private float[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (float[])value; }
        }

        public DmlSingleArray() : base() { }
        public DmlSingleArray(DmlDocument Document) : base(Document) { }
        public DmlSingleArray(DmlDocument Document, float[] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.Singles; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetSingleArray(); }
        public override void SetElement(long iElement, object element) { m_Values[iElement] = (float)element; }
        public override object GetElement(long iElement) { return m_Values[iElement]; }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlSingleArray cp = new DmlSingleArray(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    public class DmlDoubleArray : DmlFloatingPointArray
    {
        private double[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (double[])value; }
        }

        public DmlDoubleArray() : base() { }
        public DmlDoubleArray(DmlDocument Document) : base(Document) { }
        public DmlDoubleArray(DmlDocument Document, double[] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.Doubles; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetDoubleArray(); }
        public override void SetElement(long iElement, object element) { m_Values[iElement] = (double)element; }
        public override object GetElement(long iElement) { return m_Values[iElement]; }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlDoubleArray cp = new DmlDoubleArray(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

#   if false
    public class DmlDecimalArray : DmlFloatingPointArray
    {
        private decimal[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (decimal[])value; }
        }

        public DmlDecimalArray(DmlDocument Document) : base(Document) {  }
        public override ArrayTypes ArrayType { get { return ArrayTypes.Decimals; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { m_Values = Reader.GetDecimalArray(); }
        public override void WriteTo(DmlWriter Writer)
        {
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
    }
#   endif

    #endregion

    #region "Other Arrays"

    public class DmlDateTimeArray : DmlArray
    {
        private DateTime[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (DateTime[])value; }
        }

        public DmlDateTimeArray() : base() { }
        public DmlDateTimeArray(DmlDocument Document) : base(Document) { }
        public DmlDateTimeArray(DmlDocument Document, DateTime[] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.DateTimes; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetDateTimeArray(); }
        public override void SetElement(long iElement, object element) { m_Values[iElement] = (DateTime)element; }
        public override object GetElement(long iElement) { return m_Values[iElement]; }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlDateTimeArray cp = new DmlDateTimeArray(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    public class DmlStringArray : DmlArray
    {
        private string[] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (string[])value; }
        }

        public DmlStringArray() : base() { }
        public DmlStringArray(DmlDocument Document) : base(Document) { }
        public DmlStringArray(DmlDocument Document, string[] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.Strings; } }
        public override long ArrayLength { get { return m_Values.Length; } }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetStringArray(); }
        public override void SetElement(long iElement, object element) { m_Values[iElement] = (string)element; }
        public override object GetElement(long iElement) { return m_Values[iElement]; }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlStringArray cp = new DmlStringArray(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    #endregion

    #endregion

    #region "Matrix Primitives"

    public abstract class DmlMatrix : DmlPrimitive
    {
        public DmlMatrix()
        {
            Association.DMLName.ArrayType = ArrayType;
        }
        public DmlMatrix(DmlDocument Document)
            : base(Document)
        {
            Association.DMLName.ArrayType = ArrayType;
        }
        public override PrimitiveTypes PrimitiveType { get { return PrimitiveTypes.Matrix; } }
        public abstract ArrayTypes ArrayType { get; }

        public abstract void SetElement(long iRow, long iCol, object element);
        public abstract object GetElement(long iRow, long iCol);

        protected override UInt64 GetNodeHeadSize()
        {
            if (InlineIdentification)
                return DmlWriter.PredictNodeHeadSize(Name, DmlInternalData.PrimitiveTypeToString(PrimitiveType, ArrayType));
            else
                return DmlWriter.PredictNodeHeadSize(ID);
        }

        protected override void Validate()
        {
            if (Association.DMLName.NodeType != NodeTypes.Primitive)
                throw new Exception("Invalid association: DOM Primitive must be a Primitive node type.");
            if (Association.DMLName.PrimitiveType != PrimitiveType)
                throw new Exception("Invalid association: DOM Primitive type does not match associated primitive type.");
            if (Association.DMLName.ArrayType != ArrayType)
                throw new Exception("Invalid association: DOM Array type does not match associated array type.");
        }

        public abstract long Rows { get; }
        public abstract long Columns { get; }
    }

    public abstract class DmlSignedIntMatrix : DmlMatrix
    {
        public DmlSignedIntMatrix() : base() { }
        public DmlSignedIntMatrix(DmlDocument Document) : base(Document) { }
    }

    public abstract class DmlUnsignedIntMatrix : DmlMatrix
    {
        public DmlUnsignedIntMatrix() : base() { }
        public DmlUnsignedIntMatrix(DmlDocument Document) : base(Document) { }
    }

    public abstract class DmlFloatingPointMatrix : DmlMatrix
    {
        public DmlFloatingPointMatrix() : base() { }
        public DmlFloatingPointMatrix(DmlDocument Document) : base(Document) { }
    }

    #region "Signed Matrices"

    public class DmlSByteMatrix : DmlSignedIntMatrix
    {
        private sbyte[,] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (sbyte[,])value; }
        }

        public DmlSByteMatrix() : base() { }
        public DmlSByteMatrix(DmlDocument Document) : base(Document) { }
        public DmlSByteMatrix(DmlDocument Document, sbyte[,] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.I8; } }
        public override long Rows { get { return m_Values.GetLength(0); } }
        public override long Columns { get { return m_Values.GetLength(1); } }
        public override void SetElement(long iRow, long iCol, object element) { m_Values[iRow, iCol] = (sbyte)element; }
        public override object GetElement(long iRow, long iCol) { return m_Values[iRow, iCol]; }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetSByteMatrix(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlSByteMatrix cp = new DmlSByteMatrix(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    public class DmlInt16Matrix : DmlSignedIntMatrix
    {
        private Int16[,] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (short[,])value; }
        }

        public DmlInt16Matrix() : base() { }
        public DmlInt16Matrix(DmlDocument Document) : base(Document) { }
        public DmlInt16Matrix(DmlDocument Document, short[,] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.I16; } }
        public override long Rows { get { return m_Values.GetLength(0); } }
        public override long Columns { get { return m_Values.GetLength(1); } }
        public override void SetElement(long iRow, long iCol, object element) { m_Values[iRow, iCol] = (short)element; }
        public override object GetElement(long iRow, long iCol) { return m_Values[iRow, iCol]; }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetInt16Matrix(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlInt16Matrix cp = new DmlInt16Matrix(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    public class DmlInt32Matrix : DmlSignedIntMatrix
    {
        private Int32[,] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (int[,])value; }
        }

        public DmlInt32Matrix() : base() { }
        public DmlInt32Matrix(DmlDocument Document) : base(Document) { }
        public DmlInt32Matrix(DmlDocument Document, int[,] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.I32; } }
        public override long Rows { get { return m_Values.GetLength(0); } }
        public override long Columns { get { return m_Values.GetLength(1); } }
        public override void SetElement(long iRow, long iCol, object element) { m_Values[iRow, iCol] = (int)element; }
        public override object GetElement(long iRow, long iCol) { return m_Values[iRow, iCol]; }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetInt32Matrix(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlInt32Matrix cp = new DmlInt32Matrix(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    public class DmlInt64Matrix : DmlSignedIntMatrix
    {
        private Int64[,] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (long[,])value; }
        }

        public DmlInt64Matrix() : base() { }
        public DmlInt64Matrix(DmlDocument Document) : base(Document) { }
        public DmlInt64Matrix(DmlDocument Document, long[,] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.I64; } }
        public override long Rows { get { return m_Values.GetLength(0); } }
        public override long Columns { get { return m_Values.GetLength(1); } }
        public override void SetElement(long iRow, long iCol, object element) { m_Values[iRow, iCol] = (long)element; }
        public override object GetElement(long iRow, long iCol) { return m_Values[iRow, iCol]; }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetInt64Matrix(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlInt64Matrix cp = new DmlInt64Matrix(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    #endregion
    #region "Unsigned Matrices"

    public class DmlByteMatrix : DmlUnsignedIntMatrix
    {
        private byte[,] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (byte[,])value; }
        }

        public DmlByteMatrix() : base() { }
        public DmlByteMatrix(DmlDocument Document) : base(Document) { }
        public DmlByteMatrix(DmlDocument Document, byte[,] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.U8; } }
        public override long Rows { get { return m_Values.GetLength(0); } }
        public override long Columns { get { return m_Values.GetLength(1); } }
        public override void SetElement(long iRow, long iCol, object element) { m_Values[iRow, iCol] = (byte)element; }
        public override object GetElement(long iRow, long iCol) { return m_Values[iRow, iCol]; }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetByteMatrix(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlByteMatrix cp = new DmlByteMatrix(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    public class DmlUInt16Matrix : DmlUnsignedIntMatrix
    {
        private UInt16[,] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (ushort[,])value; }
        }

        public DmlUInt16Matrix() : base() { }
        public DmlUInt16Matrix(DmlDocument Document) : base(Document) { }
        public DmlUInt16Matrix(DmlDocument Document, ushort[,] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.U16; } }
        public override long Rows { get { return m_Values.GetLength(0); } }
        public override long Columns { get { return m_Values.GetLength(1); } }
        public override void SetElement(long iRow, long iCol, object element) { m_Values[iRow, iCol] = (ushort)element; }
        public override object GetElement(long iRow, long iCol) { return m_Values[iRow, iCol]; }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetUInt16Matrix(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlUInt16Matrix cp = new DmlUInt16Matrix(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    public class DmlUInt32Matrix : DmlUnsignedIntMatrix
    {
        private UInt32[,] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (uint[,])value; }
        }

        public DmlUInt32Matrix() : base() { }
        public DmlUInt32Matrix(DmlDocument Document) : base(Document) { }
        public DmlUInt32Matrix(DmlDocument Document, uint[,] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.U32; } }
        public override long Rows { get { return m_Values.GetLength(0); } }
        public override long Columns { get { return m_Values.GetLength(1); } }
        public override void SetElement(long iRow, long iCol, object element) { m_Values[iRow, iCol] = (uint)element; }
        public override object GetElement(long iRow, long iCol) { return m_Values[iRow, iCol]; }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetUInt32Matrix(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlUInt32Matrix cp = new DmlUInt32Matrix(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    public class DmlUInt64Matrix : DmlUnsignedIntMatrix
    {
        private UInt64[,] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (UInt64[,])value; }
        }

        public DmlUInt64Matrix() : base() { }
        public DmlUInt64Matrix(DmlDocument Document) : base(Document) { }
        public DmlUInt64Matrix(DmlDocument Document, ulong[,] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.U64; } }
        public override long Rows { get { return m_Values.GetLength(0); } }
        public override long Columns { get { return m_Values.GetLength(1); } }
        public override void SetElement(long iRow, long iCol, object element) { m_Values[iRow, iCol] = (ulong)element; }
        public override object GetElement(long iRow, long iCol) { return m_Values[iRow, iCol]; }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetUInt64Matrix(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlUInt64Matrix cp = new DmlUInt64Matrix(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    #endregion

    #region "Floating-Point Matrices"

    public class DmlSingleMatrix : DmlFloatingPointMatrix
    {
        private float[,] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (float[,])value; }
        }

        public DmlSingleMatrix() : base() { }
        public DmlSingleMatrix(DmlDocument Document) : base(Document) { }
        public DmlSingleMatrix(DmlDocument Document, float[,] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.Singles; } }
        public override long Rows { get { return m_Values.GetLength(0); } }
        public override long Columns { get { return m_Values.GetLength(1); } }
        public override void SetElement(long iRow, long iCol, object element) { m_Values[iRow, iCol] = (float)element; }
        public override object GetElement(long iRow, long iCol) { return m_Values[iRow, iCol]; }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetSingleMatrix(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlSingleMatrix cp = new DmlSingleMatrix(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    public class DmlDoubleMatrix : DmlFloatingPointMatrix
    {
        private double[,] m_Values;
        public override object Value
        {
            get { return m_Values; }
            set { m_Values = (double[,])value; }
        }

        public DmlDoubleMatrix() : base() { }
        public DmlDoubleMatrix(DmlDocument Document) : base(Document) { }
        public DmlDoubleMatrix(DmlDocument Document, double[,] Values) : base(Document) { this.m_Values = Values; }
        public override ArrayTypes ArrayType { get { return ArrayTypes.Doubles; } }
        public override long Rows { get { return m_Values.GetLength(0); } }
        public override long Columns { get { return m_Values.GetLength(1); } }
        public override void SetElement(long iRow, long iCol, object element) { m_Values[iRow, iCol] = (double)element; }
        public override object GetElement(long iRow, long iCol) { return m_Values[iRow, iCol]; }
        public override void LoadContent(DmlReader Reader) { Validate(); m_Values = Reader.GetDoubleMatrix(); }
        public override void WriteTo(DmlWriter Writer)
        {
            Validate(); 
            if (InlineIdentification)
                Writer.Write(Name, m_Values);
            else
                Writer.Write(DmlID, m_Values);
        }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return GetNodeHeadSize() + (UInt64)DmlWriter.PredictPrimitiveSize(m_Values);
        }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlDoubleMatrix cp = new DmlDoubleMatrix(NewDoc);
            cp.Association = Association.Clone();
            cp.IsAttribute = IsAttribute;
            cp.m_Values = m_Values;
            return cp;
        }
    }

    #endregion

    #endregion
}
