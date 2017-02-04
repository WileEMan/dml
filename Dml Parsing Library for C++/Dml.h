/*	Dml.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
	
	Exactly one compilation unit (.cpp file) in your project must define PrimaryModule before including
	Dml.h, otherwise linker errors will occur.	For example:
	
	#define PrimaryModule 				// This line in only one CPP file.
	#include "Dml.h" 					// This line in all CPP files.
*/

#ifndef __Dml_h__
#define __Dml_h__

#include "Dml_Configuration.h"

/** Support Dependencies **/

#include "Support/Platforms/Platforms.h"
#include "Support/Platforms/Language.h"
#include "Support/Platforms/COM.h"
#include "Support/Exceptions.h"
#include "Support/Matrix.h"
#include "Support/Collections/Iterators.h"
#include "Support/Collections/Pair.h"
#include "Support/Collections/UnorderedMap.h"
#include "Support/Collections/Vector.h"
#include "Support/DateTime/DateTime.h"
#include "Support/DateTime/TimeConstants.h"
#include "Support/DateTime/TimeSpan.h"
#include "Support/IO/EndianBinaryReader.h"
#include "Support/IO/EndianBinaryWriter.h"
#include "Support/IO/FileStream.h"
#include "Support/IO/MemoryStream.h"
#include "Support/IO/Streams.h"
#include "Support/Memory Management/Allocation.h"
#include "Support/Memory Management/Buffer.h"
#include "Support/Parsing/BaseTypeParsing.h"
#include "Support/Parsing/BaseTypeParsing.h"
#include "Support/Parsing/Xml/Xml.h"
#include "Support/Parsing/Xml/XmlParser.h"
#include "Support/Text/Encoding.h"
#include "Support/Text/String.h"
#include "Support/Text/StringComparison.h"
#include "Support/Text/StringConversion.h"

/** Dml Internal Data and Supporting Structures **/

namespace wb
{
	namespace dmltsl
	{
		namespace tsl2
		{
			static MaybeUnused const char* urn = "urn:dml:tsl2";
			static const UInt32 valTSLVersion = 2;
			static const UInt32 valTSLReadVersion = 2;

			static const UInt32 idDMLTranslation = 1140;
			static const UInt32 idDML_URN = 20;

			static const UInt32 idDMLIncludeTranslation = 2;
			static const UInt32 idDML_URI = 21;									

			static const UInt32 idDMLIncludePrimitives = 3;
			static const UInt32 idDMLSet = 31;
			static const UInt32 idDMLCodec = 32;			
			static const UInt32 idDMLCodecURI = 33;
						
			static const UInt32 idContainer = 40;
			static const UInt32 idNode = 41;
			static const UInt32 idName = 42;
			static const UInt32 idDMLID = 43;
			static const UInt32 idType = 44;		
			static const UInt32 idUsage = 45;
			static const UInt32 idRenumber = 46;
			static const UInt32 idNewID = 47;

			static const UInt32 idXMLRoot = 50;
		}

		namespace dml3
		{
			static MaybeUnused const char *urn = "urn:dml:dml3";			

			static const UInt32 idDMLVersion = 1104;
			static const UInt32 idDMLReadVersion = 1105;
			static const UInt32 idDMLDocType = 1106;

			static const UInt32 idDMLHeader = 71619778;			

			static const UInt32 idXMLCDATA = 123;
			static const UInt32 idDMLContentSize = 124;

			static const UInt32 idInlineIdentification = 1088;        
			static const UInt32 idDMLComment = 1089;
			static const UInt32 idDMLPadding = 1090;
			static const UInt32 idDMLPaddingByte = 125;
			static const UInt32 idDMLEndAttributes = 126;
			static const UInt32 idDMLEndContainer = 127;

			static const byte WholeDMLPaddingByte = 0xFD;
		}
	}

	namespace dml
	{
		static const UInt32 DMLVersion = 3;
        static const UInt32 DMLReadVersion = 3;

		extern DateTime ReferenceDate;

		DeclareGenericException(DmlException, S("DML error."));

		enum_class_start(Codecs,int)
		{
			NotLoaded,
			LE,
			BE
		}
		enum_class_end(Codecs);

		enum_class_start(NodeTypes,int)
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
		enum_class_end(NodeTypes)

