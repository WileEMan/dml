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
using System.Net;

namespace WileyBlack.Dml.Dom
{    
    /// <summary>
    /// DmlDocument provides the top-level of the Dml Document Object Model (DOM).  A DmlDocument
    /// can be created in memory, loaded from a stream or file, and/or saved to a stream or file.  
    /// The DmlDocument is itself a DmlContainer that represents the top-level Dml container in
    /// any document.  The document's header, translation, and other ancillary details can be 
    /// accessed through the DmlDocument's properties.  All loading processes are routed through
    /// the DmlDocument's creation factories, permitting custom loading behavior in derived
    /// classes.
    /// </summary>
    public class DmlDocument : DmlContainer
    {
        public DmlDocument()
        {
            Document = this;
            Header = new DmlHeader(this);
            Loaded = LoadState.Full;
        }

        public override DmlNode Clone(DmlDocument NewDoc)
        {
            throw new NotSupportedException();
        }

        #region "Properties"

        /// <summary>
        /// GlobalTranslation provides the top-level translation currently associated with this
        /// document.  When reading a DML document, this translation is populated by the Load()
        /// routine if name resolution is provided, or if a translation is provided.  When
        /// writing a DML document, this translation should be provided.  
        /// </summary>
        public DmlTranslation GlobalTranslation = DmlTranslation.DML3.Clone();

        /// <summary>
        /// Header contains the DmlHeader for the document.  The DML:Header container provides
        /// processing rules for the document and starts every DML document.
        /// </summary>
        public DmlHeader Header;

        /// <summary>
        /// See ResolvedHeader.
        /// </summary>
        private ResolvedTranslation m_ResolvedHeader = null;

        /// <summary>
        /// After LoadHeader() has been called, ResolvedHeader contains the header information
        /// reduced to its fully processed representation.  The ResolvedHeader will be a
        /// DomResolvedTranslation object if resolution included the XmlRoot element.  This
        /// is always true when using DOM to resolve the header, but the resulting XmlRoot
        /// will only be valid if the IResourceResolution provider also provided 
        /// DomResolvedTranslation objects when returning any pre-processed resource.  Note
        /// that ResolvedHeader is not updated if Header is changed, and changes made to the
        /// ResolvedHeader will not be captured by Header, Save(), or WriteTo() operations.
        /// </summary>
        public ResolvedTranslation ResolvedHeader
        {
            get
            {
                if (m_ResolvedHeader == null) throw new Exception("ResolvedHeader is not available until the Dml Header has been processed.");
                return m_ResolvedHeader;
            }
        }

        #endregion

        #region "Load() routines"

        /// <summary>
        /// The Load() method populates the DmlDocument from a DML formatted stream.  An in-memory
        /// tree is generated from the DML stream.  No resource resolution is provided and an error
        /// is generated if additional resources are required.
        /// </summary>
        /// <param name="Source">The stream from which to generate the in-memory DmlDocument</param>
        public void Load(Stream Source)
        {
            this.GlobalTranslation = DmlTranslation.DML3.Clone();
            Load(Source, (IResourceResolution)null);
        }

        /// <summary>
        /// The Load() method populates the DmlDocument from a DML formatted stream.  An in-memory
        /// tree is generated from the DML stream.  No resource resolution is provided and an error
        /// is generated if additional resources are required.
        /// </summary>
        public void Load(DmlReader Reader)
        {
            this.GlobalTranslation = DmlTranslation.DML3.Clone();
            Load(Reader, (IResourceResolution)null);
        }

        /// <summary>
        /// The Load() method populates the DmlDocument from a DML formatted file.  An in-memory
        /// tree is generated from the DML stream.  No resource resolution is provided and an error
        /// is generated if additional resources are required.
        /// </summary>
        public void Load(string FileName)
        {
            this.GlobalTranslation = DmlTranslation.DML3.Clone();
            using (FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read)) Load(fs, (IResourceResolution)null);
        }

        /// <summary>
        /// The Load() method populates the DmlDocument from a DML formatted stream.  An in-memory
        /// tree is generated from the DML stream.
        /// </summary>
        /// <param name="Source">The stream from which to generate the in-memory DmlDocument</param>
        public void Load(Stream Source, DmlTranslation Translation)
        {
            this.GlobalTranslation = Translation;
            Load(Source, (IResourceResolution)null);
        }

