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
using System.IO.Compression;
using System.Security.Cryptography;
using WileyBlack.Platforms;
using WileyBlack.Utility;
using WileyBlack.Dml.EC;

namespace WileyBlack.Dml
{
    /// <summary>
    /// <para>Implements a writer for the Data Markup Language (DML)</para>
    /// 
    /// <para>DmlWriter provides an encoding tool for writing a DML document or fragment.  The DmlWriter provides routines to write
    /// the DML document header, containers, attributes, elements, comments, and padding.  Primitive writers are provided that 
    /// perform all necessary translation/encoding.  Data writers are also provided.</para>
    /// 
    /// <para>The DmlWriter requires that the caller provide the structure and organization of the DML.  There is no verification
    /// against translation or schema in DmlWriter, but a DmlTranslation can be used to retrieve the IDs required for writing.</para>
    /// 
    /// <para>DmlWriter does not require an extension interface to support additional primitives like DmlReader does.  To use additional
    /// primitives with DmlWriter, see the WriteStartExtension() methods.</para>
    /// </summary>
    public class DmlWriter : IDisposable
    {
        #region "Construction / Initialization / Control"

        protected EndianBinaryWriter Writer;

        protected Codecs CommonCodec = Codecs.NotLoaded;
        protected Codecs ArrayCodec = Codecs.NotLoaded;        

        public static DmlWriter Create(string Filename)
        {
            Stream Base = new FileStream(Filename, FileMode.Create);           
            return Create(Base);
        }

        public static DmlWriter Create(Stream Stream)
        {
            DmlWriter ret = new DmlWriter();
            ret.Writer = new EndianBinaryWriter(Stream, false);
            return ret;
        }

        internal static DmlWriter Create(Stream Stream, DmlWriter Context)
        {
            DmlWriter ret = new DmlWriter();
            ret.Writer = new EndianBinaryWriter(Stream, false);
            ret.CommonCodec = Context.CommonCodec;
            ret.ArrayCodec = Context.ArrayCodec;
            return ret;
        }

        protected DmlWriter()
        {
        }

        public void Close() { Writer.Close(); }

        public void Dispose()
        {
            if (Writer != null) { Writer.Dispose(); Writer = null; }
            GC.SuppressFinalize(this);
        }

        ~DmlWriter() { 
            if (Writer != null) { Writer.Dispose(); Writer = null; } 
        }

        public DmlException CreateDmlException(string Message) { throw new DmlException(Message); }

        /// <summary>
        /// Call AddPrimitiveSet() to attempt to enable a primitive set that is required for the DML document to
        /// be written.  If a primitive set is not supported, an exception will be thrown.  The AddPrimitiveSet()
        /// call does not cause anything to be emitted to the DML stream, it only enables support for the requested
        /// primitive codec.
        /// </summary>
        /// <param name="Set">Primitive set required.</param>
        public void AddPrimitiveSet(string SetName, string Codec)
        {
            switch (SetName.ToLower())
            {
                case DmlInternalData.psBase: return;
                case DmlInternalData.psCommon:
                    switch (Codec.ToLower())
                    {
                        case "le": CommonCodec = Codecs.LE; return;
                        case "be": CommonCodec = Codecs.BE; return;
                        default: throw CreateDmlException("Common primitive set codec is not recognized by writer.");
                    }
                case DmlInternalData.psExtPrecision: throw CreateDmlException("Extended precision floating-point not supported by writer.");
                case DmlInternalData.psDecimalFloat: throw CreateDmlException("Base-10 (Decimal) floating-point not supported by writer.");
                case DmlInternalData.psArrays:
                    switch (Codec.ToLower())
                    {
                        case "le": ArrayCodec = Codecs.LE; return;
                        case "be": ArrayCodec = Codecs.BE; return;
                        default: throw CreateDmlException("Array/Matrix primitive set codec is not recognized by writer.");
                    }
                case DmlInternalData.psDecimalArray: throw CreateDmlException("Decimal floating-point arrays are not supported by writer.");
                case DmlInternalData.psDmlEC:
                    switch (Codec.ToLower())
                    {
                        case "v1": return;
                        default: throw CreateDmlException("Dml-EC primitive set codec not recognized by writer.");                            
                    }
                default: throw CreateDmlException("Primitive set not recognized by writer.");                                
            }
        }

        public void AddPrimitiveSet(PrimitiveSet ps)
        {
            AddPrimitiveSet(ps.Set, ps.Codec);
        }

        #endregion

        #region "Encoding Size Predictors"

        /// <summary>
        /// PredictNodeHeadSize() predicts the encoded size of a node's head.  PredictNodeHeadSize() 
        /// assumes that the caller would use WriteStartNode() to write the node.
        /// </summary>
        /// <param name="ID">The ID which would be written</param>
        /// <param name="DataSize">The size of data content which would be written</param>
        /// <returns>The exact size, in bytes, required to write the node head using DmlWriter.</returns>
        public static UInt64 PredictNodeHeadSize(UInt32 ID)
        {
            return (UInt64)EndianBinaryWriter.SizeCompact32(ID);
        }

        /// <summary>
        /// PredictNodeHeadSize() predicts the encoded size of a node's head when using inline identification.
        /// </summary>
        /// <param name="Name">The name which would be written.</param>
        /// <returns>The exact size, in bytes, required to write the node head using DmlWriter.</returns>
        public static UInt64 PredictNodeHeadSize(string Name, string NodeType)
        {
            int NameLength = Encoding.UTF8.GetByteCount(Name);
            int NodeTypeLength = Encoding.UTF8.GetByteCount(NodeType);
            return
                (UInt64)(
                    EndianBinaryWriter.SizeCompact32(DML3Translation.idInlineIdentification)
                    + EndianBinaryWriter.SizeCompact32((uint)NameLength)
                    + NameLength
                    + EndianBinaryWriter.SizeCompact32((uint)NodeTypeLength)
                    + NodeTypeLength
                    );
        }

