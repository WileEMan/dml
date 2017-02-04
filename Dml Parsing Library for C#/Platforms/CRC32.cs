#if true

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
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace WileyBlack.Utility
{
    /// <summary>
    /// Computes the CRC-32 of a given buffer.  An example of usage:
    /// 
    /// <code>
    /// CRC32 CrcAlg = CRC32.CreateCastagnoli();
    /// using (FileStream fs = File.Open("Example.dat", FileMode.Open))
    /// {
    ///     byte[] buffer = new byte [65536];    
    ///     CrcAlg.Initialize();
    ///     int nRead = fs.Read(buffer, 0, buffer.Length);
    ///     if (nRead == 0) break;
    ///     CrcAlg.HashCore(buffer, 0, nRead);
    /// }
    /// Console.WriteLine("CRC-32C= {0:X}", CrcAlg.Result);
    /// </code>
    /// </summary>
    public class CRC32 : HashAlgorithm
    {
        UInt32[] Table;

        private CRC32() { }

        public CRC32(UInt32 Polynomial)
        {
            Table = new UInt32[256];
            for (uint ii = 0; ii < Table.Length; ii++)
            {
                UInt32 Entry = ii;
                for (int jj = 0; jj < 8; jj++)
                {
                    if ((Entry & 1) == 1) Entry = (Entry >> 1) ^ Polynomial;
                    else Entry = Entry >> 1;
                }
                Table[ii] = Entry;
            }
        }

        private CRC32 CloneTable()
        {
            CRC32 cp = new CRC32();
            cp.Table = this.Table;
            return cp;
        }

        private static CRC32 g_IEEE = new CRC32(0x04C11DB7);
        public static CRC32 CreateIEEE() { return g_IEEE.CloneTable(); }

        private static CRC32 g_Castagnoli = new CRC32(0x1EDC6F41);
        public static CRC32 CreateCastagnoli() { return g_Castagnoli.CloneTable(); }
        
        private UInt32 m_Result = 0xFFFFFFFF;

        /// <summary>
        /// Result gives the resulting CRC-32 value computed from the Continue() function.  Call
        /// the Start() function to reset the CRC-32 value for a new computation.
        /// </summary>
        public UInt32 Result
        {
            get { return m_Result; }
        }

        /// <summary>
        /// Initialize() resets the Result value to begin a new data sequence.  To compute a CRC-32,
        /// first call Initialize(), then call HashCore() repeatedly on the data sequence.  When
        /// all of the data sequence has been fed to HashCore(), retrieve Result for the CRC value.
        /// </summary>
        public override void Initialize() { m_Result = 0xFFFFFFFF; }

        /// <summary>
        /// HashCore() computes a CRC-32, continuing from any previous calculation since the last
        /// Initialize() call.  The result is returned and available from the Result property or HashFinal()
        /// method.
        /// </summary>
        /// <param name="block">Block of data to compute the CRC over.</param>
        /// <param name="offset">Offset into Block to begin computation.</param>
        /// <param name="count">Number of bytes from Block to compute over.</param>
        /// <returns>The CRC-32 computed since the last Start() call.</returns>
        protected override void HashCore(byte[] block, int offset, int count)
        {
            int ii = 0; int endat = offset + count;
            while (ii < endat)
            {	        
		        m_Result = (m_Result >> 8) ^ Table[(m_Result ^ block[ii++]) & 0xff];
	        }
        }

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(m_Result);
        }

        public static UInt32 Calculate(Stream WholeStream)
        {
            CRC32 CrcAlg = new CRC32();
            CrcAlg.Initialize();

            byte[] WorkingBuffer = new byte[4090];
            for (; ; )
            {
                int nRead = WholeStream.Read(WorkingBuffer, 0, WorkingBuffer.Length);
                if (nRead == 0) return CrcAlg.Result;
                CrcAlg.HashCore(WorkingBuffer, 0, nRead);
            }
        }        
    }
}

#endif