        /// <summary>
        /// The Load() method populates the DmlDocument from a DML formatted stream.  An in-memory
        /// tree is generated from the DML stream.
        /// </summary>
        public void Load(DmlReader Reader, DmlTranslation Translation)
        {
            this.GlobalTranslation = Translation;
            Load(Reader, (IResourceResolution)null);
        }

        /// <summary>
        /// The Load() method populates the DmlDocument from a DML formatted stream.  An in-memory
        /// tree is generated from the DML stream.  
        /// </summary>
        /// <param name="Source">The stream from which to generate the in-memory DmlDocument</param>
        public virtual void Load(Stream Source, IResourceResolution References)
        {
            using (DmlReader Reader = DmlReader.Create(Source)) { Load(Reader, References); }
        }

        /// <summary>
        /// The Load() method populates the DmlDocument from a DML formatted stream.  An in-memory
        /// tree is generated from the DML stream.  
        /// </summary>        
        public virtual void Load(DmlReader Reader, IResourceResolution References)
        {
            LoadHeader(Reader, References);

            // Load the document's content
            Loaded = LoadState.None;
            base.LoadContent(Reader);
        }

        #endregion

        #region "LoadPartial() routines"

        /// <summary>
        /// The LoadPartial() method populates the top-level of the DmlDocument from a DML formatted stream.  An 
        /// in-memory tree is generated from the DML stream.  The portion of the tree that is loaded is controlled
        /// by the ToLoad value.  An incorrect LoadPartial() sequence will cause an exception.  No resource 
        /// resolution is provided and an error is generated if additional resources are required.
        /// </summary>
        /// <param name="Source">The stream from which to generate the in-memory DmlDocument</param>
        public DmlReader LoadPartial(Stream Source, LoadState ToLoad)
        {
            this.GlobalTranslation = DmlTranslation.DML3.Clone();
            return LoadPartial(Source, ToLoad, (IResourceResolution)null);
        }

        /// <summary>
        /// The LoadPartial() method populates the top-level of the DmlDocument from a DML formatted stream.  An 
        /// in-memory tree is generated from the DML stream.  The portion of the tree that is loaded is controlled
        /// by the ToLoad value.  An incorrect LoadPartial() sequence will cause an exception.  No resource 
        /// resolution is provided and an error is generated if additional resources are required.
        /// </summary>
        public override void LoadPartial(DmlReader Reader, LoadState ToLoad)
        {
            this.GlobalTranslation = DmlTranslation.DML3.Clone();
            LoadPartial(Reader, ToLoad, (IResourceResolution)null);
        }

        /// <summary>
        /// The LoadPartial() method populates the top-level of the DmlDocument from a DML formatted stream.  An 
        /// in-memory tree is generated from the DML stream.  The portion of the tree that is loaded is controlled
        /// by the ToLoad value.  An incorrect LoadPartial() sequence will cause an exception.  No resource 
        /// resolution is provided and an error is generated if additional resources are required.
        /// </summary>
        /// <param name="Source">The stream from which to generate the in-memory DmlDocument</param>
        public DmlReader LoadPartial(Stream Source, LoadState ToLoad, DmlTranslation Translation)
        {
            this.GlobalTranslation = Translation;
            return LoadPartial(Source, ToLoad, (IResourceResolution)null);
        }

        /// <summary>
        /// The LoadPartial() method populates the top-level of the DmlDocument from a DML formatted stream.  An 
        /// in-memory tree is generated from the DML stream.  The portion of the tree that is loaded is controlled
        /// by the ToLoad value.  An incorrect LoadPartial() sequence will cause an exception.  No resource 
        /// resolution is provided and an error is generated if additional resources are required.
        /// </summary>
        public void LoadPartial(DmlReader Reader, LoadState ToLoad, DmlTranslation Translation)
        {
            this.GlobalTranslation = Translation;
            LoadPartial(Reader, ToLoad, (IResourceResolution)null);
        }

        /// <summary>
        /// The LoadPartial() method populates the top-level of the DmlDocument from a DML formatted stream.  An 
        /// in-memory tree is generated from the DML stream.  The portion of the tree that is loaded is controlled
        /// by the ToLoad value.  An incorrect LoadPartial() sequence will cause an exception.  
        /// </summary>
        /// <param name="Source">The stream from which to generate the in-memory DmlDocument</param>
        public virtual DmlReader LoadPartial(Stream Source, LoadState ToLoad, IResourceResolution References)
        {
            DmlReader Reader = DmlReader.Create(Source);
            LoadPartial(Reader, ToLoad, References);
            return Reader;
        }