        /// <summary>
        /// PredictNodeHeadSize() predicts the encoded size of a node's head when using inline identification.
        /// </summary>
        /// <param name="Name">The name which would be written.</param>
        /// <param name="PrimitiveType">A non-array primitive type for the node.</param>
        /// <returns>The exact size, in bytes, required to write the node head using DmlWriter.</returns>
        public static UInt64 PredictNodeHeadSize(string Name, PrimitiveTypes PrimitiveType)
        {
            return PredictNodeHeadSize(Name, DmlInternalData.PrimitiveTypeToString(PrimitiveType, ArrayTypes.Unknown));            
        }

        /// <summary>
        /// PredictNodeHeadSize() predicts the encoded size of a node's head when using inline identification.
        /// </summary>
        /// <param name="Name">The name which would be written.</param>
        /// <param name="PrimitiveType">A primitive type for the node.</param>
        /// <param name="ArrayType">The array type for the node, or ArrayTypes.Unknown if not applicable.</param>
        /// <returns>The exact size, in bytes, required to write the node head using DmlWriter.</returns>
        public static UInt64 PredictNodeHeadSize(string Name, PrimitiveTypes PrimitiveType, ArrayTypes ArrayType)
        {
            return PredictNodeHeadSize(Name, DmlInternalData.PrimitiveTypeToString(PrimitiveType, ArrayType));
        }

        /// <summary>
        /// PredictContainerHeadSize() predicts the encoded size of a container's head.
        /// </summary>
        /// <param name="ID">The DMLID of the container to be written.</param>
        /// <returns>The exact size, in bytes, required to write the container head using DmlWriter.</returns>
        public static UInt64 PredictContainerHeadSize(UInt32 ID)
        {
            return PredictNodeHeadSize(ID);
        }

        /// <summary>
        /// PredictContainerHeadSize() predicts the encoded size of a container's head 
        /// when using inline identification.
        /// </summary>
        /// <param name="Name">The name which would be written.</param>
        /// <returns>The exact size, in bytes, required to write the container head using DmlWriter.</returns>
        public static UInt64 PredictContainerHeadSize(string Name)
        {
            return PredictNodeHeadSize(Name, "container");
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(UInt64 Value) { return (ulong)EndianBinaryWriter.SizeCompact64(Value); }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(Int64 Value) { return (ulong)EndianBinaryWriter.SizeCompactS64(Value); }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(bool Value) { return 1; }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(DateTime Value) { return 8; }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(float Value) { return 4; }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(double Value) { return 8; }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(decimal Value) { return 16; }
                
        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(string Value)
        {
            int ByteCount = Encoding.UTF8.GetByteCount(Value);
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)ByteCount) + (ulong)ByteCount;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(byte[] Value)
        {            
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.LongLength) + (ulong)Value.LongLength;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(ushort[] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.LongLength) + (ulong)Value.LongLength * 2;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(uint[] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.LongLength) + (ulong)Value.LongLength * 4;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(ulong[] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.LongLength) + (ulong)Value.LongLength * 8;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(sbyte[] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.LongLength) + (ulong)Value.LongLength;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(short[] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.LongLength) + (ulong)Value.LongLength * 2;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(int[] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.LongLength) + (ulong)Value.LongLength * 4;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(long[] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.LongLength) + (ulong)Value.LongLength * 8;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(float[] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.LongLength) + (ulong)Value.LongLength * 4;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(double[] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.LongLength) + (ulong)Value.LongLength * 8;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(DateTime[] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.LongLength) + (ulong)Value.LongLength * 8;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(string[] Value)
        {
            UInt64 ret = (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.LongLength);            
            foreach (string str in Value)
            {
                ulong slen = (ulong)Encoding.UTF8.GetByteCount(str);
                ret += (ulong)EndianBinaryWriter.SizeCompact64(slen) + slen;
            }
            return ret;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(byte[,] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(0))
                + (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(1)) + (ulong)Value.LongLength;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(ushort[,] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(0))
                + (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(1)) + (ulong)Value.LongLength * 2L;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(uint[,] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(0))
                + (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(1)) + (ulong)Value.LongLength * 4L;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(ulong[,] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(0))
                + (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(1)) + (ulong)Value.LongLength * 8L;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(sbyte[,] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(0))
                + (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(1)) + (ulong)Value.LongLength;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(short[,] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(0))
                + (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(1)) + (ulong)Value.LongLength * 2L;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(int[,] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(0))
                + (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(1)) + (ulong)Value.LongLength * 4L;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(long[,] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(0))
                + (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(1)) + (ulong)Value.LongLength * 8L;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(float[,] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(0))
                + (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(1)) + (ulong)Value.LongLength * 4L;
        }

