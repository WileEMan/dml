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
using System.Xml;
using System.Text;
using System.Collections.Generic;
using WileyBlack.Dml;
using WileyBlack.Dml.EC;

namespace WileyBlack.Dml.Dom
{
    public abstract class DmlNode
    {
        public Association Association = DmlTranslation.DML3.InlineIdentification.Clone();

        public uint ID { get { return Association.DMLID; } }
        public uint DmlID { get { return Association.DMLID; } }
        public string Name { 
            get { return Association.DMLName.XmlName; }
            set { Association = Association.Clone(); Association.DMLName.XmlName = value; }
        }
        public NodeTypes NodeType { 
            get { return Association.DMLName.NodeType; }
            set { Association = Association.Clone(); Association.DMLName.NodeType = value; }
        }
        public bool InlineIdentification { get { return Association.InlineIdentification; } }

        public DmlDocument Document;
        public DmlFragment Container;

        protected DmlNode() { }

        public DmlNode(DmlFragment Container) {
            if (Container is DmlDocument) this.Document = Container as DmlDocument; else this.Document = Container.Document;
            this.Container = Container; 
        }
        public DmlNode(DmlDocument Document) { this.Document = Document; this.Container = null; }        

        protected DmlException CreateDmlException(string Message) { return CreateDmlException(Message, null); }
        protected DmlException CreateDmlException(string Message, Exception innerException)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Dml Exception Context:");
            
            sb.AppendLine("\t" + ToErrorString() + "\n");
            DmlFragment Cont = Container;
            while (Cont != null)
            {
                sb.AppendLine("\t" + Cont.ToErrorString() + "\n");
                Cont = Cont.Container;
            }
            
            return new DmlException(Message + "\n" + sb.ToString(), innerException);
        }

        internal string ToErrorString()
        {
            string Ident;
            if (ID != UInt32.MaxValue && ID != DML3Translation.idInlineIdentification)
                Ident = "[" + ID.ToString("X") + "] ";
            else
                Ident = "";

            if (Association != null)
            {
                if (Association.DMLName.XmlName != null)
                {
                    string XmlName = Association.DMLName.XmlName;
                    switch (Association.DMLName.NodeType)
                    {
                        case NodeTypes.Comment: Ident = Ident + "(Comment) "; break;
                        case NodeTypes.Primitive:
                            if ((this is DmlPrimitive) && ((DmlPrimitive)this).IsAttribute)
                                Ident = Ident + XmlName + "=\"...\" ";
                            else
                                Ident = Ident + "<" + XmlName + "> "; 
                            break;
                        case NodeTypes.Container: 
                            Ident = Ident + "<" + XmlName + "> ";
                            if (this is DmlFragment && ((DmlFragment)this).StartPosition != long.MaxValue)
                                Ident = Ident + " [Stream Position " + ((DmlFragment)this).StartPosition + "] ";
                            break;
                        case NodeTypes.EndContainer: Ident = Ident + "</" + XmlName + "> "; break;
                        case NodeTypes.Unknown: Ident = Ident + XmlName + " (Unknown Type) "; break;
                    }
                }
                else
                {
                    switch (Association.DMLName.NodeType)
                    {
                        case NodeTypes.Comment: Ident = Ident + "(Comment) "; break;
                        case NodeTypes.Primitive: Ident = Ident + "(Primitive) "; break;
                        case NodeTypes.Container: Ident = Ident + "(Container) "; break;
                        case NodeTypes.EndContainer: Ident = Ident + "(End Container) "; break;
                        case NodeTypes.Unknown: break;
                    }
                }
            }

            return Ident;
        }

        public abstract void LoadContent(DmlReader Reader);        
        public abstract void WriteTo(DmlWriter Writer);

        /// <summary>
        /// GetEncodedSize() provides a prediction of the encoded size,
        /// in bytes, of the node.  When the encoded size cannot be
        /// predicted, UInt64.MaxValue is returned.
        /// </summary>
        /// <param name="Writer"></param>
        /// <returns></returns>
        public abstract UInt64 GetEncodedSize(DmlWriter Writer);

        /// <summary>
        /// DmlNode's Clone() method performs a partial copy operation.
        /// The internal components of the node are copied (i.e. association,
        /// values, etc.)  The new object is potentially associated with
        /// a new document (given by the NewDoc) parameter.  The new DmlNode
        /// object is not attached to any dml tree and has no parent.
        /// </summary>
        /// <param name="NewDoc">Document to associate the new node with.
        /// Can be the same document.</param>
        /// <returns>A new DmlNode object with identical properties.</returns>
        public abstract DmlNode Clone(DmlDocument NewDoc);
    }

    public class DmlElements : List<DmlNode>
    {
        #region "Child element management routines"

        DmlFragment Container;

        internal DmlElements(DmlFragment Container)
        {
            this.Container = Container;
        }        

        public new void Add(DmlNode Child)
        {
            Child.Document = Container.Document;
            Child.Container = Container;
            if (Child is DmlPrimitive) ((DmlPrimitive)Child).IsAttribute = false;
            base.Add(Child);
        }

        public new void AddRange(IEnumerable<DmlNode> collection)
        {
            foreach (DmlNode node in collection) Add(node);
        }

        public new void Insert(int index, DmlNode item)
        {
            item.Document = Container.Document;
            item.Container = Container;
            if (item is DmlPrimitive) ((DmlPrimitive)item).IsAttribute = false;
            base.Insert(index, item);
        }

        #endregion

        #region "Retrieval routines"

        public DmlNode GetByID(uint DmlID)
        {
            foreach (DmlNode el in this)
            {
                if (el.DmlID == DmlID) return el;
            }
            return null;
        }

        public bool ContainsID(uint DmlID) { return GetByID(DmlID) != null; }

        public int GetIndex(uint DmlID)
        {
            for (int ii = 0; ii < Count; ii++)
            {
                if (base[ii].DmlID == DmlID) return ii;
            }
            return -1;
        }

        public int GetIndex(string Name)
        {
            for (int ii = 0; ii < Count; ii++)
            {
                if (base[ii].Name == Name) return ii;
            }
            return -1;
        }

        public DmlNode this[string Name]
        {
            get
            {
                foreach (DmlNode child in this)
                {
                    if (child.Name == Name) return child;
                }
                return null;
            }

            set
            {
                for (int ii = 0; ii < base.Count; ii++)
                {
                    if (base[ii].Name == Name)
                    {
                        base[ii] = value;
                        return;
                    }
                }
                base.Add(value);
            }
        }

        #endregion

        #region "Get..() by name routines"

        public DmlContainer GetContainer(string XMLName)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return null;
            if (!(dml is DmlContainer)) throw new FormatException("Expected container type.");
            return (dml as DmlContainer);
        }

        #region "Base primitives"

        public uint GetUInt(string XMLName, uint DefaultValue) { return (uint)GetULong(XMLName, (ulong)DefaultValue); }
        public int GetInt(string XMLName, int DefaultValue) { return (int)GetLong(XMLName, (long)DefaultValue); }

        public ulong GetULong(string XMLName, ulong DefaultValue)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlUInt)) throw new FormatException("Primitive type mismatch.");
            return (ulong)((DmlUInt)dml).Value;
        }

        public long GetLong(string XMLName, long DefaultValue)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlInt)) throw new FormatException("Primitive type mismatch.");
            return (long)((DmlInt)dml).Value;
        }

        public bool GetBool(string XMLName, bool DefaultValue)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlBool)) throw new FormatException("Primitive type mismatch.");
            return (bool)((DmlBool)dml).Value;
        }

        public string GetString(string XMLName, string DefaultValue)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlString)) throw new FormatException("Primitive type mismatch.");
            return (string)((DmlString)dml).Value;
        }

        public byte[] GetU8Array(string XMLName, byte[] Default)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlByteArray)) throw new FormatException("Primitive type mismatch.");
            return (byte[])((DmlByteArray)dml).Value;
        }        

        #endregion

        #region "Common Primitives"

        public float GetSingle(string XMLName, float DefaultValue)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlSingle)) throw new FormatException("Primitive type mismatch.");
            return (float)((DmlSingle)dml).Value;
        }

        public double GetDouble(string XMLName, double DefaultValue)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlDouble)) throw new FormatException("Primitive type mismatch.");
            return (double)((DmlDouble)dml).Value;
        }

        public DateTime GetDateTime(string XMLName, DateTime DefaultValue)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlSingle)) throw new FormatException("Primitive type mismatch.");
            return (DateTime)((DmlDateTime)dml).Value;
        }

        #endregion

        #region "Decimal-Float Primitives"
#       if false
        public decimal GetDecimal(string XMLName, decimal DefaultValue)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlDecimal)) throw new FormatException("Primitive type mismatch.");
            return (decimal)((DmlDecimal)dml).Value;
        }
#       endif
        #endregion

        #region "Array Primitives"

        public UInt16[] GetU16Array(string XMLName, UInt16[] Default)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlUInt16Array)) throw new FormatException("Primitive type mismatch.");
            return (ushort[])((DmlUInt16Array)dml).Value;
        }

        public UInt32[] GetU32Array(string XMLName, UInt32[] Default)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlUInt32Array)) throw new FormatException("Primitive type mismatch.");
            return (uint[])((DmlUInt32Array)dml).Value;
        }

        public UInt64[] GetU64Array(string XMLName, UInt64[] Default)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlUInt64Array)) throw new FormatException("Primitive type mismatch.");
            return (ulong[])((DmlUInt64Array)dml).Value;
        }

        public sbyte[] GetI8Array(string XMLName, sbyte[] Default)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlSByteArray)) throw new FormatException("Primitive type mismatch.");
            return (sbyte[])((DmlSByteArray)dml).Value;
        }

        public Int16[] GetI16Array(string XMLName, Int16[] Default)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlInt16Array)) throw new FormatException("Primitive type mismatch.");
            return (short[])((DmlInt16Array)dml).Value;
        }

        public Int32[] GetI32Array(string XMLName, Int32[] Default)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlInt32Array)) throw new FormatException("Primitive type mismatch.");
            return (int[])((DmlInt32Array)dml).Value;
        }

        public Int64[] GetI64Array(string XMLName, Int64[] Default)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlInt64Array)) throw new FormatException("Primitive type mismatch.");
            return (long[])((DmlInt64Array)dml).Value;
        }

        public DateTime[] GetDateTimeArray(string XMLName, DateTime[] Default)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlDateTimeArray)) throw new FormatException("Primitive type mismatch.");
            return (DateTime[])((DmlDateTimeArray)dml).Value;
        }

        public string[] GetStringArray(string XMLName, string[] Default)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlStringArray)) throw new FormatException("Primitive type mismatch.");
            return (string[])((DmlStringArray)dml).Value;
        }

#       if false
        public decimal[] GetDecimalArray(string XMLName, decimal[] Default)
        {
            DmlNode dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlDecimalArray)) throw new FormatException("Primitive type mismatch.");
            return (decimal[])((DmlDecimalArray)dml).Value;
        }
