#if false

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

namespace WileyBlack.Dml.FileFormats
{
    #if false

    <Packed>
        <Directory>
            <Directory Name="Examples">
                <File Name="Example.cs" Content-Address="1592" id="1" />
                <Chain Content-Address="912835" id="2" />
            </Directory>
    
            <File Name="Readme.txt" Content-Address="123456" id="3" />    
            <File Name="License.txt" Content-Address="125678" id="4" />                        
        </Directory>
    </Packed>

    <File-Content id="1">This should be C# code.</Content>
    <Directory-Content id="2">
        <File Name="InExamples.txt" Content-Address="913456" id="5" />        
    </Directory-Content>
    <File-Content id="3">This is the readme file.</File-Content>    
    <File-Content id="4">
        <Compressed>
            <Encrypted>
                This is another file.  This one is compressed and encrypted.
            </Encrypted>
        </Compressed>
    </File-Content>
    <File-Content id="5">
        This is an example of a file which was appended to the structure.  A reader
        would want to take everything inside the "Directory-Content" container and
        use it to replace the "Chain" container.  A writer would want to leave
        a few bytes of padding at the end of each Directory so that "Chain"
        elements can overwrite the padding later.  The "Directory-Content" container
        is considered out-of-band because it is not written inside the top-level
        container of the file, but that makes it easy to append data to the end of
        the file.
    </File-Content>
#endif

    static private class PackedFileTranslation
    {
        internal static const string uriTranslation = "http://www.optics.arizona.edu/asl/Packed.dml";        

        internal static const uint idPacked = 0x504143;        // PAC file
        internal static const uint idDirectory = 0;
        internal static const uint idFile = 1;
        internal static const uint idChain = 2;
        //internal static const uint idSymbolicLink = 3;
        internal static const uint idUserAttributes = 4;
        internal static const uint idOwnerAttributes = 5;
        internal static const uint idSystemAttributes = 6;
        internal static const uint idExtendedAttributes = 7;

            // Structure referencing information
        internal static const uint idDirectoryContent = 10;
        internal static const uint idFileContent = 11;
        internal static const uint idContentAddress = 15;
        internal static const uint idContentId = 16;

            // Universal file & directory properties
        internal static const uint idName = 20;
        internal static const uint idCreationTimeUtc = 21;
        internal static const uint idLastAccessTimeUtc = 22;
        internal static const uint idLastWriteTimeUtc = 23;
        internal static const uint idLength = 30;               // Only applies to files

            // Windows and common file attributes
        internal static const uint idReadOnly = 60;
        internal static const uint idHidden = 61;
        internal static const uint idSystem = 62;
        internal static const uint idArchive = 63;
        internal static const uint idTemporary = 64;
        internal static const uint idSparseFile = 65;
        internal static const uint idReparsePoint = 66;
        internal static const uint idCompressed = 67;
        internal static const uint idOffline = 68;
        internal static const uint idNotContentIndexed = 69;
        internal static const uint idEncrypted = 70;                
            // Linux ext2 file attributes        
        internal static const uint idAppendOnly = 80;
        internal static const uint idNoAccessTime = 81;     // (A) Don't update access time
        internal static const uint idNoCopyOnWrite = 82;
        internal static const uint idNoDump = 83;
        internal static const uint idSynchronousDirectoryUpdates = 84;
        internal static const uint idImmutable = 85;
        internal static const uint idJournaling = 86;
        internal static const uint idSecureDeletion = 87;
        internal static const uint idSynchronousUpdates = 88;
        internal static const uint idNoTailMerging = 89;
        internal static const uint idUndeletable = 90;
        internal static const uint idOpaque = 91;

        internal static Association PackedAssociation = new Association("Packed", ElementalTypes.Container);
        internal static Association DirectoryAssociation = new Association("Directory", ElementalTypes.Container);
        internal static Association FileAssociation = new Association("File", ElementalTypes.Container);
        internal static Association ChainAssociation = new Association("Chain", ElementalTypes.Container);

