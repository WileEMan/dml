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

/** TODO List:
 *      - Add a serialization-related attribute called DmlCompress (definitely) and DmlEncrypt (maybe?) that mark 
 *          classes or properties that should be enclosed in encryption.
 *      - The EndianBinaryReader and EndianBinaryWriter really need an unsafe version for arrays and matrices, or possibly
 *          a C++ implementation for that part.  The DML Parsing Library will have to be offered in three editions, managed,
 *          native x86, and native x64.
 *      - DmlReader should provide a public function called Reader.GetContextAsString() which returns an exception or string
 *          similar to CreateDmlException() so that callers can locate themselves.
 *          + The DmlSerialization should include the Reader.GetContextAsString() call whenever an exception is thrown for
 *            deserialization.
 *      - Check if List<type> is supported by serialization.  It probably is not as-is, but it should be relatively easy to
 *          add support for it by making a special case.  Reflection for generics is certainly supported in C#. 
 *      - Additional documentation
 *      - Work out some way in which the DmlDocument type can analyze/detect which primitives it requires in forming a namespace
 *          for purposes of autogenerating a namespace and/or automated robustness.
 *      - Add XML load/save to DmlNamespaceDocument class.
 *      - Search the code for NotImplementedException() and implement more of them.
 *      - PrimitiveTypes and ArrayTypes should probably be merged into a little value structure that just contains those two.  This
 *          was done (required) in the Java implementation and worked out alright, although having PrimitiveTypes, ArrayTypes,
 *          AND a third enclosure class floating around just added to the confusion.
 *      - Search through and use a consistent "DMLID" instead of "DML ID", and reduce use of XMLName to Name except for mention 
 *          of XML Compatibility when it is first described.  Decide on DmlID vs DMLID.
 *      - Do a search on TODO, revisit, and DTL (now called EC) for any unfinished items.  Maybe do a search on #if false
 *      - The DmlEncrypted node is written and ready to roll except for one issue: There needs to be a way to provide a key
 *          during the loading process.  I suppose the only way to accomplish it is to load the encrypted data in memory and
 *          apply decryption later in a call.
  */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using WileyBlack.Dml.EC;
using System.Net;

namespace WileyBlack.Dml
{
    /// <summary>
    /// ResolvedTranslation provides the processed form of a DML Translation
    /// document or DML Header.  It can be provided as a response from 
    /// IResourceResolution to provide cache'd (previously processed) results.
    /// When using DOM, the DomResolvedTranslation class is preferable as it 
    /// also retains the XmlRoot value.
    /// </summary>
    public class ResolvedTranslation : IDisposable
    {
        public DmlTranslation Translation;
        public List<PrimitiveSet> PrimitiveSets = new List<PrimitiveSet>();

        public void Dispose()
        {            
            Translation = null;
            PrimitiveSets = null;
        }

        public ResolvedTranslation() { }
        public ResolvedTranslation(DmlTranslation Translation, IEnumerable<PrimitiveSet> PrimitiveSets)
        {
            this.Translation = Translation.Clone();
            if (PrimitiveSets != null)
            {
                foreach (PrimitiveSet ps in PrimitiveSets) this.PrimitiveSets.Add(ps.Clone());
            }
        }
    }    

    /// <summary>
    /// The IResourceResolution interface provides a name resolution service that DOM or
    /// DmlReader can call on when additional translation resources are required.
    /// A null IResourceResolution can be provided to disable additional resource retrieval.
    /// </summary>
    public interface IResourceResolution
    {
        /// <summary>
        /// The Resolve interface method retrieves or opens the URI specified and returns a
        /// resource.  The resource can be a Stream that can be used to parse the resource, 
        /// or a ResolvedTranslation that contains the already processed resource.  If the 
        /// returned object is a Stream and contains an XML document, IsXml should be true.  
        /// If it specifies a DML document, IsXml should be false.  When a ResolvedTranslation
        /// is returned, the IsXml parameter is ignored.  A DomResolvedTranslation object
        /// (which is derived from ResolvedTranslation) can also be returned.
        /// </summary>
        /// <param name="Uri">The URI containing the requested resource.</param>
        /// <param name="IsXml">True if the URI points to an XML document.  False if the URI points to a DML document.</param>
        /// <returns>A Stream that can be used to access the resource or a ResolvedTranslation object.</returns>
        IDisposable Resolve(string Uri, out bool IsXml);
    }

    /// <summary>
    /// WebResourceResolution provides a simple full-access resource retrieval via URI which 
    /// can be provided to enable web and local URI retrieval for DML translation documents.  
    /// </summary>
    public class WebResourceResolution : IResourceResolution
    {
        public IDisposable Resolve(string Uri, out bool IsXml)
        {
            WebClient wc = new WebClient();

            if (Uri.ToLower().Contains(".xml")) IsXml = true;
            else if (Uri.ToLower().Contains(".dml")) IsXml = false;
            else
            {                
                using (Stream sPreview = wc.OpenRead(Uri))                
                {
                    byte[] ID = new byte[4];
                    sPreview.Read(ID, 0, 4);                    
                    // DML:Header ID is 0x444D4C2, which encodes as 0x1444D4C2.
                    IsXml = (ID[0] != 0x14 || ID[1] != 0x44 || ID[2] != 0xD4 || ID[3] != 0xC2);
                }
            }

            return wc.OpenRead(Uri);
        }
    }