        private void LoadHeader(DmlReader Reader, IResourceResolution References)
        {
            if (References == null) Reader.GlobalTranslation = GlobalTranslation;

            // Load header
            Header = new DmlHeader(this);
            Header.Loaded = LoadState.None;
            Header.LoadContent(Reader);
            if (Header.ReadVersion != DmlInternalData.DMLReadVersion)
                throw CreateDmlException("DML Parser does not support this version of DML content.");            

            // Identify or parse translation if necessary/possible            
            m_ResolvedHeader = Header.ToTranslation(References, Reader.Options);
            Reader.GlobalTranslation = GlobalTranslation = m_ResolvedHeader.Translation;

            // Load primitive sets from the document...
            foreach (PrimitiveSet ps in ResolvedHeader.PrimitiveSets) Reader.AddPrimitiveSet(ps);
        }

        //// <summary>
        /// The LoadPartial() method populates the top-level of the DmlDocument from a DML formatted stream.  An 
        /// in-memory tree is generated from the DML stream.  The portion of the tree that is loaded is controlled
        /// by the ToLoad value.  An incorrect LoadPartial() sequence will cause an exception.
        /// </summary>
        public virtual void LoadPartial(DmlReader Reader, LoadState ToLoad, IResourceResolution References)
        {
            LoadHeader(Reader, References);
            
            // Load the document's content
            Loaded = LoadState.None;
            base.LoadPartial(Reader, ToLoad);
        }

        #endregion

        #region "Save() routines"

        public virtual void Save(Stream outStream)
        {
            DmlWriter Writer = DmlWriter.Create(outStream);
            WriteTo(Writer);
        }

        public void SaveToFile(string Path, bool Overwrite)
        {
            using (FileStream fs = new FileStream(Path, Overwrite ? FileMode.Create : FileMode.CreateNew))
                Save(fs);
        }

        #endregion

        #region "WriteTo() and GetEncodedSize()"

        public override void WriteTo(DmlWriter Writer)
        {
            Header.WriteTo(Writer);
            base.WriteTo(Writer);
        }

        public override ulong GetEncodedSize(DmlWriter Writer)
        {
            UInt64 BodySize = base.GetEncodedSize(Writer);
            if (BodySize == UInt64.MaxValue) return UInt64.MaxValue;
            UInt64 HeaderSize = Header.GetEncodedSize(Writer);
            if (HeaderSize == UInt64.MaxValue) return UInt64.MaxValue;
            return BodySize + HeaderSize;
        }

        #endregion

        #region "In-memory creation factories"
        
        public virtual DmlContainer CreateContainer(Association assoc, DmlFragment Context)
        {
            DmlContainer ret;
            if (Context != null && Context is DmlHeader)
            {
                switch (assoc.DMLID)
                {
                    case TSL2Translation.idDMLIncludeTranslation: ret = new DmlIncludeTranslation(); break;
                    case TSL2Translation.idDMLIncludePrimitives: ret = new DmlIncludePrimitives(); break;
                    case TSL2Translation.idNode: ret = new DmlNodeDefinition(); break;
                    case TSL2Translation.idContainer: ret = new DmlContainerDefinition(); break;
                    case TSL2Translation.idRenumber: ret = new DmlTranslationRenumber(); break;
                    default: ret = new DmlContainer(); ret.Association = assoc; break;
                }
                ret.Document = this;
                ret.Container = Context;
                return ret;
            }
            ret = new DmlContainer(this); 
            ret.Association = assoc;
            ret.Container = Context;
            return ret;
        }
        
        public virtual DmlFragment CreateFragment() { DmlFragment ret = new DmlFragment(this); return ret; }