        internal static Association[] BuiltinNS
            = {
                  DirectoryAssociation,
                  FileAssociation,

                  new Association(idName, ElementalTypes.Attribute, PrimitiveTypes.String),
                  new Association(idCreationTimeUtc, ElementalTypes.Attribute, PrimitiveTypes.DateTime),
                  new Association(idLastAccessTimeUtc, ElementalTypes.Attribute, PrimitiveTypes.DateTime),
                  new Association(idLastWriteTimeUtc, ElementalTypes.Attribute, PrimitiveTypes.DateTime)

#error TODO
              };

        internal static DmlTranslation Translation = DmlTranslation.CreateFrom(BuiltinNS);
    }

    public abstract class PackedFileSystemInfo : DmlContainer
    {
        public PackedFileSystemInfo(DmlContainer Container, Association Definition)
            : base(Container, Definition)
        {
        }

        public string Name
        {
            get { return (base.Attributes.GetByID(PackedFileTranslation.idName) as DmlString).Value; }
            set { base.SetAttribute(PackedFileTranslation.idName, value); }
        }

        public DateTime CreationTimeUtc
        {
            get { return (base.Attributes.GetByID(PackedFileTranslation.idCreationTimeUtc) as DmlDateTime).Value; }
            set { base.SetAttribute(PackedFileTranslation.idCreationTimeUtc, value); }
        }

        public DateTime LastAccessTimeUtc
        {
            get { return (base.Attributes.GetByID(PackedFileTranslation.idLastAccessTimeUtc) as DmlDateTime).Value; }
            set { base.SetAttribute(PackedFileTranslation.idLastAccessTimeUtc, value); }
        }

        public DateTime LastWriteTimeUtc
        {
            get { return (base.Attributes.GetByID(PackedFileTranslation.idLastWriteTimeUtc) as DmlDateTime).Value; }
            set { base.SetAttribute(PackedFileTranslation.idLastWriteTimeUtc, value); }
        }

        /// <summary>
        /// DmlAttributes provides an alias for DmlContainer's attributes so that Attributes
        /// can provide FileAttributes instead.
        /// </summary>
        protected DmlAttributes DmlAttributes { get { return base.Attributes; } }

        private FileAttributes DirtyFileAttributes = true;
        private FileAttributes FileAttributes;
        public new FileAttributes Attributes
        {
            get 
            {
                if (DirtyFileAttributes)
                {
                    FileAttributes = FileAttributes.Normal;
                    if (base.Attributes.GetBool(PackedFileTranslation.idReadOnly, false)) FileAttributes |= FileAttributes.ReadOnly;
                    if (base.Attributes.GetBool(PackedFileTranslation.idHidden, false)) FileAttributes |= FileAttributes.Hidden;
                    if (base.Attributes.GetBool(PackedFileTranslation.idSystem, false)) FileAttributes |= FileAttributes.System;
                    if (base.Attributes.GetBool(PackedFileTranslation.idArchive, false)) FileAttributes |= FileAttributes.Archive;
                    if (base.Attributes.GetBool(PackedFileTranslation.idTemporary, false)) FileAttributes |= FileAttributes.Temporary;
                    if (base.Attributes.GetBool(PackedFileTranslation.idSparseFile, false)) FileAttributes |= FileAttributes.SparseFile;
                    if (base.Attributes.GetBool(PackedFileTranslation.idReparsePoint, false)) FileAttributes |= FileAttributes.ReparsePoint;
                    if (base.Attributes.GetBool(PackedFileTranslation.idCompressed, false)) FileAttributes |= FileAttributes.Compressed;
                    if (base.Attributes.GetBool(PackedFileTranslation.idOffline, false)) FileAttributes |= FileAttributes.Offline;
                    if (base.Attributes.GetBool(PackedFileTranslation.idNotContentIndexed, false)) FileAttributes |= FileAttributes.NotContentIndexed;
                    if (base.Attributes.GetBool(PackedFileTranslation.idEncrypted, false)) FileAttributes |= FileAttributes.Encrypted;
                    if (IsDirectory) FileAttributes |= FileAttributes.Directory;
                    DirtyFileAttributes = false;
                }
                return FileAttributes; 
            }
            set
            {
                if (value & FileAttributes.ReadOnly) SetAttribute(PackedFileTranslation.idReadOnly, true); else RemoveAttribute(PackedFileTranslation.idReadOnly);
                if (value & FileAttributes.Hidden) SetAttribute(PackedFileTranslation.idHidden, true); else RemoveAttribute(PackedFileTranslation.idHidden);
                if (value & FileAttributes.System) SetAttribute(PackedFileTranslation.idSystem, true); else RemoveAttribute(PackedFileTranslation.idSystem);
                if (value & FileAttributes.Archive) SetAttribute(PackedFileTranslation.idArchive, true); else RemoveAttribute(PackedFileTranslation.idArchive);
                if (value & FileAttributes.Temporary) SetAttribute(PackedFileTranslation.idTemporary, true); else RemoveAttribute(PackedFileTranslation.idTemporary);
                if (value & FileAttributes.SparseFile) SetAttribute(PackedFileTranslation.idSparseFile, true); else RemoveAttribute(PackedFileTranslation.idSparseFile);
                if (value & FileAttributes.ReparsePoint) SetAttribute(PackedFileTranslation.idReparsePoint, true); else RemoveAttribute(PackedFileTranslation.idReparsePoint);
                if (value & FileAttributes.Compressed) SetAttribute(PackedFileTranslation.idCompressed, true); else RemoveAttribute(PackedFileTranslation.idCompressed);
                if (value & FileAttributes.Offline) SetAttribute(PackedFileTranslation.idOffline, true); else RemoveAttribute(PackedFileTranslation.idOffline);
                if (value & FileAttributes.NotContentIndexed) SetAttribute(PackedFileTranslation.idNotContentIndexed, true); else RemoveAttribute(PackedFileTranslation.idNotContentIndexed);
                if (value & FileAttributes.Encrypted) SetAttribute(PackedFileTranslation.idEncrypted, true); else RemoveAttribute(PackedFileTranslation.idEncrypted);
                DirtyFileAttributes = false;
                FileAttributes = value;
            }
        }