    public static class Dml
    {        
        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return System.IO.Path.GetDirectoryName(path);
            }
        }
    }

    public class DmlException : Exception
    {
        public DmlException()
        {
        }

        public DmlException(string Message) : base(Message)
        {
        }

        public DmlException(string Message, Exception innerException)
            : base(Message, innerException)
        {
        }
    }

    public class DmlLicenseException : Exception
    {
        public DmlLicenseException()
        {
        }

        public DmlLicenseException(string Message)
            : base(Message)
        {
        }
    }

    public class DmlName : IEquatable<DmlName>, ICloneable
    {
        public string XmlName;

        public NodeTypes NodeType;
        
        /// <summary>
        /// PrimitiveType is only relevant for NodeTypes.Primitive.  Otherwise its value should be ignored.
        /// </summary>
        public PrimitiveTypes PrimitiveType;

        /// <summary>
        /// ArrayType is only relevant when PrimitiveType is Array or Matrix.
        /// </summary>
        public ArrayTypes ArrayType;

        /// <summary>
        /// Extension is only relevant when PrimitiveType is Extension.
        /// </summary>
        public IDmlReaderExtension Extension;

        /// <summary>
        /// TypeId is provided by the Extension for its own recognition purposes.
        /// </summary>
        public uint TypeId = 0;

        public DmlName(string XmlName, NodeTypes NodeType)
        {
            this.XmlName = XmlName;
            this.NodeType = NodeType;
            this.PrimitiveType = PrimitiveTypes.Unknown;
            if (NodeType == NodeTypes.Primitive)
                throw new Exception("Must specify a primitive type for primitive nodes.");
        }

        public DmlName(string XmlName, PrimitiveTypes PrimitiveType)
        {
            this.XmlName = XmlName;
            this.NodeType = NodeTypes.Primitive;
            this.PrimitiveType = PrimitiveType;
            if (NodeType != NodeTypes.Primitive && PrimitiveType != PrimitiveTypes.Unknown)
                throw new Exception("Only an unknown primitive type is permitted for non-primitive nodes.");
        }        

        public DmlName(string XmlName, PrimitiveTypes PrimitiveType, ArrayTypes ArrayType)
        {
            this.XmlName = XmlName;
            this.NodeType = NodeTypes.Primitive;
            this.PrimitiveType = PrimitiveType;
            this.ArrayType = ArrayType;
            if (NodeType != NodeTypes.Primitive && PrimitiveType != PrimitiveTypes.Unknown)
                throw new Exception("Only an unknown primitive type is permitted for non-primitive nodes.");
        }

        public DmlName(string XmlName, IDmlReaderExtension Extension, uint TypeId)
        {
            this.XmlName = XmlName;
            this.NodeType = NodeTypes.Primitive;
            this.PrimitiveType = PrimitiveTypes.Extension;
            this.ArrayType = ArrayTypes.Unknown;
            this.Extension = Extension;
            this.TypeId = TypeId;
        }

        public DmlName(DmlName cp)
        {
            this.XmlName = cp.XmlName;
            this.NodeType = cp.NodeType;
            this.PrimitiveType = cp.PrimitiveType;
            this.ArrayType = cp.ArrayType;
            this.Extension = cp.Extension;
            this.TypeId = cp.TypeId;
            if (NodeType != NodeTypes.Primitive && PrimitiveType != PrimitiveTypes.Unknown)
                throw new Exception("Only an unknown primitive type is permitted for non-primitive nodes.");
        }

        public override bool Equals(object obj) { return Equals(obj as DmlName); }
        public override int GetHashCode() { return XmlName.GetHashCode() + ((int)NodeType << 16) + ((int)PrimitiveType << 24); }

        public bool Equals(DmlName b)
        {
            if (Object.ReferenceEquals(b, null)) return false;
            if (Object.ReferenceEquals(this, b)) return true;
            if (this.GetType() != b.GetType()) return false;
            if (NodeType != b.NodeType) return false;
            if (NodeType == NodeTypes.Primitive)
            {
                if (PrimitiveType == PrimitiveTypes.Extension)
                    return (Extension == b.Extension && TypeId == b.TypeId && XmlName == b.XmlName);
                else if (PrimitiveType == PrimitiveTypes.Array || PrimitiveType == PrimitiveTypes.Matrix)
                    return (ArrayType == b.ArrayType && PrimitiveType == b.PrimitiveType && XmlName == b.XmlName);
                else
                    return (PrimitiveType == b.PrimitiveType && XmlName == b.XmlName);
            }
            else
                return XmlName == b.XmlName;
        }

        public static bool operator ==(DmlName lhs, DmlName rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null)) return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator !=(DmlName lhs, DmlName rhs)
        {
            return !(lhs == rhs);
        }

        object ICloneable.Clone() { return Clone(); }
        public DmlName Clone()
        {
            return new DmlName(this);
        }

        public override string ToString()
        {
            string NType = NodeType.ToString();
            if (NodeType == NodeTypes.Primitive)
            {
                NType = PrimitiveType.ToString();
                if (PrimitiveType == PrimitiveTypes.Array)
                    NType = "Array of " + ArrayType.ToString();
                else if (PrimitiveType == PrimitiveTypes.Matrix)
                    NType = "Matrix of " + ArrayType.ToString();
            }

            return "DMLName (" + XmlName + " -> " + NType + ")";
        }
    }

    public class Association : IEquatable<Association>, ICloneable
    {
        public uint DMLID;
        public DmlName DMLName;

        /// <summary>
        /// LocalTranslation provides a DmlTranslation if a local subset is defined by use of this node.  
        /// LocalTranslation is null if no local translation subset is defined, in which case the parent's translation
        /// is in effect.  Local translations can only be provided by DML Containers.
        /// </summary>
        public DmlTranslation LocalTranslation;

        /// <summary>
        /// InlineIdentification, when true, indicates that the association should be made using inline 
        /// identification at the node.  If false, the association is part of the translation 
        /// document instead.
        /// </summary>
        public bool InlineIdentification
        {
            get { return DMLID == DML3Translation.idInlineIdentification; }
        }

        #region "Constructors for associations in translation documents"

        public Association(uint DMLID, string XmlName, NodeTypes NodeType)
        {
            this.DMLID = DMLID;
            this.DMLName = new DmlName(XmlName, NodeType);
        }

        public Association(uint DMLID, string XmlName, PrimitiveTypes PrimitiveType)
        {
            this.DMLID = DMLID;
            this.DMLName = new DmlName(XmlName, PrimitiveType);
        }        

        public Association(uint DMLID, string XmlName, PrimitiveTypes PrimitiveType, ArrayTypes ArrayType)
        {
            this.DMLID = DMLID;
            this.DMLName = new DmlName(XmlName, PrimitiveType, ArrayType);
        }                

        public Association(uint DMLID, string XmlName, DmlTranslation LocalTranslation)
        {
            this.DMLID = DMLID;
            this.DMLName = new DmlName(XmlName, NodeTypes.Container);
            this.LocalTranslation = LocalTranslation;
        }        

        public Association(uint DMLID, DmlName Name, DmlTranslation LocalTranslation)
        {
            this.DMLID = DMLID;
            this.DMLName = Name.Clone();
            if (LocalTranslation != null && Name.NodeType != NodeTypes.Container)
                throw new FormatException("Cannot attach a local translation to a non-container association.");
            this.LocalTranslation = LocalTranslation;
        }

        public Association(uint DMLID, DmlName Name)
        {
            this.DMLID = DMLID;
            this.DMLName = Name.Clone();
        }

        #endregion

        #region "Constructors for inline identification"

        public Association(string XmlName, NodeTypes NodeType)
        {
            this.DMLID = DML3Translation.idInlineIdentification;
            this.DMLName = new DmlName(XmlName, NodeType);
        }

        public Association(string XmlName, PrimitiveTypes PrimitiveType)
        {
            this.DMLID = DML3Translation.idInlineIdentification;
            this.DMLName = new DmlName(XmlName, PrimitiveType);
        }

        public Association(string XmlName, PrimitiveTypes PrimitiveType, ArrayTypes ArrayType)
        {
            this.DMLID = DML3Translation.idInlineIdentification;
            this.DMLName = new DmlName(XmlName, PrimitiveType, ArrayType);
        }        

        public Association(DmlName Name)
        {
            this.DMLID = DML3Translation.idInlineIdentification;
            this.DMLName = Name.Clone();
        }

        #endregion

#if false
        public override bool Equals(object obj) { return Equals(obj as DmlName); }
        public override int GetHashCode() { 
            return (int)DMLID + DMLName.XmlName.GetHashCode() + ((int)DMLName.NodeType << 16) + ((int)DMLName.PrimitiveType << 24)
                + (int)DMLName.ArrayType; 
        }

        public bool Equals(Association b)
        {
            if (Object.ReferenceEquals(b, null)) return false;
            if (Object.ReferenceEquals(this, b)) return true;
            if (this.GetType() != b.GetType()) return false;
            return (DMLID == b.DMLID && DMLName == b.DMLName && LocalTranslation == b.LocalTranslation);
#error This LocalTranslation == b.LocalTranslation is problematic.  It could be a lengthy comparison.  But, Equals() could also be in use
#error by DmlTranslation's Dictionary to identify the correct Association, and needs to be fast.  There are different use cases...
#error The answer is going to be that we need to check a complete DMLName instead of a local referenced DmlName.  So instead of checking that
#error we're looking at MyDmlName, we check ::A::B::MyDmlName.  Or maybe that's equality for DmlNames only, still relevant I think.
#error Also applies to DmlHeaderAnalysis, where we will almost certainly be comparing Associations that are not reference identical, but
#error should logically match except if the header falls out of sync.
        }

        public static bool operator ==(Association lhs, Association rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null)) return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Association lhs, Association rhs)
        {
            return !(lhs == rhs);
        }
