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
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using WileyBlack.Platforms;
using WileyBlack.Utility;
using WileyBlack.Dml.Dom;
using WileyBlack.Dml.EC;

namespace WileyBlack.Dml.Dom
{
    #region "DOM/In-Memory Compression/Decompression"

    public class DmlCompressed : DmlPrimitive
    {
        /// <summary>
        /// Validated is true when the decompressed fragment was validated against a CRC-32 provided
        /// during the loading process.  If no CRC-32 code was provided, or this node was generated
        /// in-memory instead of from a stream, then Validated is false.  If a validation code
        /// was provided but failed the validation process, an exception would have been thrown
        /// during LoadContent().  DmlCompressed nodes written out to a stream using this class
        /// always include a CRC-32 in the output stream.
        /// </summary>
        public bool Validated = false;
        
        /// <summary>
        /// During the writing process, both GetEncodedSize() and WriteTo() are called sequentially.  We
        /// could simply return an unknown size, but that would reduce one of the key uses of the DOM
        /// representation - writing containers with known sizes.  We want to avoid performing the
        /// compression operation twice, so we cache the compression in GetEncodedSize() and reuse it in
        /// WriteTo().  The cache'd version is m_CompressedFragment.  Retrieving or setting 
        /// DecompressedFragment invalidates the cache.  The CRC-32 is also cached.
        /// </summary>
        private MemoryStream m_CompressedFragment;
        private byte[] m_Crc32;

        private DmlFragment m_DecompressedFragment;
        public DmlFragment DecompressedFragment
        {
            get
            {
                if (m_CompressedFragment != null) { m_CompressedFragment.Dispose(); m_CompressedFragment = null; }
                return m_DecompressedFragment;
            }
            set
            {
                if (m_CompressedFragment != null) { m_CompressedFragment.Dispose(); m_CompressedFragment = null; }
                m_DecompressedFragment = value;
            }
        }

        public DmlCompressed()
        {
            Association = DmlTranslation.EC2.Compressed;
            m_DecompressedFragment = new DmlFragment();
            m_DecompressedFragment.Association = EC2Translation.CompressedFragmentAssociation;
        }

        public DmlCompressed(DmlDocument Document)
            : base(Document)
        {
            Association = DmlTranslation.EC2.Compressed;
            m_DecompressedFragment = new DmlFragment(Document);
            m_DecompressedFragment.Association = EC2Translation.CompressedFragmentAssociation;
        }

        public DmlCompressed(DmlContainer Container)
            : base(Container.Document)
        {
            Association = DmlTranslation.EC2.Compressed;
            m_DecompressedFragment = new DmlFragment(Document);
            m_DecompressedFragment.Association = EC2Translation.CompressedFragmentAssociation;
        }

        protected MemoryStream GetCompressedFragment(DmlWriter Writer, out byte[] Crc32)
        {
            if (m_CompressedFragment != null) { Crc32 = m_Crc32; return m_CompressedFragment; }

            using (MemoryStream CompressedFragment1 = new MemoryStream())
            using (Stream BackWriterStream = new DeflateStream(CompressedFragment1, CompressionMode.Compress))
            using (StreamWithHash WriterStream = new StreamWithHash(BackWriterStream, CRC32.CreateCastagnoli()))
            using (DmlWriter FrontWriter = DmlWriter.Create(WriterStream, Writer))
            {                
                m_DecompressedFragment.WriteTo(FrontWriter);
                WriterStream.Flush();
                FrontWriter.Close();
                m_Crc32 = WriterStream.Hash; Crc32 = m_Crc32;
                WriterStream.Close();

                // In order to flush the DeflateStream, we have to close it.  Sadly, this also closes the
                // underlying stream.  This forces us to use a 2nd MemoryStream.  Flaw in the .NET classes,
                // or perhaps there's a better way that I'm not aware of.
                BackWriterStream.Close();
                m_CompressedFragment = new MemoryStream(CompressedFragment1.ToArray());
            }
            return m_CompressedFragment;
        }

        public override void LoadContent(DmlReader Reader)
        {
            if (m_CompressedFragment != null) { m_CompressedFragment.Dispose(); m_CompressedFragment = null; }

            m_DecompressedFragment = Document.CreateFragment();
            m_DecompressedFragment.Association = EC2Translation.CompressedFragmentAssociation;
            m_DecompressedFragment.Loaded = DmlFragment.LoadState.None;
            DmlReader DecompressedReader = Reader.GetCompressedDml();
            m_DecompressedFragment.LoadContent(DecompressedReader);
            Validated = Reader.ValidateCompressedDml();
        }

        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            byte[] Crc32;
            MemoryStream Compressed = GetCompressedFragment(Writer, out Crc32);
            return DmlWriter.PredictNodeHeadSize(EC2Translation.idCompressed)
                + (ulong)Compressed.Length
                + (ulong)Crc32.Length;
        }        

        public override void WriteTo(DmlWriter Writer)
        {
            byte[] Crc32;            
            MemoryStream Compressed = GetCompressedFragment(Writer, out Crc32);            
            Writer.WriteCompressedDml(Compressed, Crc32);
        }

        public override PrimitiveTypes PrimitiveType { get { return PrimitiveTypes.CompressedDML; } }