        public string Extension
        {
            get { return Path.GetExtension(Name); }
        }

        public PackedDirectoryInfo Directory
        {
            get { return (Container as PackedDirectoryInfo); }
        }

        public abstract bool IsDirectory;
        public abstract bool IsFile;
    }

    public class PackedFileInfo : PackedFileSystemInfo
    {
        public PackedFileInfo(DmlFragment Container)
            : base(Container, PackedFileTranslation.FileAssociation)
        {
        }

        public UInt64 Length
        {
            get { return DmlAttributes.GetULong(PackedFileTranslation.idLength, 0); }
            set { SetAttribute(PackedFileTranslation.idLength, value); }
        }

        public override bool IsDirectory { get { return false; } }
        public override bool IsFile { get { return true; } }

        
    }    

    public class PackedDirectoryInfo : PackedFileInfo
    {
        public PackedDirectoryInfo(DmlFragment Container)
            : base(Container, PackedFileTranslation.DirectoryAssociation)
        {
        }

        public PackedDirectoryInfo[] GetDirectories()
        {
            if (!Loaded) FinishLoad();
        }

        public PackedFileInfo[] GetFiles()
        {
            if (!Loaded) FinishLoad();
        }

        public PackedFileSystemInfo[] GetFileSystemInfos()
        {
            if (!Loaded) FinishLoad();
        }

        public override bool IsDirectory { get { return true; } }
        public override bool IsFile { get { return false; } }

        DmlReader Reader;
        DmlContext Context;
        bool Loaded = false;

        public void StartLoad(DmlReader Reader)
        {
            this.Reader = Reader;
            Document.LoadAttributes(Reader, this);
            this.Context = Reader.GetContext();
        }

        private void ReplaceChain(int iChild, long ContentAddress, int ContentID)
        {
            DmlContext Resume = Reader.SeekOutOfBand(ContentAddress);
            if (!Reader.Read()) throw new EndOfStreamException("Expected additional Directory-Content at chained location.");
            if (Reader.ID != PackedFileTranslation.idDirectoryContent) throw new FormatException("Expected additional Directory-Content at chained location.");
            PackedDirectoryInfo DirectoryContent = new PackedDirectoryInfo(this);
            DirectoryContent.StartLoad(Reader);
            DirectoryContent.FinishLoad();
            Reader.Seek(Resume);

            Children.RemoveAt(iChild);
            Children.InsertRange(iChild, DirectoryContent.Children);
        }