#endif
        // Some exceptions to make sure we're not using these...
        public override bool Equals(object obj) { throw new NotSupportedException(); }
        public override int GetHashCode() { throw new NotSupportedException(); }
        public bool Equals(Association b) { throw new NotSupportedException(); }        

        object ICloneable.Clone() { return Clone(); }
        public virtual Association Clone()
        {
            if (LocalTranslation != null)
                return new Association(DMLID, DMLName.Clone(), LocalTranslation.Clone(false));
            else
                return new Association(DMLID, DMLName.Clone(), null);
        }

        public override string ToString()
        {
            string NType = DMLName.NodeType.ToString();
            if (DMLName.NodeType == NodeTypes.Primitive)
            {
                NType = DMLName.PrimitiveType.ToString();
                if (DMLName.PrimitiveType == PrimitiveTypes.Array)
                    NType = "Array of " + DMLName.ArrayType.ToString();
                else if (DMLName.PrimitiveType == PrimitiveTypes.Matrix)
                    NType = "Matrix of " + DMLName.ArrayType.ToString();
            }

            if (InlineIdentification)
            {
                return "Association (" + DMLName.XmlName + " -> " + NType + ")";
            }
            else
            {
                return "Association (" + DMLID + ": " + DMLName.XmlName + " -> " + NType + ")";
            }
        }
    }

    /// <summary>
    /// A DmlTranslation consists of a set of associations.  Depending on the caller's action,
    /// there may be multiple ways to reference a particular assocation.  For example, a DmlReader
    /// locates associations using the DMLID.  A DmlWriter may utilize the DMLID, but could also
    /// be using the name and type to find an association.  The DmlTranslation class provides
    /// access to the set of associations from either the DMLID or the DMLName.  It also provides
    /// the hierarchical scan algorithm that DML defines, permitting a search at the current
    /// level of the tree and then advancing up if no match is found.  Access to the associations
    /// is provided as an O(1) operation by DML ID or Name.
    /// </summary>
    public class DmlTranslation : IEnumerable<Association>
    {
        Dictionary<uint, Association> ByID = new Dictionary<uint, Association>();
        Dictionary<DmlName, Association> ByName = new Dictionary<DmlName, Association>();
        public DmlTranslation ParentTranslation;        

        /// <summary>
        /// A custom IEnumerator implementation is required because the dictionary's
        /// enumerators include the key-value pairing, whereas we incorporate all that
        /// information into an Association.
        /// </summary>
        public class Enumerator : IEnumerator<Association>
        {
            IEnumerator<KeyValuePair<uint, Association>> IDEnumerator;

            internal Enumerator(IEnumerator<KeyValuePair<uint, Association>> ByIDEnumerator) { IDEnumerator = ByIDEnumerator; }

            public bool MoveNext() { return IDEnumerator.MoveNext(); }
            public void Reset() { IDEnumerator.Reset(); }
            public void Dispose() { IDEnumerator.Dispose(); IDEnumerator = null; }

            public Association Current
            {
                get { return IDEnumerator.Current.Value; }
            }

            Association IEnumerator<Association>.Current
            {
                get { return IDEnumerator.Current.Value; }
            }

            object IEnumerator.Current
            {
                get { return IDEnumerator.Current.Value; }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(ByID.GetEnumerator());
        }

        IEnumerator<Association> IEnumerable<Association>.GetEnumerator()
        {
            return new Enumerator(ByID.GetEnumerator());
        }

        DmlTranslation GetGlobalTranslation()
        {
            DmlTranslation Iter = this;
            while (Iter.ParentTranslation != null) Iter = Iter.ParentTranslation;
            return Iter;
        }

        /// <summary>
        /// Adds the specified association to the translation.  An exception is thrown if a DMLID or
        /// properties conflict occur on this level.
        /// </summary>
        /// <param name="Assoc">Information to associate</param>
        public void Add(Association Assoc)
        {
            if (Assoc == null) throw new ArgumentNullException("Assoc");

            if (Assoc.LocalTranslation != null && Assoc.DMLName.NodeType != NodeTypes.Container)
                throw new Exception("Local translations can only be associated with container elements.");

            if (ByID.ContainsKey(Assoc.DMLID))
            {
                if (ByID[Assoc.DMLID] != Assoc) throw new Exception("DML ID is already associated in this translation (with '" + ByID[Assoc.DMLID].DMLName.XmlName + "').");
                return;
            }

            if (ByName.ContainsKey(Assoc.DMLName))
            {
                if (ByName[Assoc.DMLName] != Assoc) throw new Exception("DML properties are already associated with a different DML ID in this translation.");
                return;
            }

            if (Assoc.LocalTranslation != null)
            {
                if (Assoc.LocalTranslation.ParentTranslation == null) Assoc.LocalTranslation.ParentTranslation = this;
                if (Assoc.LocalTranslation.ParentTranslation != this) throw new Exception("Cannot add an association with an already-attached local translation.  Proposed association: " + Assoc.DMLID + " -> " + Assoc.DMLName.XmlName + ".");
            }

            ByID.Add(Assoc.DMLID, Assoc);
            ByName.Add(Assoc.DMLName, Assoc);
        }

        protected uint NextAssignment = 0x1;

        /// <summary>
        /// Assign() adds a new association to the translation by locating the next unused DML ID value.
        /// </summary>
        /// <param name="Name">The XMLName and type to create an automatic association with</param>
        /// <param name="LocalNamespace">A local translation to associate with this entry (containers only)</param>
        /// <returns>The DML ID chosen for the association</returns>        
        public uint Assign(DmlName Name, DmlTranslation LocalTranslation)
        {
            if (LocalTranslation != null && Name.NodeType != NodeTypes.Container) 
                throw new Exception("Local translations can only be associated with container elements.");

            while (Contains(NextAssignment))
            {
                NextAssignment++;

                // Avoid the [4][xx] block since it is used by DML and CLE.  Although there are lots
                // of unused identifiers in the region, there is no reason to create ambiguity with
                // plenty of ID space available.
                if (NextAssignment >= 0x400 && NextAssignment <= 0x4FF) NextAssignment = 0x500;
            }
            Add(new Association(NextAssignment, Name, LocalTranslation));
            uint ret = NextAssignment;
            NextAssignment++;
            return ret;
        }

        /// <summary>
        /// Assign() adds a new association to the translation by locating the next unused DML ID value.
        /// </summary>
        /// <param name="Name">The XMLName and type to create an automatic association with</param>        
        /// <returns>The DML ID chosen for the association</returns>
        public uint Assign(DmlName Name)
        {
            return Assign(Name, null);
        }

        /// <summary>
        /// Renumber() can be used to change the DMLID of an association.  An exception is
        /// thrown if the NewDMLID value is already in use.
        /// </summary>
        /// <param name="DMLID">Dml ID value to change.</param>
        /// <param name="NewDMLID">New Dml ID value to assign for the association.</param>
        public void Renumber(uint DMLID, uint NewDMLID)
        {
            Association Assoc;
            if (!TryGet(DMLID, out Assoc))
                throw new Exception("Cannot renumber an identifier which is not listed.");
            if (ByID.ContainsKey(NewDMLID)) 
                throw new Exception("DML ID is already associated in this translation.");
            ByID.Remove(DMLID);
            Assoc.DMLID = NewDMLID;
            ByID.Add(NewDMLID, Assoc);
        }

        /// <summary>
        /// Determines whether the translation contains an association for the DML ID.
        /// </summary>
        /// <param name="DMLID">DML ID to check for an association</param>
        /// <returns>True if an association exists for this ID.  False otherwise.</returns>
        public bool Contains(uint DMLID) { return ByID.ContainsKey(DMLID); }

        /// <summary>
        /// Determines whether the translation contains an association for the XML Name and DML Type.
        /// </summary>
        /// <param name="Name">The DML Name (consisting of an XML Name and DML Type) to check for an association</param>
        /// <returns>True if an association exists for this name.  False otherwise.</returns>
        public bool Contains(DmlName Name) { return ByName.ContainsKey(Name); }

        /// <summary>
        /// Gets the association for the specified DML ID.
        /// </summary>
        /// <param name="DMLID">The ID of the name to retrieve.</param>
        /// <param name="Result">When this method returns, contains the association with the given DMLID if it
        /// exists.  Otherwise contains null.</param>
        /// <returns>True if the translation contains an association for the given DML ID.</returns>
        public bool TryGet(uint DMLID, out Association Result) { return ByID.TryGetValue(DMLID, out Result); }

        /// <summary>
        /// Gets the association for the specified XML Name and DML Type.
        /// </summary>
        /// <param name="Name">The name of the DML ID to retrieve.</param>
        /// <param name="Result">When this method returns, contains the association with the given name if it
        /// exists.  Otherwise contains null.</param>
        /// <returns>True if the translation contains an association for the given name.</returns>
        public bool TryGet(DmlName Name, out Association Result) { return ByName.TryGetValue(Name, out Result); }

        /// <summary>
        /// Gets the association for the specified DML ID, including a search up the translation tree.
        /// </summary>
        /// <param name="DMLID">The ID of the name to retrieve.</param>
        /// <param name="Result">When this method returns, contains the association with the given DMLID if it
        /// exists.  Otherwise contains null.</param>
        /// <returns>True if the translation or its parents contain an association for the given DML ID.</returns>
        public bool TryFind(uint DMLID, out Association Result)
        {
            if (TryGet(DMLID, out Result)) return true;
            if (ParentTranslation == null) return false;
            return ParentTranslation.TryFind(DMLID, out Result);
        }

        /// <summary>
        /// Gets the association for the specified XML Name and DML Type, including a search up the translation tree.
        /// </summary>
        /// <param name="Name">The name of the DML ID to retrieve.</param>
        /// <param name="Result">When this method returns, contains the association with the given name if it
        /// exists.  Otherwise contains null.</param>
        /// <returns>True if the translation or its parents contain an association for the given name.</returns>
        public bool TryFind(DmlName Name, out Association Result) 
        {
            if (TryGet(Name, out Result)) return true;
            if (ParentTranslation == null) return false;
            return ParentTranslation.TryFind(Name, out Result);
        }

        /// <summary>
        /// Retrieves or creates the local association for the specified DMLID.
        /// </summary>
        /// <param name="DMLID">DML ID to retrieve or associate the name with.</param>
        /// <returns>The local association for this DMLID.</returns>
        public Association this[uint DMLID]
        {
            get
            {
                Association ret;
                if (!TryGet(DMLID, out ret)) throw new Exception("Association not found for specified DML ID.");
                return ret;
            }
            set
            {
                Add(value);
            }
        }

        /// <summary>
        /// Retrieves or creates the local association for the specified DML Name.
        /// </summary>
        /// <param name="Name">Name to retrieve or associate the ID with.</param>
        /// <returns>The local association for this DMLName.</returns>
        public Association this[DmlName Name]
        {
            get
            {
                Association ret;
                if (!TryGet(Name, out ret)) throw new Exception("DML ID not found for specified Name.");
                return ret;
            }
            set
            {
                Add(value);
            }
        }

        /// <summary>
        /// Gets the number of associations contained in this translation.
        /// </summary>
        public int Count
        {
            get
            {
                return ByID.Count;
            }
        }

        /// <summary>
        /// Removes all associations from this translation.  Does not affect the translation's parent relationship.
        /// </summary>
        public void Clear()
        {
            ByID.Clear();
            ByName.Clear();
        }

        public DmlTranslation(DmlTranslation ParentTranslation)
        {
            this.ParentTranslation = ParentTranslation;
        }

        /// <summary>
        /// Use this constructor to create a local translation only.  The parent translation will
        /// be assigned by the Association() constructor when this translation is passed as the local
        /// translation.  To create a top-level translation instead, begin with a clone of the DML3 
        /// translation and use Add() calls.  
        /// </summary>
        public DmlTranslation() { }

        protected DmlTranslation(IEnumerable<Association> Associations)
        {
            try
            {
                if (Associations == null) throw new ArgumentNullException("Associations");

                foreach (Association assoc in Associations)
                {
                    if (assoc == null)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("Association list:");
                        foreach (Association assn in Associations)
                        {
                            if (assn == null)
                                sb.AppendLine("\tNULL Association\n");
                            else
                                sb.AppendLine("\t" + assn.ToString() + "\n");
                        }
                        sb.AppendLine("End of association list.");
                        throw new NullReferenceException(sb.ToString());
                    }

                    Add(assoc);
                }
            }
            catch (Exception ex) { throw new Exception("While initializing DML Translation from association list: " + ex.ToString()); }
        }               

        /// <summary>
        /// CloneUp() is a helper function for the Clone() routine that deep copies a DmlTranslation and all
        /// its parents, but excludes one specific local translation entry.  That entry is not cloned but
        /// replaced with the provided version instead.
        /// </summary>
        private DmlTranslation CloneUp(DmlTranslation Original, DmlTranslation Replacement)
        {
            DmlTranslation cp = new DmlTranslation();
            foreach (Association Assoc in this)            
            {                
                if (Assoc.LocalTranslation == null) cp.Add(Assoc.Clone());
                else if (Assoc.LocalTranslation == Original)
                {                    
                    Association Cloned = new Association(Assoc.DMLID, Assoc.DMLName, Replacement);
                    cp.Add(Cloned);
                }
                else
                {
                    DmlTranslation LocalTranslationClone = Assoc.LocalTranslation.Clone(false);
                    Association Cloned = new Association(Assoc.DMLID, Assoc.DMLName, LocalTranslationClone);
                    cp.Add(Cloned);
                }
            }
            if (ParentTranslation != null) cp.ParentTranslation = ParentTranslation.CloneUp(this, cp);
            return cp;
        }

        /// <summary>
        /// Clone() generates a deep copy of this DmlTranslation, including local translations
        /// that are attached.  If IncludeParents is true, then parent translations are also
        /// cloned via deep copy.  If IncludeParents is false, then the parent translation is
        /// set to null for the returned translation (that is, it is detached).
        /// </summary>
        /// <returns>An identical copy of the original DmlTranslation, including clones of
        /// any parent translations and clones of all associations.</returns>
        public DmlTranslation Clone(bool IncludeParents = true)
        {
            DmlTranslation cp = new DmlTranslation();

            // First, clone this and "down" the tree.
            foreach (Association Assoc in this)
            {
                if (Assoc.LocalTranslation == null) cp.Add(Assoc.Clone());
                else
                {
                    DmlTranslation LocalTranslationClone = Assoc.LocalTranslation.Clone(false);
                    Association Cloned = new Association(Assoc.DMLID, Assoc.DMLName, LocalTranslationClone);
                    cp.Add(Cloned);                 // Add() will set cp.ParentTranslation to cp.
                }
            }
            cp.ParentTranslation = null;            
            
            // Next, clone up the tree if requested.
            if (IncludeParents)
            {
                if (ParentTranslation != null) cp.ParentTranslation = ParentTranslation.CloneUp(this, cp);
            }

            return cp;
        }

        public static DmlTranslation CreateFrom(IEnumerable<Association> Associations)
        {
            try
            {
                if (Associations == null) throw new ArgumentNullException("Associations");

                DmlTranslation cp = new DmlTranslation();
                foreach (Association assoc in Associations)
                {
                    if (assoc == null)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("Association list:");
                        foreach (Association assn in Associations)
                        {
                            if (assn == null)
                                sb.AppendLine("\tNULL Association\n");
                            else
                                sb.AppendLine("\t" + assn.ToString() + "\n");
                        }
                        sb.AppendLine("End of association list.");
                        throw new NullReferenceException(sb.ToString());
                    }

                    cp.Add(assoc.Clone());
                }
                return cp;
            }
            catch (Exception ex) { throw new Exception("While initializing DML Translation from association list: " + ex.ToString()); }
        }

        internal static DmlTranslation CreateFrom(IEnumerable<Association> Set1, IEnumerable<Association> Set2)
        {
            try
            {
                if (Set1 == null) throw new ArgumentNullException("Set1");
                if (Set2 == null) throw new ArgumentNullException("Set2");

                DmlTranslation cp = CreateFrom(Set1);
                foreach (Association assoc in Set2) cp.Add(assoc);
                return cp;
            }
            catch (Exception ex) { throw new Exception("While initializing DML Translation from association lists: " + ex.ToString()); }
        }

        public void Add(IEnumerable<Association> Associations)
        {
            foreach (Association assoc in Associations)
            {
                try
                {
                    Add(assoc.Clone());
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message + "\n" + assoc.ToString(), ex);
                }
            }
        }

        /// <summary>
        /// DML3 provides the built-in DML Version 3 translation.  The DmlTranslation type does not
        /// provide any public constructors because all translations should begin with one of the 
        /// built-in translations.  Use DmlTranslation MyTranslation = DmlTranslation.DML3.Clone() to 
        /// initialize a new translation based upon the DML3 translation.
        /// </summary>        
        public static DML3Translation DML3 = new DML3Translation();

        /// <summary>
        /// TSL2 provides the built-in DML Translation language translation, which also includes the
        /// DML3 built-in translation.
        /// </summary>
        public static TSL2Translation TSL2 = new TSL2Translation();

        /// <summary>
        /// EC2 provides the built-in DML-EC2 Namespace, which also includes the DML3 built-in
        /// namespace.
        /// </summary>        
        public static EC2Translation EC2 = new EC2Translation();        
    }

    public class PrimitiveSet
    {
        /// <summary>
        /// Set gives the name of the set of primitives referenced.  For example,
        /// "common" or "arrays".
        /// </summary>
        public string Set;

        /// <summary>
        /// Codec provides the name of the codec for encoding and decoding the primitives,
        /// if one is specified.  If the codec is not specified, this string is empty.
        /// </summary>
        public string Codec;

        /// <summary>
        /// CodecURI can provide a location for software that can handle this codec.
        /// </summary>
        public string CodecURI;

        public PrimitiveSet() { }        

        public PrimitiveSet(string Set, string Codec = null, string CodecURI = null)
        {
            this.Set = Set;
            this.Codec = Codec;
            this.CodecURI = CodecURI;
        }

        public PrimitiveSet Clone()
        {
            return new PrimitiveSet(this.Set, this.Codec, this.CodecURI);
        }
    }

    #region "Dml Internals"

    public enum NodeTypes
    {
        /// <summary>
        /// Composite represents a higher-level structure which may contain multiple DML
        /// types.  Composite is not present in DML encoding, but in-memory structures
        /// may utilize it.
        /// </summary>
        Composite = -3,

        /// <summary>
        /// Structural nodes are utilized as part of the encoding and should not
        /// be utilized at a higher-level.
        /// </summary>
        Structural = -2,

        /// <summary>
        /// Unknown indicates that the node type is not recognized.
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// DML Container Elements contain attributes, and any number of children.
        /// </summary>
        Container = 0,        

        /// <summary>
        /// Indicates an element containing a single primitive type.
        /// </summary>
        Primitive,

        /// <summary>
        /// EndAttributes indicates that the attributes section of the current container
        /// is closed and the elements section follows.
        /// </summary>
        EndAttributes,

        /// <summary>
        /// EndContainer indicates that the last Container Element read has been closed.  
        /// This may or may not correspond to an actual node in the DML stream (as this
        /// condition is usually implied), but it is a proper structural representation of 
        /// the DML.
        /// </summary>
        EndContainer,                

        /// <summary>
        /// Comments are nodes which are to be ignored for all purposes except to
        /// describe something about the structure during encoding or tree analysis.
        /// </summary>
        Comment,

        /// <summary>
        /// Padding are nodes which are to be ignored.  They can be used to reserve
        /// space for later overwriting or to mark regions invalid.
        /// </summary>
        Padding
    }

    public enum Codecs
    {
        NotLoaded,
        LE,
        BE
    }

    public enum PrimitiveTypes
    {
        Unknown,

        /// <summary>Extension indicates that the primitive is recognized and supported by an extension handler.</summary>
        Extension,
        
        UInt,
        String,

        Int,
        Boolean,        
        DateTime,
        Single,
        Double,

        Decimal,

        Array,
        Matrix,        

        EncryptedDML,
        CompressedDML
    }

    public enum ArrayTypes
    {
        Unknown,
        U8,
        U16,
        U24,
        U32,
        U64,
        I8,
        I16,
        I24,
        I32,
        I64,
        DateTimes,
        Singles,
        Doubles,
        Decimals,
        Strings
    }

    public class ParsingOptions
    {
        internal Codecs CommonCodec = Codecs.NotLoaded;

        internal Codecs ArrayCodec = Codecs.NotLoaded;

        /// <summary>
        /// Set DiscardComments to true (default) to instruct the DmlReader to automatically discard comments
        /// without presenting them as a result of a Read() call.  When false, Read() will provide
        /// comments.
        /// </summary>
        public bool DiscardComments = true;

        /// <summary>
        /// Set DiscardPadding to true (default) to instruct the DmlReader to automatically discard padding
        /// without presenting them as a result of a Read() call.  When false, Read() will provide
        /// padding.
        /// </summary>
        public bool DiscardPadding = true;

        internal List<IDmlReaderExtension> Extensions = new List<IDmlReaderExtension>();

        public void AddExtension(IDmlReaderExtension Extension)
        {
            Extensions.Add(Extension);
        }
    }

    public class TSL2Translation : DmlTranslation
    {
        public const string urn = "urn:dml:tsl2";
        public const uint TSLVersion = 2;
        public const uint TSLReadVersion = 2;

        public const uint idDMLTranslation = 1140;
        internal static Association DMLTranslationAssociation = new Association(idDMLTranslation, "DML:Translation", NodeTypes.Container);

        public const uint idDML_URN = 20;
        public const uint idDMLIncludeTranslation = 2;
        public const uint idDML_URI = 21;
        public Association URN = new Association(idDML_URN, "DML:URN", PrimitiveTypes.String);
        public Association IncludeTranslation = new Association(idDMLIncludeTranslation, "DML:Include-Translation", NodeTypes.Container);
        public Association URI = new Association(idDML_URI, "DML:URI", PrimitiveTypes.String);

        public const uint idDMLIncludePrimitives = 3;
        public const uint idDMLSet = 31;
        public const uint idDMLCodec = 32;        
        public const uint idDMLCodecURI = 33;        

        public Association IncludePrimitives = new Association(idDMLIncludePrimitives, "DML:Include-Primitives", NodeTypes.Container);
        public Association Set = new Association(idDMLSet, "DML:Set", PrimitiveTypes.String);
        public Association Codec = new Association(idDMLCodec, "DML:Codec", PrimitiveTypes.String);
        public Association CodecURI = new Association(idDMLCodecURI, "DML:CodecURI", PrimitiveTypes.String);

        internal const uint idContainer = 40;
        internal const uint idNode = 41;
        internal const uint idName = 42;
        internal const uint idDMLID = 43;
        internal const uint idType = 44;
        internal const uint idUsage = 45;
        internal const uint idRenumber = 46;
        internal const uint idNewID = 47;
        internal static Association ContainerAssociation = new Association(idContainer, "Container", NodeTypes.Container);
        internal static Association NodeAssociation = new Association(idNode, "Node", NodeTypes.Container);
        internal static Association RenumberAssociation = new Association(idRenumber, "Renumber", NodeTypes.Container);

        internal const uint idXMLRoot = 50;        

        public TSL2Translation()
        {
            Add(
                new Association[] {                    
                    DMLTranslationAssociation,

                    URN,
                    IncludeTranslation,
                    URI,

                    IncludePrimitives,
                    Set,
                    Codec,
                    CodecURI,

                    ContainerAssociation,
                    NodeAssociation,
                    new Association(idName, "name", PrimitiveTypes.String),
                    new Association(idDMLID, "id", PrimitiveTypes.UInt),
                    new Association(idType, "type", PrimitiveTypes.String),
                    new Association(idUsage, "usage", PrimitiveTypes.String),
                    RenumberAssociation,
                    new Association(idNewID, "new-id", PrimitiveTypes.UInt),

                    new Association(idXMLRoot, "XMLRoot", NodeTypes.Container)
              });
        }
    }

    public class DML3Translation : DmlTranslation
    {
        public string urn = "urn:dml:dml3";        
        
        public Association Fragment = new Association(71619779, "DML:Fragment", NodeTypes.Structural);
        
        public const uint idDMLVersion = 1104;
        public const uint idDMLReadVersion = 1105;
        public Association Version = new Association(idDMLVersion, "DML:Version", PrimitiveTypes.UInt);
        public Association ReadVersion = new Association(idDMLReadVersion, "DML:ReadVersion", PrimitiveTypes.UInt);

        public const uint idDMLHeader = 71619778;
        public const uint idDMLDocType = 1106;
        public Association Header = new Association(idDMLHeader, "DML:Header", new TSL2Translation());
        public Association DocType = new Association(idDMLDocType, "DML:DocType", PrimitiveTypes.String);

        public const uint idXMLCDATA = 123;
        public const uint idDMLContentSize = 124;
        public Association CDATA = new Association(idXMLCDATA, "XML:CDATA", PrimitiveTypes.String);
        public Association ContentSize = new Association(idDMLContentSize, "DML:ContentSize", PrimitiveTypes.UInt);

        public static uint idInlineIdentification = 1088;
        public const uint idDMLComment = 1089;
        public const uint idDMLPadding = 1090;
        public const uint idDMLPaddingByte = 125;
        public const byte WholePaddingByte = 0xFD;        
        public const uint idDMLEndAttributes = 126;
        public const uint idDMLEndContainer = 127;
        public Association InlineIdentification = new Association(idInlineIdentification, "DML:InlineIdentification", NodeTypes.Structural);
        public Association Comment = new Association(idDMLComment, "DML:Comment", NodeTypes.Comment);
        public Association Padding = new Association(idDMLPadding, "DML:Padding", NodeTypes.Padding);
        public Association PaddingByte = new Association(idDMLPaddingByte, "DML:PaddingByte", NodeTypes.Padding);
        public Association EndAttributes = new Association(idDMLEndAttributes, "DML:EndAttributes", NodeTypes.EndAttributes);
        public Association EndContainer = new Association(idDMLEndContainer, "DML:EndContainer", NodeTypes.EndContainer);        

        public DML3Translation()
        {
            Add(
                new Association[] {                    
                    Version,
                    ReadVersion,

                    Header,
                    DocType,                    

                    CDATA,
                    ContentSize,
                    Comment

                    /**
                     * These entries are handled explicitly by DmlReader.Read() and 
                     * need not be automatically recognized:
                    InlineIdentification,
                    Padding,                    
                    PaddingByte,
                    EndAttributes,
                    EndContainer,                    
                     */
              });
        }
    }

    #endregion

    #region "Dml Internal Data"
            
    internal static class DmlInternalData
    {
        internal static DateTime ReferenceDate      = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);        
        internal const uint DMLVersion = 3;
        internal const uint DMLReadVersion = 3;

        internal const string urnBuiltinDML = "urn:dml:dml3";

        internal const string psBase = "base";
        internal const string psCommon = "common";
        internal const string psExtPrecision = "ext-precision";
        internal const string psDecimalFloat = "decimal-float";
        internal const string psArrays = "arrays";
        internal const string psDecimalArray = "decimal-array";
        internal const string psDmlEC = "dml-ec1";

        internal static bool IsCommonSet(PrimitiveTypes PrimitiveType)
        {
            switch (PrimitiveType)
            {
                case PrimitiveTypes.Int: return true;
                case PrimitiveTypes.Boolean: return true;
                case PrimitiveTypes.DateTime: return true;
                case PrimitiveTypes.Double: return true;
                case PrimitiveTypes.Single: return true;
                default: return false;
            }
        }        
        
        internal static Type GetArrayUnitType(ArrayTypes DmlType)
        {
            switch (DmlType)
            {
                case ArrayTypes.U8: return typeof(byte);
                case ArrayTypes.U16: return typeof(UInt16);
                case ArrayTypes.U32: return typeof(UInt32);
                case ArrayTypes.U64: return typeof(UInt64);
                case ArrayTypes.I8: return typeof(sbyte);
                case ArrayTypes.I16: return typeof(Int16);
                case ArrayTypes.I32: return typeof(Int32);
                case ArrayTypes.I64: return typeof(Int64);
                case ArrayTypes.Singles: return typeof(float);
                case ArrayTypes.Doubles: return typeof(double);
                case ArrayTypes.Decimals: return typeof(decimal);
                default: throw new NotSupportedException();
            }
        }

        internal static ArrayTypes GetArrayUnitType(Type CType)
        {
            if (CType == typeof(byte)) return ArrayTypes.U8;
            if (CType == typeof(UInt16)) return ArrayTypes.U16;
            if (CType == typeof(UInt32)) return ArrayTypes.U32;
            if (CType == typeof(UInt64)) return ArrayTypes.U64;
            if (CType == typeof(sbyte)) return ArrayTypes.I8;
            if (CType == typeof(Int16)) return ArrayTypes.I16;
            if (CType == typeof(Int32)) return ArrayTypes.I32;
            if (CType == typeof(Int64)) return ArrayTypes.I64;
            if (CType == typeof(float)) return ArrayTypes.Singles;
            if (CType == typeof(double)) return ArrayTypes.Doubles;
            if (CType == typeof(decimal)) return ArrayTypes.Decimals;
            if (CType == typeof(DateTime)) return ArrayTypes.DateTimes;
            if (CType == typeof(string)) return ArrayTypes.Strings;
            throw new NotSupportedException();
        }

        private static ArrayTypes SuffixToArrayType(string TypeSuffix)
        {
            switch (TypeSuffix.ToLower())
            {
                case "u8": return ArrayTypes.U8;
                case "u16": return ArrayTypes.U16;
                case "u24": return ArrayTypes.U24;
                case "u32": return ArrayTypes.U32;
                case "u64": return ArrayTypes.U64;
                case "i8": return ArrayTypes.I8;
                case "i16": return ArrayTypes.I16;
                case "i24": return ArrayTypes.I24;
                case "i32": return ArrayTypes.I32;
                case "i64": return ArrayTypes.I64;
                case "sf": return ArrayTypes.Singles;
                case "df": return ArrayTypes.Doubles;
                case "dt": return ArrayTypes.DateTimes;
                case "s": return ArrayTypes.Strings;
                // decimal-float-array types
                case "10f": return ArrayTypes.Decimals;
                default: throw new Exception("Not a recognized type indicator.");
            }
        }

        public static bool StringToPrimitiveType(string TypeStr, out PrimitiveTypes Type, out ArrayTypes ArrayType)
        {
            ArrayType = ArrayTypes.Unknown;

            string TypeStrl = TypeStr.ToLower();
            if (TypeStrl.Contains("array-"))
            {
                Type = PrimitiveTypes.Array;
                ArrayType = SuffixToArrayType(TypeStr.Substring("array-".Length));
                return true;
            }

            if (TypeStrl.Contains("matrix-"))
            {
                Type = PrimitiveTypes.Matrix;
                ArrayType = SuffixToArrayType(TypeStr.Substring("matrix-".Length));
                return true;
            }

            switch (TypeStrl)
            {
                case "int": Type = PrimitiveTypes.Int; return true;
                case "uint": Type = PrimitiveTypes.UInt; return true;
                case "boolean": Type = PrimitiveTypes.Boolean; return true;
                case "single": Type = PrimitiveTypes.Single; return true;
                case "double": Type = PrimitiveTypes.Double; return true;
                case "datetime": Type = PrimitiveTypes.DateTime; return true;
                case "string": Type = PrimitiveTypes.String; return true;
                // ext-precision types
                // decimal-float types
                case "decimal128": Type = PrimitiveTypes.Decimal; return true;
                case "compressed-dml": Type = PrimitiveTypes.CompressedDML; return true;
                case "encrypted-dml": Type = PrimitiveTypes.EncryptedDML; return true;
                default: Type = PrimitiveTypes.Unknown; return false;
            }
        }

        public static string PrimitiveTypeToString(PrimitiveTypes Type, ArrayTypes ArrayType)
        {
            switch (Type)
            {
                case PrimitiveTypes.Boolean: return "boolean";
                case PrimitiveTypes.DateTime: return "datetime";
                case PrimitiveTypes.Single: return "single";
                case PrimitiveTypes.Double: return "double";
                case PrimitiveTypes.Int: return "int";
                case PrimitiveTypes.UInt: return "uint";
                case PrimitiveTypes.String: return "string";
                // ext-precision types
                // decimal-float types
                case PrimitiveTypes.Decimal: return "decimal128";
                // array-types
                case PrimitiveTypes.Array: break;
                case PrimitiveTypes.Matrix: break;
                case PrimitiveTypes.CompressedDML: return "compressed-dml";
                case PrimitiveTypes.EncryptedDML: return "encrypted-dml";
                default: throw new Exception("Not a recognized primitive type.");
            }

            string ret;
            if (Type == PrimitiveTypes.Array) ret = "array-";
            else if (Type == PrimitiveTypes.Matrix) ret = "matrix-";
            else throw new Exception();

            switch (ArrayType)
            {
                case ArrayTypes.U8: return ret + "U8";
                case ArrayTypes.U16: return ret + "U16";
                case ArrayTypes.U24: return ret + "U24";
                case ArrayTypes.U32: return ret + "U32";
                case ArrayTypes.U64: return ret + "U64";
                case ArrayTypes.I8: return ret + "I8";
                case ArrayTypes.I16: return ret + "I16";
                case ArrayTypes.I24: return ret + "I24";
                case ArrayTypes.I32: return ret + "I32";
                case ArrayTypes.I64: return ret + "I64";
                case ArrayTypes.Singles: return ret + "SF";
                case ArrayTypes.Doubles: return ret + "DF";
                case ArrayTypes.DateTimes: return ret + "DT";
                case ArrayTypes.Strings: return ret + "S";

                case ArrayTypes.Decimals: return ret + "10F";

                default: throw new ArgumentException("Not a recognized array type.");
            }
        }
    }        