#       endif

        #endregion

        #endregion

        #region "Get..() by DML ID routines"

        public DmlContainer GetContainer(uint DmlId)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return null;
            if (!(dml is DmlContainer)) throw new FormatException("Expected container type.");
            return (dml as DmlContainer);
        }

        #region "Base Primitives"

        public uint GetUInt(uint DmlId, uint DefaultValue) { return (uint)GetULong(DmlId, (ulong)DefaultValue); }
        public int GetInt(uint DmlId, int DefaultValue) { return (int)GetLong(DmlId, (long)DefaultValue); }

        public ulong GetULong(uint DmlId, ulong DefaultValue)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlUInt)) throw new FormatException("Primitive type mismatch.");
            return (ulong)((DmlUInt)dml).Value;
        }

        public long GetLong(uint DmlId, long DefaultValue)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlInt)) throw new FormatException("Primitive type mismatch.");
            return (long)((DmlInt)dml).Value;
        }

        public bool GetBool(uint DmlId, bool DefaultValue)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlBool)) throw new FormatException("Primitive type mismatch.");
            return (bool)((DmlBool)dml).Value;
        }

        public string GetString(uint DmlId, string DefaultValue)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlString)) throw new FormatException("Primitive type mismatch.");
            return (string)((DmlString)dml).Value;
        }

        public byte[] GetU8Array(uint DmlId, byte[] Default)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlByteArray)) throw new FormatException("Primitive type mismatch.");
            return (byte[])((DmlByteArray)dml).Value;
        }

        #endregion

        #region "Common Primitives"

        public float GetSingle(uint DmlId, float DefaultValue)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlSingle)) throw new FormatException("Primitive type mismatch.");
            return (float)((DmlSingle)dml).Value;
        }

        public double GetDouble(uint DmlId, double DefaultValue)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlDouble)) throw new FormatException("Primitive type mismatch.");
            return (double)((DmlDouble)dml).Value;
        }

        public DateTime GetDateTime(uint DmlId, DateTime DefaultValue)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlSingle)) throw new FormatException("Primitive type mismatch.");
            return (DateTime)((DmlDateTime)dml).Value;
        }

        #endregion

        #region "Decimal-Float Primitives"
#       if false
        public decimal GetDecimal(uint DmlId, decimal DefaultValue)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlDecimal)) throw new FormatException("Primitive type mismatch.");
            return (decimal)((DmlDecimal)dml).Value;
        }
#       endif
        #endregion

        #region "Array Primitives"

        public UInt16[] GetU16Array(uint DmlId, UInt16[] Default)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlUInt16Array)) throw new FormatException("Primitive type mismatch.");
            return (ushort[])((DmlUInt16Array)dml).Value;
        }

        public UInt32[] GetU32Array(uint DmlId, UInt32[] Default)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlUInt32Array)) throw new FormatException("Primitive type mismatch.");
            return (uint[])((DmlUInt32Array)dml).Value;
        }

        public UInt64[] GetU64Array(uint DmlId, UInt64[] Default)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlUInt64Array)) throw new FormatException("Primitive type mismatch.");
            return (ulong[])((DmlUInt64Array)dml).Value;
        }

        public sbyte[] GetI8Array(uint DmlId, sbyte[] Default)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlSByteArray)) throw new FormatException("Primitive type mismatch.");
            return (sbyte[])((DmlSByteArray)dml).Value;
        }

        public Int16[] GetI16Array(uint DmlId, Int16[] Default)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlInt16Array)) throw new FormatException("Primitive type mismatch.");
            return (short[])((DmlInt16Array)dml).Value;
        }

        public Int32[] GetI32Array(uint DmlId, Int32[] Default)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlInt32Array)) throw new FormatException("Primitive type mismatch.");
            return (int[])((DmlInt32Array)dml).Value;
        }

        public Int64[] GetI64Array(uint DmlId, Int64[] Default)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlInt64Array)) throw new FormatException("Primitive type mismatch.");
            return (long[])((DmlInt64Array)dml).Value;
        }

        public DateTime[] GetDateTimeArray(uint DmlId, DateTime[] Default)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlDateTimeArray)) throw new FormatException("Primitive type mismatch.");
            return (DateTime[])((DmlDateTimeArray)dml).Value;
        }

        public string[] GetStringArray(uint DmlId, string[] Default)
        {
            DmlNode dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlStringArray)) throw new FormatException("Primitive type mismatch.");
            return (string[])((DmlStringArray)dml).Value;
        }

#       if false
        public decimal[] GetDecimalArray(uint DmlId, decimal[] Default)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlDecimalArray)) throw new FormatException("Attribute type mismatch.");
            return (decimal[])((DmlDecimalArray)dml).Value;
        }
#       endif

        #endregion

        #endregion
    }

    public class DmlFragment : DmlNode
    {
        #region "Properties"

        public enum LoadState
        {
            Unknown,
            None,
            IdOnly,
            Attributes,
            Full
        }

        /// <summary>
        /// Loaded provides the current state of the node.  Nodes created in memory are always fully loaded,
        /// but nodes initialized from a LoadPartial() call can be partially loaded.  A node that has the
        /// Loaded state of Full indicates that all attributes and direct children are loaded, but it does
        /// not verify that the children themselves are also fully loaded.  Use the IsFullyLoaded property
        /// to also ensure the children's state.
        /// </summary>
        public LoadState Loaded = LoadState.Full;

        /// <summary>
        /// IsFullyLoaded indicates if the DML container and all children are loaded.
        /// </summary>
        public bool IsFullyLoaded
        {
            get
            {
                if (Loaded != LoadState.Full) return false;
                foreach (DmlNode Node in Children)
                {
                    if (Node is DmlFragment && !((DmlFragment)Node).IsFullyLoaded) return false;
                }
                return true;
            }
        }

        protected DmlElements m_Children;
        public DmlElements Children
        {
            get
            {
                if (Loaded != LoadState.Full)
                    throw CreateDmlException("Elements accessed before being loaded.");
                return m_Children;
            }
        }

        /// <summary>
        /// NextPosition is an internal reference used during the partial loading process.
        /// </summary>
        protected DmlContext NextPosition;
        
        /// <summary>
        /// For diagnostic use only.  Only available from seekable streams.
        /// </summary>        
        public long StartPosition = long.MaxValue;

        #endregion

        public DmlFragment() 
        {
            m_Children = new DmlElements(this);
        }

        public DmlFragment(DmlDocument Document) : base(Document)
        {
            m_Children = new DmlElements(this);
            Association.DMLName.NodeType = NodeTypes.Container;            
        }

        public DmlFragment(DmlFragment Container) : base(Container) 
        {
            m_Children = new DmlElements(this);
            Association.DMLName.NodeType = NodeTypes.Container;
        }

        public void Merge(DmlFragment From)
        {
            Children.AddRange(From.Children);
        }

        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlFragment cp = new DmlFragment(NewDoc);
            cp.Association = Association;
            cp.Loaded = Loaded;
            cp.NextPosition = NextPosition;
            cp.StartPosition = StartPosition;
            foreach (DmlNode Child in Children) cp.Children.Add(Child.Clone(NewDoc));
            return cp;
        }

        protected void GoToPosition(DmlReader Reader)
        {
            if (Reader.CanSeek && NextPosition != null)
            {
                Reader.Seek(NextPosition);
                NextPosition = null;
            }
        }

        /// <summary>
        /// LoadElements() performs a full or partial load of all elements.  Use of the Partial option
        /// requires a seekable stream, since it loads only the location of child elements when possible.
        /// </summary>
        /// <param name="Reader"></param>
        protected virtual void LoadElements(DmlReader Reader, bool Partial)
        {
            if (Loaded != LoadState.None && Loaded != LoadState.IdOnly) throw CreateDmlException("LoadElements() for fragments may only be called when loaded state is None or IdOnly.");
            if (Partial && !Reader.CanSeek) throw CreateDmlException("Seekable stream required for partial loading method.");

            for (; ; )
            {
                if (!Reader.Read()) { Loaded = LoadState.Full; return; }

                switch (Reader.NodeType)
                {
                    case NodeTypes.EndContainer: throw CreateDmlException("Unmatched End-Container marker.");
                    case NodeTypes.EndAttributes: throw CreateDmlException("Unmatched End-Attributes marker.");
                    case NodeTypes.Padding:
                        // Padding is set to be discarded by the DmlReader by default.  If it gets to here,
                        // we assume the caller wants it.  First, try merging new padding with previous
                        // padding...
                        DmlPadding pad = Document.CreatePadding();
                        pad.Container = this;
                        pad.LoadContent(Reader);
                        if (m_Children.Count > 0)
                        {
                            DmlPadding PrevPad = m_Children[m_Children.Count - 1] as DmlPadding;
                            if (PrevPad != null) PrevPad.Merge(pad);
                            else m_Children.Add(pad);
                        }
                        else m_Children.Add(pad);
                        continue;
                    case NodeTypes.Comment:
                        {
                            DmlComment dml = Document.CreateComment();
                            dml.Container = this;
                            dml.LoadContent(Reader);
                            m_Children.Add(dml);
                            continue;
                        }
                    case NodeTypes.Container:
                        {
                            DmlContainer dml = Document.CreateContainer(Reader.Association, this);
                            dml.Container = this;
                            dml.Loaded = LoadState.IdOnly;
                            dml.StartPosition = Reader.Container.StartPosition;
                            if (!Partial)
                            {
                                // Perform a full load of the element...
                                dml.LoadContent(Reader);
                                m_Children.Add(dml);
                                continue;
                            }
                            else
                            {
                                // Check if the element contains navigation so that we can perform a partial load...
                                dml.LoadPartial(Reader, LoadState.Attributes);
                                DmlPrimitive ContentSize = dml.Attributes.GetByID(DML3Translation.idDMLContentSize);
                                if (ContentSize == null || !(ContentSize is DmlUInt))
                                {
                                    // No ContentSize attribute, so we are required to load the element.  Or, at least
                                    // we are required to load the next level of the element and look for ContentSize
                                    // attributes on deeper children levels.
                                    dml.LoadPartial(Reader, LoadState.Full);
                                }
                                else
                                {
                                    // Perform a partial load of the element...
                                    dml.NextPosition = Reader.GetContext();
                                    Reader.SeekRelative(dml.NextPosition, (ulong)(ContentSize.Value));
                                    if (!Reader.Read()) throw CreateDmlException("Invalid ContentSize indicator, missing End-Container marker, or incomplete structure.");
                                }
                                m_Children.Add(dml);
                                continue;
                            }
                        }
                    case NodeTypes.Primitive:
                        {
                            DmlPrimitive dml = Document.CreatePrimitive(Reader.PrimitiveType, Reader.ArrayType);
                            dml.Container = this;
                            dml.Association = Reader.Association;
                            dml.LoadContent(Reader);
                            dml.IsAttribute = false;
                            m_Children.Add(dml);
                            continue;
                        }
                    default: throw CreateDmlException("Unrecognized node type.");
                }
            }
        }

        public override void LoadContent(DmlReader Reader)
        {
            GoToPosition(Reader);            

            if (Loaded == LoadState.IdOnly || Loaded == LoadState.Attributes) throw CreateDmlException("Illegal state for DmlFragment.");            

            if (Loaded != LoadState.Full) LoadElements(Reader, false);

            // The LoadContent() call requests that the node be loaded in its entirety - even if it
            // has previously been partially loaded.  To accomplish this, we iterate through all
            // children and ensure they are also recursively fully loaded.
            if (Loaded == LoadState.Full)
            {
                foreach (DmlNode Node in Children)
                {
                    if (Node is DmlFragment && !((DmlFragment)Node).IsFullyLoaded)                    
                        ((DmlFragment)Node).LoadContent(Reader);
                }
            }
        }

        /// <summary>
        /// LoadPartial() is similar to LoadContent() but provides control over what portion of the fragment
        /// should be loaded.  LoadPartial() will throw an exception if the stream is not seekable.  LoadPartial() will 
        /// load the section requested by ToLoad, but will generate minimally-loaded DmlContainers whenever 
        /// possible (that is, every time the container provides a data size).  LoadPartial() can then be called
        /// on a child element to load an additional level.  LoadContent() can be called on any of the partially 
        /// loaded Children to fully load that container and all children.  Providing a ToLoad value of 
        /// LoadState.Full to LoadPartial() loads everything in the current level, but does not load the child
        /// elements.
        /// </summary>
        /// <seealso>Loaded, IsFullyLoaded, LoadContent()</seealso>
        /// <param name="Reader">DmlReader providing stream and dml access.</param>
        /// <param name="ToLoad">Section of the container to be loaded.</param>
        public virtual void LoadPartial(DmlReader Reader, LoadState ToLoad)
        {
            GoToPosition(Reader);

            if (Loaded == LoadState.IdOnly || Loaded == LoadState.Attributes) throw CreateDmlException("Illegal state for DmlFragment.");

            if (Loaded != LoadState.Full)
            {
                if (ToLoad == LoadState.Full) LoadElements(Reader, false);
            }

            if (Loaded != LoadState.Full) NextPosition = Reader.GetContext();
        }

        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            UInt64 DataSize = 0;
            foreach (DmlNode Child in Children)
            {
                UInt64 ChildSize = Child.GetEncodedSize(Writer);
                if (ChildSize == UInt64.MaxValue) return UInt64.MaxValue;
                DataSize += ChildSize;
            }
            return DataSize;
        }

        public override void WriteTo(DmlWriter Writer)
        {
            foreach (DmlNode Child in Children) Child.WriteTo(Writer);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name)) return base.ToString();
            return base.ToString() + " (" + Name + ")";
        }
    }

    public class DmlAttributes : List<DmlPrimitive>
    {
        #region "Attribute management routines"

        DmlContainer Container;

        internal DmlAttributes(DmlContainer Container)
        {
            this.Container = Container;
        }

        public new void Add(DmlPrimitive Attr)
        {
            Attr.Document = Container.Document;
            Attr.Container = Container;
            Attr.IsAttribute = true;            
            base.Add(Attr);
        }

        public new void AddRange(IEnumerable<DmlPrimitive> collection)
        {            
            foreach (DmlPrimitive prim in collection) Add(prim);
        }

        public new void Insert(int index, DmlPrimitive item)
        {
            item.Document = Container.Document;
            item.Container = Container;
            item.IsAttribute = true;
            base.Insert(index, item);
        }

        #endregion

        #region "Retrieval routines"

        public DmlPrimitive GetByID(uint DmlID)
        {
            foreach (DmlPrimitive attr in this)
            {
                if (attr.DmlID == DmlID) return attr;
            }
            return null;
        }

        public bool ContainsID(uint DmlID) { return GetByID(DmlID) != null; }

        public int GetIndex(uint DmlID)
        {
            for (int ii = 0; ii < Count; ii++)
            {
                if (base[ii].DmlID == DmlID) return ii;
            }
            return -1;
        }

        public int GetIndex(string Name)
        {
            for (int ii=0; ii < Count; ii++)            
            {
                if (base[ii].Name == Name) return ii;
            }
            return -1;
        }

        public DmlPrimitive this[string Name]
        {
            get 
            { 
                foreach (DmlPrimitive attr in this)
                {
                    if (attr.Name == Name) return attr;
                }
                return null;
            }

            set
            {
                for (int ii=0; ii < base.Count; ii++)                
                {
                    if (base[ii].Name == Name) 
                    {
                        base[ii] = value;
                        return;
                    }
                }
                base.Add(value);
            }
        }

#       if false
        public DmlPrimitive this[int Index]
        {
            get { return this[Index]; }
            set { base[Index] = value; }
        }
#       endif

        #endregion

        #region "Get..() by name routines"

        #region "Base primitives"

        public uint GetUInt(string XMLName, uint DefaultValue) { return (uint)GetULong(XMLName, (ulong)DefaultValue); }                
        public ulong GetULong(string XMLName, ulong DefaultValue)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlUInt)) throw new FormatException("Attribute type mismatch.");
            return (ulong)((DmlUInt)dml).Value;
        }
        
        public string GetString(string XMLName, string DefaultValue)
        {            
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlString)) throw new FormatException("Attribute type mismatch.");
            return (string)((DmlString)dml).Value;
        }

        public byte[] GetU8Array(string XMLName, byte[] Default)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlByteArray)) throw new FormatException("Attribute type mismatch.");
            return (byte[])((DmlByteArray)dml).Value;
        }        

        #endregion

        #region "Common Primitives"

        public int GetInt(string XMLName, int DefaultValue) { return (int)GetLong(XMLName, (long)DefaultValue); }
        public long GetLong(string XMLName, long DefaultValue)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlInt)) throw new FormatException("Attribute type mismatch.");
            return (long)((DmlInt)dml).Value;
        }

        public bool GetBool(string XMLName, bool DefaultValue)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlBool)) throw new FormatException("Attribute type mismatch.");
            return (bool)((DmlBool)dml).Value;
        }        

        public float GetSingle(string XMLName, float DefaultValue)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlSingle)) throw new FormatException("Attribute type mismatch.");
            return (float)((DmlSingle)dml).Value;
        }

        public double GetDouble(string XMLName, double DefaultValue)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlDouble)) throw new FormatException("Attribute type mismatch.");
            return (double)((DmlDouble)dml).Value;
        }

        public DateTime GetDateTime(string XMLName, DateTime DefaultValue)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlSingle)) throw new FormatException("Attribute type mismatch.");
            return (DateTime)((DmlDateTime)dml).Value;
        }

        #endregion

        #region "Decimal-Float Primitives"