        public override object Value
        {
            get { return DecompressedFragment; }
            set { DecompressedFragment = Value as DmlFragment; }
        }

        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlCompressed cp = new DmlCompressed(NewDoc);
            cp.Association = Association;
            cp.IsAttribute = IsAttribute;
            cp.m_DecompressedFragment = (DmlFragment)m_DecompressedFragment.Clone(NewDoc);
            cp.Validated = Validated;
            return cp;
        }
    }

    #endregion

#   if false        // All works, except that there needs to be a way to provide a Key during the loading process...
    #region "DTL In-Memory Encryption/Decryption"

    public class DmlEncrypted : DmlNode
    {
        public bool Validated = false;

        public byte[] Key;

        private byte[] m_IV;
        private byte[] m_HMAC;
        private MemoryStream m_EncryptedFragment;

        private DmlFragment m_DecryptedFragment;
        public DmlFragment DecryptedFragment
        {
            get
            {
                if (m_EncryptedFragment != null) { m_EncryptedFragment.Dispose(); m_EncryptedFragment = null; }
                return m_DecryptedFragment;
            }
            set
            {
                if (m_EncryptedFragment != null) { m_EncryptedFragment.Dispose(); m_EncryptedFragment = null; }
                m_DecryptedFragment = value;
            }
        }

        public DmlEncrypted()
        {
            Association = DmlECInternalData.EncryptedAssociation;
            m_DecryptedFragment = new DmlFragment(Container.Document);
            m_DecryptedFragment.Association = DmlECInternalData.EncryptedFragmentAssociation;
        }

        public DmlEncrypted(DmlDocument Document)
            : base(Document)
        {
            Association = DmlECInternalData.EncryptedAssociation;
            m_DecryptedFragment = new DmlFragment(Container.Document);
            m_DecryptedFragment.Association = DmlECInternalData.EncryptedFragmentAssociation;
        }

        public DmlEncrypted(DmlFragment Container)
            : base(Container)
        {
            Association = DmlECInternalData.EncryptedAssociation;
            m_DecryptedFragment = new DmlFragment(Container.Document);
            m_DecryptedFragment.Association = DmlECInternalData.EncryptedFragmentAssociation;
        }

        protected MemoryStream GetEncryptedFragment(DmlWriter Writer, out byte[] IV, out byte[] HMAC)
        {
            if (m_EncryptedFragment != null) { IV = m_IV; HMAC = m_HMAC; return m_EncryptedFragment; }

            m_EncryptedFragment = new MemoryStream();

            RijndaelManaged AES = new RijndaelManaged();
            AES.Key = Key;
            AES.GenerateIV();
            AES.Padding = PaddingMode.ISO10126;
            AES.Mode = CipherMode.CBC;
            ICryptoTransform Encryptor = AES.CreateEncryptor();

            m_IV = AES.IV;

            Stream WriterStream1 = new DmlWriter.EncMessageStream(m_EncryptedFragment);
            Stream WriterStream2 = new CryptoStream(WriterStream1, Encryptor, CryptoStreamMode.Write);
            StreamWithHash WriterStream3 = new StreamWithHash(WriterStream2, new HMACSHA384(Key));

            DmlWriter FrontWriter = DmlWriter.Create(WriterStream3, Writer);
            m_DecryptedFragment.WriteTo(FrontWriter);
            WriterStream3.Flush();
            // Don't forget to write out the terminating 0 block count.  It isn't included in the m_EncryptedFragment.
            FrontWriter.Close();
            m_HMAC = WriterStream3.Hash;
            WriterStream3.Close();
            WriterStream2.Close();
            WriterStream1.Close();

            IV = m_IV; HMAC = m_HMAC;
            return m_EncryptedFragment;
        }

        public override void LoadContent(DmlReader Reader)
        {
            DecryptedFragment = Document.CreateFragment();
            DmlReader DecryptedReader = Reader.GetEncryptedDml(Key);
            DecryptedFragment.LoadContent(DecryptedReader);
            Validated = DecryptedReader.ValidateEncryptedDml();
        }

        internal override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            byte[] IV;
            byte[] HMAC;
            MemoryStream Encrypted = GetEncryptedFragment(Writer, out IV, out HMAC);
            return DmlWriter.PredictNodeHeadSize(DmlECInternalData.idEncrypted)
                + (ulong)IV.Length
                + (ulong)Encrypted.Length
                + (ulong)EndianBinaryWriter.SizeCompact64(0)           // Termination marker
                + (ulong)HMAC.Length;
        }

        public override void WriteTo(DmlWriter Writer)
        {
            byte[] IV;
            byte[] HMAC;
            MemoryStream Encrypted = GetEncryptedFragment(Writer, out IV, out HMAC);
            Writer.WriteEncryptedDml(Encrypted, IV, HMAC);
        }

        public override DmlPrimitive Clone(DmlDocument NewDoc)
        {
            DmlEncrypted cp = new DmlEncrypted(NewDoc);
            cp.Association = Association;
            cp.IsAttribute = IsAttribute;
            cp.m_DecryptedFragment = (DmlFragment)m_DecryptedFragment.Clone(NewDoc);
            cp.Validated = Validated;
            cp.Key = Key.Clone();
            cp.m_IV = m_IV.Clone() ??
            return cp;
        }
    }

    #endregion
#   endif
}