#   if false
    internal class DmlErrorContext
    {
        public UInt32 DMLID = UInt32.MaxValue;
        public long RelativePosition = long.MaxValue;
        public Association Association = null;

        public override string ToString()
        {
            string Ident;
            if (DMLID != UInt32.MaxValue && Association != null) DMLID = Association.DMLID;
            if (DMLID != UInt32.MaxValue)
                Ident = "[" + DMLID.ToString("X") + "] ";
            else
                Ident = "";

            if (Association != null)
            {
                if (Association.DMLName.XmlName != null)
                {
                    string XmlName = Association.DMLName.XmlName;
                    switch (Association.DMLName.NodeType)
                    {
                        case NodeTypes.Attribute: Ident = Ident + XmlName + "= "; break;
                        case NodeTypes.Comment: Ident = Ident + "(Comment) "; break;
                        case NodeTypes.Primitive: Ident = Ident + "<" + XmlName + "> "; break;
                        case NodeTypes.Container: Ident = Ident + "<" + XmlName + "> "; break;
                        case NodeTypes.EndContainer: Ident = Ident + "</" + XmlName + "> "; break;
                        case NodeTypes.Unknown: Ident = Ident + XmlName + " (Unknown Type) "; break;
                    }
                }
                else
                {
                    switch (Association.DMLName.NodeType)
                    {
                        case NodeTypes.Attribute: Ident = Ident + "(Attribute) "; break;
                        case NodeTypes.Comment: Ident = Ident + "(Comment) "; break;
                        case NodeTypes.Primitive: Ident = Ident + "(Primitive) "; break;
                        case NodeTypes.Container: Ident = Ident + "(Container) "; break;
                        case NodeTypes.EndContainer: Ident = Ident + "(End Container) "; break;
                        case NodeTypes.Unknown: break;
                    }
                }
            }
            
            if (RelativePosition != long.MaxValue)
            {
                Ident = Ident + " and offset 0x" + RelativePosition.ToString("X") + " ";
            }

            return Ident;
        }
    }
#   endif

    #endregion
}