		enum_class_start(PrimitiveTypes,int)
		{
			Unknown,

			/// <summary>Extension indicates that the primitive is recognized and supported by an extension handler.</summary>
			Extension,
        
			Int,
			UInt,
			Boolean,        
			String,

			DateTime,
			Single,
			Double,

			Decimal,

			Array,
			Matrix,        

			EncryptedDML,
			CompressedDML
		}
		enum_class_end(PrimitiveTypes);

		enum_class_start(ArrayTypes,int)
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
		enum_class_end(ArrayTypes);

		const char* PrimitiveTypeToString(PrimitiveTypes Type, ArrayTypes ArrayType);		
		bool StringToPrimitiveType(string TypeStr, PrimitiveTypes& Type, ArrayTypes& ArrayType);

		const char* NodeTypeToString(NodeTypes NodeType, PrimitiveTypes Type, ArrayTypes ArrayType);

		struct PrimitiveSet
		{
		private:
			bool IsEqual(const vector<byte>& A, const vector<byte>& B)
			{
				if (A.size() != B.size()) return false;
				for (unsigned int ii=0; ii < A.size(); ii++)
					if (A[ii] != B[ii]) return false;
				return true;
			}

		public:
			/// <summary>
			/// Set gives the name of the set of primitives referenced.  For example,
			/// "common" or "arrays".
			/// </summary>
			string Set;

			/// <summary>
			/// Codec provides the name of the codec for encoding and decoding the primitives,
			/// if one is specified.  If the codec is not specified, this string is empty.
			/// </summary>
			string Codec;			

			/// <summary>
			/// CodecURI provides a location to look for software to implement the requested
			/// Codec.  No software should be acquired/installed without proper authorization
			/// and security checks.
			/// </summary>
			string CodecURI;

			/// Note: PrimitiveSet does not store DML Codec configuration information.

			PrimitiveSet() { }

			PrimitiveSet(string _Set, string _Codec = "", string _CodecURI = "")
			{
				Set = _Set;
				Codec = _Codec;
				CodecURI = _CodecURI;
			}

			PrimitiveSet(PrimitiveSet& cp)
				: Set(cp.Set), Codec(cp.Codec), CodecURI(cp.CodecURI)
			{ }

			PrimitiveSet(PrimitiveSet&& mv)
				: Set(std::move(mv.Set)), Codec(std::move(mv.Codec)),
				CodecURI(std::move(mv.CodecURI))
			{ }

			PrimitiveSet& operator=(PrimitiveSet& cp)
			{
				Set = cp.Set;
				Codec = cp.Codec;				
				CodecURI = cp.CodecURI;
				return *this;
			}

			PrimitiveSet& operator=(PrimitiveSet&& mv)
			{
				Set = std::move(mv.Set);
				Codec = std::move(mv.Codec);				
				CodecURI = std::move(mv.CodecURI);
				return *this;
			}

			bool operator==(PrimitiveSet& rhs)
			{
				if (compare_no_case(Set, rhs.Set) != 0) return false;
				if (compare_no_case(Codec, rhs.Codec) != 0) return false;
				if (Codec.length() == 0) return true;
				else
					return compare_no_case(CodecURI, rhs.CodecURI) == 0;
			}

			bool operator!=(PrimitiveSet& rhs)
			{
				return !(operator==(rhs));					
			}
		};

		class Translation;

		class Association
		{					
		public:
			UInt32		DMLID;
			string		Name;
			NodeTypes	NodeType;

			/// <summary>
			/// LocalTranslation provides a Translation if a local subset is defined by use of this node.  
			/// LocalTranslation is null if no local translation subset is defined, in which case the parent's translation
			/// is in effect.  Local translations can only be provided by DML Containers.  If non-null, the pLocalTranslation
			/// object will be deleted when the Association is destroyed.
			/// </summary>
			Translation *pLocalTranslation;

			/// <summary>
			/// PrimitiveType is only relevant for NodeTypes.Primitive.  Otherwise its value should be ignored.
			/// </summary>
			PrimitiveTypes PrimitiveType;

			/// <summary>
			/// ArrayType is only relevant when PrimitiveType is Array or Matrix.
			/// </summary>
			ArrayTypes ArrayType;