        public DmlBool CreateBool() { DmlBool ret = new DmlBool(this); return ret; }
        public DmlDateTime CreateDateTime() { DmlDateTime ret = new DmlDateTime(this); return ret; }
        //public DmlDecimal CreateDecimal() { DmlDecimal ret = new DmlDecimal(this); return ret; }
        public DmlDouble CreateDouble() { DmlDouble ret = new DmlDouble(this); return ret; }
        public DmlSingle CreateSingle() { DmlSingle ret = new DmlSingle(this); return ret; }
        public DmlInt CreateInt() { DmlInt ret = new DmlInt(this); return ret; }
        public DmlUInt CreateUInt() { DmlUInt ret = new DmlUInt(this); return ret; }
        public DmlString CreateString() { DmlString ret = new DmlString(this); return ret; }
        public DmlByteArray CreateByteArray() { DmlByteArray ret = new DmlByteArray(this); return ret; }

        public DmlBool CreateBool(Association Assoc, bool Value) { DmlBool ret = new DmlBool(this); ret.Association = Assoc; ret.Value = Value; return ret; }
        public DmlDateTime CreateDateTime(Association Assoc, DateTime Value) { DmlDateTime ret = new DmlDateTime(this); ret.Association = Assoc; ret.Value = Value; return ret; }
        //public DmlDecimal CreateDecimal(Association Assoc, decimal Value) { DmlDecimal ret = new DmlDecimal(this); ret.Association = Assoc; ret.Value = Value; return ret; }
        public DmlDouble CreateDouble(Association Assoc, double Value) { DmlDouble ret = new DmlDouble(this); ret.Association = Assoc; ret.Value = Value; return ret; }
        public DmlSingle CreateSingle(Association Assoc, float Value) { DmlSingle ret = new DmlSingle(this); ret.Association = Assoc; ret.Value = Value; return ret; }
        public DmlInt CreateInt(Association Assoc, int Value) { DmlInt ret = new DmlInt(this); ret.Association = Assoc; ret.Value = Value; return ret; }
        public DmlUInt CreateUInt(Association Assoc, uint Value) { DmlUInt ret = new DmlUInt(this); ret.Association = Assoc; ret.Value = Value; return ret; }
        public DmlString CreateString(Association Assoc, string Value) { DmlString ret = new DmlString(this); ret.Association = Assoc; ret.Value = Value; return ret; }
        public DmlByteArray CreateByteArray(Association Assoc, byte[] Values) { DmlByteArray ret = new DmlByteArray(this); ret.Association = Assoc; ret.Value = Values; return ret; }

        public DmlUInt16Array CreateUInt16Array() { DmlUInt16Array ret = new DmlUInt16Array(this); return ret; }
        //public DmlUInt24Array CreateUInt24Array() { DmlUInt24Array ret = new DmlUInt24Array(this); return ret; }
        public DmlUInt32Array CreateUInt32Array() { DmlUInt32Array ret = new DmlUInt32Array(this); return ret; }
        public DmlUInt64Array CreateUInt64Array() { DmlUInt64Array ret = new DmlUInt64Array(this); return ret; }
        public DmlSByteArray CreateSByteArray() { DmlSByteArray ret = new DmlSByteArray(this); return ret; }
        public DmlInt16Array CreateInt16Array() { DmlInt16Array ret = new DmlInt16Array(this); return ret; }
        //public DmlInt24Array CreateInt24Array() { DmlInt24Array ret = new DmlInt24Array(this); return ret; }
        public DmlInt32Array CreateInt32Array() { DmlInt32Array ret = new DmlInt32Array(this); return ret; }
        public DmlInt64Array CreateInt64Array() { DmlInt64Array ret = new DmlInt64Array(this); return ret; }
        public DmlSingleArray CreateSingleArray() { DmlSingleArray ret = new DmlSingleArray(this); return ret; }
        public DmlDoubleArray CreateDoubleArray() { DmlDoubleArray ret = new DmlDoubleArray(this); return ret; }
        //public DmlDecimalArray CreateDecimalArray() { DmlDecimalArray ret = new DmlDecimalArray(this); return ret; }
        public DmlDateTimeArray CreateDateTimeArray() { DmlDateTimeArray ret = new DmlDateTimeArray(this); return ret; }
        public DmlStringArray CreateStringArray() { DmlStringArray ret = new DmlStringArray(this); return ret; }

