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
using WileyBlack.Utility;

namespace WileyBlack.Dml.EC
{    
    #region "Dml-EC Internals"

    public class EC2Translation : DmlTranslation
    {
        public const string urn = "urn:dml:dml-ec2";
        public PrimitiveSet[] RequiredPrimitiveSets = new PrimitiveSet[] {
		    new PrimitiveSet("dml-ec2", "v2", null)
		};

        internal const uint idCompressed = 0x77;
        internal const uint idVerifiedCompressed = 0x78;
        internal const uint idEncrypted = 0x79;
        internal const uint idAuthenticEncrypted = 0x7A;

        public Association Encrypted = new Association(idEncrypted, "Encrypted", PrimitiveTypes.EncryptedDML);
        public Association AuthenticEncrypted = new Association(idAuthenticEncrypted, "Authentic-Encrypted", PrimitiveTypes.EncryptedDML);
        public Association Compressed = new Association(idCompressed, "Compressed", PrimitiveTypes.CompressedDML);
        public Association VerifiedCompressed = new Association(idVerifiedCompressed, "Verified-Compressed", PrimitiveTypes.CompressedDML);

        internal static Association EncryptedFragmentAssociation = new Association(idEncrypted, "Encrypted", NodeTypes.Structural);
        internal static Association CompressedFragmentAssociation = new Association(idCompressed, "Compressed", NodeTypes.Structural);

        public EC2Translation()
        {
            Add(new Association[] {
                Encrypted,
                AuthenticEncrypted,
                Compressed,
                VerifiedCompressed
            });
        }

#if false
        internal static Association[] BuiltinAssociations
            = {
                
            };
#endif
    }

    #endregion
}