			/// <summary>
			/// Extension is only relevant when PrimitiveType is Extension.
			/// </summary>
			// IDmlReaderExtension Extension;

			/// <summary>
			/// TypeId is provided by the Extension for its own recognition purposes.
			/// </summary>
			// uint TypeId = 0;
		
			/// <summary>
			/// InlineIdentification, when true, indicates that the association should be made using inline 
			/// identification at the node.  If false, the association is part of the namespace 
			/// document instead.
			/// </summary>
			bool IsInlineIdentification() const { return DMLID == dmltsl::dml3::idInlineIdentification; }

			/** Constructors for associations in namespace documents **/

			Association(UInt32 _DMLID, string _Name, NodeTypes _NodeType) 
				: DMLID(_DMLID), Name(_Name), NodeType(_NodeType), 
				pLocalTranslation(nullptr), PrimitiveType(PrimitiveTypes::Unknown), ArrayType(ArrayTypes::Unknown) 
			{ }

			Association(UInt32 _DMLID, string _Name, PrimitiveTypes _PrimitiveType)
				: DMLID(_DMLID), Name(_Name), NodeType(NodeTypes::Primitive), 
				pLocalTranslation(nullptr), PrimitiveType(_PrimitiveType), ArrayType(ArrayTypes::Unknown) 
			{ }        

			Association(UInt32 _DMLID, string _Name, PrimitiveTypes _PrimitiveType, ArrayTypes _ArrayType)
				: DMLID(_DMLID), Name(_Name), NodeType(NodeTypes::Primitive), 
				pLocalTranslation(nullptr), PrimitiveType(_PrimitiveType), ArrayType(_ArrayType) 
			{ }
			
			Association(UInt32 _DMLID, string _Name, const Translation& _LocalTranslation);			
			
			// The Association copy constructor creates a clone of the Association including any descendant local
			// translation.  The new object is created "detached" from any parent translation, although lower-level 
			// local translations are properly connected.  Associations are only attached to a translation by the
			// effect of an Translation::Add() call.
			Association(const Association& cp);

			/** Constructors for inline identification **/

			Association(string _Name, NodeTypes _NodeType)
				: DMLID(dmltsl::dml3::idInlineIdentification), Name(_Name), NodeType(_NodeType), 
				pLocalTranslation(nullptr), PrimitiveType(PrimitiveTypes::Unknown), ArrayType(ArrayTypes::Unknown) 
			{
			}

			Association(string _Name, PrimitiveTypes _PrimitiveType)
				: DMLID(dmltsl::dml3::idInlineIdentification), Name(_Name), NodeType(NodeTypes::Primitive), 
				pLocalTranslation(nullptr), PrimitiveType(_PrimitiveType), ArrayType(ArrayTypes::Unknown) 
			{				
			}

			Association(string _Name, PrimitiveTypes _PrimitiveType, ArrayTypes _ArrayType)
				: DMLID(dmltsl::dml3::idInlineIdentification), Name(_Name), NodeType(NodeTypes::Primitive), 
				pLocalTranslation(nullptr), PrimitiveType(_PrimitiveType), ArrayType(_ArrayType) 
			{				
			}        			

			/** Destructor **/

			~Association();			

			/** Comparison **/

			//bool operator==(const Association& rhs);				
			//bool operator!=(const Association& rhs) { return !(operator==(rhs)); }

			/// <summary>IsEqualFast() is similar to operator==(), but performs a reduced check 
			/// for equality.  It verifies the basic information including the size of any child 
			/// translations, but does not descend into parent or child translation.</summary>
//			static bool IsEqualFast(const Association& a, const Association& b);			
		};

		class Translation
		{
			typedef unordered_map<UInt32, Association*> map_type;			

			friend class Association;
			Translation	*pParentTranslation;
			map_type	ByID;

			void Clear()
			{
				for (auto ii = ByID.begin(); ii != ByID.end(); ii++)
					delete ii->second;
				ByID.clear();
			}						

		private:
			// To avoid confusion about the parent pointer, we do not allow an implicit copy.  The move constructor and operator= move are still available.
			Translation(const Translation& cp);		// In C++11, could write = delete here instead.
			Translation& operator=(const Translation& cp);
			/*
			{
				pParentTranslation = cp.pParentTranslation;
				Clear();
				for (auto ii = cp.ByID.begin(); ii != cp.ByID.end(); ii++) ByID.insert(ii->first, new Association(*ii->second));
				return *this;
			}
			*/