        public void FinishLoad()
        {
            if (Loaded) return;
            Reader.Seek(Context);            
            LoadChildren(Reader);

            // Locate and replace any <Chain> elements with their directory content.
            for (int ii=0; ii < Children.Count; )
            {
                if (Child[ii] is DmlContainer && Child[ii].Association == PackedFileTranslation.ChainAssociation)
                {
                    DmlContainer CChild = (DmlContainer)Child[ii];
                    ReplaceChain(ii,
                        CChild.Attributes.GetUInt(PackedFileTranslation.idContentAddress, 0),
                        CChild.Attributes.GetUInt(PackedFileTranslation.idContentId, 0));
                }
                else ii++;
            }

            Loaded = true;
        }
    }

    public class Packed : DmlDocument
    {
        public PackedDirectoryInfo Directory
        {
            get
            {
                foreach (DmlElemental elm in Children)
                    if (elm is PackedDirectoryInfo) return elm;
            }

            set
            {
                for (int ii = 0; ii < Children.Count;)
                {
                    if (Children[ii] is PackedDirectoryInfo) Children.RemoveAt(ii); else ii++;
                }
                Children.Add(value);
            }
        }

        DmlReader Reader;
        bool LoadPartial;

        public void StartLoad(Stream Source)
        {
            LoadPartial = true;
            if (!Source.CanSeek) throw new Exception("The StartLoad() method requires a seekable stream.  Use the Load() method instead.");

            Reader = DmlReader.Create(Source);
            Reader.GlobalTranslation = PackedFileTranslation.Translation;            
            Header = new DmlHeader(this);
            Header.Load(Reader);
            if (!Reader.Read()) throw new FormatException();
            if (Reader.Association != PackedFileTranslation.PackedAssociation)            
                throw new FormatException("Not a packed file.");
            if (!Reader.Read()) throw new EndOfStreamException();
            if (Reader.Association != PackedFileTranslation.DirectoryAssociation)
                throw new FormatException("No directory found.");
            Children.Clear();
            Children.Add(new PackedDirectoryInfo());
            Children[0].StartLoad(Reader);
        }

        public void Load(Stream Source)
        {
            LoadPartial = false;
            base.Load(Source, PackedFileTranslation.Translation);

            /** In order to Load() without seeking support, we need another step where after we've loaded
             *  the top-level container we continue loading any Directory-Content or File-Content containers
             *  that we encounter and then affix them to the tree appropriately.  The id's will tell us
             *  where they match up to in the tree.
             */
            throw new NotImplementedException();
#           error TODO
        }

        protected override bool LoadElement(DmlReader Reader, DmlFragment Fragment)
        {
            switch (Reader.ElementalType)
            {
                case ElementalTypes.Container:
                    if (Reader.Association == PackedFileTranslation.DirectoryAssociation)
                    {
                        PackedDirectoryInfo container = new PackedDirectoryInfo(Fragment);
                        container.VariableLength = Reader.Container.IsVariableLength;
                        if (LoadPartial) container.StartLoad(Reader); else container.LoadContent(Reader);
                        Fragment.Children.Add(container);
                        return true;
                    }
                    if (Reader.Association == PackedFileTranslation.FileAssociation)
                    {
                        PackedFileInfo container = new PackedFileInfo(Fragment);
                        container.VariableLength = Reader.Container.IsVariableLength;
                        if (LoadPartial) container.StartLoad(Reader); else container.LoadContent(Reader);
                        Fragment.Children.Add(container);
                        return true;
                    }
                    if (Reader.ID == PackedFileTranslation.idFileContent)
                    {
                        // We ignore file-content until a specific file is requested.
                        Reader.MoveOutOfContainer();
                        return true;
                    }
                    break;
            }
            return base.LoadElement(Reader, Fragment);
        }
    }
}

#endif