        public DmlByteMatrix CreateByteMatrix() { DmlByteMatrix ret = new DmlByteMatrix(this); return ret; }
        public DmlUInt16Matrix CreateUInt16Matrix() { DmlUInt16Matrix ret = new DmlUInt16Matrix(this); return ret; }
        //public DmlUInt24Matrix CreateUInt24Matrix() { DmlUInt24Matrix ret = new DmlUInt24Matrix(this); return ret; }
        public DmlUInt32Matrix CreateUInt32Matrix() { DmlUInt32Matrix ret = new DmlUInt32Matrix(this); return ret; }
        public DmlUInt64Matrix CreateUInt64Matrix() { DmlUInt64Matrix ret = new DmlUInt64Matrix(this); return ret; }
        public DmlSByteMatrix CreateSByteMatrix() { DmlSByteMatrix ret = new DmlSByteMatrix(this); return ret; }
        public DmlInt16Matrix CreateInt16Matrix() { DmlInt16Matrix ret = new DmlInt16Matrix(this); return ret; }
        //public DmlInt24Matrix CreateInt24Matrix() { DmlInt24Matrix ret = new DmlInt24Matrix(this); return ret; }
        public DmlInt32Matrix CreateInt32Matrix() { DmlInt32Matrix ret = new DmlInt32Matrix(this); return ret; }
        public DmlInt64Matrix CreateInt64Matrix() { DmlInt64Matrix ret = new DmlInt64Matrix(this); return ret; }
        public DmlSingleMatrix CreateSingleMatrix() { DmlSingleMatrix ret = new DmlSingleMatrix(this); return ret; }
        public DmlDoubleMatrix CreateDoubleMatrix() { DmlDoubleMatrix ret = new DmlDoubleMatrix(this); return ret; }
        //public DmlDecimalMatrix CreateDecimalMatrix() { DmlDecimalMatrix ret = new DmlDecimalMatrix(this); return ret; }

        public DmlComment CreateComment() { DmlComment ret = new DmlComment(this); return ret; }
        public DmlPadding CreatePadding() { DmlPadding ret = new DmlPadding(this); return ret; }        
        
        public DmlCompressed CreateCompressed() { DmlCompressed ret = new DmlCompressed(this); return ret; }
        //public DmlEncrypted CreateEncrypted() { DtlEncrypted ret = new DtlEncrypted(this); return ret; }

        public virtual DmlPrimitive CreatePrimitive(PrimitiveTypes PrimitiveType, ArrayTypes ArrayType)
        {
            switch (PrimitiveType)
            {
                case PrimitiveTypes.Boolean: return CreateBool();
                case PrimitiveTypes.Int: return CreateInt();
                case PrimitiveTypes.UInt: return CreateUInt();
                case PrimitiveTypes.String: return CreateString();

                case PrimitiveTypes.DateTime: return CreateDateTime();
                case PrimitiveTypes.Single: return CreateSingle();
                case PrimitiveTypes.Double: return CreateDouble();

                case PrimitiveTypes.Array:
                    switch (ArrayType)
                    {
                        case ArrayTypes.U8: return CreateByteArray();
                        case ArrayTypes.U16: return CreateUInt16Array();
                        //case ArrayTypes.U24: return CreateUInt24Array();
                        case ArrayTypes.U32: return CreateUInt32Array();
                        case ArrayTypes.U64: return CreateUInt64Array();
                        case ArrayTypes.I8: return CreateSByteArray();
                        case ArrayTypes.I16: return CreateInt16Array();
                        //case ArrayTypes.I24: return CreateInt24Array(); 
                        case ArrayTypes.I32: return CreateInt32Array();
                        case ArrayTypes.I64: return CreateInt64Array();
                        case ArrayTypes.Singles: return CreateSingleArray();
                        case ArrayTypes.Doubles: return CreateDoubleArray();
                        //case ArrayTypes.Decimals: return CreateDecimalArray();
                        case ArrayTypes.DateTimes: return CreateDateTimeArray();
                        case ArrayTypes.Strings: return CreateStringArray();
                        default: throw new NotSupportedException("Unrecognized array type.");
                    }

                case PrimitiveTypes.Matrix:
                    switch (ArrayType)
                    {
                        case ArrayTypes.U8: return CreateByteMatrix();
                        case ArrayTypes.U16: return CreateUInt16Matrix();
                        //case ArrayTypes.U24: return CreateUInt24Matrix();
                        case ArrayTypes.U32: return CreateUInt32Matrix();
                        case ArrayTypes.U64: return CreateUInt64Matrix();
                        case ArrayTypes.I8: return CreateSByteMatrix();
                        case ArrayTypes.I16: return CreateInt16Matrix();
                        //case ArrayTypes.I24: return CreateInt24Matrix(); 
                        case ArrayTypes.I32: return CreateInt32Matrix();
                        case ArrayTypes.I64: return CreateInt64Matrix();
                        case ArrayTypes.Singles: return CreateSingleMatrix();
                        case ArrayTypes.Doubles: return CreateDoubleMatrix();
                        default: throw new NotSupportedException("Unrecognized matrix type.");
                    }

                case PrimitiveTypes.CompressedDML: return CreateCompressed();

                default: throw CreateDmlException("Not supported.", new NotSupportedException());
            }
        }