			/// <summary>
			/// CloneWithoutParent() generates a deep copy of this Translation, including local translations
			/// that are attached.  The parent translation is set to null for the returned translation 
			/// (that is, it is detached).
			/// </summary>
			/// <returns>An identical copy of the original Translation allocated using new, including 
			/// clones of clones of all associations and their local translations.</returns>
			Translation* CloneWithoutParent() const
			{
				Translation *pCopy = new Translation();

				// First, clone this and "down" the tree.
				for (auto it = ByID.begin(); it != ByID.end(); it++)
				{					
					const Association& Assoc = *it->second;
						// cp.Add() creates a copy of Assoc, and the private Association copy constructor creates a detached copy of 
						// local translations.  cp.Add() then sets the copy's local translation parent to point to this translation.
						// Since the private Association copy constructor can call CloneWithoutParent() for local translation, a
						// mutual recursion occurs here as we descend to any lower levels.
					pCopy->Add(Assoc);
				}

				return pCopy;
			}

		public:
			Translation()
				: pParentTranslation(nullptr)
			{
			}

			/*	pParentTranslation should always be set by the Translation's Add() call.
			Translation(Translation* _pParentTranslation)
				: pParentTranslation(_pParentTranslation)
			{
			}
			*/
			
			Translation(Translation&& mv) { operator=(std::move(mv)); }			

			Translation& operator=(Translation&& mv)
			{
				pParentTranslation = mv.pParentTranslation;
				ByID = std::move(mv.ByID);

				// Unfortunately, all of our child translations have a pParentTranslation pointer
				// targeting mv.  We need to update it.  Not the fastest move operation out there.
				for (auto it = ByID.begin(); it != ByID.end(); it++)
				{
					if (it->second->pLocalTranslation != nullptr)
						it->second->pLocalTranslation->pParentTranslation = this;
				}

				return *this;
			}

			#if 0
			// Note: This comparison is potentially quite slow as it checks every
			// entry against b.  The parentage is not compared.
			bool operator==(const Translation& b)
			{
				if (ByID.size() != b.ByID.size()) return false;

				for (auto it = ByID.begin(); it != ByID.end(); it++)
				{
					auto b_it = b.ByID.find(it->first);					
					if (b_it == b.ByID.end()) return false;
					if (*it->second != *b_it->second) return false;
				}

				return true;
			}

			bool operator!=(const Translation& b) { return !(operator==(b)); }
			#endif

			UInt64 GetCount()
			{
				return ByID.size();
			}

			Translation* GetGlobalTranslation()
			{
				Translation *pIter = this;
				while (pIter->pParentTranslation != nullptr) pIter = pIter->pParentTranslation;
				return pIter;
			}

			/// <summary>
			/// Adds the specified association to the translation.  If the Association is an exact duplicate then no action is taken.  An exception 
			/// is thrown if the DMLID is associated with a different association.  
			/// </summary>
			/// <param name="Assoc">Information to associate</param>
			void Add(const Association& Assoc)
			{
				if (Assoc.pLocalTranslation != nullptr && Assoc.NodeType != NodeTypes::Container)
					throw DmlException(S("Local translations can only be associated with container elements."));
				
				map_type::iterator entry = ByID.find(Assoc.DMLID);
				if (entry != ByID.end())
				{
					if (!IsEqual(entry->second->Name, Assoc.Name)
						|| entry->second->NodeType != Assoc.NodeType
						|| (entry->second->pLocalTranslation != nullptr && Assoc.pLocalTranslation == nullptr)
						|| (entry->second->pLocalTranslation == nullptr && Assoc.pLocalTranslation != nullptr)
						|| (entry->second->pLocalTranslation != nullptr && Assoc.pLocalTranslation != nullptr && entry->second->pLocalTranslation->GetCount() != Assoc.pLocalTranslation->GetCount()))
					{
						#ifdef _DEBUG
						string CompleteTSL = "";
						for (auto it = ByID.begin(); it != ByID.end(); it++)
						{
							CompleteTSL = CompleteTSL + S("\n\tDMLID ") + to_string(it->first) + S(" -> ") + it->second->Name + "::[" + NodeTypeToString(it->second->NodeType, it->second->PrimitiveType, it->second->ArrayType) + "]";
						}
						throw DmlException(S("DML ID conflict in this translation between '" + entry->second->Name + "' and '" + Assoc.Name + "'.  Existing translation contents:" + CompleteTSL));
						#else
						throw DmlException(S("DML ID conflict in this translation between '" + entry->second->Name + "' and '" + Assoc.Name + "'."));
						#endif
					}
					// We haven't exhaustively verified that the entry is identical to the requested Association, but we've checked the basic properties.  The user looks to be
					// adding an association that is already there.
					return;
				}

				Association *pCopy = new Association(Assoc);			// This copy includes cloning any local translation(s), but pCopy will be detached from any parent translation.
				if (pCopy->pLocalTranslation != nullptr) pCopy->pLocalTranslation->pParentTranslation = this;

				map_type::value_type KeyValuePair(Assoc.DMLID, pCopy);
				ByID.insert(KeyValuePair);
			}