#       if false
        public decimal GetDecimal(string XMLName, decimal DefaultValue)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return DefaultValue;
            if (!(dml is DmlDecimal)) throw new FormatException("Attribute type mismatch.");
            return (decimal)((DmlDecimal)dml).Value;
        }
#       endif
        #endregion

        #region "Array Primitives"

        public UInt16[] GetU16Array(string XMLName, UInt16[] Default)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlUInt16Array)) throw new FormatException("Attribute type mismatch.");
            return (ushort[])((DmlUInt16Array)dml).Value;
        }

        public UInt32[] GetU32Array(string XMLName, UInt32[] Default)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlUInt32Array)) throw new FormatException("Attribute type mismatch.");
            return (uint[])((DmlUInt32Array)dml).Value;
        }

        public UInt64[] GetU64Array(string XMLName, UInt64[] Default)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlUInt64Array)) throw new FormatException("Attribute type mismatch.");
            return (ulong[])((DmlUInt64Array)dml).Value;
        }

        public sbyte[] GetI8Array(string XMLName, sbyte[] Default)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlSByteArray)) throw new FormatException("Attribute type mismatch.");
            return (sbyte[])((DmlSByteArray)dml).Value;
        }

        public Int16[] GetI16Array(string XMLName, Int16[] Default)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlInt16Array)) throw new FormatException("Attribute type mismatch.");
            return (short[])((DmlInt16Array)dml).Value;
        }

        public Int32[] GetI32Array(string XMLName, Int32[] Default)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlInt32Array)) throw new FormatException("Attribute type mismatch.");
            return (int[])((DmlInt32Array)dml).Value;
        }

        public Int64[] GetI64Array(string XMLName, Int64[] Default)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlInt64Array)) throw new FormatException("Attribute type mismatch.");
            return (long[])((DmlInt64Array)dml).Value;
        }

        public DateTime[] GetDateTimeArray(string XMLName, DateTime[] Default)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlDateTimeArray)) throw new FormatException("Attribute type mismatch.");
            return (DateTime[])((DmlDateTimeArray)dml).Value;
        }

        public string[] GetStringArray(string XMLName, string[] Default)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlStringArray)) throw new FormatException("Attribute type mismatch.");
            return (string[])((DmlStringArray)dml).Value;
        }

#       if false
        public decimal[] GetDecimalArray(string XMLName, decimal[] Default)
        {
            DmlPrimitive dml = this[XMLName];
            if (dml == null) return Default;
            if (!(dml is DmlDecimalArray)) throw new FormatException("Attribute type mismatch.");
            return (decimal[])((DmlDecimalArray)dml).Value;
        }
#       endif

        #endregion

        #endregion

        #region "Get..() by DML ID routines"

        #region "Base Primitives"

        public uint GetUInt(uint DmlId, uint DefaultValue) { return (uint)GetULong(DmlId, (ulong)DefaultValue); }                

        public ulong GetULong(uint DmlId, ulong DefaultValue)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlUInt)) throw new FormatException("Attribute type mismatch.");
            return (ulong)((DmlUInt)dml).Value;
        }        

        public string GetString(uint DmlId, string DefaultValue)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlString)) throw new FormatException("Attribute type mismatch.");
            return (string)((DmlString)dml).Value;
        }

        public byte[] GetU8Array(uint DmlId, byte[] Default)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlByteArray)) throw new FormatException("Attribute type mismatch.");
            return (byte[])((DmlByteArray)dml).Value;
        }        

        #endregion

        #region "Common Primitives"

        public int GetInt(uint DmlId, int DefaultValue) { return (int)GetLong(DmlId, (long)DefaultValue); }
        public long GetLong(uint DmlId, long DefaultValue)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlInt)) throw new FormatException("Attribute type mismatch.");
            return (long)((DmlInt)dml).Value;
        }

        public bool GetBool(uint DmlId, bool DefaultValue)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlBool)) throw new FormatException("Attribute type mismatch.");
            return (bool)((DmlBool)dml).Value;
        }        

        public float GetSingle(uint DmlId, float DefaultValue)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlSingle)) throw new FormatException("Attribute type mismatch.");
            return (float)((DmlSingle)dml).Value;
        }

        public double GetDouble(uint DmlId, double DefaultValue)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlDouble)) throw new FormatException("Attribute type mismatch.");
            return (double)((DmlDouble)dml).Value;
        }

        public DateTime GetDateTime(uint DmlId, DateTime DefaultValue)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlSingle)) throw new FormatException("Attribute type mismatch.");
            return (DateTime)((DmlDateTime)dml).Value;
        }

        #endregion

        #region "Decimal-Float Primitives"
#       if false
        public decimal GetDecimal(uint DmlId, decimal DefaultValue)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return DefaultValue;
            if (!(dml is DmlDecimal)) throw new FormatException("Attribute type mismatch.");
            return (decimal)((DmlDecimal)dml).Value;
        }
#       endif
        #endregion

        #region "Array Primitives"

        public UInt16[] GetU16Array(uint DmlId, UInt16[] Default)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlUInt16Array)) throw new FormatException("Attribute type mismatch.");
            return (ushort[])((DmlUInt16Array)dml).Value;
        }

        public UInt32[] GetU32Array(uint DmlId, UInt32[] Default)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlUInt32Array)) throw new FormatException("Attribute type mismatch.");
            return (uint[])((DmlUInt32Array)dml).Value;
        }

        public UInt64[] GetU64Array(uint DmlId, UInt64[] Default)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlUInt64Array)) throw new FormatException("Attribute type mismatch.");
            return (ulong[])((DmlUInt64Array)dml).Value;
        }

        public sbyte[] GetI8Array(uint DmlId, sbyte[] Default)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlSByteArray)) throw new FormatException("Attribute type mismatch.");
            return (sbyte[])((DmlSByteArray)dml).Value;
        }

        public Int16[] GetI16Array(uint DmlId, Int16[] Default)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlInt16Array)) throw new FormatException("Attribute type mismatch.");
            return (short[])((DmlInt16Array)dml).Value;
        }

        public Int32[] GetI32Array(uint DmlId, Int32[] Default)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlInt32Array)) throw new FormatException("Attribute type mismatch.");
            return (int[])((DmlInt32Array)dml).Value;
        }

        public Int64[] GetI64Array(uint DmlId, Int64[] Default)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlInt64Array)) throw new FormatException("Attribute type mismatch.");
            return (long[])((DmlInt64Array)dml).Value;
        }

        public DateTime[] GetDateTimeArray(uint DmlId, DateTime[] Default)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlDateTimeArray)) throw new FormatException("Attribute type mismatch.");
            return (DateTime[])((DmlDateTimeArray)dml).Value;
        }

        public string[] GetStringArray(uint DmlId, string[] Default)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlStringArray)) throw new FormatException("Attribute type mismatch.");
            return (string[])((DmlStringArray)dml).Value;
        }

