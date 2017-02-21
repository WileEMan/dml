using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using WileyBlack.Dml;
using WileyBlack.Dml.Dom;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dml_Editor
{
    public class DmlType
    {
        public string Label;
        public Type ObjType;
        public PrimitiveTypes PrimitiveType;

        public DmlType(PrimitiveTypes PrimType, string Label, Type ObjType)
        {
            this.PrimitiveType = PrimType;
            this.Label = Label;
            this.ObjType = ObjType;
        }

        public override string ToString() { return Label; }

        public virtual void ShowValue(PrimitivePanel Panel, DmlPrimitive Value)
        {
        }

        public virtual DmlPrimitive ConvertType(DmlPrimitive From)
        {
            return null;
        }

        #region "Type Data"

        public static DmlArrayType      DmlArrayType = new DmlArrayType(ArrayTypes.Unknown, "Array", null);
        public static DmlBoolType       DmlBoolType = new DmlBoolType(PrimitiveTypes.Boolean, "Boolean", typeof(DmlBool));
            // new DmlType(PrimitiveTypes.CompressedDML, "Compressed-DML", typeof()),        
        public static DmlDateTimeType   DmlDateTimeType = new DmlDateTimeType(PrimitiveTypes.DateTime, "Date/Time", typeof(DmlDateTime));
            // new DmlType(PrimitiveTypes.Decimal, "Decimal", 
        public static DmlDoubleType     DmlDoubleType = new DmlDoubleType(PrimitiveTypes.Double, "Double-Precision Floating-Point", typeof(DmlDouble));
            // new DmlType(PrimitiveTypes.EncryptedDML, "Encrypted-DML", typeof()),
            // new DmlType(PrimitiveTypes.Extension, "Extension", 
        public static DmlIntType        DmlIntType = new DmlIntType(PrimitiveTypes.Int, "Integer", typeof(DmlInt));
        public static DmlType           DmlMatrixType = new DmlMatrixType(ArrayTypes.Unknown, "Matrix", null);
        public static DmlSingleType     DmlSingleType = new DmlSingleType(PrimitiveTypes.Single, "Single-Precision Floating-Point", typeof(DmlSingle));
        public static DmlType           DmlStringType = new DmlStringType(PrimitiveTypes.String, "String", typeof(DmlString));
        public static DmlUIntType       DmlUIntType = new DmlUIntType(PrimitiveTypes.UInt, "Unsigned Integer", typeof(DmlUInt));
        public static DmlType           DmlUnknownType = new DmlType(PrimitiveTypes.Unknown, "Unknown", null);

        public static DmlType[] Types = new DmlType[] {
            DmlArrayType,
            DmlBoolType,
            // Compressed            
            DmlDateTimeType,
            // Decimal
            DmlDoubleType,
            // Encrypted
            // Extension
            DmlIntType,
            DmlMatrixType,
            DmlSingleType,
            DmlStringType,
            DmlUIntType,
            DmlUnknownType            
        };

        #endregion
        #region "Array Types"

        public static DmlArrayType DmlDTArrayType = new DmlArrayType(ArrayTypes.DateTimes, "Date-Time Array", typeof(DmlDateTimeArray));        
        public static DmlArrayType DmlSFArrayType = new DmlArrayType(ArrayTypes.Singles, "Floating-Point (single-precision) Array", typeof(DmlSingleArray));
        public static DmlArrayType DmlDFArrayType = new DmlArrayType(ArrayTypes.Doubles, "Floating-Point (double-precision) Array", typeof(DmlDoubleArray));
        public static DmlArrayType DmlI8ArrayType = new DmlArrayType(ArrayTypes.I8, "Integer (8-bit) Array", typeof(DmlSByteArray));
        public static DmlArrayType DmlI16ArrayType = new DmlArrayType(ArrayTypes.I16, "Integer (16-bit) Array", typeof(DmlInt16Array));
        //public static DmlArrayType DmlI24ArrayType = new DmlArrayType(ArrayTypes.I24, "Integer (8-bit) Array", typeof(sbyte[]));
        public static DmlArrayType DmlI32ArrayType = new DmlArrayType(ArrayTypes.I32, "Integer (32-bit) Array", typeof(DmlInt32Array));
        public static DmlArrayType DmlI64ArrayType = new DmlArrayType(ArrayTypes.I64, "Integer (64-bit) Array", typeof(DmlInt64Array));
        public static DmlArrayType DmlU8ArrayType = new DmlArrayType(ArrayTypes.U8, "Unsigned Integer (8-bit) Array", typeof(DmlByteArray));
        public static DmlArrayType DmlU16ArrayType = new DmlArrayType(ArrayTypes.U16, "Unsigned Integer (16-bit) Array", typeof(DmlUInt16Array));
        //public static DmlArrayType DmlU24ArrayType = new DmlArrayType(ArrayTypes.U24, "Unsigned Integer (8-bit) Array", typeof(sbyte[]));
        public static DmlArrayType DmlU32ArrayType = new DmlArrayType(ArrayTypes.U32, "Unsigned Integer (32-bit) Array", typeof(DmlUInt32Array));
        public static DmlArrayType DmlU64ArrayType = new DmlArrayType(ArrayTypes.U64, "Unsigned Integer (64-bit) Array", typeof(DmlUInt64Array));
        public static DmlArrayType DmlSArrayType = new DmlArrayType(ArrayTypes.Strings, "String Array", typeof(DmlStringArray));
        public static DmlArrayType DmlUnknownArrayType = new DmlArrayType(ArrayTypes.Unknown, "Unknown Array", null);

        public static DmlArrayType[] ArrayTypeList = new DmlArrayType[] {
            DmlSArrayType,
            DmlI8ArrayType,
            DmlI16ArrayType,
            DmlI32ArrayType,
            DmlI64ArrayType,
            DmlSFArrayType,
            DmlDFArrayType,
            DmlU8ArrayType,
            DmlU16ArrayType,
            DmlU32ArrayType,
            DmlU64ArrayType,            
            DmlDTArrayType
        };

        #endregion
        #region "Matrix Types"
                
        public static DmlMatrixType DmlSFMatrixType = new DmlMatrixType(ArrayTypes.Singles, "Floating-Point (single-precision) Matrix", typeof(DmlSingleMatrix));
        public static DmlMatrixType DmlDFMatrixType = new DmlMatrixType(ArrayTypes.Doubles, "Floating-Point (double-precision) Matrix", typeof(DmlDoubleMatrix));
        public static DmlMatrixType DmlI8MatrixType = new DmlMatrixType(ArrayTypes.I8, "Integer (8-bit) Matrix", typeof(DmlSByteMatrix));
        public static DmlMatrixType DmlI16MatrixType = new DmlMatrixType(ArrayTypes.I16, "Integer (16-bit) Matrix", typeof(DmlInt16Matrix));
        //public static DmlMatrixType DmlI24MatrixType = new DmlMatrixType(ArrayTypes.I24, "Integer (8-bit) Matrix", typeof(sbyte[,]));
        public static DmlMatrixType DmlI32MatrixType = new DmlMatrixType(ArrayTypes.I32, "Integer (32-bit) Matrix", typeof(DmlInt32Matrix));
        public static DmlMatrixType DmlI64MatrixType = new DmlMatrixType(ArrayTypes.I64, "Integer (64-bit) Matrix", typeof(DmlInt64Matrix));
        public static DmlMatrixType DmlU8MatrixType = new DmlMatrixType(ArrayTypes.U8, "Unsigned Integer (8-bit) Matrix", typeof(DmlByteMatrix));
        public static DmlMatrixType DmlU16MatrixType = new DmlMatrixType(ArrayTypes.U16, "Unsigned Integer (16-bit) Matrix", typeof(DmlUInt16Matrix));
        //public static DmlMatrixType DmlU24MatrixType = new DmlMatrixType(ArrayTypes.U24, "Unsigned Integer (8-bit) Matrix", typeof(sbyte[,]));
        public static DmlMatrixType DmlU32MatrixType = new DmlMatrixType(ArrayTypes.U32, "Unsigned Integer (32-bit) Matrix", typeof(DmlUInt32Matrix));
        public static DmlMatrixType DmlU64MatrixType = new DmlMatrixType(ArrayTypes.U64, "Unsigned Integer (64-bit) Matrix", typeof(DmlUInt64Matrix));        
        public static DmlMatrixType DmlUnknownMatrixType = new DmlMatrixType(ArrayTypes.Unknown, "Unknown Matrix", null);

        public static DmlMatrixType[] MatrixTypeList = new DmlMatrixType[] {            
            DmlI8MatrixType,
            DmlI16MatrixType,
            DmlI32MatrixType,
            DmlI64MatrixType,
            DmlSFMatrixType,
            DmlDFMatrixType,
            DmlU8MatrixType,
            DmlU16MatrixType,
            DmlU32MatrixType,
            DmlU64MatrixType            
        };

        #endregion

        public static DmlType GetDmlType(object obj)
        {
            if (obj is Array)
            {
                if (((Array)obj).Rank == 1)
                {
                    if (obj is byte[]) return DmlU8ArrayType;
                    else if (obj is ushort[]) return DmlU16ArrayType;
                    else if (obj is uint[]) return DmlU32ArrayType;
                    else if (obj is ulong[]) return DmlU64ArrayType;
                    else if (obj is sbyte[]) return DmlI8ArrayType;
                    else if (obj is short[]) return DmlI16ArrayType;
                    else if (obj is int[]) return DmlI32ArrayType;
                    else if (obj is long[]) return DmlI64ArrayType;
                    else if (obj is float[]) return DmlSFArrayType;
                    else if (obj is double[]) return DmlDFArrayType;
                    else if (obj is DateTime[]) return DmlDTArrayType;
                    else if (obj is string[]) return DmlSArrayType;
                    return null;
                }
                else return DmlMatrixType;
            }
            else if (obj is DateTime) return DmlDateTimeType;
            else if (obj is bool) return DmlBoolType;
            else if (obj is double) return DmlDoubleType;
            else if (obj is int) return DmlIntType;
            else if (obj is float) return DmlSingleType;
            else if (obj is string) return DmlStringType;
            else if (obj is uint) return DmlUIntType;
            else return null;
        }

        public static DmlArrayType GetDmlType(DmlArray obj)
        {
            if (obj is DmlSByteArray) return DmlI8ArrayType;
            else if (obj is DmlInt16Array) return DmlI16ArrayType;
            else if (obj is DmlInt32Array) return DmlI32ArrayType;
            else if (obj is DmlInt64Array) return DmlI64ArrayType;
            else if (obj is DmlByteArray) return DmlU8ArrayType;
            else if (obj is DmlUInt16Array) return DmlU16ArrayType;
            else if (obj is DmlInt32Array) return DmlU32ArrayType;
            else if (obj is DmlInt64Array) return DmlU64ArrayType;
            else if (obj is DmlSingleArray) return DmlSFArrayType;
            else if (obj is DmlDoubleArray) return DmlDFArrayType;
            else if (obj is DmlDateTimeArray) return DmlDTArrayType;
            else if (obj is DmlStringArray) return DmlSArrayType;
            else return null;
        }

        public static DmlArrayType GetDmlArrayType(ArrayTypes ArrayType)
        {
            switch (ArrayType)
            {
                case ArrayTypes.I8: return DmlI8ArrayType;
                case ArrayTypes.I16: return DmlI16ArrayType;
                //case ArrayTypes.I24: return DmlI24ArrayType;
                case ArrayTypes.I32: return DmlI32ArrayType;
                case ArrayTypes.I64: return DmlI64ArrayType;
                case ArrayTypes.U8: return DmlU8ArrayType;
                case ArrayTypes.U16: return DmlU16ArrayType;
                //case ArrayTypes.U24: return DmlU24ArrayType;
                case ArrayTypes.U32: return DmlU32ArrayType;
                case ArrayTypes.U64: return DmlU64ArrayType;
                case ArrayTypes.Singles: return DmlSFArrayType;
                case ArrayTypes.Doubles: return DmlDFArrayType;
                //case ArrayTypes.Decimals: return Dml10FMatrixType;                
                case ArrayTypes.Strings: return DmlSArrayType;
                case ArrayTypes.DateTimes: return DmlDTArrayType;
                default: return null;
            }
        }

        public static DmlMatrixType GetDmlMatrixType(ArrayTypes ArrayType)
        {
            switch (ArrayType)
            {
                case ArrayTypes.I8: return DmlI8MatrixType;
                case ArrayTypes.I16: return DmlI16MatrixType;
                //case ArrayTypes.I24: return DmlI24MatrixType;
                case ArrayTypes.I32: return DmlI32MatrixType;
                case ArrayTypes.I64: return DmlI64MatrixType;
                case ArrayTypes.U8: return DmlU8MatrixType;
                case ArrayTypes.U16: return DmlU16MatrixType;
                //case ArrayTypes.U24: return DmlU24MatrixType;
                case ArrayTypes.U32: return DmlU32MatrixType;
                case ArrayTypes.U64: return DmlU64MatrixType;
                case ArrayTypes.Singles: return DmlSFMatrixType;
                case ArrayTypes.Doubles: return DmlDFMatrixType;
                //case ArrayTypes.Decimals: return Dml10FMatrixType;                
                default: return null;
            }
        }
    }    

    public class DmlTextType : DmlType
    {
        public DmlTextType(PrimitiveTypes PrimType, string Label, Type ObjType)
            : base(PrimType, Label, ObjType)
        {
        }

        public static string StaticToString(DmlPrimitive Value)
        {
#           if false
            if (Value is DmlDouble) return (Value as DmlDouble).Value.ToString();
            else if (Value is DmlInt) return (Value as DmlInt).Value.ToString();
            else if (Value is DmlSingle) return (Value as DmlSingle).Value.ToString();
            else if (Value is DmlString) return (Value as DmlString).Value;
            else if (Value is DmlUInt) return (Value as DmlUInt).Value.ToString();
            else throw new FormatException();
#           else
            return Value.Value.ToString();
#           endif
        }

        public virtual string ToString(DmlPrimitive Value)
        {
#           if false
            if (Value is DmlDouble) return (Value as DmlDouble).Value.ToString();
            else if (Value is DmlInt) return (Value as DmlInt).Value.ToString();
            else if (Value is DmlSingle) return (Value as DmlSingle).Value.ToString();
            else if (Value is DmlString) return (Value as DmlString).Value;
            else if (Value is DmlUInt) return (Value as DmlUInt).Value.ToString();
            else if (Value is DmlBool) return (Value as DmlBool).Value.ToString();
            else if (Value is DmlDateTime) return (Value as DmlDateTime).Value.ToString();
            else throw new FormatException();
#           else
            return Value.Value.ToString();
#           endif
        }

        public override void ShowValue(PrimitivePanel Panel, DmlPrimitive Value)
        {
            Panel.HideValueDisplay();
            
            string NewText = ToString(Value);
            if (Panel.ValueTextBox.Text != NewText) Panel.ValueTextBox.Text = NewText;
            if (!Panel.ValueTextBox.Visible)
            {
                Panel.ValueTextBox.Visible = true;
                Panel.ValueTextBox.BringToFront();
            }
        }

        public void UpdateValue(DmlPrimitive Value, string NewText)
        {
            if (Value is DmlDouble) (Value as DmlDouble).Value = double.Parse(NewText);
            else if (Value is DmlInt) (Value as DmlInt).Value = long.Parse(NewText);
            else if (Value is DmlSingle) (Value as DmlSingle).Value = float.Parse(NewText);
            else if (Value is DmlString) (Value as DmlString).Value = NewText;
            else if (Value is DmlUInt) (Value as DmlUInt).Value = ulong.Parse(NewText);            
            else throw new FormatException();
        }
    }

    public class DmlBoolType : DmlTextType
    {
        public DmlBoolType(PrimitiveTypes PrimType, string Label, Type ObjType)
            : base(PrimType, Label, ObjType) { }

        public override void ShowValue(PrimitivePanel Panel, DmlPrimitive Value)
        {
            Panel.HideValueDisplay();

            Panel.rbTrueValue.Visible = true;
            Panel.rbFalseValue.Visible = true;
            Panel.rbTrueValue.BringToFront();
            Panel.rbFalseValue.BringToFront();
            DmlBool dbValue = Value as DmlBool;
            if ((bool)dbValue.Value) Panel.rbTrueValue.Checked = true; else Panel.rbFalseValue.Checked = true;
        }

        public override DmlPrimitive ConvertType(DmlPrimitive From) { return StaticConvertType(From); }
        public static DmlBool StaticConvertType(DmlPrimitive From)
        {
            try
            {
                string Text = DmlTextType.StaticToString(From);
                bool NewValue = false;
                if (Text.Trim().Length > 0) NewValue = bool.Parse(Text);
                DmlBool NewPrim = From.Document.CreateBool();
                NewPrim.Name = From.Name;
                NewPrim.Value = NewValue;
                return NewPrim;
            }
            catch (Exception) { return null; }
        }
    }

    public class DmlUIntType : DmlTextType
    {
        public DmlUIntType(PrimitiveTypes PrimType, string Label, Type ObjType)
            : base(PrimType, Label, ObjType) { }

        public override DmlPrimitive ConvertType(DmlPrimitive From) { return StaticConvertType(From); }
        public static DmlUInt StaticConvertType(DmlPrimitive From)
        {
            try
            {
                string Text = DmlTextType.StaticToString(From);
                ulong NewValue = 0;
                if (Text.Trim().Length > 0) NewValue = ulong.Parse(Text);                
                DmlUInt NewPrim = From.Document.CreateUInt();
                NewPrim.Name = From.Name;
                NewPrim.Value = NewValue;
                return NewPrim;
            }
            catch (Exception) { return null; } 
        }
    }

    public class DmlIntType : DmlTextType
    {
        public DmlIntType(PrimitiveTypes PrimType, string Label, Type ObjType)
            : base(PrimType, Label, ObjType) { }

        public override DmlPrimitive ConvertType(DmlPrimitive From) { return StaticConvertType(From); }
        public static DmlInt StaticConvertType(DmlPrimitive From)
        {
            try
            {
                string Text = DmlTextType.StaticToString(From);
                long NewValue = 0;
                if (Text.Trim().Length > 0) NewValue = long.Parse(Text);                
                DmlInt NewPrim = From.Document.CreateInt();
                NewPrim.Name = From.Name;
                NewPrim.Value = NewValue;
                return NewPrim;
            }
            catch (Exception) { return null; }
        }
    }

    public class DmlSingleType : DmlTextType
    {
        public DmlSingleType(PrimitiveTypes PrimType, string Label, Type ObjType)
            : base(PrimType, Label, ObjType) { }

        public override DmlPrimitive ConvertType(DmlPrimitive From) { return StaticConvertType(From); }
        public static DmlSingle StaticConvertType(DmlPrimitive From)
        {
            try
            {
                string Text = DmlTextType.StaticToString(From);
                float NewValue = 0.0f;
                if (Text.Trim().Length > 0) NewValue = float.Parse(Text);
                DmlSingle NewPrim = From.Document.CreateSingle();
                NewPrim.Name = From.Name;
                NewPrim.Value = NewValue;
                return NewPrim;
            }
            catch (Exception) { return null; }
        }
    }

    public class DmlDoubleType : DmlTextType
    {
        public DmlDoubleType(PrimitiveTypes PrimType, string Label, Type ObjType)
            : base(PrimType, Label, ObjType) { }

        public override DmlPrimitive ConvertType(DmlPrimitive From) { return StaticConvertType(From); }
        public static DmlDouble StaticConvertType(DmlPrimitive From)
        {
            try
            {
                string Text = DmlTextType.StaticToString(From);
                double NewValue = 0.0;
                if (Text.Trim().Length > 0) NewValue = double.Parse(Text);
                DmlDouble NewPrim = From.Document.CreateDouble();
                NewPrim.Name = From.Name;
                NewPrim.Value = NewValue;
                return NewPrim;
            }
            catch (Exception) { return null; }
        }
    }

    public class DmlStringType : DmlTextType
    {
        public DmlStringType(PrimitiveTypes PrimType, string Label, Type ObjType)
            : base(PrimType, Label, ObjType) { }

        public override DmlPrimitive ConvertType(DmlPrimitive From) { return StaticConvertType(From); }
        public static DmlString StaticConvertType(DmlPrimitive From)
        {
            DmlString ret = new DmlString(From.Document);
            ret.Name = From.Name;
            if (From is DmlDouble) ret.Value = (From as DmlDouble).Value.ToString();
            else if (From is DmlInt) ret.Value = (From as DmlInt).Value.ToString();
            else if (From is DmlSingle) ret.Value = (From as DmlSingle).Value.ToString();
            else if (From is DmlString) return From as DmlString;
            else if (From is DmlUInt) ret.Value = (From as DmlUInt).Value.ToString();
            else if (From is DmlDateTime) ret.Value = ((DmlDateTime)From).Value.ToString();
            else if (From is DmlBool) ret.Value = (From as DmlBool).Value.ToString();
            else return null;
            return ret;
        }
    }

    public class DmlDateTimeType : DmlTextType
    {
        public DmlDateTimeType(PrimitiveTypes PrimType, string Label, Type ObjType)
            : base(PrimType, Label, ObjType) { }

        public override DmlPrimitive ConvertType(DmlPrimitive From) { return StaticConvertType(From); }
        public static DmlDateTime StaticConvertType(DmlPrimitive From)
        {
            DmlDateTime ret = new DmlDateTime(From.Document);
            ret.Name = From.Name;
            if (From is DmlString)
            {
                DmlString ds = (DmlString)From; DateTime dt;
                if (((string)ds.Value).Trim().Length == 0) { ret.Value = DateTime.Now; return ret; }
                if (DateTime.TryParse((string)ds.Value, out dt)) { ret.Value = dt; return ret; }
                return null;
            }
            else return null;
        }

        public override void ShowValue(PrimitivePanel Panel, DmlPrimitive Value)
        {
            Panel.HideValueDisplay();

            Panel.DateTimePicker.Visible = true;
            DmlDateTime dtValue = Value as DmlDateTime;
            Panel.DateTimePicker.Value = ((DateTime)dtValue.Value).ToLocalTime();
            Panel.DateTimePicker.BringToFront();
        }
    }

    public class DmlArrayType : DmlType
    {
        public ArrayTypes ArrayType;

        public DmlArrayType(ArrayTypes ArrType, string Label, Type ObjType)
            : base(PrimitiveTypes.Array, Label, ObjType)
        {
            this.ArrayType = ArrType;
        }

        public DmlArray CreateDmlArray(DmlDocument Document, long nElements)
        {
            switch (ArrayType)
            {                
                case ArrayTypes.Singles: return new DmlSingleArray(Document, new float[nElements]);
                case ArrayTypes.Doubles: return new DmlDoubleArray(Document, new double[nElements]);
                case ArrayTypes.I8: return new DmlSByteArray(Document, new sbyte[nElements]);
                case ArrayTypes.I16: return new DmlInt16Array(Document, new short[nElements]);
                case ArrayTypes.I32: return new DmlInt32Array(Document, new int[nElements]);
                case ArrayTypes.I64: return new DmlInt64Array(Document, new long[nElements]);
                case ArrayTypes.U8: return new DmlByteArray(Document, new byte[nElements]);
                case ArrayTypes.U16: return new DmlUInt16Array(Document, new ushort[nElements]);
                case ArrayTypes.U32: return new DmlUInt32Array(Document, new uint[nElements]);
                case ArrayTypes.U64: return new DmlUInt64Array(Document, new ulong[nElements]);
                case ArrayTypes.DateTimes: return new DmlDateTimeArray(Document, new DateTime[nElements]);
                case ArrayTypes.Strings: return new DmlStringArray(Document, new string[nElements]);
                default: throw new NotSupportedException("Unrecognized array type, cannot instantiate.");
            }
        }

        public object ConvertToElement(object From)
        {
            TypeCode DestCode = TypeCode.Empty;
            switch (ArrayType)
            {
                case ArrayTypes.Singles: DestCode = TypeCode.Single; break;
                case ArrayTypes.Doubles: DestCode = TypeCode.Double; break;
                case ArrayTypes.I8: DestCode = TypeCode.SByte; break;
                case ArrayTypes.I16: DestCode = TypeCode.Int16; break;
                case ArrayTypes.I32: DestCode = TypeCode.Int32; break;
                case ArrayTypes.I64: DestCode = TypeCode.Int64; break;
                case ArrayTypes.U8: DestCode = TypeCode.Byte; break;
                case ArrayTypes.U16: DestCode = TypeCode.UInt16; break;
                case ArrayTypes.U32: DestCode = TypeCode.UInt32; break;
                case ArrayTypes.U64: DestCode = TypeCode.UInt64; break;
                case ArrayTypes.DateTimes: DestCode = TypeCode.DateTime; break;
                case ArrayTypes.Strings: DestCode = TypeCode.String; break;
                default: throw new NotSupportedException("Unrecognized array type, cannot convert.");
            }
            return Convert.ChangeType(From, DestCode);
        }

        public override DmlPrimitive ConvertType(DmlPrimitive From) { return StaticConvertType(From); }
        public static DmlArray StaticConvertType(DmlPrimitive From)
        {
            try
            {
                if (From is DmlInt || From is DmlUInt || From is DmlDateTime || From is DmlString
                 || From is DmlBool)
                {
                    string Text = From.Value.ToString();
                    string[] Values = Text.Split(new char[] { ',', '\n', '\r', '\t' });

                    try
                    {
                        if (Values.Length >= 1)     // Don't default to DateTimeArrays...
                        {
                            DmlDateTimeArray DTArray = From.Document.CreateDateTimeArray();
                            DTArray.Name = From.Name;
                            DTArray.Value = new DateTime[Values.Length];
                            for (int ii = 0; ii < Values.Length; ii++) DTArray.SetElement(ii, DateTime.Parse(Values[ii]));
                            return DTArray;
                        }
                    }
                    catch (Exception) { }

                    try
                    {
                        DmlInt64Array IArray = From.Document.CreateInt64Array();
                        IArray.Name = From.Name;
                        IArray.Value = new long[Values.Length];
                        for (int ii = 0; ii < Values.Length; ii++) IArray.SetElement(ii, long.Parse(Values[ii]));
                        return IArray;
                    }
                    catch (Exception) { }

                    try
                    {
                        DmlUInt64Array UArray = From.Document.CreateUInt64Array();
                        UArray.Name = From.Name;
                        UArray.Value = new ulong[Values.Length];
                        for (int ii = 0; ii < Values.Length; ii++) UArray.SetElement(ii, ulong.Parse(Values[ii]));
                        return UArray;
                    }
                    catch (Exception) { }

                    try
                    {
                        DmlDoubleArray DArray = From.Document.CreateDoubleArray();
                        DArray.Name = From.Name;
                        DArray.Value = new double[Values.Length];
                        for (int ii = 0; ii < Values.Length; ii++) DArray.SetElement(ii, double.Parse(Values[ii]));
                        return DArray;
                    }
                    catch (Exception) { }

                    DmlStringArray SArray = From.Document.CreateStringArray();
                    SArray.Name = From.Name;
                    SArray.Value = Values;
                    return SArray;
                }

                if (From is DmlMatrix)
                {
                    DmlMatrix FromMatrix = (DmlMatrix)From;
                    ArrayTypes ElementType = FromMatrix.ArrayType;
                    DmlArrayType ToType = DmlType.GetDmlArrayType(ElementType);
                    if (ToType == null) return null;
                    if (FromMatrix.Rows == 1)
                    {
                        DmlArray ret = ToType.CreateDmlArray(From.Document, FromMatrix.Columns);
                        ret.Name = From.Name;
                        for (long ii = 0; ii < FromMatrix.Columns; ii++)
                            ret.SetElement(ii, FromMatrix.GetElement(0, ii));
                        return ret;
                    }
                    else if (FromMatrix.Columns == 1)
                    {
                        DmlArray ret = ToType.CreateDmlArray(From.Document, FromMatrix.Rows);
                        ret.Name = From.Name;
                        for (long ii = 0; ii < FromMatrix.Rows; ii++)
                            ret.SetElement(ii, FromMatrix.GetElement(ii, 0));
                        return ret;
                    }
                }

                return null;
            }
            catch (Exception) { return null; }
        }

        public override void ShowValue(PrimitivePanel Panel, DmlPrimitive Value)
        {
            Panel.HideValueDisplay();

            Panel.MatrixValue.Visible = true;
            Panel.MatrixValue.PrimitiveInfo = Panel.SelectedPrimitiveInfo;
            Panel.MatrixValue.BringToFront();
        }
    }

    public class DmlMatrixType : DmlType
    {
        public ArrayTypes MatrixType;

        public DmlMatrixType(ArrayTypes MtxType, string Label, Type ObjType)
            : base(PrimitiveTypes.Array, Label, ObjType)
        {
            this.MatrixType = MtxType;
        }

        public override DmlPrimitive ConvertType(DmlPrimitive From) { return StaticConvertType(From); }
        public static DmlMatrix StaticConvertType(DmlPrimitive From)
        {
            try
            {
                if (From is DmlInt || From is DmlUInt || From is DmlDateTime || From is DmlString)
                {
                    string Text = From.Value.ToString();
                    string[] Rows = Text.Split(new char[] { ';', '\n', '\r' });

                    if (string.IsNullOrEmpty(Text.Trim()) || Rows.Length == 0)
                    {
                        DmlInt64Matrix DefMatrix = From.Document.CreateInt64Matrix();
                        DefMatrix.Name = From.Name;
                        DefMatrix.Value = new long[0, 0];
                        return DefMatrix;
                    }

                    char[] ColSeparators = new char[] { ',', ' ', '\t' };
                    string[] FirstRow = Rows[0].Split(ColSeparators);

                    try
                    {
                        DmlInt64Matrix IMatrix = From.Document.CreateInt64Matrix();
                        IMatrix.Name = From.Name;
                        IMatrix.Value = new long[Rows.Length, FirstRow.Length];
                        for (int iRow = 0; iRow < Rows.Length; iRow++)
                        {
                            string[] Columns = Rows[iRow].Split(ColSeparators);
                            for (int iCol = 0; iCol < Columns.Length; iCol++) IMatrix.SetElement(iRow, iCol, long.Parse(Columns[iCol]));
                        }
                        return IMatrix;
                    }
                    catch (Exception) { }

                    try
                    {
                        DmlDoubleMatrix DMatrix = From.Document.CreateDoubleMatrix();
                        DMatrix.Name = From.Name;
                        DMatrix.Value = new double[Rows.Length, FirstRow.Length];
                        for (int iRow = 0; iRow < Rows.Length; iRow++)
                        {
                            string[] Columns = Rows[iRow].Split(ColSeparators);
                            for (int iCol = 0; iCol < Columns.Length; iCol++) DMatrix.SetElement(iRow, iCol, double.Parse(Columns[iCol]));
                        }
                        return DMatrix;
                    }
                    catch (Exception) { }
                }

                if (From is DmlArray)
                {
                    DmlArray FromArray = (DmlArray)From;
                    ArrayTypes ElementType = FromArray.ArrayType;
                    DmlMatrixType MatrixType = DmlType.GetDmlMatrixType(ElementType);
                    if (MatrixType == null) return null;
                    DmlMatrix ret = MatrixType.CreateDmlMatrix(From.Document, FromArray.ArrayLength, 1);
                    ret.Name = From.Name;
                    for (int iRow = 0; iRow < FromArray.ArrayLength; iRow++)                    
                        ret.SetElement(iRow, 0, FromArray.GetElement(iRow));
                    return ret;
                }

                return null;
            }
            catch (Exception) { return null; }
        }

        public DmlMatrix CreateDmlMatrix(DmlDocument Document, long nRows, long nColumns)
        {
            switch (MatrixType)
            {
                case ArrayTypes.Singles: return new DmlSingleMatrix(Document, new float[nRows,nColumns]);
                case ArrayTypes.Doubles: return new DmlDoubleMatrix(Document, new double[nRows,nColumns]);
                case ArrayTypes.I8: return new DmlSByteMatrix(Document, new sbyte[nRows,nColumns]);
                case ArrayTypes.I16: return new DmlInt16Matrix(Document, new short[nRows,nColumns]);
                case ArrayTypes.I32: return new DmlInt32Matrix(Document, new int[nRows,nColumns]);
                case ArrayTypes.I64: return new DmlInt64Matrix(Document, new long[nRows,nColumns]);
                case ArrayTypes.U8: return new DmlByteMatrix(Document, new byte[nRows,nColumns]);
                case ArrayTypes.U16: return new DmlUInt16Matrix(Document, new ushort[nRows,nColumns]);
                case ArrayTypes.U32: return new DmlUInt32Matrix(Document, new uint[nRows,nColumns]);
                case ArrayTypes.U64: return new DmlUInt64Matrix(Document, new ulong[nRows,nColumns]);
                default: throw new NotSupportedException("Unrecognized matrix type, cannot instantiate.");
            }
        }

        public object ConvertToElement(string From)
        {
            switch (MatrixType)
            {
                case ArrayTypes.Singles: return float.Parse(From);
                case ArrayTypes.Doubles: return double.Parse(From);
                case ArrayTypes.I8: return sbyte.Parse(From);
                case ArrayTypes.I16: return short.Parse(From);
                case ArrayTypes.I32: return int.Parse(From);
                case ArrayTypes.I64: return long.Parse(From);
                case ArrayTypes.U8: return byte.Parse(From);
                case ArrayTypes.U16: return ushort.Parse(From);
                case ArrayTypes.U32: return uint.Parse(From);
                case ArrayTypes.U64: return ulong.Parse(From);
                case ArrayTypes.DateTimes: return DateTime.Parse(From);
                case ArrayTypes.Strings: return From;
                default: throw new NotSupportedException("Unrecognized matrix type, cannot convert.");
            }
        }

        public override void ShowValue(PrimitivePanel Panel, DmlPrimitive Value)
        {
            Panel.HideValueDisplay();

            Panel.MatrixValue.Visible = true;
            Panel.MatrixValue.PrimitiveInfo = Panel.SelectedPrimitiveInfo;
            Panel.MatrixValue.BringToFront();
        }
    }
}