			/// <summary>
			/// Adds all associations from the translation into this translation.  Duplicates are ignored.  Copies of all entries are made, including
			/// local translations.
			/// </summary>
			/// <param name="cp">Translation to append to this one</param>
			void Add(const Translation& cp)
			{
				for (auto it = cp.ByID.begin(); it != cp.ByID.end(); it++) Add(*it->second);
			}

			/// <summary>
			/// Renumber() can be used to change the DMLID of an association.  An exception is
			/// thrown if the NewDMLID value is already in use.
			/// </summary>
			/// <param name="DMLID">Dml ID value to change.</param>
			/// <param name="NewDMLID">New Dml ID value to assign for the association.</param>
			void Renumber(uint DMLID, uint NewDMLID)
			{
				map_type::iterator old_entry = ByID.find(DMLID);
				if (old_entry == ByID.end()) 
					throw new Exception("Cannot renumber an identifier that is not listed.");				
				map_type::iterator entry = ByID.find(NewDMLID);
				if (entry != ByID.end())
					throw new Exception("DML ID is already associated in this translation.");				
				ByID.erase(DMLID);
				Association* pAssoc = old_entry->second;
				pAssoc->DMLID = NewDMLID;				
				map_type::value_type KeyValuePair(pAssoc->DMLID, pAssoc);
				ByID.insert(KeyValuePair);
			}

			/// <summary>
			/// Gets the association for the specified DML ID.
			/// </summary>
			/// <param name="DMLID">The ID of the name to retrieve.</param>
			/// <param name="Result">When this method returns, contains the association with the given DMLID if it
			/// exists.  Otherwise contains null.</param>
			/// <returns>True if the Translation contains an association for the given DML ID.</returns>
			bool TryGet(UInt32 DMLID, Association*& pResult) { 
				map_type::iterator entry = ByID.find(DMLID);
				if (entry == ByID.end()) return false;
				pResult = entry->second;
				return true;
			}

			/// <summary>
			/// Gets the association for the specified DML ID, including a search up the translation tree.
			/// </summary>
			/// <param name="DMLID">The ID of the name to retrieve.</param>
			/// <param name="Result">When this method returns, contains the association with the given DMLID if it
			/// exists.  Otherwise contains null.</param>
			/// <returns>True if the Translation or its parents contain an association for the given DML ID.</returns>
			bool TryFind(UInt32 DMLID, Association*& pResult)
			{
				if (TryGet(DMLID, pResult)) return true;
				if (pParentTranslation == nullptr) return false;
				return pParentTranslation->TryFind(DMLID, pResult);
			}

			/*** Builtin/Predefined namespaces ***/

			static Translation DML3;
			static Translation TSL2;
		};

		/*** Inline Functions ***/
		
		inline Association::Association(const Association& cp)
		{
			DMLID = cp.DMLID;
			Name = cp.Name;
			NodeType = cp.NodeType;
			PrimitiveType = cp.PrimitiveType;
			ArrayType = cp.ArrayType;
			if (cp.pLocalTranslation == nullptr) pLocalTranslation = nullptr;
			else pLocalTranslation = cp.pLocalTranslation->CloneWithoutParent();
			// Note that pLocalTranslation->pParentTranslation will be nullptr upon return.  The clone
			// has been created "detached" from any parent.  The parent will be set by Translation.Add()
			// when it is attached to a translation.
		}