        #endregion

        #region "Operations"

#       if false    // TODO: Revisit compression

        /// <summary>
        /// DecompressAll() can be called to "unwrap" any compressed structure in the document
        /// tree.  Any DtlCompressed elementals will be decompressed and replaced by their 
        /// fragment in the tree.
        /// </summary>
        /// <param name="ValidationRequired">If true, an exception is thrown if a CRC is not provided for any compressed
        /// data.  If a CRC is provided, the decompressed data is validated whether this parameter is true or false and
        /// an exception is thrown if the validation fails.</param>
        public void DecompressAll(bool ValidationRequired)
        {
            DecompressAll(this, ValidationRequired);
        }

        private void DecompressAll(DmlFragment Fragment, bool ValidationRequired)
        {
            for (int ii = 0; ii < Fragment.Children.Count; )
            {
                DmlNode Child = Fragment.Children[ii];
                if (Child is DmlFragment) { DecompressAll((DmlFragment)Child, ValidationRequired); ii++; }
                else if (Child is DtlCompressed)
                {
                    DtlCompressed CompChild = (DtlCompressed)Child;
                    DmlFragment ChildFragment = CompChild.DecompressedFragment;
                    if (ValidationRequired && !CompChild.Validated)
                        throw CreateDmlException("Unable to validate compressed data content.  Validation information was not provided.");
                    DmlFragment NewContainer = Fragment.Container;
                    Fragment.Children.RemoveAt(ii);
                    for (int jj = 0; jj < ChildFragment.Children.Count; jj++)
                    {
                        ChildFragment.Children[jj].Container = NewContainer;
                        Fragment.Children.Insert(ii, ChildFragment.Children[jj]);
                    }
                }
            }
        }

        /// <summary>
        /// DecryptAll() can be called to "unwrap" any encrypted structure in the document and
        /// replace them with their decrypted DML content.  DecryptAll() can only be used in a 
        /// document where all encryption uses a single key.
        /// </summary>
        /// <param name="Key">The encryption key to utilize for decrypting.</param>
        /// <param name="ValidationRequired">If true, an exception is thrown if an HMAC is not provided for all encrypted
        /// data.  If a HMAC is provided, the decrypted data is validated whether this parameter is true or false and
        /// an exception is thrown if the validation fails.</param>
        public void DecryptAll(byte[] Key, bool ValidationRequired)
        {
            DecryptAll(this, Key, ValidationRequired);
        }

        private void DecryptAll(DmlFragment Fragment, byte[] Key, bool ValidationRequired)
        {
            for (int ii = 0; ii < Fragment.Children.Count; )
            {
                DmlNode Child = Fragment.Children[ii];
                if (Child is DmlFragment) { DecryptAll((DmlFragment)Child, Key, ValidationRequired); ii++; }
                else if (Child is DtlEncrypted)
                {
                    DtlEncrypted EncChild = (DtlEncrypted)Child;
                    EncChild.Key = Key;
                    DmlFragment ChildFragment = EncChild.DecryptedFragment;
                    if (ValidationRequired && !EncChild.Validated)
                        throw CreateDmlException("Unable to validate encrypted data content.  Validation information was not provided.");
                    DmlFragment NewContainer = Fragment.Container;
                    Fragment.Children.RemoveAt(ii);
                    for (int jj = 0; jj < ChildFragment.Children.Count; jj++)
                    {
                        ChildFragment.Children[jj].Container = NewContainer;
                        Fragment.Children.Insert(ii, ChildFragment.Children[jj]);
                    }
                }
            }
        }

#       endif

        #endregion
    }
}