        /// <summary>
        /// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
        /// encode the primitive's content, not including the node head.
        /// </summary>
        /// <param name="Value">The value to be represented in the primitive.</param>
        /// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
        public static UInt64 PredictPrimitiveSize(double[,] Value)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(0))
                + (ulong)EndianBinaryWriter.SizeCompact64((ulong)Value.GetLongLength(1)) + (ulong)Value.LongLength * 8L;
        }

        /// <summary>
        /// PredictCommentSize() provides the number of bytes of data that will be required to 
        /// encode the comment's content, not including the node head.
        /// </summary>
        /// <param name="Value">The text to be placed in the comment.</param>
        /// <returns>The number of bytes of data required to represent the comment's content in DML encoding.</returns>        
        public static UInt64 PredictCommentSize(string Value)
        {
            int ByteCount = Encoding.UTF8.GetByteCount(Value);
            return (ulong)EndianBinaryWriter.SizeCompact64((ulong)ByteCount) + (ulong)ByteCount;
        }

        /// <summary>
        /// PredictPaddingSize() provides the number of bytes of data that will be required to 
        /// encode the padding's content, not including the node head.  PredictPaddingSize()
        /// assumes that a multi-byte padding node will be utilized.  For single-byte padding,
        /// the content size is zero.
        /// </summary>
        /// <param name="Length">The length of padding that would be encoded.</param>
        /// <returns>The number of bytes of data required to represent the padding's content in DML encoding.</returns>        
        public static UInt64 PredictPaddingSize(UInt64 PaddingLength)
        {
            return (ulong)EndianBinaryWriter.SizeCompact64(PaddingLength) + PaddingLength;
        }

        #endregion

        #region "Low-Level Data Format"

        /// <summary>
        /// WriteStartNode() writes the head of a DML node.  It does not write the node's 
        /// data.
        /// </summary>        
        /// <param name="ID">DMLID to write into the node head.</param>        
        public void WriteStartNode(UInt32 ID)
        {
            if (ID == DML3Translation.idInlineIdentification) throw new Exception("This overload cannot be used with inline identification.");
            Writer.WriteCompact32(ID);
        }

        private static int InlineIdentifierSize = EndianBinaryWriter.SizeCompact32(DML3Translation.idInlineIdentification);

        /// <summary>
        /// This overload of WriteStartNode() writes the DML node head using inline identification.  
        /// The data content is not written.
        /// </summary>
        /// <param name="Name">Text name of the node.  Must be compatible with XML naming conventions.</param>
        /// <param name="Type">Node type.</param>        
        public void WriteStartNode(string Name, string NodeType)
        {
            byte[] RawName = Encoding.UTF8.GetBytes(Name);
            byte[] RawNodeType = Encoding.UTF8.GetBytes(NodeType);
            Writer.WriteCompact32(DML3Translation.idInlineIdentification);            
            Writer.WriteCompact32((uint)RawName.Length);
            Writer.Write(RawName);
            Writer.WriteCompact32((uint)RawNodeType.Length);            
            Writer.Write(RawNodeType);
        }        

        #endregion        

        #region "Container Writers"

        /// <summary>
        /// This overload of WriteStartContainer() writes the container head.  The 
        /// data content is not written.
        /// </summary>
        /// <param name="ID">DMLID of the container.</param>
        public void WriteStartContainer(uint ID)
        {
            WriteStartNode(ID);
        }

        /// <summary>
        /// This overload of WriteStartContainer() writes the container head using inline
        /// identification.  The data content is not written.
        /// </summary>
        /// <param name="Name">Text name of the element.</param>
        public void WriteStartContainer(string Name)
        {
            WriteStartNode(Name, "container");
        }

        /// <summary>
        /// This overload of WriteStartContainer() writes the container head using the 
        /// identification information provided in an Association type.  The data content 
        /// is not written.
        /// </summary>
        /// <param name="Identity">Identification of the element.</param>
        public void WriteStartContainer(Association Identity)
        {
            if (Identity.DMLName.NodeType != NodeTypes.Container)
                throw new Exception("Only container node types can be written using WriteStartContainer().");
            if (Identity.InlineIdentification)
                WriteStartNode(Identity.DMLName.XmlName, "container");
            else
                WriteStartNode(Identity.DMLID);
        }

        public void WriteEndAttributes()
        {
            WriteStartNode(DML3Translation.idDMLEndAttributes);
        }

        public void WriteEndContainer()
        {
            WriteStartNode(DML3Translation.idDMLEndContainer);
        }

        #endregion

        #region "Header Writer"

        /// <summary>
        /// WriteHeader() emits a DML header that references an external translation document.  A header is required
        /// on every complete DML document, but can be generated directly using other DmlWriter calls.  WriteHeader() 
        /// is a convenience function only.
        /// </summary>
        /// <param name="TranslationURI">URI providing the DML Translation document for the format.  Can be null.</param>
        /// <param name="TranslationURN">URN providing verification of the DML Translation for this content.  Can be null.</param>
        /// <param name="DocType">Document type string, usually the same as the name of the document's top-level container.  Can be null.</param>
        /// <param name="PrimitiveSets">List of any primitive sets required.  Can be null.</param>
        public void WriteHeader(string TranslationURI, string TranslationURN, string DocType, PrimitiveSet[] PrimitiveSets)
        {
            WriteStartContainer(DML3Translation.idDMLHeader);
            Write(DML3Translation.idDMLVersion, DmlInternalData.DMLVersion);
            Write(DML3Translation.idDMLReadVersion, DmlInternalData.DMLReadVersion);
            if (DocType != null) Write(DML3Translation.idDMLDocType, DocType);
            WriteEndAttributes();
            if (TranslationURI != null && TranslationURI.Length > 0)
            {
                WriteStartContainer(TSL2Translation.idDMLIncludeTranslation);
                Write(TSL2Translation.idDML_URI, TranslationURI);
                if (TranslationURN != null && TranslationURN.Length > 0) Write(TSL2Translation.idDML_URN, TranslationURN);
                WriteEndContainer();
            }
            if (PrimitiveSets != null)
            {
                foreach (PrimitiveSet ps in PrimitiveSets)
                {
                    WriteStartContainer(TSL2Translation.idDMLIncludePrimitives);
                    Write(TSL2Translation.idDMLSet, ps.Set);
                    if (ps.Codec != null) Write(TSL2Translation.idDMLCodec, ps.Codec);
                    if (ps.CodecURI != null) Write(TSL2Translation.idDMLCodecURI, ps.CodecURI);
                    // TODO: Should have an IDmlWriterExtension that can generate configuration information here.
                    WriteEndContainer();
                }
            }
            WriteEndContainer();
        }

        #endregion

        #region "Base Primitive Writers"
        
        #region "By DMLID"

        public void Write(uint ID, UInt64 Value) { WriteStartNode(ID); Writer.WriteCompact64(Value); }                                

        public void Write(uint ID, string Value)
        {
            byte[] ValueData = Encoding.UTF8.GetBytes(Value);
            WriteStartNode(ID); Writer.WriteCompact64((ulong)ValueData.LongLength); Writer.Write(ValueData);
        }

        public void Write(uint ID, byte[] Data)
        {
            WriteStartNode(ID); Writer.WriteCompact64((ulong)Data.LongLength); Writer.Write(Data);            
        }

        public void Write(uint ID, byte[] Data, int index, int count)
        {
            WriteStartNode(ID); Writer.WriteCompact64((ulong)count); Writer.Write(Data, index, count);
        }

        #endregion

        #region "By Inline Identification"

        public void Write(string Name, UInt64 Value) { WriteStartNode(Name, "uint"); Writer.WriteCompact64(Value); }        

        public void Write(string Name, string Value)
        {
            byte[] ValueData = Encoding.UTF8.GetBytes(Value);
            WriteStartNode(Name, "string"); Writer.WriteCompact64((ulong)ValueData.LongLength); Writer.Write(ValueData);
        }

        public void Write(string Name, byte[] Data)
        {
            WriteStartNode(Name, "array-U8"); Writer.WriteCompact64((ulong)Data.LongLength); Writer.Write(Data);
        }

        public void Write(string Name, byte[] Data, int index, int count)
        {
            WriteStartNode(Name, "array-U8"); Writer.WriteCompact64((ulong)count); Writer.Write(Data, index, count);
        }

        #endregion

        #endregion

        #region "Common Primitives"

        #region "By DMLID"

        public void Write(uint ID, Int64 Value) { WriteStartNode(ID); Writer.WriteCompactS64(Value); }

        public void Write(uint ID, bool Value) { WriteStartNode(ID); Writer.Write((byte)(Value ? 1 : 0)); }

        public void Write(uint ID, float Value)
        {
            switch (CommonCodec)
            {
                case Codecs.LE: WriteStartNode(ID); Writer.IsLittleEndian = true; Writer.Write(Value); return;
                case Codecs.BE: WriteStartNode(ID); Writer.IsLittleEndian = false; Writer.Write(Value); return;
                default: throw new DmlException("Common primitive set not selected.");
            }
        }

        public void Write(uint ID, double Value)
        {
            switch (CommonCodec)
            {
                case Codecs.LE: WriteStartNode(ID); Writer.IsLittleEndian = true; Writer.Write(Value); return;
                case Codecs.BE: WriteStartNode(ID); Writer.IsLittleEndian = false; Writer.Write(Value); return;
                default: throw new DmlException("Common primitive set not selected.");
            }
        }        
        
        public void Write(uint ID, DateTime Date)
        {
            Int64 DateValue = (Int64)((Date - DmlInternalData.ReferenceDate).TotalMilliseconds * 1000000.0) /* 10^6 ns per millisecond */;

            switch (CommonCodec)
            {
                case Codecs.LE: WriteStartNode(ID); Writer.IsLittleEndian = true; Writer.Write(DateValue); return;
                case Codecs.BE: WriteStartNode(ID); Writer.IsLittleEndian = false; Writer.Write(DateValue); return;
                default: throw new DmlException("Common primitive set not selected.");
            }
        }

        #endregion

        #region "By Inline Identification"

        public void Write(string Name, Int64 Value) { WriteStartNode(Name, "int"); Writer.WriteCompactS64(Value); }

        public void Write(string Name, bool Value) { WriteStartNode(Name, "boolean"); Writer.Write((byte)(Value ? 1 : 0)); }

        public void Write(string Name, float Value)
        {
            switch (CommonCodec)
            {
                case Codecs.LE: WriteStartNode(Name, "single"); Writer.IsLittleEndian = true; Writer.Write(Value); return;
                case Codecs.BE: WriteStartNode(Name, "single"); Writer.IsLittleEndian = false; Writer.Write(Value); return;
                default: throw new DmlException("Common primitive set not selected.");
            }
        }

        public void Write(string Name, double Value)
        {
            switch (CommonCodec)
            {
                case Codecs.LE: WriteStartNode(Name, "double"); Writer.IsLittleEndian = true; Writer.Write(Value); return;
                case Codecs.BE: WriteStartNode(Name, "double"); Writer.IsLittleEndian = false; Writer.Write(Value); return;
                default: throw new DmlException("Common primitive set not selected.");
            }
        }

        public void Write(string Name, DateTime Date)
        {
            Int64 DateValue = (Int64)((Date - DmlInternalData.ReferenceDate).TotalMilliseconds * 1000000.0) /* 10^6 ns per millisecond */;

            switch (CommonCodec)
            {
                case Codecs.LE: WriteStartNode(Name, "datetime"); Writer.IsLittleEndian = true; Writer.Write(DateValue); return;
                case Codecs.BE: WriteStartNode(Name, "datetime"); Writer.IsLittleEndian = false; Writer.Write(DateValue); return;
                default: throw new DmlException("Common primitive set not selected.");
            }
        }

        #endregion

        #endregion

        #region "Decimal-Float Primitives"