		inline Association::Association(UInt32 _DMLID, string _Name, const Translation& _LocalTranslation)
			: DMLID(_DMLID), Name(_Name), NodeType(NodeTypes::Container), 
			PrimitiveType(PrimitiveTypes::Unknown), ArrayType(ArrayTypes::Unknown) 
		{			
			pLocalTranslation = _LocalTranslation.CloneWithoutParent();
		}

		inline Association::~Association()
		{
			if (pLocalTranslation != nullptr)
			{
				delete pLocalTranslation;
				pLocalTranslation = nullptr;
			}
		}

#if 0
		inline bool Association::IsEqualFast(const Association& a, const Association& b)
		{
			if (a.DMLID != b.DMLID) return false;
			if (a.Name.compare(b.Name) != 0) return false;
			if (a.NodeType != b.NodeType) return false;

			if (a.NodeType == NodeTypes::Container)
			{
				if ((a.pLocalTranslation == nullptr && b.pLocalTranslation != nullptr)
					|| (b.pLocalTranslation == nullptr && a.pLocalTranslation != nullptr)) return false;
				if (a.pLocalTranslation != nullptr && a.pLocalTranslation->GetCount() != b.pLocalTranslation->GetCount()) return false;
				return true;
			}
			else if (a.NodeType == NodeTypes::Primitive)
			{
				if (a.PrimitiveType != b.PrimitiveType) return false;
				if (a.PrimitiveType == PrimitiveTypes::Array || a.PrimitiveType == PrimitiveTypes::Matrix)
				{
					if (a.ArrayType != b.ArrayType) return false;
				}					
			}
			else return true;
		}
#endif

		#if 0
		inline bool Association::operator==(const Association& rhs) { 
			if (DMLID != rhs.DMLID || NodeType != rhs.NodeType || Name.compare(rhs.Name) != 0) return false;

			if (NodeType == NodeTypes::Container)
			{
				if (*pLocalTranslation != *rhs.pLocalTranslation) return false;
			}
			else if (NodeType == NodeTypes::Primitive)
			{
				if (PrimitiveType != rhs.PrimitiveType) return false;
				if (PrimitiveType == PrimitiveTypes::Array || PrimitiveType == PrimitiveTypes::Matrix)
				{
					if (ArrayType != rhs.ArrayType) return false;
				}					
			}

			return true;
		}
		#endif
	}

	namespace dmltsl
	{
		namespace tsl2
		{
			using namespace dml;

			extern Association DMLTranslation;

			extern Association URN;
			extern Association IncludeTranslation;
			extern Association URI;

			extern Association IncludePrimitives;
			extern Association Set;
			extern Association Codec;			
			extern Association CodecURI;
			
			extern Association Container;
			extern Association Node;
			extern Association Name;
			extern Association ID;
			extern Association Type;
			extern Association Usage;
			extern Association Renumber;
			extern Association NewID;
			
			extern Association XMLRoot;
		}

		namespace dml3
		{
			using namespace dml;

			extern Association Fragment;
			
			extern Association Version;
			extern Association ReadVersion;
			extern Association DocType;

			extern Association Header;
		
			extern Association ContentSize;

			extern Association InlineIdentification;			
			extern Association Comment;
			extern Association Padding;
			extern Association PaddingByte;			
			extern Association EndAttributes;
			extern Association EndContainer;        			
		}		
	}
}

/** Core Dependencies **/

#include "Core/DmlWriter.h"
#include "Core/DmlReader.h"

// Incorporate DML Support Library modules directly (in only the PrimaryModule compilation unit)
#ifdef PrimaryModule
#include "Core/Dml.cpp"
#include "Core/DmlWriter.cpp"
#include "Support/Exceptions.cpp"
#include "Support/DateTime/DateTime.cpp"
#include "Support/DateTime/TimeSpan.cpp"
#include "Support/Text/Encoding.cpp"
#endif	// if PrimaryModule

#endif	// __Dml_h__

//	End of Dml.h