#       if false
        public decimal[] GetDecimalArray(uint DmlId, decimal[] Default)
        {
            DmlPrimitive dml = GetByID(DmlId);
            if (dml == null) return Default;
            if (!(dml is DmlDecimalArray)) throw new FormatException("Attribute type mismatch.");
            return (decimal[])((DmlDecimalArray)dml).Value;
        }
#       endif

        #endregion

        #endregion
    }    

    public class DmlContainer : DmlFragment
    {
        #region "Initialization"

        public DmlContainer() 
        {
            m_Attributes = new DmlAttributes(this);
            Association.DMLName.NodeType = NodeTypes.Container;
        }

        public DmlContainer(DmlDocument Document) : base(Document)
        {
            m_Attributes = new DmlAttributes(this);
            Association.DMLName.NodeType = NodeTypes.Container;
        }

        public DmlContainer(DmlFragment Container) : base(Container) 
        {
            m_Attributes = new DmlAttributes(this);
            Association.DMLName.NodeType = NodeTypes.Container;
        }

        protected DmlContainer(DmlDocument Document, Association Definition)
            : base(Document)
        {
            m_Attributes = new DmlAttributes(this);
            this.Association = Definition;
        }

        protected DmlContainer(DmlFragment Container, Association Definition)
            : base(Container)
        {
            m_Attributes = new DmlAttributes(this);
            this.Association = Definition;
        }

        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlContainer cp = new DmlContainer(NewDoc);
            cp.Association = Association.Clone();
            cp.Loaded = Loaded;
            cp.NextPosition = NextPosition;
            cp.StartPosition = StartPosition;
            foreach (DmlPrimitive Attr in Attributes) cp.Attributes.Add((DmlPrimitive)Attr.Clone(NewDoc));
            foreach (DmlNode Child in Children) cp.Children.Add(Child.Clone(NewDoc));
            return cp;
        }

        #endregion

        #region "Properties"

        protected DmlAttributes m_Attributes;
        public DmlAttributes Attributes
        {
            get
            {
                if (Loaded != LoadState.Attributes && Loaded != LoadState.Full)
                    throw CreateDmlException("Attributes accessed before being loaded.");
                return m_Attributes;
            }
        }        

        /// <summary>Translation retrieves the DML Translation in effect at this container.  DML Containers
        /// may (re)define DML ID associations as they nest, or they may propagate their parent's translation
        /// unaltered.</summary>
        public DmlTranslation ActiveTranslation
        {
            get
            {
                DmlFragment Iter = this;
                while (Iter != null)
                {                    
                    if (Iter.Association.LocalTranslation != null) return Iter.Association.LocalTranslation;
                    Iter = Iter.Container;
                }
                if (Document != null) return Document.GlobalTranslation;
                return null;
            }
        }        

        #endregion

        #region "Operations"

        public void Merge(DmlContainer From)
        {
            base.Merge(From);
            Attributes.AddRange(From.Attributes);
        }

        public void MergeAsAttributes(DmlFragment From)
        {
            for (int ii = 0; ii < From.Children.Count; ii++)
            {
                DmlNode node = From.Children[ii];
                if (!(node is DmlPrimitive)) throw CreateDmlException("Only primitive types are permitted as attributes.");
                Attributes.Add((DmlPrimitive)node);
                ((DmlPrimitive)node).IsAttribute = true;
            }
            From.Children.Clear();
        }

        public void MergeAsChildren(DmlFragment From)
        {
            for (int ii = 0; ii < From.Children.Count; ii++)
                if (From.Children[ii] is DmlPrimitive) ((DmlPrimitive)From.Children[ii]).IsAttribute = false;
            base.Merge(From);
            From.Children.Clear();
        }

        /// <summary>
        /// LoadAttributes() loads only the attributes section.  The loaded state must be IdOnly coming in and will be
        /// either Full or Attributes after completion.
        /// </summary>
        /// <param name="Reader"></param>
        protected void LoadAttributes(DmlReader Reader)
        {
            if (Loaded != LoadState.IdOnly) throw CreateDmlException("LoadAttributes() may only be called when loaded state is IdOnly.");

            for (; ; )
            {
                if (!Reader.Read()) throw CreateDmlException("Unterminated DML container.");

                switch (Reader.NodeType)
                {
                    case NodeTypes.EndContainer:
                        Loaded = LoadState.Full;
                        return;
                    case NodeTypes.EndAttributes:
                        Loaded = LoadState.Attributes;
                        return;
                    case NodeTypes.Padding:
                    case NodeTypes.Comment:
                        throw CreateDmlException("Padding and comments are not permitted as DML attributes.");
                    case NodeTypes.Container:
                        throw CreateDmlException("Containers are not permitted as DML attributes.\nInvalid container/attribute name: " + Reader.Association.DMLName.XmlName);
                    case NodeTypes.Primitive:
                        {
                            DmlPrimitive dml = Document.CreatePrimitive(Reader.PrimitiveType, Reader.ArrayType);
                            dml.Container = this;
                            dml.Association = Reader.Association;
                            dml.LoadContent(Reader);
                            dml.IsAttribute = true;
                            m_Attributes.Add(dml);
                            continue;
                        }
                    default: throw CreateDmlException("Unrecognized node type.");
                }
            }
        }

        /// <summary>
        /// LoadElements() performs a full or partial load of all elements.  Use of the Partial option
        /// requires a seekable stream, since it loads only the location of child elements when possible.
        /// </summary>
        /// <param name="Reader"></param>
        protected override void LoadElements(DmlReader Reader, bool Partial)
        {
            if (Loaded != LoadState.Attributes) throw CreateDmlException("LoadElements() may only be called when loaded state is Attributes.");
            if (Partial && !Reader.CanSeek) throw CreateDmlException("Seekable stream required for partial loading method.");

            for (; ; )
            {
                if (!Reader.Read())
                {
                    Loaded = LoadState.Full;
                    throw CreateDmlException("Unterminated DML container.");
                }

                switch (Reader.NodeType)
                {
                    case NodeTypes.EndContainer:
                        Loaded = LoadState.Full;
                        return;
                    case NodeTypes.EndAttributes:
                        throw CreateDmlException("End-Attributes marker received while parsing elements.");
                    case NodeTypes.Padding:
                        // Padding is set to be discarded by the DmlReader by default.  If it gets to here,
                        // we assume the caller wants it.  First, try merging new padding with previous
                        // padding...
                        DmlPadding pad = Document.CreatePadding();
                        pad.Container = this;
                        pad.LoadContent(Reader);
                        if (m_Children.Count > 0)
                        {
                            DmlPadding PrevPad = m_Children[m_Children.Count - 1] as DmlPadding;
                            if (PrevPad != null) PrevPad.Merge(pad);
                            else m_Children.Add(pad);
                        }
                        else m_Children.Add(pad);
                        continue;
                    case NodeTypes.Comment:
                        {
                            DmlComment dml = Document.CreateComment();
                            dml.Container = this;
                            dml.LoadContent(Reader);
                            m_Children.Add(dml);
                            continue;
                        }
                    case NodeTypes.Container:
                        {
                            DmlContainer dml = Document.CreateContainer(Reader.Association, this);
                            dml.Container = this;
                            dml.Loaded = LoadState.IdOnly;
                            dml.StartPosition = Reader.Container.StartPosition;                                                    
                            if (!Partial)
                            {
                                // Perform a full load of the element...
                                dml.LoadContent(Reader);
                                m_Children.Add(dml);
                                continue;
                            }
                            else
                            {
                                // Check if the element contains navigation so that we can perform a partial load...
                                dml.LoadPartial(Reader, LoadState.Attributes);

                                // Since we are creating the DmlContainer object here it is impossible for it to start out in any state but
                                // LoadState.IdOnly.  However, when we perform the LoadPartial to Attributes, it is possible that we will
                                // encounter an EndContainer marker, and that this container has no elements.  In that instance, it has
                                // already been fully loaded.

                                if (dml.Loaded != LoadState.Full)
                                {
                                    System.Diagnostics.Debug.Assert(dml.Loaded == LoadState.Attributes);
                                    // It was not fully loaded, so there must be elements present.  Next, find out if we can avoid fully
                                    // loading these elements by the presence of a DML:Content-Size attribute.

                                    DmlPrimitive ContentSize = dml.Attributes.GetByID(DML3Translation.idDMLContentSize);
                                    if (ContentSize == null || !(ContentSize is DmlUInt))
                                    {
                                        // No ContentSize attribute, so we are required to load the element.  Or, at least
                                        // we are required to load the next level of the element and look for ContentSize
                                        // attributes on deeper children levels.
                                        dml.LoadPartial(Reader, LoadState.Full);
                                    }
                                    else
                                    {
                                        // As with the above logic and assertion, the container's state must be LoadState.Attributes at this
                                        // time.  When the Loaded state is LoadState.Attributes, the NextPosition value points to the start
                                        // of element content.

                                        // Perform a partial load of the element...
                                        dml.NextPosition = Reader.GetContext();
                                        Reader.SeekRelative(dml.NextPosition, (ulong)(ContentSize.Value));
                                        if (!Reader.Read() || Reader.NodeType != NodeTypes.EndContainer) throw CreateDmlException("Invalid ContentSize indicator, missing End-Container marker, or incomplete structure.");
                                    }
                                }
                                m_Children.Add(dml);
                                continue;
                            }
                        }
                    case NodeTypes.Primitive:
                        {
                            DmlPrimitive dml = Document.CreatePrimitive(Reader.PrimitiveType, Reader.ArrayType);
                            dml.Container = this;
                            dml.Association = Reader.Association;
                            dml.LoadContent(Reader);
                            dml.IsAttribute = false;
                            m_Children.Add(dml);
                            continue;
                        }
                    default: throw CreateDmlException("Unrecognized node type.");
                }
            }
        }

        protected void Validate()
        {
            if (Association == null || Association.DMLName == null)
                throw new Exception("Dml DOM Container must have an association before reading/writing.");
            if (Association.DMLName.NodeType != NodeTypes.Container)
                throw new Exception("Dml DOM Container must have a Container node type association.");
        }

        /// <summary>
        /// LoadContent() will completely load the remainder of the container's content.  If a LoadPartial()
        /// operation has previously been performed on this container, LoadContent() will seek as necessary
        /// and completely load the container and all children.
        /// </summary>
        /// <seealso>Loaded, IsFullyLoaded, LoadPartial()</seealso>
        /// <param name="Reader">DmlReader to read the container from.</param>
        public override void LoadContent(DmlReader Reader)
        {
            GoToPosition(Reader);

            if (Loaded == LoadState.None)
            {
                if (!Reader.Read()) throw CreateDmlException("Expected DML Element Container");
                if (Reader.NodeType != NodeTypes.Container) throw CreateDmlException("Expected DML Element Container");
                Association = Reader.Association;
                Loaded = LoadState.IdOnly;
            }

            if (Loaded == LoadState.IdOnly) { Validate(); LoadAttributes(Reader); }

            if (Loaded == LoadState.Attributes) LoadElements(Reader, false);

            // The LoadContent() call requests that the node be loaded in its entirety - even if it
            // has previously been partially loaded.  To accomplish this, we iterate through all
            // children and ensure they are also recursively fully loaded.
            if (Loaded == LoadState.Full)
            {
                foreach (DmlNode Node in Children)
                {
                    if (Node is DmlFragment && !((DmlFragment)Node).IsFullyLoaded)
                        ((DmlFragment)Node).LoadContent(Reader);
                }
            }
        }

        /// <summary>
        /// LoadPartial() is similar to LoadContent() but provides control over what portion of the container
        /// should be loaded.  LoadPartial() will throw an exception if the stream is not seekable.  LoadPartial() will 
        /// load the section requested by ToLoad, but will generate minimally-loaded DmlContainers whenever 
        /// possible (that is, every time the container provides a data size).  LoadPartial() can then be called
        /// on a child element to load an additional level.  LoadContent() can be called on any of the partially 
        /// loaded Children to fully load that container and all children.
        /// </summary>
        /// <seealso>Loaded, IsFullyLoaded, LoadContent()</seealso>
        /// <param name="Reader">DmlReader providing stream and dml access.</param>
        /// <param name="ToLoad">Section of the container to be loaded.</param>
        public override void LoadPartial(DmlReader Reader, LoadState ToLoad)
        {
            GoToPosition(Reader);
     
            if (Loaded == LoadState.None)
            {
                if (ToLoad != LoadState.None)
                {
                    if (!Reader.Read()) throw CreateDmlException("Expected DML Element Container");
                    if (Reader.NodeType != NodeTypes.Container) throw CreateDmlException("Expected DML Element Container");
                    Association = Reader.Association;                    
                    Loaded = LoadState.IdOnly;
                }
            }

            if (Loaded == LoadState.IdOnly)
            {
                if (ToLoad == LoadState.Attributes || ToLoad == LoadState.Full) { Validate(); LoadAttributes(Reader); }
            }

            if (Loaded == LoadState.Attributes)
            {
                if (ToLoad == LoadState.Full) LoadElements(Reader, true);
            }

            if (Loaded != LoadState.Full) NextPosition = Reader.GetContext();
        }

        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            UInt64 DataSize = GetDataSize(Writer);
            if (InlineIdentification)
                return DataSize + DmlWriter.PredictContainerHeadSize(Name);
            else
                return DataSize + DmlWriter.PredictContainerHeadSize(ID);
        }

        internal UInt64 GetDataSize(DmlWriter Writer)
        {
            UInt64 DataSize = 1;        // Allot one byte for required End-Container marker.
            foreach (DmlPrimitive Attr in Attributes)
            {
                UInt64 AttrSize = Attr.GetEncodedSize(Writer);
                if (AttrSize == UInt64.MaxValue) return UInt64.MaxValue;
                DataSize += AttrSize;
            }
            if (Children.Count > 0) DataSize += 1;      // Allot one byte for required End-Attributes marker.
            foreach (DmlNode Child in Children)
            {
                UInt64 ChildSize = Child.GetEncodedSize(Writer);
                if (ChildSize == UInt64.MaxValue) return UInt64.MaxValue;
                DataSize += ChildSize;
            }
            return DataSize;
        }

        internal UInt64 GetContentSize(DmlWriter Writer)
        {
            UInt64 DataSize = 0;
            foreach (DmlNode Child in Children)
            {
                UInt64 ChildSize = Child.GetEncodedSize(Writer);
                if (ChildSize == UInt64.MaxValue) return UInt64.MaxValue;
                DataSize += ChildSize;
            }
            return DataSize;
        }

        /// <summary>
        /// Enable the WriteContentSize flag in order to automatically generate a DML:Content-Size attribute during
        /// the WriteTo() operation.  All child nodes must implement a GetEncodedSize() operation without returning
        /// UInt64.MaxValue or a NotSupportedException will be generated.
        /// </summary>
        public bool WriteContentSize = false;

        public override void WriteTo(DmlWriter Writer)
        {
            Validate();

            if (WriteContentSize)
            {
                UInt64 ContentSize = GetContentSize(Writer);
                if (ContentSize == UInt64.MaxValue) throw new NotSupportedException("Cannot include DML:Content-Size attribute when one or more child nodes do not provide size estimate.  Disable the WriteContentSize property or enable the child's size estimate");
                DmlPrimitive DmlContentSize = Attributes.GetByID(DML3Translation.idDMLContentSize);
                if (DmlContentSize == null) { DmlContentSize = new DmlUInt(Document); Attributes.Add(DmlContentSize); }
                DmlContentSize.Value = ContentSize;
            }

            if (InlineIdentification)
                Writer.WriteStartContainer(Name);
            else
                Writer.WriteStartContainer(ID);

            foreach (DmlPrimitive Attr in Attributes) Attr.WriteTo(Writer);
            if (Children.Count > 0)
            {
                Writer.WriteEndAttributes();
                foreach (DmlNode Child in Children) Child.WriteTo(Writer);
            }
            Writer.WriteEndContainer();
        }

        #endregion

        #region "AddAttribute(...) routines"

        private Association DefineAttribute(uint DmlID, PrimitiveTypes PType)
        {
            Association Def;
            if (ActiveTranslation == null)
                throw CreateDmlException("Translation must be available to define new attribute by DMLID.");
            if (!ActiveTranslation.TryFind(DmlID, out Def))
                throw CreateDmlException("DML Identifier not found in translation.");
            if (Def.DMLName.NodeType != NodeTypes.Primitive
             || Def.DMLName.PrimitiveType != PType)
                throw CreateDmlException("DML Identifier association does not match required type.");
            return Def;
        }

        private Association DefineAttribute(string Name, PrimitiveTypes PType)
        {
            DmlName dname = new DmlName(Name, PType);
            Association Def;
            if (ActiveTranslation == null || !ActiveTranslation.TryFind(dname, out Def))                            
                return new Association(Name, PType);
            return Def;
        }

        private Association DefineAttribute(uint DmlID, ArrayTypes AType)
        {
            Association Def;
            if (ActiveTranslation == null)
                throw CreateDmlException("Translation must be available to define new attribute by DMLID.");
            if (!ActiveTranslation.TryFind(DmlID, out Def))
                throw CreateDmlException("DML Identifier not found in translation.");
            if (Def.DMLName.NodeType != NodeTypes.Primitive
             || Def.DMLName.PrimitiveType != PrimitiveTypes.Array
             || Def.DMLName.ArrayType != AType)
                throw CreateDmlException("DML Identifier association does not match required type.");
            return Def;
        }

        private Association DefineAttribute(string Name, ArrayTypes AType)
        {
            DmlName dname = new DmlName(Name, PrimitiveTypes.Array, AType);
            Association Def;
            if (ActiveTranslation == null || !ActiveTranslation.TryFind(dname, out Def))
                return new Association(Name, PrimitiveTypes.Array, AType);
            return Def;
        }

        public void AddAttribute(string XMLName, object Value)
        {
            if (string.IsNullOrEmpty(XMLName))
                throw new ArgumentNullException("XMLName", "AddAttribute() parameter XMLName cannot be null or empty.");
            if (Value == null)
                throw new ArgumentNullException("Value", "AddAttribute() parameter Value cannot be null.");

            if (Value is bool) AddAttribute(XMLName, (bool)Value);
            else if (Value is DateTime) AddAttribute(XMLName, (DateTime)Value);
            else if (Value is float) AddAttribute(XMLName, (float)Value);
            else if (Value is double) AddAttribute(XMLName, (double)Value);
            else if (Value is string) AddAttribute(XMLName, (string)Value);
            else if (Value is int) AddAttribute(XMLName, (int)Value);
            else if (Value is long) AddAttribute(XMLName, (long)Value);
            else if (Value is uint) AddAttribute(XMLName, (uint)Value);
            else if (Value is ulong) AddAttribute(XMLName, (ulong)Value);
            else if (Value is byte[]) AddAttribute(XMLName, (byte[])Value);
            else if (Value is ushort[]) AddAttribute(XMLName, (ushort[])Value);
            else if (Value is uint[]) AddAttribute(XMLName, (uint[])Value);
            else if (Value is ulong[]) AddAttribute(XMLName, (ulong[])Value);
            else if (Value is sbyte[]) AddAttribute(XMLName, (sbyte[])Value);
            else if (Value is short[]) AddAttribute(XMLName, (short[])Value);
            else if (Value is int[]) AddAttribute(XMLName, (int[])Value);
            else if (Value is long[]) AddAttribute(XMLName, (long[])Value);
            else if (Value is float[]) AddAttribute(XMLName, (float[])Value);
            else if (Value is double[]) AddAttribute(XMLName, (double[])Value);
            else if (Value is DateTime[]) AddAttribute(XMLName, (DateTime[])Value);
            else if (Value is string[]) AddAttribute(XMLName, (string[])Value);
            else if (Value is byte[,]) AddAttribute(XMLName, (byte[,])Value);
            else if (Value is ushort[,]) AddAttribute(XMLName, (ushort[,])Value);
            else if (Value is uint[,]) AddAttribute(XMLName, (uint[,])Value);
            else if (Value is ulong[,]) AddAttribute(XMLName, (ulong[,])Value);
            else if (Value is sbyte[,]) AddAttribute(XMLName, (sbyte[,])Value);
            else if (Value is short[,]) AddAttribute(XMLName, (short[,])Value);
            else if (Value is int[,]) AddAttribute(XMLName, (int[,])Value);
            else if (Value is long[,]) AddAttribute(XMLName, (long[,])Value);
            else if (Value is float[,]) AddAttribute(XMLName, (float[,])Value);
            else if (Value is double[,]) AddAttribute(XMLName, (double[,])Value);
            else throw new FormatException("Unsupport attribute primitive type: " + Value.GetType().ToString());            
        }

        public void AddAttribute(string XMLName, bool Value)
        {
            Association Def = DefineAttribute(XMLName, PrimitiveTypes.Boolean);
            DmlBool dml = new DmlBool();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(uint DmlId, bool Value)
        {
            Association Def = DefineAttribute(DmlId, PrimitiveTypes.Boolean);
            DmlBool dml = new DmlBool();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(string XMLName, DateTime Value)
        {
            Association Def = DefineAttribute(XMLName, PrimitiveTypes.DateTime);
            DmlDateTime dml = new DmlDateTime();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(uint DmlId, DateTime Value)
        {
            Association Def = DefineAttribute(DmlId, PrimitiveTypes.Boolean);
            DmlDateTime dml = new DmlDateTime();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
#       if false
        public void AddAttribute(string XMLName, decimal Value)
        {
            Association Def = DefineAttribute(XMLName, PrimitiveTypes.Decimal);
            DmlDecimal dml = Document.CreateDecimal();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(uint DmlId, decimal Value)
        {
            Association Def = DefineAttribute(DmlId, PrimitiveTypes.Decimal);
            DmlDecimal dml = Document.CreateDecimal();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
#       endif
        public void AddAttribute(string XMLName, float Value)
        {
            Association Def = DefineAttribute(XMLName, PrimitiveTypes.Single);
            DmlSingle dml = new DmlSingle();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(uint DmlId, float Value)
        {
            Association Def = DefineAttribute(DmlId, PrimitiveTypes.Single);
            DmlSingle dml = new DmlSingle();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(string XMLName, double Value)
        {
            Association Def = DefineAttribute(XMLName, PrimitiveTypes.Double);
            DmlDouble dml = new DmlDouble();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(uint DmlId, double Value)
        {
            Association Def = DefineAttribute(DmlId, PrimitiveTypes.Double);
            DmlDouble dml = new DmlDouble();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(string XMLName, int Value) { AddAttribute(XMLName, (long)Value); }
        public void AddAttribute(string XMLName, long Value)
        {
            Association Def = DefineAttribute(XMLName, PrimitiveTypes.Int);
            DmlInt dml = new DmlInt();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(uint DmlId, int Value) { AddAttribute(DmlId, (long)Value); }
        public void AddAttribute(uint DmlId, long Value)
        {
            Association Def = DefineAttribute(DmlId, PrimitiveTypes.Int);
            DmlInt dml = new DmlInt();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(string XMLName, uint Value) { AddAttribute(XMLName, (ulong)Value); }
        public void AddAttribute(string XMLName, ulong Value)
        {
            Association Def = DefineAttribute(XMLName, PrimitiveTypes.UInt);
            DmlUInt dml = new DmlUInt();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(uint DmlId, uint Value) { AddAttribute(DmlId, (ulong)Value); }
        public void AddAttribute(uint DmlId, ulong Value)
        {
            Association Def = DefineAttribute(DmlId, PrimitiveTypes.UInt);
            DmlUInt dml = new DmlUInt();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(string XMLName, string Value)
        {
            Association Def = DefineAttribute(XMLName, PrimitiveTypes.String);
            DmlString dml = new DmlString();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(uint DmlId, string Value)
        {
            Association Def = DefineAttribute(DmlId, PrimitiveTypes.String);
            DmlString dml = new DmlString();
            dml.Association = Def; dml.Value = Value;
            AddAttribute(dml);  
        }
        public void AddAttribute(string XMLName, byte[] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.U8);
            DmlByteArray dml = new DmlByteArray();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, byte[] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.U8);
            DmlByteArray dml = new DmlByteArray();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, ushort[] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.U16);
            DmlUInt16Array dml = new DmlUInt16Array();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, ushort[] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.U16);
            DmlUInt16Array dml = new DmlUInt16Array();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, uint[] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.U32);
            DmlUInt32Array dml = new DmlUInt32Array();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, uint[] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.U32);
            DmlUInt32Array dml = new DmlUInt32Array();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, ulong[] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.U64);
            DmlUInt64Array dml = new DmlUInt64Array();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, ulong[] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.U64);
            DmlUInt64Array dml = new DmlUInt64Array();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, sbyte[] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.I8);
            DmlSByteArray dml = new DmlSByteArray();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, sbyte[] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.I8);
            DmlSByteArray dml = new DmlSByteArray();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, short[] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.I16);
            DmlInt16Array dml = new DmlInt16Array();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, short[] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.I16);
            DmlInt16Array dml = new DmlInt16Array();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, int[] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.I32);
            DmlInt32Array dml = new DmlInt32Array();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, int[] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.I32);
            DmlInt32Array dml = new DmlInt32Array();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, long[] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.I64);
            DmlInt64Array dml = new DmlInt64Array();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, long[] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.I64);
            DmlInt64Array dml = new DmlInt64Array();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, float[] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.Singles);
            DmlSingleArray dml = new DmlSingleArray();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, float[] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.Singles);
            DmlSingleArray dml = new DmlSingleArray();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, double[] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.Doubles);
            DmlDoubleArray dml = new DmlDoubleArray();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, double[] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.Doubles);
            DmlDoubleArray dml = new DmlDoubleArray();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, string[] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.Strings);
            DmlStringArray dml = new DmlStringArray();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, string[] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.Strings);
            DmlStringArray dml = new DmlStringArray();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, DateTime[] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.DateTimes);
            DmlDateTimeArray dml = new DmlDateTimeArray();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, DateTime[] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.DateTimes);
            DmlDateTimeArray dml = new DmlDateTimeArray();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, byte[,] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.U8);
            DmlByteMatrix dml = new DmlByteMatrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, ushort[,] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.U16);
            DmlUInt16Matrix dml = new DmlUInt16Matrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, ushort[,] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.U16);
            DmlUInt16Matrix dml = new DmlUInt16Matrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, uint[,] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.U32);
            DmlUInt32Matrix dml = new DmlUInt32Matrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, uint[,] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.U32);
            DmlUInt32Matrix dml = new DmlUInt32Matrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, ulong[,] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.U64);
            DmlUInt64Matrix dml = new DmlUInt64Matrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, ulong[,] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.U64);
            DmlUInt64Matrix dml = new DmlUInt64Matrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, sbyte[,] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.I8);
            DmlSByteMatrix dml = new DmlSByteMatrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, sbyte[,] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.I8);
            DmlSByteMatrix dml = new DmlSByteMatrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, short[,] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.I16);
            DmlInt16Matrix dml = new DmlInt16Matrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, short[,] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.I16);
            DmlInt16Matrix dml = new DmlInt16Matrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, int[,] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.I32);
            DmlInt32Matrix dml = new DmlInt32Matrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, int[,] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.I32);
            DmlInt32Matrix dml = new DmlInt32Matrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, long[,] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.I64);
            DmlInt64Matrix dml = new DmlInt64Matrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, long[,] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.I64);
            DmlInt64Matrix dml = new DmlInt64Matrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, float[,] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.Singles);
            DmlSingleMatrix dml = new DmlSingleMatrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, float[,] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.Singles);
            DmlSingleMatrix dml = new DmlSingleMatrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(string XMLName, double[,] Data)
        {
            Association Def = DefineAttribute(XMLName, ArrayTypes.Doubles);
            DmlDoubleMatrix dml = new DmlDoubleMatrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }
        public void AddAttribute(uint DmlId, double[,] Data)
        {
            Association Def = DefineAttribute(DmlId, ArrayTypes.Doubles);
            DmlDoubleMatrix dml = new DmlDoubleMatrix();
            dml.Association = Def; dml.Value = Data;
            AddAttribute(dml);
        }

        public void AddAttribute(DmlPrimitive Attr)
        {
            Attr.IsAttribute = true;
            Attr.Container = this;
            Attr.Document = Document;
            Attributes.Add(Attr);
        }

        #endregion

        #region "SetAttribute(...) by name routines"        

        public void SetAttribute(string XMLName, bool Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlBool) ((DmlBool)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, uint Value) { SetAttribute(XMLName, (ulong)Value); }
        public void SetAttribute(string XMLName, int Value) { SetAttribute(XMLName, (long)Value); }

        public void SetAttribute(string XMLName, ulong Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlUInt) ((DmlUInt)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, long Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlInt) ((DmlInt)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, float Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlSingle) ((DmlSingle)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, double Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlDouble) ((DmlDouble)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

#       if false
        public void SetAttribute(string XMLName, decimal Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlDecimal) ((DmlDecimal)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }
#       endif

        public void SetAttribute(string XMLName, DateTime Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlDateTime) ((DmlDateTime)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, string Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlString) ((DmlString)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, byte[] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlByteArray) ((DmlByteArray)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, ushort[] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlUInt16Array) ((DmlUInt16Array)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, uint[] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlUInt32Array) ((DmlUInt32Array)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, ulong[] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlUInt64Array) ((DmlUInt64Array)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, sbyte[] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlSByteArray) ((DmlSByteArray)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, short[] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlInt16Array) ((DmlInt16Array)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, int[] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlInt32Array) ((DmlInt32Array)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, long[] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlInt64Array) ((DmlInt64Array)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, float[] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlSingleArray) ((DmlSingleArray)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, double[] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlDoubleArray) ((DmlDoubleArray)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, DateTime[] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlDateTimeArray) ((DmlDateTimeArray)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, string[] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlStringArray) ((DmlStringArray)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, byte[,] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlByteArray) ((DmlByteMatrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, ushort[,] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlUInt16Array) ((DmlUInt16Matrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, uint[,] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlUInt32Array) ((DmlUInt32Matrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, ulong[,] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlUInt64Array) ((DmlUInt64Matrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, sbyte[,] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlSByteArray) ((DmlSByteMatrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, short[,] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlInt16Array) ((DmlInt16Matrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, int[,] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlInt32Array) ((DmlInt32Matrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, long[,] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlInt64Array) ((DmlInt64Matrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, float[,] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlSingleArray) ((DmlSingleMatrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }

        public void SetAttribute(string XMLName, double[,] Value)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index < 0) AddAttribute(XMLName, Value);
            else if (Attributes[Index] is DmlDoubleArray) ((DmlDoubleMatrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(XMLName, Value); }
        }        

        #endregion

        #region "SetAttribute(...) by DML ID routines"

        public void SetAttribute(uint DmlId, bool Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlBool) ((DmlBool)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, uint Value) { SetAttribute(DmlId, (ulong)Value); }
        public void SetAttribute(uint DmlId, int Value) { SetAttribute(DmlId, (long)Value); }

        public void SetAttribute(uint DmlId, ulong Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlUInt) ((DmlUInt)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, long Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlInt) ((DmlInt)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, float Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlSingle) ((DmlSingle)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, double Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlDouble) ((DmlDouble)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }        

#       if false
        public void SetAttribute(uint DmlId, decimal Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            if (Attributes[Index] is DmlDecimal) ((DmlDecimal)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }
#       endif

        public void SetAttribute(uint DmlId, DateTime Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlDateTime) ((DmlDateTime)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, string Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlString) ((DmlString)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, byte[] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlByteArray) ((DmlByteArray)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, ushort[] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlUInt16Array) ((DmlUInt16Array)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, uint[] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlUInt32Array) ((DmlUInt32Array)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, ulong[] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlUInt64Array) ((DmlUInt64Array)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, sbyte[] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlSByteArray) ((DmlSByteArray)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, short[] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlInt16Array) ((DmlInt16Array)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, int[] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlInt32Array) ((DmlInt32Array)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, long[] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlInt64Array) ((DmlInt64Array)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, float[] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlSingleArray) ((DmlSingleArray)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, double[] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlDoubleArray) ((DmlDoubleArray)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, DateTime[] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlDateTimeArray) ((DmlDateTimeArray)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, string[] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlStringArray) ((DmlStringArray)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, byte[,] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlByteArray) ((DmlByteMatrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, ushort[,] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlUInt16Array) ((DmlUInt16Matrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, uint[,] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlUInt32Array) ((DmlUInt32Matrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, ulong[,] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlUInt64Array) ((DmlUInt64Matrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, sbyte[,] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlSByteArray) ((DmlSByteMatrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, short[,] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlInt16Array) ((DmlInt16Matrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, int[,] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlInt32Array) ((DmlInt32Matrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, long[,] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlInt64Array) ((DmlInt64Matrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, float[,] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlSingleArray) ((DmlSingleMatrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(uint DmlId, double[,] Value)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index < 0) AddAttribute(DmlId, Value);
            else if (Attributes[Index] is DmlDoubleArray) ((DmlDoubleMatrix)Attributes[Index]).Value = Value;
            else { Attributes.RemoveAt(Index); AddAttribute(DmlId, Value); }
        }

        public void SetAttribute(DmlPrimitive Attr)
        {
            int Index = Attributes.GetIndex(Attr.DmlID);
            if (Index < 0) AddAttribute(Attr);
            else { Attributes.RemoveAt(Index); AddAttribute(Attr); }
        }

        #endregion

        #region "RemoveAttribute(...) routines"

        public void RemoveAttribute(string XMLName)
        {
            int Index = Attributes.GetIndex(XMLName);
            if (Index >= 0) Attributes.RemoveAt(Index);
        }

        public void RemoveAttribute(uint DmlId)
        {
            int Index = Attributes.GetIndex(DmlId);
            if (Index >= 0) Attributes.RemoveAt(Index);
        }

        #endregion

        #region "AddElement(...) routines"

        private Association DefineElement(uint DmlID, PrimitiveTypes PType)
        {
            Association Def;
            if (ActiveTranslation == null)
                throw CreateDmlException("Translation must be available to write element by DMLID.");
            if (!ActiveTranslation.TryFind(DmlID, out Def))
                throw CreateDmlException("DML Identifier not found in translation.");
            if (Def.DMLName.NodeType != NodeTypes.Primitive
             || Def.DMLName.PrimitiveType != PType)
                throw CreateDmlException("DML Identifier association does not match required type.");
            return Def;
        }

        private Association DefineElement(string Name, PrimitiveTypes PType)
        {
            DmlName dname = new DmlName(Name, PType);
            Association Def;
            if (ActiveTranslation == null || !ActiveTranslation.TryFind(dname, out Def))
                return new Association(Name, PType);
            return Def;
        }

        private Association DefineElement(uint DmlID, ArrayTypes AType)
        {
            Association Def;
            if (ActiveTranslation == null)
                throw CreateDmlException("Translation must be available to write element by DMLID.");
            if (!ActiveTranslation.TryFind(DmlID, out Def))
                throw CreateDmlException("DML Identifier not found in translation.");
            if (Def.DMLName.NodeType != NodeTypes.Primitive
             || Def.DMLName.PrimitiveType != PrimitiveTypes.Array
             || Def.DMLName.ArrayType != AType)
                throw CreateDmlException("DML Identifier association does not match required type.");
            return Def;
        }

        private Association DefineElement(string Name, ArrayTypes AType)
        {
            DmlName dname = new DmlName(Name, PrimitiveTypes.Array, AType);
            Association Def;
            if (ActiveTranslation == null || !ActiveTranslation.TryFind(dname, out Def))
                return new Association(Name, PrimitiveTypes.Array, AType);
            return Def;
        }

        public void AddElement(string XMLName, object Value)
        {
            if (string.IsNullOrEmpty(XMLName))
                throw new ArgumentNullException("XMLName", "AddElement() parameter XMLName cannot be null or empty.");
            if (Value == null)
                throw new ArgumentNullException("Value", "AddElement() parameter Value cannot be null.");

            if (Value is bool) AddElement(XMLName, (bool)Value);
            else if (Value is DateTime) AddElement(XMLName, (DateTime)Value);
            else if (Value is float) AddElement(XMLName, (float)Value);
            else if (Value is double) AddElement(XMLName, (double)Value);
            else if (Value is string) AddElement(XMLName, (string)Value);
            else if (Value is int) AddElement(XMLName, (int)Value);
            else if (Value is long) AddElement(XMLName, (long)Value);
            else if (Value is uint) AddElement(XMLName, (uint)Value);
            else if (Value is ulong) AddElement(XMLName, (ulong)Value);
            else if (Value is byte[]) AddElement(XMLName, (byte[])Value);
            else if (Value is ushort[]) AddElement(XMLName, (ushort[])Value);
            else if (Value is uint[]) AddElement(XMLName, (uint[])Value);
            else if (Value is ulong[]) AddElement(XMLName, (ulong[])Value);
            else if (Value is sbyte[]) AddElement(XMLName, (sbyte[])Value);
            else if (Value is short[]) AddElement(XMLName, (short[])Value);
            else if (Value is int[]) AddElement(XMLName, (int[])Value);
            else if (Value is long[]) AddElement(XMLName, (long[])Value);
            else if (Value is float[]) AddElement(XMLName, (float[])Value);
            else if (Value is double[]) AddElement(XMLName, (double[])Value);
            else if (Value is DateTime[]) AddElement(XMLName, (DateTime[])Value);
            else if (Value is string[]) AddElement(XMLName, (string[])Value);
            else if (Value is byte[,]) AddElement(XMLName, (byte[,])Value);
            else if (Value is ushort[,]) AddElement(XMLName, (ushort[,])Value);
            else if (Value is uint[,]) AddElement(XMLName, (uint[,])Value);
            else if (Value is ulong[,]) AddElement(XMLName, (ulong[,])Value);
            else if (Value is sbyte[,]) AddElement(XMLName, (sbyte[,])Value);
            else if (Value is short[,]) AddElement(XMLName, (short[,])Value);
            else if (Value is int[,]) AddElement(XMLName, (int[,])Value);
            else if (Value is long[,]) AddElement(XMLName, (long[,])Value);
            else if (Value is float[,]) AddElement(XMLName, (float[,])Value);
            else if (Value is double[,]) AddElement(XMLName, (double[,])Value);
            else throw new FormatException("Unsupport element primitive type: " + Value.GetType().ToString());
        }

        public void AddElement(string XMLName, bool Value)
        {
            Association Def = DefineElement(XMLName, PrimitiveTypes.Boolean);
            DmlBool dml = new DmlBool();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, bool Value)
        {
            Association Def = DefineElement(DmlId, PrimitiveTypes.Boolean);
            DmlBool dml = new DmlBool();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
        public void AddElement(string XMLName, DateTime Value)
        {
            Association Def = DefineElement(XMLName, PrimitiveTypes.DateTime);
            DmlDateTime dml = new DmlDateTime();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, DateTime Value)
        {
            Association Def = DefineElement(DmlId, PrimitiveTypes.Boolean);
            DmlDateTime dml = new DmlDateTime();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
#       if false
        public void AddElement(string XMLName, decimal Value)
        {
            Association Def = DefineElement(XMLName, PrimitiveTypes.Decimal);
            DmlDecimal dml = Document.CreateDecimal();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);  
        }
        public void AddElement(uint DmlId, decimal Value)
        {
            Association Def = DefineElement(DmlId, PrimitiveTypes.Decimal);
            DmlDecimal dml = Document.CreateDecimal();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);  
        }
#       endif
        public void AddElement(string XMLName, float Value)
        {
            Association Def = DefineElement(XMLName, PrimitiveTypes.Single);
            DmlSingle dml = new DmlSingle();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, float Value)
        {
            Association Def = DefineElement(DmlId, PrimitiveTypes.Single);
            DmlSingle dml = new DmlSingle();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
        public void AddElement(string XMLName, double Value)
        {
            Association Def = DefineElement(XMLName, PrimitiveTypes.Double);
            DmlDouble dml = new DmlDouble();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, double Value)
        {
            Association Def = DefineElement(DmlId, PrimitiveTypes.Double);
            DmlDouble dml = new DmlDouble();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
        public void AddElement(string XMLName, int Value) { AddElement(XMLName, (long)Value); }
        public void AddElement(string XMLName, long Value)
        {
            Association Def = DefineElement(XMLName, PrimitiveTypes.Int);
            DmlInt dml = new DmlInt();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, int Value) { AddElement(DmlId, (long)Value); }
        public void AddElement(uint DmlId, long Value)
        {
            Association Def = DefineElement(DmlId, PrimitiveTypes.Int);
            DmlInt dml = new DmlInt();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
        public void AddElement(string XMLName, uint Value) { AddElement(XMLName, (ulong)Value); }
        public void AddElement(string XMLName, ulong Value)
        {
            Association Def = DefineElement(XMLName, PrimitiveTypes.UInt);
            DmlUInt dml = new DmlUInt();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, uint Value) { AddElement(DmlId, (ulong)Value); }
        public void AddElement(uint DmlId, ulong Value)
        {
            Association Def = DefineElement(DmlId, PrimitiveTypes.UInt);
            DmlUInt dml = new DmlUInt();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
        public void AddElement(string XMLName, string Value)
        {
            Association Def = DefineElement(XMLName, PrimitiveTypes.String);
            DmlString dml = new DmlString();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, string Value)
        {
            Association Def = DefineElement(DmlId, PrimitiveTypes.String);
            DmlString dml = new DmlString();
            dml.Association = Def; dml.Value = Value;
            AddElement(dml);
        }
        public void AddElement(string XMLName, byte[] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.U8);
            DmlByteArray dml = new DmlByteArray();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, byte[] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.U8);
            DmlByteArray dml = new DmlByteArray();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, ushort[] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.U16);
            DmlUInt16Array dml = new DmlUInt16Array();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, ushort[] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.U16);
            DmlUInt16Array dml = new DmlUInt16Array();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, uint[] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.U32);
            DmlUInt32Array dml = new DmlUInt32Array();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, uint[] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.U32);
            DmlUInt32Array dml = new DmlUInt32Array();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, ulong[] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.U64);
            DmlUInt64Array dml = new DmlUInt64Array();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, ulong[] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.U64);
            DmlUInt64Array dml = new DmlUInt64Array();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, sbyte[] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.I8);
            DmlSByteArray dml = new DmlSByteArray();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, sbyte[] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.I8);
            DmlSByteArray dml = new DmlSByteArray();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, short[] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.I16);
            DmlInt16Array dml = new DmlInt16Array();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, short[] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.I16);
            DmlInt16Array dml = new DmlInt16Array();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, int[] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.I32);
            DmlInt32Array dml = new DmlInt32Array();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, int[] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.I32);
            DmlInt32Array dml = new DmlInt32Array();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, long[] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.I64);
            DmlInt64Array dml = new DmlInt64Array();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, long[] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.I64);
            DmlInt64Array dml = new DmlInt64Array();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, float[] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.Singles);
            DmlSingleArray dml = new DmlSingleArray();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, float[] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.Singles);
            DmlSingleArray dml = new DmlSingleArray();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, double[] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.Doubles);
            DmlDoubleArray dml = new DmlDoubleArray();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, double[] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.Doubles);
            DmlDoubleArray dml = new DmlDoubleArray();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, string[] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.Strings);
            DmlStringArray dml = new DmlStringArray();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, string[] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.Strings);
            DmlStringArray dml = new DmlStringArray();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, DateTime[] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.DateTimes);
            DmlDateTimeArray dml = new DmlDateTimeArray();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, DateTime[] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.DateTimes);
            DmlDateTimeArray dml = new DmlDateTimeArray();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, byte[,] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.U8);
            DmlByteMatrix dml = new DmlByteMatrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, byte[,] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.U8);
            DmlByteMatrix dml = new DmlByteMatrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, ushort[,] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.U16);
            DmlUInt16Matrix dml = new DmlUInt16Matrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, ushort[,] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.U16);
            DmlUInt16Matrix dml = new DmlUInt16Matrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, uint[,] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.U32);
            DmlUInt32Matrix dml = new DmlUInt32Matrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, uint[,] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.U32);
            DmlUInt32Matrix dml = new DmlUInt32Matrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, ulong[,] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.U64);
            DmlUInt64Matrix dml = new DmlUInt64Matrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, ulong[,] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.U64);
            DmlUInt64Matrix dml = new DmlUInt64Matrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, sbyte[,] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.I8);
            DmlSByteMatrix dml = new DmlSByteMatrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, sbyte[,] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.I8);
            DmlSByteMatrix dml = new DmlSByteMatrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, short[,] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.I16);
            DmlInt16Matrix dml = new DmlInt16Matrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, short[,] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.I16);
            DmlInt16Matrix dml = new DmlInt16Matrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, int[,] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.I32);
            DmlInt32Matrix dml = new DmlInt32Matrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, int[,] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.I32);
            DmlInt32Matrix dml = new DmlInt32Matrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, long[,] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.I64);
            DmlInt64Matrix dml = new DmlInt64Matrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, long[,] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.I64);
            DmlInt64Matrix dml = new DmlInt64Matrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, float[,] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.Singles);
            DmlSingleMatrix dml = new DmlSingleMatrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, float[,] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.Singles);
            DmlSingleMatrix dml = new DmlSingleMatrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(string XMLName, double[,] Data)
        {
            Association Def = DefineElement(XMLName, ArrayTypes.Doubles);
            DmlDoubleMatrix dml = new DmlDoubleMatrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }
        public void AddElement(uint DmlId, double[,] Data)
        {
            Association Def = DefineElement(DmlId, ArrayTypes.Doubles);
            DmlDoubleMatrix dml = new DmlDoubleMatrix();
            dml.Association = Def; dml.Value = Data;
            AddElement(dml);
        }

        public void AddElement(DmlPrimitive Elm)
        {
            Elm.IsAttribute = false;
            Elm.Container = this;
            Elm.Document = Document;
            Children.Add(Elm);
        }

        #endregion

        #region "GetElements...() routine"

        public List<DmlNode> GetElementsByID(uint DmlID)
        {
            List<DmlNode> ret = new List<DmlNode>();
            foreach (DmlNode elm in Children)
            {
                if (elm.DmlID == DmlID) ret.Add(elm);
            }
            return ret;
        }

        public List<DmlNode> GetElementsByName(string Name)
        {
            List<DmlNode> ret = new List<DmlNode>();
            foreach (DmlNode elm in Children)
            {
                if (elm.Name == Name) ret.Add(elm);
            }
            return ret;
        }

        #endregion
    }

    public class DmlHeader : DmlContainer, ITranslationLanguage
    {
        public DmlHeader()
        {
            Association = DmlTranslation.DML3.Header;
        }

        public DmlHeader(DmlDocument Document) : base(Document)
        {
            Association = DmlTranslation.DML3.Header;
        }

        #region "Alternative Loading"

        public static ResolvedTranslation LoadAndResolve(DmlReader Reader, IResourceResolution Resources)
        {
            DmlHeader Header = new DmlHeader();
            Header.LoadContent(Reader);
            return Header.ToTranslation(Resources, Reader.Options);
        }

        public void LoadFromXml(XmlReader Reader)
        {
            // Check if the caller has already discovered the DML:Header element.  If so, dive right in.
            if (Reader.NodeType == XmlNodeType.Element && (Reader.Name == "DML:Header" || Reader.Name == "Header"))
            {
                if (Reader.IsEmptyElement) return;          // No content in the Header.

                // Parse translation language in header
                DmlTranslationDocument.LoadTSLFromXml(this, Reader);
                return;
            }

            // Find translation document top-level container            
            for (; ; )
            {
                if (!Reader.Read()) throw new FormatException("Expected DML:Header element.");
                if (Reader.NodeType == XmlNodeType.Comment || Reader.NodeType == XmlNodeType.ProcessingInstruction
                 || Reader.NodeType == XmlNodeType.SignificantWhitespace || Reader.NodeType == XmlNodeType.Whitespace
                 || Reader.NodeType == XmlNodeType.XmlDeclaration) continue;
                if (Reader.NodeType != XmlNodeType.Element) throw new FormatException("Expected DML:Header element.");
                if (Reader.Name == "DML:Header" || Reader.Name == "Header")
                {
                    if (Reader.IsEmptyElement) return;              // No content in the Header.

                    // Parse translation language in header
                    DmlTranslationDocument.LoadTSLFromXml(this, Reader);
                    return;
                }
            }
        }
        
        public void LoadFromXml(Stream Stream)
        {
            // Setup an XmlParserContext so that we can pre-define the DML namespace...            
            NameTable nt = new NameTable();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(nt);
            nsmgr.AddNamespace("DML", "urn:dml:dml3");
            XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.None);

            // Set XmlReaderSettings so that multiple top-level elements are allowed (permits an optional DML:Header)...
            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ConformanceLevel = ConformanceLevel.Fragment;   // Permit multiple top-level elements (for DML:Header).

            using (XmlReader Reader = XmlReader.Create(Stream, xrs, context)) LoadFromXml(Reader);            
        }

        #endregion

        #region "Data Properties"

        /// <summary>
        /// Retrieves or sets the DML:Version value for this document.  If a DML document does not contain
        /// this attribute, then the default value is assumed.
        /// </summary>
        public uint Version
        {
            get
            {
                return Attributes.GetUInt(DML3Translation.idDMLVersion, DmlInternalData.DMLVersion);
            }
            set
            {
                SetAttribute(DML3Translation.idDMLVersion, value);
            }
        }

        /// <summary>
        /// Retrieves or sets the DML:ReadVersion value for this document.  If a DML document does not contain
        /// this attribute, then the DML:Version value is assumed.  DML:ReadVersion specifies the minimum DML format
        /// version number required for a reader to process this file correctly.
        /// </summary>
        public uint ReadVersion
        {
            get
            {
                return Attributes.GetUInt(DML3Translation.idDMLReadVersion, Version);
            }
            set
            {
                SetAttribute(DML3Translation.idDMLReadVersion, value);
            }
        }

        /// <summary>
        /// Retrieves or sets the DML:DocType value for this document, which is an informational,
        /// human-readable string that a person can use to identify the general document category.
        /// Automated identification of a DML document occurs through the combination of the
        /// translation and the top-level element, the DocType provides only a backup for diagnostics.
        /// </summary>
        public string DocType
        {
            get
            {
                return Attributes.GetString(DML3Translation.idDMLDocType, "");
            }
            set
            {
                SetAttribute(DML3Translation.idDMLDocType, value);
            }
        }        

        #endregion

        #region "Operations"

        public static DmlHeader CopyFrom(DmlDocument NewDoc, DmlContainer Generic)
        {
            if (Generic.Name != "DML:Header")
                throw new ArgumentException("Expected compatible container.");
            DmlHeader cp = new DmlHeader(NewDoc);
            foreach (DmlPrimitive Attr in Generic.Attributes) cp.Attributes.Add((DmlPrimitive)Attr.Clone(NewDoc));
            foreach (DmlNode Node in Generic.Children) cp.Children.Add(Node.Clone(NewDoc));
            return cp;
        }        

        /// <summary>
        /// Call the ToTranslation() method to interpret the content of the DmlHeader into
        /// a DmlTranslation.  An IResourceResolution object may be provided to retrieve dependencies,
        /// or null can be provided.  An exception is thrown if any inclusions are encountered and
        /// cannot be resolved.
        /// </summary>
        /// <returns>The DmlTranslation and primitives required by this DML Header.</returns>
        public ResolvedTranslation ToTranslation(IResourceResolution References = null, ParsingOptions Options = null)
        {
            if (Loaded != LoadState.Full) throw new NotSupportedException("Cannot convert to a translation until header has been fully loaded.");

            if (Attributes.GetUInt(DML3Translation.idDMLReadVersion,
                    Attributes.GetUInt(DML3Translation.idDMLVersion, DmlInternalData.DMLVersion)) < DmlInternalData.DMLReadVersion)
                throw CreateDmlException("DML version is not supported by this reader.");

            DomResolvedTranslation ret = new DomResolvedTranslation();
            ret.Translation = new DmlTranslation(null);
            ret.Translation.Add(DmlTranslation.DML3);         // A top-level translation should build on DML3.
            ret.PrimitiveSets = new List<PrimitiveSet>();
            ret.XmlRoot = new DmlContainer();
            DmlTranslationDocument.AppendFragmentToTranslation(ret, this, References, Options);
            DmlTranslationDocument.ReducePrimitiveSets(ret.PrimitiveSets);
            return ret;
        }

        /// <summary>
        /// Call AddTranslationReference() to add a top-level reference to an additional
        /// translation document.
        /// </summary>
        /// <param name="TranslationURI">The URI of the translation document to be referenced.</param>
        /// <param name="TranslationURN">Optional URN to provide positive identification of the translation document.</param>
        public void AddTranslationReference(string TranslationURI, string TranslationURN = null)
        {
            DmlContainer Include = Document.CreateContainer(DmlTranslation.TSL2.IncludeTranslation, this);
            Include.AddAttribute(TSL2Translation.idDML_URI, TranslationURI);
            if (TranslationURN != null && TranslationURN.Length > 0) Include.AddAttribute(TSL2Translation.idDML_URN, TranslationURN);            
            Children.Add(Include);
        }

        /// <summary>
        /// Call AddPrimitiveSet() to add a top-level reference to an additional primitive set.  
        /// </summary>
        /// <param name="ps">The primitive set to reference/include by this header.</param>
        public void AddPrimitiveSet(DomPrimitiveSet ps)
        {
            DmlContainer Include = Document.CreateContainer(DmlTranslation.TSL2.IncludePrimitives, this);
            Include.AddAttribute(TSL2Translation.idDMLSet, ps.Set);
            if (ps.Codec != null) Include.AddAttribute(TSL2Translation.idDMLCodec, ps.Codec);            
            if (ps.CodecURI != null) Include.AddAttribute(TSL2Translation.idDMLCodecURI, ps.CodecURI);
            if (ps.Configuration != null)
            {
                foreach (DmlNode ConfigChild in ps.Configuration.Children) Include.Children.Add(ConfigChild.Clone(Document));
            }
            Children.Add(Include);
        }        

        /// <summary>
        /// <para>Call the FromTranslation() method to translate a DmlTranslation object into the DML representation
        /// provided by DmlHeader.  Any existing document content is replaced by the translation representation.</para>
        /// <para>Note that the representation provided by the FromTranslation() method is machine-generated and
        /// may not be the most clear representation of a DML translation.  In particular, any references that
        /// were already resolved are not replaced by &lt;Include&gt; directives that can significantly
        /// simplify a DML translation document.</para>
        /// </summary>
        /// <param name="TSL">The DmlTranslation from which to generate this DmlHeader's content.</param>
        /// <param name="PrimitiveSets">An optional list of primitive sets to be required by this 
        /// DmlHeader.</param>        
        public void FromTranslation(DmlTranslation TSL, List<DomPrimitiveSet> PrimitiveSets = null)
        {
            Children.Clear();

            foreach (Association Assoc in TSL)
            {
                bool IsImplied = false;
                foreach (Association Reserved in DmlTranslation.DML3)
                {
                    if (Assoc.DMLID == Reserved.DMLID)
                    {
                        if (Assoc.DMLName != Reserved.DMLName) throw CreateDmlException("Cannot override a DML ID defined in the DML3 translation.");
                        IsImplied = true;
                        break;
                    }
                }
                if (IsImplied) continue;        // DML3 associations do not need to be written out.

                switch (Assoc.DMLName.NodeType)
                {
                    case NodeTypes.Container: Children.Add(new DmlContainerDefinition(this, Assoc)); continue;
                    case NodeTypes.Primitive: Children.Add(new DmlNodeDefinition(this, Assoc)); continue;
                    default: throw CreateDmlException("Unsupported association in translation document model.");
                }
            }            

            if (PrimitiveSets != null)
            {
                foreach (DomPrimitiveSet ps in PrimitiveSets) AddPrimitiveSet(ps);
            }
        }

        #endregion
    }

    public class DmlComment : DmlNode
    {
        public string Text;

        public DmlComment()
        {
            Association = DmlTranslation.DML3.Comment;
        }

        public DmlComment(DmlDocument Document) : base(Document)
        {
            Association = DmlTranslation.DML3.Comment;
        }

        public override void LoadContent(DmlReader Reader) { Text = Reader.GetComment(); }
        public override void WriteTo(DmlWriter Writer) { Writer.WriteComment(Text); }
        public override UInt64 GetEncodedSize(DmlWriter Writer)
        {
            return DmlWriter.PredictNodeHeadSize(DML3Translation.idDMLComment) + DmlWriter.PredictCommentSize(Text);
        }

        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlComment cp = new DmlComment(NewDoc);
            cp.Association = Association;
            cp.Text = Text;
            return cp;
        }
    }

    public class DmlPadding : DmlNode
    {
        /// <summary>
        /// Length gives the total number of bytes encoded in the padding node.  The Length
        /// includes the size of the node head, the data size (when present), and the padding
        /// bytes (when present).  For single-byte padding nodes Length has a value of 1.
        /// </summary>
        public UInt64 Length;

        public DmlPadding()
        {
            Association = DmlTranslation.DML3.Padding;
        }

        public DmlPadding(DmlDocument Document)
            : base(Document)
        {
            Association = DmlTranslation.DML3.Padding;
        }

        public override void LoadContent(DmlReader Reader) {
            Length = Reader.GetPaddingSize() + 1;
        }
        public void Merge(DmlPadding dp) { Length += dp.Length; }
        public override void WriteTo(DmlWriter Writer) { Writer.WriteReservedSpace(Length); }
        public override UInt64 GetEncodedSize(DmlWriter Writer) { return Length; }
        public override DmlNode Clone(DmlDocument NewDoc)
        {
            DmlPadding cp = new DmlPadding(NewDoc);
            cp.Association = Association;
            cp.Length = Length;
            return cp;
        }
    }
}