#       if false
        public void Write(uint ID, decimal Value)
        {
            WriteStartNode(ID, 16); Writer.Write(Value);
        }

        public void Write(string Name, decimal Value)
        {
            WriteStartNode(Name, "decimal-float", 16); Writer.Write(Value);
        }
#       endif
        #endregion

        #region "Array Writers"

        private bool IsLEArray
        {
            get
            {
                if (ArrayCodec == Codecs.LE) return true;
                if (ArrayCodec == Codecs.BE) return false;
                throw new Exception("Array primitives not enabled at writer.");
            }
        }

        #region "By DMLID"

        public void Write(uint ID, UInt16[] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(uint ID, UInt32[] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(uint ID, UInt64[] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(uint ID, SByte[] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(uint ID, Int16[] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(uint ID, Int32[] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(uint ID, Int64[] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(uint ID, float[] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(uint ID, double[] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(uint ID, decimal[] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(uint ID, DateTime[] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.LongLength);            
            Writer.IsLittleEndian = IsLEArray;

            long[] DateValues = new long[Data.Length];
            for (int ii = 0; ii < Data.Length; ii++)
                DateValues[ii] = (Int64)((Data[ii] - DmlInternalData.ReferenceDate).TotalMilliseconds * 1000000.0) /* 10^6 ns per millisecond */;
            Writer.Write(DateValues);            
        }

        public void Write(uint ID, string[] Data)
        {            
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.LongLength);
            foreach (string s in Data)
            {
                byte[] Encoded = Encoding.UTF8.GetBytes(s);
                Writer.WriteCompact64((ulong)Encoded.LongLength);
                Writer.Write(Encoded);
            }           
        }

        #endregion        

        #region "By Inline Identification"

        public void Write(string Name, UInt16[] Data)
        {
            WriteStartNode(Name, "array-U16");
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(string Name, UInt32[] Data)
        {
            WriteStartNode(Name, "array-U32");
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(string Name, UInt64[] Data)
        {
            WriteStartNode(Name, "array-U64");
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(string Name, SByte[] Data)
        {
            WriteStartNode(Name, "array-I8");
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(string Name, Int16[] Data)
        {
            WriteStartNode(Name, "array-I16");
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(string Name, Int32[] Data)
        {
            WriteStartNode(Name, "array-I32");
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(string Name, Int64[] Data)
        {
            WriteStartNode(Name, "array-I64");
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(string Name, float[] Data)
        {
            WriteStartNode(Name, "array-SF");
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(string Name, double[] Data)
        {
            WriteStartNode(Name, "array-DF");
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(string Name, decimal[] Data)
        {
            WriteStartNode(Name, "array-10F");
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }
        public void Write(string Name, DateTime[] Data)
        {
            WriteStartNode(Name, "array-DT");
            Writer.WriteCompact64((ulong)Data.LongLength);
            Writer.IsLittleEndian = IsLEArray;

            long[] DateValues = new long[Data.Length];
            for (int ii = 0; ii < Data.Length; ii++)
                DateValues[ii] = (Int64)((Data[ii] - DmlInternalData.ReferenceDate).TotalMilliseconds * 1000000.0) /* 10^6 ns per millisecond */;
            Writer.Write(DateValues);
        }

        public void Write(string Name, string[] Data)
        {
            WriteStartNode(Name, "array-S");
            Writer.WriteCompact64((ulong)Data.LongLength);
            foreach (string s in Data)
            {
                byte[] Encoded = Encoding.UTF8.GetBytes(s);
                Writer.WriteCompact64((ulong)Encoded.LongLength);
                Writer.Write(Encoded);
            }
        }

        #endregion

        #endregion

        #region "Matrix Writers"

        #region "By DMLID"

        public void Write(uint ID, byte[,] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(uint ID, UInt16[,] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(uint ID, UInt32[,] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(uint ID, UInt64[,] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(uint ID, sbyte[,] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(uint ID, Int16[,] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(uint ID, Int32[,] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(uint ID, Int64[,] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(uint ID, float[,] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(uint ID, double[,] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(uint ID, decimal[,] Data)
        {
            WriteStartNode(ID);
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        #endregion

        #region "By Inline Identification"

        public void Write(string Name, byte[,] Data)
        {
            WriteStartNode(Name, "matrix-U8");
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(string Name, UInt16[,] Data)
        {
            WriteStartNode(Name, "matrix-U16");
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(string Name, UInt32[,] Data)
        {
            WriteStartNode(Name, "matrix-U32");
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(string Name, UInt64[,] Data)
        {
            WriteStartNode(Name, "matrix-U64");
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(string Name, sbyte[,] Data)
        {
            WriteStartNode(Name, "matrix-I8");
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(string Name, Int16[,] Data)
        {
            WriteStartNode(Name, "matrix-I16");
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(string Name, Int32[,] Data)
        {
            WriteStartNode(Name, "matrix-I32");
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(string Name, Int64[,] Data)
        {
            WriteStartNode(Name, "matrix-I64");
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(string Name, float[,] Data)
        {
            WriteStartNode(Name, "matrix-SF");
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(string Name, double[,] Data)
        {
            WriteStartNode(Name, "matrix-DF");
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        public void Write(string Name, decimal[,] Data)
        {
            WriteStartNode(Name, "matrix-10F");
            Writer.WriteCompact64((ulong)Data.GetLongLength(0));
            Writer.WriteCompact64((ulong)Data.GetLongLength(1));
            Writer.IsLittleEndian = IsLEArray; Writer.Write(Data);
        }

        #endregion

        #endregion        

        #region "Other Writers"

        public void Write(uint ID, object Value)
        {
            if (Value is bool) Write(ID, (bool)Value);
            else if (Value is DateTime) Write(ID, (DateTime)Value);
            else if (Value is float) Write(ID, (float)Value);
            else if (Value is double) Write(ID, (double)Value);
            else if (Value is string) Write(ID, (string)Value);
            else if (Value is int) Write(ID, (int)Value);
            else if (Value is long) Write(ID, (long)Value);
            else if (Value is uint) Write(ID, (uint)Value);
            else if (Value is ulong) Write(ID, (ulong)Value);
            else if (Value is byte[]) Write(ID, (byte[])Value);
            else if (Value is ushort[]) Write(ID, (ushort[])Value);
            else if (Value is uint[]) Write(ID, (uint[])Value);
            else if (Value is ulong[]) Write(ID, (ulong[])Value);
            else if (Value is sbyte[]) Write(ID, (sbyte[])Value);
            else if (Value is short[]) Write(ID, (short[])Value);
            else if (Value is int[]) Write(ID, (int[])Value);
            else if (Value is long[]) Write(ID, (long[])Value);
            else if (Value is float[]) Write(ID, (float[])Value);
            else if (Value is double[]) Write(ID, (double[])Value);
            else if (Value is DateTime[]) Write(ID, (DateTime[])Value);
            else if (Value is string[]) Write(ID, (string[])Value);
            else if (Value is byte[,]) Write(ID, (byte[,])Value);
            else if (Value is ushort[,]) Write(ID, (ushort[,])Value);
            else if (Value is uint[,]) Write(ID, (uint[,])Value);
            else if (Value is ulong[,]) Write(ID, (ulong[,])Value);
            else if (Value is sbyte[,]) Write(ID, (sbyte[,])Value);
            else if (Value is short[,]) Write(ID, (short[,])Value);
            else if (Value is int[,]) Write(ID, (int[,])Value);
            else if (Value is long[,]) Write(ID, (long[,])Value);
            else if (Value is float[,]) Write(ID, (float[,])Value);
            else if (Value is double[,]) Write(ID, (double[,])Value);
            else throw new FormatException("Unsupport primitive type: " + Value.GetType().ToString());
        }

        public void Write(string XMLName, object Value)
        {
            if (Value is bool) Write(XMLName, (bool)Value);
            else if (Value is DateTime) Write(XMLName, (DateTime)Value);
            else if (Value is float) Write(XMLName, (float)Value);
            else if (Value is double) Write(XMLName, (double)Value);
            else if (Value is string) Write(XMLName, (string)Value);
            else if (Value is int) Write(XMLName, (int)Value);
            else if (Value is long) Write(XMLName, (long)Value);
            else if (Value is uint) Write(XMLName, (uint)Value);
            else if (Value is ulong) Write(XMLName, (ulong)Value);
            else if (Value is byte[]) Write(XMLName, (byte[])Value);
            else if (Value is ushort[]) Write(XMLName, (ushort[])Value);
            else if (Value is uint[]) Write(XMLName, (uint[])Value);
            else if (Value is ulong[]) Write(XMLName, (ulong[])Value);
            else if (Value is sbyte[]) Write(XMLName, (sbyte[])Value);
            else if (Value is short[]) Write(XMLName, (short[])Value);
            else if (Value is int[]) Write(XMLName, (int[])Value);
            else if (Value is long[]) Write(XMLName, (long[])Value);
            else if (Value is float[]) Write(XMLName, (float[])Value);
            else if (Value is double[]) Write(XMLName, (double[])Value);
            else if (Value is DateTime[]) Write(XMLName, (DateTime[])Value);
            else if (Value is string[]) Write(XMLName, (string[])Value);
            else if (Value is byte[,]) Write(XMLName, (byte[,])Value);
            else if (Value is ushort[,]) Write(XMLName, (ushort[,])Value);
            else if (Value is uint[,]) Write(XMLName, (uint[,])Value);
            else if (Value is ulong[,]) Write(XMLName, (ulong[,])Value);
            else if (Value is sbyte[,]) Write(XMLName, (sbyte[,])Value);
            else if (Value is short[,]) Write(XMLName, (short[,])Value);
            else if (Value is int[,]) Write(XMLName, (int[,])Value);
            else if (Value is long[,]) Write(XMLName, (long[,])Value);
            else if (Value is float[,]) Write(XMLName, (float[,])Value);
            else if (Value is double[,]) Write(XMLName, (double[,])Value);
            else throw new FormatException("Unsupport primitive type: " + Value.GetType().ToString());
        }

        private static UInt64 PaddingNodeHeadSize = (UInt64)EndianBinaryWriter.SizeCompact32(DML3Translation.idDMLPadding);

        public bool CanSeek { get { return Writer.BaseStream.CanSeek; } }
        public UInt64 Position { get { return (UInt64)Writer.BaseStream.Position; } }
        public void Seek(UInt64 Position) { Writer.BaseStream.Seek((long)Position, SeekOrigin.Begin);  }

        /// <summary>
        /// WriteReservedSpace() emits a sequence of padding into the stream of length 'ReservedSpace'.
        /// In seekable streams, the sequence may be overwritten by DML later, although another WriteReserveSpace()
        /// call must be made at the end of the rewrite to "re-reserve" any portion not utilized in
        /// the rewrite.
        /// </summary>
        /// <param name="ReservedSpace">The number of bytes of reserved space to emit.  The node head
        /// will be part of the reserved space.  For example, a ReservedSpace value of 1 will result in
        /// a single-byte padding node.</param>
        public void WriteReservedSpace(UInt64 ReservedSpace)
        {
            if (ReservedSpace < 8)
            {
                for (int ii = 0; ii < (int)ReservedSpace; ii++) Writer.Write(DML3Translation.WholePaddingByte);
            }
            else
            {
                /** Multi-byte padding nodes include a data size value that is itself encoded
                 *  as Compact-64.  A Compact-64's encoded size is variable, which makes hitting
                 *  an intended size a bit tricky.  We'll emit some single-byte paddings to round 
                 *  it out prior to writing the multi-byte padding.
                 */
                UInt64 DataSize = ReservedSpace - PaddingNodeHeadSize - 1UL;
                UInt64 SizeSize = (UInt64)EndianBinaryWriter.SizeCompact64(DataSize);
                while (PaddingNodeHeadSize + SizeSize + DataSize > ReservedSpace)
                {
                    DataSize--;
                    SizeSize = (UInt64)EndianBinaryWriter.SizeCompact64(DataSize);
                }
                while (PaddingNodeHeadSize + SizeSize + DataSize < ReservedSpace)
                {
                    // Emit some single-byte padding to round it out.
                    Writer.Write(DML3Translation.WholePaddingByte);
                    ReservedSpace--;
                }
                WriteStartNode(DML3Translation.idDMLPadding);
                ReservedSpace -= PaddingNodeHeadSize;
                Writer.WriteCompact64(DataSize);
                ReservedSpace -= SizeSize;

                byte[] Buffer = new byte [4090];
                while (ReservedSpace > 0)
                {
                    UInt64 WriteSize = ReservedSpace;
                    if (WriteSize > (UInt64)Buffer.Length) WriteSize = (UInt64)Buffer.Length;
                    Writer.Write(Buffer, 0, (int)WriteSize);
                    ReservedSpace -= WriteSize;
                }
            }
        }    

        /// <summary>
        /// WriteComment() can be used to emit a comment node.
        /// </summary>
        /// <param name="Text">Text to incorporate into comment.</param>
        public void WriteComment(string Text)
        {
            byte[] ValueData = Encoding.UTF8.GetBytes(Text);
            WriteStartNode(DML3Translation.idDMLComment);
            Writer.WriteCompact64((ulong)ValueData.LongLength);
            Writer.Write(ValueData);
        }
        
        /// <summary>
        /// Call WriteStartExtension() to write an extended primitive node.  The WriteStartExtension()
        /// method writes the node head with the given parameters, then returns an EndianBinaryWriter
        /// that can be used to write the node content.
        /// </summary>
        /// <param name="ID">DMLID of the node.</param>        
        /// <returns>An EndianBinaryWriter which can be used to write the node's content.</returns>
        public EndianBinaryWriter WriteStartExtension(UInt32 ID)
        {
            WriteStartNode(ID);
            return Writer;
        }

        /// <summary>
        /// Call WriteStartExtension() to write an extended primitive node.  The WriteStartExtension()
        /// method writes the node head with the given parameters, then returns an EndianBinaryWriter
        /// that can be used to write the node content.  This overload generates an inline identification
        /// node.
        /// </summary>
        /// <param name="Name">XML-Compatible name of the node.</param>
        /// <param name="NodeType">Type string identifying the node primitive.</param>        
        /// <returns>An EndianBinaryWriter which can be used to write the node's content.</returns>
        public EndianBinaryWriter WriteStartExtension(string Name, string NodeType)
        {
            WriteStartNode(Name, NodeType);
            return Writer;
        }

        #endregion

        #region "DML-EC (Encrypted/Compressed) Writers"        

        /// <summary>
        /// EncMessageStream writes the "Encrypted Message" format utilized in DML-EC's encrypted
        /// nodes.  It provides a buffer layer which will write the block count values out when
        /// a certain threshold is reached.
        /// </summary>
        internal class EncMessageStream : Stream
        {
            public Stream BaseStream;
            private EndianBinaryWriter Writer;
            private const int BlockSize = 128 / 8;
            private const int BlockQueueSize = 127;     // 127 is the largest single-byte Compact-64 value, a buffer size of 2,032 bytes.
            private byte[] Buffer = new byte[BlockSize * BlockQueueSize];
            private int Used = 0;

            public EncMessageStream(Stream BaseStream)
            {
                this.BaseStream = BaseStream;
                this.Writer = new EndianBinaryWriter(BaseStream, false);
            }

            public override bool CanRead { get { return false; } }
            public override bool CanWrite { get { return BaseStream.CanWrite; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanTimeout { get { return BaseStream.CanTimeout; } }

            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position { 
                get { throw new NotSupportedException(); }
                set { throw new NotImplementedException(); }
            }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override int Read(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

            public override void Close()
            {
                BaseStream = null;
                Writer = null;
            }

            public override void Flush()
            {
                if (BaseStream == null) throw new Exception("Stream already closed.");
                if (Used == 0) { BaseStream.Flush(); return; }
                if ((Used % BlockSize) != 0) throw new Exception("Cannot flush stream - can only write in 128-bit segments.");
                int BlockCount = Used / BlockSize;
                Writer.WriteCompact64((ulong)BlockCount);
                BaseStream.Write(Buffer, 0, Used);
                BaseStream.Flush();
                Used = 0;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {                
                for (;;)
                {
                    int Avail = Buffer.Length - Used;
                    if (Avail < count)
                    {
                        for (int ii = 0; ii < Avail; ii++, count--) Buffer[Used++] = buffer[offset++];
                        Flush();
                    }
                    else
                    {
                        while (count > 0) { Buffer[Used++] = buffer[offset++]; count--; }
                        return;
                    }
                }
            }

            public override void WriteByte(byte value)
            {
                if (Used >= Buffer.Length) Flush();
                Buffer[Used++] = value;
            }            
        }

        /// <summary>
        /// WriteStartEncryptedDml() begins the writing and AES encryption of a DML fragment.
        /// The WriteStartEncryptedDml() method returns a DmlWriter which accepts ordinary 
        /// writing of DML but writes it to an encryptor which is fed into this DmlWriter.  In
        /// this way, DML can be encrypted in an as-you-go method which does not require
        /// retaining the encrypted message in memory before writing it to the underlying
        /// stream.  The WriteEndEncryptedDml() method must be called to terminate the
        /// encryption node.  
        /// </summary>
        /// <param name="Key">Secret encryption key.  Can be 128, 192, or 256 bits in length.</param>
        /// <param name="AttachHMAC">True to calculate and attach an authentication code to the message.  False to omit.</param>
        /// <returns>A DmlWriter which will output encrypted content to the node.</returns>
        public DmlWriter WriteStartEncryptedDml(byte[] Key, bool AttachHMAC)
        {
            if (AttachHMAC)
                WriteStartNode(EC2Translation.idAuthenticEncrypted);
            else
                WriteStartNode(EC2Translation.idEncrypted);

            RijndaelManaged AES = new RijndaelManaged();
            AES.Key = Key;
            AES.GenerateIV();
            AES.Padding = PaddingMode.ISO10126;
            AES.Mode = CipherMode.CBC;
            ICryptoTransform Encryptor = AES.CreateEncryptor();
            
            Writer.Write(AES.IV);

            Stream WriterStream = new EncMessageStream(Writer.BaseStream);            
            WriterStream = new CryptoStream(WriterStream, Encryptor, CryptoStreamMode.Write);

            if (AttachHMAC)
                WriterStream = new StreamWithHash(WriterStream, new HMACSHA384(Key));

            return DmlWriter.Create(WriterStream, this);
        }

        /// <summary>
        /// Call WriteEndEncryptedDml() to terminate an encrypted DML node started with an earlier
        /// WriteStartEncryptedDml() call.
        /// </summary>
        /// <param name="EncryptedWriter">The DmlWriter provided by a previous WriteStartEncrypedDml()
        /// call.</param>
        public void WriteEndEncryptedDml(DmlWriter EncryptedWriter)
        {
            Stream WriterStream = EncryptedWriter.Writer.BaseStream;
            WriterStream.Flush();
            Writer.WriteCompact64(0);               // Write termination block count.
            if (WriterStream is StreamWithHash)
            {
                byte[] HMAC = ((StreamWithHash)WriterStream).Hash;
                WriterStream.Close();
                Writer.Write(HMAC);
            }
            else WriterStream.Close();
        }

        /// <summary>
        /// WriteEncryptedDml() writes an encrypted node out in its entirety.  This overload
        /// requires that the encrypted message be provided from a stream, usually in-memory.
        /// The initialization vector must be provided.  An HMAC-384 code can be provided.
        /// </summary>
        /// <seealso>WriteStartEncryptedDml(), WriteEndEncryptedDml()</seealso>
        /// <param name="EncryptedMessage">The encrypted message, including block count markers
        /// and the terminating empty count.</param>
        /// <param name="IV">Initialization vector</param>
        /// <param name="HMAC384">HMAC-384 Authentication Code, or null.</param>
        internal void WriteEncryptedDml(Stream EncryptedMessage, byte[] IV, byte[] HMAC384)
        {
            if (HMAC384 == null)
                WriteStartNode(EC2Translation.idEncrypted);
            else
                WriteStartNode(EC2Translation.idAuthenticEncrypted);
            Writer.Write(IV);

            byte[] buffer = new byte[4090];
            for (; ; )
            {
                int nLength = EncryptedMessage.Read(buffer, 0, buffer.Length);
                if (nLength == 0) break;
                Writer.Write(buffer, 0, nLength);
            }

            // Write termination block count.  This should be part of 'EncryptedMessage',
            // which is why I made this an internal method - since it isn't part of 'EncryptedMessage'
            // when DmlEncrypted generates it.
            Writer.WriteCompact64(0);

            if (HMAC384 != null)
            {
                if (HMAC384.Length != 48) throw new FormatException("Expected 384-bit hash during dml encryption.");                
                Writer.Write(HMAC384);
            }
        }

        /// <summary>
        /// WriteStartCompressedDml() begins the writing and compression of a DML fragment.
        /// The WriteStartCompressedDml() method returns a DmlWriter which accepts ordinary 
        /// writing of DML but writes it to a compressor which is fed into this DmlWriter.  In
        /// this way, DML can be compressed in an as-you-go method which does not require
        /// retaining the compressed message in memory before writing it to the underlying
        /// stream.  The WriteEndCompressedDml() method must be called to terminate the
        /// compressed node.  
        /// </summary>                
        /// <param name="AttachCRC32">True to calculate and attach a verification code to the message.  False to omit.</param>
        /// <returns>A DmlWriter which will output compressed content to the node.</returns>
        public DmlWriter WriteStartCompressedDml(bool AttachCRC32)
        {
            if (AttachCRC32)
                WriteStartNode(EC2Translation.idVerifiedCompressed);
            else
                WriteStartNode(EC2Translation.idCompressed);

            Stream WriterStream;
            WriterStream = new DeflateStream(Writer.BaseStream, CompressionMode.Compress);

            if (AttachCRC32)
                WriterStream = new StreamWithHash(WriterStream, CRC32.CreateCastagnoli());

            return DmlWriter.Create(WriterStream, this);
        }

        /// <summary>
        /// Call WriteEndCompressedDml() to terminate a compressed DML node started with an earlier
        /// WriteStartCompressedDml() call.
        /// </summary>
        /// <param name="CompressedWriter">The DmlWriter provided by a previous WriteStartCompressedDml()
        /// call.</param>
        public void WriteEndCompressedDml(DmlWriter CompressedWriter)
        {
            Stream WriterStream = CompressedWriter.Writer.BaseStream;
            WriterStream.Flush();
            if (WriterStream is StreamWithHash)
            {
                byte[] CRC32 = ((StreamWithHash)WriterStream).Hash;
                if (CRC32.Length != 4) throw new FormatException("Expected 32-bit hash during dml compression.");                
                WriterStream.Close();
                Writer.IsLittleEndian = false;
                Writer.WriteLargeUI(CRC32);
            }
            else WriterStream.Close();
        }

        /// <summary>
        /// WriteCompressedDml() writes a compressed node out in its entirety.  This overload
        /// requires that the compressed data be provided from a stream, usually in-memory.
        /// A CRC-32 code can be provided.
        /// </summary>
        /// <seealso>WriteStartCompressedDml(), WriteEndCompressedDml()</seealso>
        /// <param name="CompressedDml">The compressed message (DEFLATE stream), including block 
        /// headers.</param>
        /// <param name="Crc32">CRC-32 Code, or null.  If provided, the CRC-32 code should be
        /// calculated from the decompressed form of the dml.</param>
        internal void WriteCompressedDml(Stream CompressedDml, byte[] Crc32)
        {
            if (Crc32 == null)
                WriteStartNode(EC2Translation.idCompressed);
            else
                WriteStartNode(EC2Translation.idVerifiedCompressed);

            byte[] buffer = new byte [4090];
            for (;;)
            {
                int nLength = CompressedDml.Read(buffer, 0, buffer.Length);
                if (nLength == 0) break;
                Writer.Write(buffer, 0, nLength);
            }

            if (Crc32 != null)
            {
                if (Crc32.Length != 4) throw new ArgumentException("Crc32");
                Writer.IsLittleEndian = false;
                Writer.WriteLargeUI(Crc32);
            }
        }

        #endregion
    }
}
