/*	DmlReader.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
*/

#ifndef __DmlReader_h__
#define __DmlReader_h__

#include "../Support/Platforms/Platforms.h"
#include "../Support/Memory Management/Allocation.h"
#include "../Support/Collections/Vector.h"
#include "../Support/Matrix.h"
#include "../Support/DateTime/DateTime.h"
#include "../Support/IO/EndianBinaryReader.h"
#include "../Support/Text/StringComparison.h"
#include "../Support/Parsing/Xml/XmlParser.h"
#include "../Dml.h"

namespace wb
{
	namespace dml
	{
		using namespace wb;
		using namespace wb::io;
		using namespace wb::memory;

		class DmlReader
		{
			#pragma region "Internal Parsing"

			r_ptr<BinaryReader>	m_pReader;

			/** Internal Parsing Data **/

			/// <summary>
			/// Used with data, encrypted, and decrypted nodes.
			/// </summary>
			// Stream OutstandingStream = null;       

			template <class T> vector<T> GetTemplateArray(ArrayTypes ExpectedArrayType)
			{
				if (GetPrimitiveType() != PrimitiveTypes::Array || GetArrayType() != ExpectedArrayType) throw CreateDmlException("Cannot read array of a different type.");
				if (Options.ArrayCodec == Codecs::NotLoaded) throw CreateDmlException("A codec for the arrays primitive set has not been loaded by the DML stream.");
				m_pReader->IsLittleEndian = (Options.ArrayCodec == Codecs::LE);				

				UInt64 Elements = m_pReader->ReadCompact64();
				if (Elements > size_t_MaxValue) throw CreateDmlException("Array size exceeds platform capacity.");
				vector<T> Value((size_t)Elements);
				m_pReader->Read(&(Value[0]), Elements);
				m_pAssociation = nullptr;
				return Value;
			}

			template <class T> matrix<T> GetTemplateMatrix(ArrayTypes ExpectedMatrixType)
			{
				if (GetPrimitiveType() != PrimitiveTypes::Matrix || GetArrayType() != ExpectedMatrixType) throw CreateDmlException("Cannot read matrix of a different type.");
				if (Options.ArrayCodec == Codecs::NotLoaded) throw CreateDmlException("A codec for the arrays primitive set has not been loaded by the DML stream.");
				m_pReader->IsLittleEndian = (Options.ArrayCodec == Codecs::LE);
				
				UInt64 Columns = m_pReader->ReadCompact64();        // Columns
				UInt64 Rows = m_pReader->ReadCompact64();			// Rows
				if (Columns > size_t_MaxValue || Rows > size_t_MaxValue) throw CreateDmlException("Matrix size exceeds platform capacity.");
				matrix<T> Value((size_t)Rows, (size_t)Columns);
				m_pReader->Read(&(Value(0,0)), Rows * Columns);
				m_pAssociation = nullptr;
				return Value;
			}

			#pragma endregion

		public:			

			#pragma region "Types and Properties"

			/** ParsingOptions **/
			struct ParsingOptions
			{
				/// <summary>
				/// Set DiscardComments to true (default) to instruct the DmlReader to automatically discard comments
				/// without presenting them as a result of a Read() call.  When false, Read() will provide
				/// comments.
				/// </summary>
				bool DiscardComments;

				/// <summary>
				/// Set DiscardPadding to true (default) to instruct the DmlReader to automatically discard padding
				/// without presenting them as a result of a Read() call.  When false, Read() will provide
				/// padding.
				/// </summary>
				bool DiscardPadding;

				Codecs CommonCodec;
				Codecs ArrayCodec;

				#if 0
				internal List<IDmlReaderExtension> Extensions = new List<IDmlReaderExtension>();

				public void AddExtension(IDmlReaderExtension Extension)
				{
					Extensions.Add(Extension);
				}
				#endif

				ParsingOptions()
				{
					DiscardComments = true;
					DiscardPadding = true;
					CommonCodec = Codecs::NotLoaded;
					ArrayCodec = Codecs::NotLoaded;
				}

				ParsingOptions(const ParsingOptions& cp)
					: DiscardComments(cp.DiscardComments), DiscardPadding(cp.DiscardPadding),
					CommonCodec(cp.CommonCodec), ArrayCodec(cp.ArrayCodec)
				{ }
			};

			ParsingOptions Options;

			/** DmlContext **/

			class DmlContext
			{
				friend class DmlReader;

				bool OutOfBand;

				/// <summary>
				/// StartPosition stores the starting position of the container data if it is available (seekable
				/// stream).  It has the value long.MaxValue if the stream is not seekable.
				/// </summary>
				Int64 StartPosition;

				/// <summary>
				/// ContextPosition is used only with specific navigation functions, and only on seekable streams.
				/// </summary>
				Int64 ContextPosition;				

			public:

				/** Responsibility for freeing this object lies with DmlReader, which will free all objects connected
					up the tree.  Since responsibility lies with the DmlReader or other owner of the object, the copy
					of a DmlContext results in a shallow copy of this pointer. **/
				DmlContext *m_pContainer;

				/// <summary>Provides the DML association of this container.</summary>
				r_ptr<Association> m_pAssociation;
				
				DmlContext(const DmlContext& cp)
					: 
					OutOfBand(cp.OutOfBand),
					StartPosition(cp.StartPosition),
					ContextPosition(cp.ContextPosition),
					m_pContainer(cp.m_pContainer),
					m_pAssociation(r_ptr<Association>::responsible(new Association(*cp.m_pAssociation)))
				{
				}
				
				DmlContext()
					: 
					OutOfBand(false),
					StartPosition(Int64_MaxValue),
					ContextPosition(Int64_MaxValue),
					m_pContainer(nullptr),
					m_pAssociation(nullptr)
				{
				}

/*
				DmlContext& operator=(const DmlContext& cp)
				{					
					OutOfBand = cp.OutOfBand;
					StartPosition = cp.StartPosition;					
					ContextPosition = cp.ContextPosition;
					m_pContainer = cp.m_pContainer;
					m_pAssociation = new Association(*cp.m_pAssociation);
					return *this;
				}
				*/

				/// <summary>
				/// Name provides the XML-Compatible name for this container.
				/// </summary>
				string GetName() { return m_pAssociation->Name; }
			};

			#pragma endregion

			#pragma region "State"

			/** State/Properties **/

			/// <summary>Association provides the complete association for the node from the most recent Read() call.</summary>
			/// <comment>Responsibility for freeing the association may or may not lie here as tracked in the r_ptr object.  The
			/// responsibility is transferred to m_pContainer in the case of a container, as the active object will change before
			/// the context is ready to be destroyed.  In the case of a primitive with inline identification, the responsibility 
			/// will be here since the Association was made on-the-fly.  In the case of a named primitive, the responsibility lies
			/// in the translation where the Association is retained.</comment>
			r_ptr<Association> m_pAssociation;

			/// <summary>Provides the properties of the current container.  When the NodeType is NodeTypes::EndContainer this provides 
			/// information on the container being closed.</summary>
			/// <comment>DmlReader has responsibility for freeing this object (if not nullptr) as well as all nested m_pContainer objects.</comment>
			DmlContext* m_pContainer;

			/// <summary>
			/// Provides the numeric DML Identifier of the node from the most recent Read() call.  To locate the XML-Compatible 
			/// text name of an element, see the Name property of the DmlReader class instead.
			/// </summary>
			UInt32 GetID() { return m_pAssociation->DMLID; }									

			/// <summary>
			/// Name provides the XML-Compatible name for the node from the most recent Read() call.
			/// </summary>
			string GetName() { return m_pAssociation->Name; }

			/// <summary>
			/// NodeType identifies the kind of node from the most recent Read() call.  
			/// </summary>
			NodeTypes GetNodeType() { return m_pAssociation->NodeType; }

			/// <summary>
			/// PrimitiveType identifies the data type of an attribute or element.  PrimitiveType can only be 
			/// retrieved when the NodeType is NodeTypes::Primitive.
			/// </summary>
			PrimitiveTypes GetPrimitiveType()
			{
				if (m_pAssociation == nullptr)
					throw CreateDmlException("No node is currently open for reading.");
				if (m_pAssociation->NodeType != NodeTypes::Primitive)
					throw CreateDmlException("The PrimitiveType property is only applicable to primitive type nodes.");
				return m_pAssociation->PrimitiveType;
			}

			/// <summary>
			/// ArrayType identifies the unit primitive of an array attribute or element.  When PrimitiveType is
			/// not an Array or Matrix, ArrayType returns ArrayTypes.Unknown.
			/// </summary>
			ArrayTypes GetArrayType()
			{
				if (GetPrimitiveType() != PrimitiveTypes::Array && GetPrimitiveType() != PrimitiveTypes::Matrix) return ArrayTypes::Unknown;
				return m_pAssociation->ArrayType;
			}

			/// <summary>
			/// When NodeOpen is true, a Read() call has been made and a Get..() call is possible.  When it is false, a Read() call
			/// is allowed but a Get..() call will raise an exception.  NodeOpen (when true) indicates that we have parsed a node's 
			/// head and are waiting to retrieve the node content.
			/// </summary>
			bool IsNodeOpen() { return m_pAssociation != nullptr && (m_pContainer == nullptr || m_pAssociation != m_pContainer->m_pAssociation); }

			/// <summary>
			/// IsAttribute (when true) indicates that the current node is part of the attribute list of the current container.  When
			/// false, the current node is part of the elements list of the current container.
			/// </summary>
			bool IsAttribute;			

			Translation GlobalTranslation;

			#pragma endregion

			#pragma region "Initialization"

			/** Initialization **/			

			DmlReader()
			{
				m_pAssociation = nullptr;
				m_pContainer = nullptr;
				IsAttribute = false;
				GlobalTranslation.Add(Translation::DML3);
			}

			DmlReader(DmlReader&& mv)
				: m_pReader(std::move(mv.m_pReader)),
				Options(std::move(mv.Options)),
				m_pAssociation(std::move(mv.m_pAssociation)),
				m_pContainer(std::move(mv.m_pContainer)),				
				IsAttribute(mv.IsAttribute),
				GlobalTranslation(std::move(mv.GlobalTranslation))
			{
			}

			~DmlReader()
			{
				while (m_pContainer != nullptr)
				{
					DmlContext* pParent = m_pContainer->m_pContainer;
					delete m_pContainer;
					m_pContainer = pParent;
				}
			}

			/** Create() **/

			static DmlReader Create(string Filename)
			{
				DmlReader ret;
				r_ptr<Stream> pBase = r_ptr<Stream>::responsible(new FileStream(Filename.c_str(), FileMode::Open));
				ret.m_pReader = r_ptr<BinaryReader>::responsible(new BinaryReader(std::move(pBase), false));
				return ret;
			}

			static DmlReader Create(string Filename, ParsingOptions Options)
			{
				DmlReader ret;
				r_ptr<Stream> pBase = r_ptr<Stream>::responsible(new FileStream(Filename.c_str(), FileMode::Open));
				ret.m_pReader = r_ptr<BinaryReader>::responsible(new BinaryReader(std::move(pBase), false));
				ret.Options = Options;
				return ret;
			}

			static DmlReader Create(r_ptr<wb::io::Stream>&& input)
			{
				DmlReader ret;
				ret.m_pReader = r_ptr<BinaryReader>::responsible(new BinaryReader(std::move(input), false));
				return ret;
			}

			static DmlReader Create(r_ptr<wb::io::Stream>&& Input, ParsingOptions Options)
			{
				DmlReader ret;
				ret.m_pReader = r_ptr<BinaryReader>::responsible(new BinaryReader(std::move(Input), false));
				ret.Options = Options;
				return ret;
			}

			static DmlReader Create(r_ptr<Stream>&& Input, ParsingOptions Options, DmlContext* pContext)
			{
				DmlReader ret;
				ret.m_pReader = r_ptr<BinaryReader>::responsible(new BinaryReader(std::move(Input), false));
				ret.Options = Options;
				ret.m_pContainer = pContext;
				return ret;
			}
			
			#pragma endregion

			#pragma region "Primitive Sets"

			/** Primitive Sets **/
			
			/// <summary>
			/// Call AddPrimitiveSet() to attempt to enable a primitive set and matching codec that is required for 
			/// the DML document to be read.  If a primitive set or codec is not supported, an exception will be thrown.
			/// </summary>
			/// <param name="SetName">Primitive set required.</param>
			/// <param name="Codec">Codec name for primitive set.</param>
			void AddPrimitiveSet(string SetName, string Codec, string CodecURI)
			{
				SetName = to_lower(SetName);
				Codec = to_lower(Codec);

				if (SetName.compare("base") == 0) return;
				if (SetName.compare("common") == 0)
				{
					if (Codec.compare("le") == 0) { Options.CommonCodec = Codecs::LE; return; }
					if (Codec.compare("be") == 0) { Options.CommonCodec = Codecs::BE; return; }
					throw CreateDmlException("Common primitive set codec is not recognized by reader.");
				}
				if (SetName.compare("ext-precision") == 0) throw CreateDmlException("Extended precision floating-point not supported by reader.");
				if (SetName.compare("decimal-float") == 0) throw CreateDmlException("Base-10 (Decimal) floating-point not supported by reader.");
				if (SetName.compare("arrays") == 0)
				{
					if (Codec.compare("le") == 0) { Options.ArrayCodec = Codecs::LE; return; }
					if (Codec.compare("be") == 0) { Options.ArrayCodec = Codecs::BE; return; }
					throw CreateDmlException("Array/Matrix primitive set codec is not recognized by reader.");
				}					
				if (SetName.compare("decimal-array") == 0) throw CreateDmlException("Decimal floating-point arrays are not supported by reader.");
				if (SetName.compare("dml-ec2") == 0)
				{
					throw CreateDmlException("DML-EC2 is not supported by reader.");
					// if (Codec.ToLower() != "v1") throw CreateDmlException("DML-EC codec is not recognized by reader.");
					return;
				}
				/**					
				try
				{
					foreach (IDmlReaderExtension Ext in Options.Extensions)
					{
						if (Ext.AddPrimitiveSet(SetName, Codec, Configuration, CodecURI)) return;
					}
				}
				catch (Exception& ex) { throw CreateDmlException(ex.Message, ex); }					
				**/
				throw CreateDmlException("Primitive set not recognized by reader.");
			}

			/// <summary>
			/// Call AddPrimitiveSet() to attempt to enable a primitive set and matching codec that is required for 
			/// the DML document to be read.  If a primitive set or codec is not supported, an exception will be thrown.
			/// </summary>
			/// <param name="Set">Primitive set required.</param>        
			void AddPrimitiveSet(PrimitiveSet Set) { AddPrimitiveSet(Set.Set, Set.Codec, Set.CodecURI); }

			#pragma endregion

			#pragma region "Core Read()"

			/** Read() Method **/

			/// <summary>
			/// The Read() function reads the node head for the next node.  The actual contents of the
			/// node are retrieved by the Get..() calls or are skipped if another Read() call is made before
			/// being retrieved.
			/// </summary>
			/// <returns>True if a node was read.  False if the end of the document has been reached.  Throws
			/// exceptions if any format violation occurs.</returns>
			bool Read()
			{
				FinishNode();
				// FinishNode() ensures that m_pAssociation is null upon successful return.
				assert(!IsNodeOpen());

				UInt32 DMLID;
				for (; ; )
				{
					/** Read Identifier **/

					try
					{
						DMLID = m_pReader->ReadCompact32();
					}
					catch (EndOfStreamException&) { return false; }
					catch (std::exception& ex) { throw CreateDmlException(ex.what()); }

					// Handle special cases first...
					switch (DMLID)
					{
					case dmltsl::dml3::idDMLEndAttributes:
						m_pAssociation = r_ptr<Association>::absolved(dmltsl::dml3::EndAttributes);
						return true;

					case dmltsl::dml3::idDMLEndContainer:
						if (m_pContainer == nullptr || m_pContainer->OutOfBand) throw CreateDmlException("Mismatch between opening and closing of containers.");
						m_pAssociation = r_ptr<Association>::absolved(dmltsl::dml3::EndContainer);
						return true;						

					case dmltsl::dml3::idDMLPadding:
						if (Options.DiscardPadding)
						{							
							UInt64 DataSize = m_pReader->ReadCompact64();
							if (DataSize == 0) continue;
							DiscardBytes(DataSize);							
							continue;
						}
						else
						{
							m_pAssociation = r_ptr<Association>::absolved(dmltsl::dml3::Padding);
							return true;
						}

					case dmltsl::dml3::idDMLPaddingByte:
						if (Options.DiscardPadding) continue;						
						else
						{
							m_pAssociation = r_ptr<Association>::absolved(dmltsl::dml3::PaddingByte);
							return true;
						}
					}

					// An EndOfStreamException anywhere beyond this first byte would mean an abruptly terminated
					// stream, which should produce an exception instead of just end-of-file.

					// Otherwise, we need to know the type to know how to decode it.                
					r_ptr<Association> pCurrentAssociation;
					if (DMLID == dmltsl::dml3::idInlineIdentification)
					{
						pCurrentAssociation = ReadIdentificationInformation();			// pCurrentAssociation has responsibility for this pointer.
					}
					else 
					{
						Association* pFound;
						if (!GetActiveTranslation()->TryFind(DMLID, pFound))
							throw CreateDmlException("Association for DMLID 0x" + to_hex_string(DMLID) + " not found in active DML translation.");
						pCurrentAssociation = r_ptr<Association>::absolved(pFound);									// Non-responsible transfer.
					}

					/** Process based on type **/

					switch (pCurrentAssociation->NodeType)
					{
					case NodeTypes::Container:
						{
							DmlContext* pNewContainer = new DmlContext();
							pNewContainer->m_pContainer = m_pContainer;
							pNewContainer->m_pAssociation = std::move(pCurrentAssociation);		// Transfer responsibility.
							if (m_pReader->m_pStream->CanSeek()) pNewContainer->StartPosition = m_pReader->m_pStream->GetPosition();							
							m_pAssociation = r_ptr<Association>::absolved(*pNewContainer->m_pAssociation);					// Make a non-responsible copy.
							m_pContainer = pNewContainer;
							IsAttribute = true;
							return true;
						}

					case NodeTypes::Primitive:
						{
							m_pAssociation = std::move(pCurrentAssociation);
							// if (m_pAssociation->PrimitiveType == PrimitiveTypes::Extension) Association.DMLName.Extension.OpenNode(Association, Reader);
							return true;
						}

					case NodeTypes::Comment:
						{
							if (Options.DiscardComments)
							{
								UInt64 DataSize = m_pReader->ReadCompact64();
								DiscardBytes(DataSize);
								continue;
							}
							m_pAssociation = r_ptr<Association>::absolved(dmltsl::dml3::Comment);
							return true;
						}

					default: throw CreateDmlException("Unrecognized or disallowed node type in translation.");
					}
				}
			}     

			#pragma endregion

			#pragma region "Get...() primitives"

			/** Get..(): Base primitives **/

			/// <summary>Retrieves the value for an unsigned integer primitive.</summary>
			/// <returns>Value</returns>
			UInt64 GetUInt()
			{            
				if (GetPrimitiveType() != PrimitiveTypes::UInt) throw CreateDmlException("Node type does not match Get..() type.");
				UInt64 Value = m_pReader->ReadCompact64();
				m_pAssociation = nullptr;
				return Value;
			}			

			/// <summary>Retrieves the value for a string primitive.</summary>
			/// <returns>Value</returns>
			string GetString()
			{
				try
				{
					if (GetPrimitiveType() != PrimitiveTypes::String) throw CreateDmlException("Node type does not match Get..() type.");
					UInt64 FullLength = m_pReader->ReadCompact64();
					// This is an implementation limitation, not a limitation of DML.  Strings longer than 2^32 are unlikely anyway.
					if (FullLength > Int32_MaxValue) throw CreateDmlException("Strings longer than 2^32 are not supported.");
					int Length = (int)FullLength;
					string ret;
					ret.resize(Length);
					m_pReader->Read(&(ret[0]), Length);					
					m_pAssociation = nullptr;
					return ret;
				}
				catch (DmlException& dex) { throw dex; }
				catch (std::exception& ex) { throw CreateDmlException(ex.what()); }
			}

			/// <summary>Retrieves the value of an array node.</summary>
			/// <returns>Data content</returns>
			vector<byte> GetByteArray() 
			{ 
				if (GetPrimitiveType() != PrimitiveTypes::Array || GetArrayType() != ArrayTypes::U8) throw CreateDmlException("Cannot read array of a different type.");				

				UInt64 Elements = m_pReader->ReadCompact64();
				if (Elements > size_t_MaxValue) throw Exception("DML array size exceeds platform capacity.");
				vector<byte> Value((size_t)Elements);
				m_pReader->Read(&(Value[0]), Elements);
				m_pAssociation = nullptr;
				return Value;
			}

			/** Get..(): Miscellaneous **/

			/// <summary>
			/// Retrieves the text for a comment node.        
			/// </summary>
			/// <returns>Comment text</returns>
			string GetComment()
			{
				try
				{
					if (GetNodeType() != NodeTypes::Comment) throw CreateDmlException("Node type does not match Get..() type.");
					UInt64 FullLength = m_pReader->ReadCompact64();
					// This is an implementation limitation, not a limitation of DML.  Strings longer than 2^32 are unlikely anyway.
					if (FullLength > Int32_MaxValue) throw CreateDmlException("Strings longer than 2^32 are not supported.");
					int Length = (int)FullLength;
					string ret;
					ret.resize(Length);
					m_pReader->Read(&(ret[0]), Length);					
					m_pAssociation = nullptr;
					return ret;
				}
				catch (DmlException& dex) { throw dex; }
				catch (std::exception& ex) { throw CreateDmlException(ex.what()); }
			}

			/// <summary>
			/// Retrieves the size of a padding node, including the bytes used for the data size but not counting 
			/// the node head.  For single-byte padding, returns zero.
			/// </summary>
			/// <returns>The size, in bytes, of the padding content and padding data size indicator.</returns>
			UInt64 GetPaddingSize()
			{				
				if (GetNodeType() != NodeTypes::Padding) throw CreateDmlException("Node type does not match Get..() type.");
				if (GetID() == dmltsl::dml3::idDMLPaddingByte) { m_pAssociation = nullptr; return 0; }
				m_pAssociation = nullptr;
				UInt64 Length = m_pReader->ReadCompact64();
				DiscardBytes(Length);
				return Length + BinaryWriter::SizeCompact64(Length);
			}

			/** Get..(): Common primitives **/

			/// <summary>Retrieves the value for a signed integer primitive.</summary>
			/// <returns>Value</returns>
			Int64 GetInt()
			{
				if (GetPrimitiveType() != PrimitiveTypes::Int) throw CreateDmlException("Node type does not match Get..() type.");
				Int64 Value = m_pReader->ReadCompactS64();
				m_pAssociation = nullptr;
				return Value;
			}			

			/// <summary>Retrieves the value for a boolean primitive.</summary>
			/// <returns>Value</returns>
			bool GetBoolean()
			{
				if (GetPrimitiveType() != PrimitiveTypes::Boolean) throw CreateDmlException("Node type does not match Get..() type.");
				bool Value = (m_pReader->ReadByte() != 0);
				m_pAssociation = nullptr;
				return Value;
			}

			/// <summary>Retrieves the value for a date/time primitive.</summary>
			/// <returns>Value</returns>
			DateTime GetDateTime()
			{            
				if (GetPrimitiveType() != PrimitiveTypes::DateTime) throw CreateDmlException("Node type does not match Get..() type.");
				if (Options.CommonCodec == Codecs::NotLoaded) throw CreateDmlException("A codec for the common primitive set has not been loaded by the DML stream.");
				m_pReader->IsLittleEndian = (Options.CommonCodec == Codecs::LE);
				Int64 Value = m_pReader->ReadInt64();
				m_pAssociation = nullptr;
				return FromNanoseconds(Value);
			}
        
			/// <summary>Retrieves the value for a single-precision floating-point primitive.</summary>
			/// <returns>Value</returns>
			float GetSingle()
			{            
				if (GetPrimitiveType() != PrimitiveTypes::Single) throw CreateDmlException("Node type does not match Get..() type.");
				if (Options.CommonCodec == Codecs::NotLoaded) throw CreateDmlException("A codec for the common primitive set has not been loaded by the DML stream.");
				m_pReader->IsLittleEndian = (Options.CommonCodec == Codecs::LE);
				float Value = m_pReader->ReadSingle();
				m_pAssociation = nullptr;
				return Value;
			}			

			/// <summary>Retrieves the value for a double-precision floating-point primitive.</summary>
			/// <returns>Value</returns>
			double GetDouble()
			{            
				if (GetPrimitiveType() != PrimitiveTypes::Double) throw CreateDmlException("Node type does not match Get..() type.");
				if (Options.CommonCodec == Codecs::NotLoaded) throw CreateDmlException("A codec for the common primitive set has not been loaded by the DML stream.");
				m_pReader->IsLittleEndian = (Options.CommonCodec == Codecs::LE);
				double Value = m_pReader->ReadDouble();
				m_pAssociation = nullptr;
				return Value;
			}
			
			/** Get..(): Array Primitives **/

			/// <summary>Retrieves the value of an array node.</summary>
			/// <returns>Data content</returns>
			vector<UInt16> GetUInt16Array() { return GetTemplateArray<UInt16>(ArrayTypes::U16); }			

			/// <summary>Retrieves the value of an array node.</summary>
			/// <returns>Data content</returns>
			vector<UInt32> GetUInt32Array() { return GetTemplateArray<UInt32>(ArrayTypes::U32); }			

			/// <summary>Retrieves the value of an array node.</summary>
			/// <returns>Data content</returns>
			vector<UInt64> GetUInt64Array() { return GetTemplateArray<UInt64>(ArrayTypes::U64); }

			/// <summary>Retrieves the value of an array node.</summary>
			/// <returns>Data content</returns>
			vector<char> GetInt8Array() { return GetTemplateArray<char>(ArrayTypes::I8); }

			/// <summary>Retrieves the value of an array node.</summary>
			/// <returns>Data content</returns>
			vector<Int16> GetInt16Array() { return GetTemplateArray<Int16>(ArrayTypes::I16); }

			/// <summary>Retrieves the value of an array node.</summary>
			/// <returns>Data content</returns>
			vector<Int32> GetInt32Array() { return GetTemplateArray<Int32>(ArrayTypes::I32); }

			/// <summary>Retrieves the value of an array node.</summary>
			/// <returns>Data content</returns>
			vector<Int64> GetInt64Array() { return GetTemplateArray<Int64>(ArrayTypes::I64); }

			/// <summary>Retrieves the value of an array node.</summary>
			/// <returns>Data content</returns>
			vector<float> GetSingleArray() { return GetTemplateArray<float>(ArrayTypes::Singles); }

			/// <summary>Retrieves the value of an array node.</summary>
			/// <returns>Data content</returns>
			vector<double> GetDoubleArray() { return GetTemplateArray<double>(ArrayTypes::Doubles); }

			/// <summary>Retrieves the value of an array node.</summary>
			/// <returns>Data content</returns>
			vector<DateTime> GetDateTimeArray() 
			{ 
				if (GetPrimitiveType() != PrimitiveTypes::Array || GetArrayType() != ArrayTypes::DateTimes) throw CreateDmlException("Cannot read array of a different type.");				
				if (Options.ArrayCodec == Codecs::NotLoaded) throw CreateDmlException("A codec for the arrays primitive set has not been loaded by the DML stream.");
				m_pReader->IsLittleEndian = (Options.ArrayCodec == Codecs::LE);

				UInt64 Elements = m_pReader->ReadCompact64();
				if (Elements > size_t_MaxValue) throw Exception("DML array size exceeds platform capacity.");
				vector<Int64> RawValue((size_t)Elements);
				m_pReader->Read(&(RawValue[0]), Elements);
				m_pAssociation = nullptr;
				vector<DateTime> Value((size_t)Elements);
				for (size_t ii=0; ii < RawValue.size(); ii++) Value[ii] = FromNanoseconds(RawValue[ii]);
				return Value;
			}

			vector<string> GetStringArray()
			{
				if (GetPrimitiveType() != PrimitiveTypes::Array || GetArrayType() != ArrayTypes::Strings) throw CreateDmlException("Cannot read array of a different type.");				
				if (Options.ArrayCodec == Codecs::NotLoaded) throw CreateDmlException("A codec for the arrays primitive set has not been loaded by the DML stream.");				

				UInt64 NStrings = m_pReader->ReadCompact64();
				vector<string> Value;
				for (UInt64 ii = 0; ii < NStrings; ii++)
				{
					UInt64 NBytes = m_pReader->ReadCompact64();
					if (NBytes > (UInt32)Int32_MaxValue) throw CreateDmlException("String exceeds maximum supported length.");
					string entry;
					entry.resize((UInt32)NBytes);
					m_pReader->Read(&(entry[0]), NBytes);
					Value.push_back(entry);
				}

				m_pAssociation = nullptr;
				return Value;
			}

			/** Get..(): Matrix Primitives **/

			/// <summary>Retrieves the value of a matrix node.</summary>
			/// <returns>Data content</returns>
			matrix<UInt8> GetUInt8Matrix() { return GetTemplateMatrix<UInt8>(ArrayTypes::U8); }			

			/// <summary>Retrieves the value of a matrix node.</summary>
			/// <returns>Data content</returns>
			matrix<UInt16> GetUInt16Matrix() { return GetTemplateMatrix<UInt16>(ArrayTypes::U16); }			

			/// <summary>Retrieves the value of a matrix node.</summary>
			/// <returns>Data content</returns>
			matrix<UInt32> GetUInt32Matrix() { return GetTemplateMatrix<UInt32>(ArrayTypes::U32); }			

			/// <summary>Retrieves the value of a matrix node.</summary>
			/// <returns>Data content</returns>
			matrix<UInt64> GetUInt64Matrix() { return GetTemplateMatrix<UInt64>(ArrayTypes::U64); }

			/// <summary>Retrieves the value of a matrix node.</summary>
			/// <returns>Data content</returns>
			matrix<char> GetInt8Matrix() { return GetTemplateMatrix<char>(ArrayTypes::I8); }

			/// <summary>Retrieves the value of a matrix node.</summary>
			/// <returns>Data content</returns>
			matrix<Int16> GetInt16Matrix() { return GetTemplateMatrix<Int16>(ArrayTypes::I16); }

			/// <summary>Retrieves the value of a matrix node.</summary>
			/// <returns>Data content</returns>
			matrix<Int32> GetInt32Matrix() { return GetTemplateMatrix<Int32>(ArrayTypes::I32); }

			/// <summary>Retrieves the value of a matrix node.</summary>
			/// <returns>Data content</returns>
			matrix<Int64> GetInt64Matrix() { return GetTemplateMatrix<Int64>(ArrayTypes::I64); }

			/// <summary>Retrieves the value of a matrix node.</summary>
			/// <returns>Data content</returns>
			matrix<float> GetSingleMatrix() { return GetTemplateMatrix<float>(ArrayTypes::Singles); }

			/// <summary>Retrieves the value of a matrix node.</summary>
			/// <returns>Data content</returns>
			matrix<double> GetDoubleMatrix() { return GetTemplateMatrix<double>(ArrayTypes::Doubles); }

			#pragma endregion

			#pragma region "GetAs...() primitives with conversion"

			/** GetAs..(): Base primitives with conversion **/			

			/// <summary>
			/// Retrieves the value of the current node with conversion to an unsigned integer.  An exception is thrown if the
			/// node is not an attribute or primitive of a compatible numeric type.
			/// </summary>
			/// <returns>The unsigned integer form of the value.</returns>
			unsigned int GetAsUInt()
			{
				switch (GetPrimitiveType())
				{
				case PrimitiveTypes::Int: return (UInt32)GetInt();
				case PrimitiveTypes::UInt: return (UInt32)GetUInt();
				case PrimitiveTypes::Single: return (UInt32)GetSingle();
				case PrimitiveTypes::Double: return (UInt32)GetDouble();
				//case PrimitiveTypes::Decimal: return (uint)GetDecimal();
				case PrimitiveTypes::Boolean: return GetBoolean() ? 1U : 0U;
				default: throw CreateDmlException("Unable to retrieve as numeric type.");
				}
			}

			/// <summary>
			/// Retrieves the value of the current node with conversion to an unsigned long.  An exception is thrown if the
			/// node is not an attribute or primitive of a compatible numeric type.
			/// </summary>
			/// <returns>The unsigned long form of the value.</returns>
			UInt64 GetAsU64()
			{
				switch (GetPrimitiveType())
				{
				case PrimitiveTypes::Int: return (UInt64)GetInt();
				case PrimitiveTypes::UInt: return (UInt64)GetUInt();
				case PrimitiveTypes::Single: return (UInt64)GetSingle();
				case PrimitiveTypes::Double: return (UInt64)GetDouble();
				// case PrimitiveTypes::Decimal: return (UInt64)GetDecimal();
				case PrimitiveTypes::Boolean: return GetBoolean() ? 1ull : 0ull;
				default: throw CreateDmlException("Unable to retrieve as numeric type.");
				}
			}

			/// <summary>
			/// Retrieves the value of the current node with conversion to an integer.  An exception is thrown if the
			/// node is not an attribute or primitive of a compatible numeric type.
			/// </summary>
			/// <returns>The integer form of the value.</returns>
			int GetAsInt()
			{
				switch (GetPrimitiveType())
				{
				case PrimitiveTypes::Int: return (int)GetInt();
				case PrimitiveTypes::UInt: return (int)GetUInt();
				case PrimitiveTypes::Single: return (int)GetSingle();
				case PrimitiveTypes::Double: return (int)GetDouble();
				// case PrimitiveTypes::Decimal: return (int)GetDecimal();
				case PrimitiveTypes::Boolean: return GetBoolean() ? 1 : 0;
				default: throw CreateDmlException("Unable to retrieve as numeric type.");
				}
			}

			/// <summary>
			/// Retrieves the value of the current node with conversion to a 64-bit integer.  An exception is thrown if the
			/// node is not an attribute or primitive of a compatible numeric type.
			/// </summary>
			/// <returns>The 64-bit integer form of the value.</returns>
			Int64 GetAsI64()
			{
				switch (GetPrimitiveType())
				{
				case PrimitiveTypes::Int: return (Int64)GetInt();
				case PrimitiveTypes::UInt: return (Int64)GetUInt();
				case PrimitiveTypes::Single: return (Int64)GetSingle();
				case PrimitiveTypes::Double: return (Int64)GetDouble();
				// case PrimitiveTypes::Decimal: return (Int64)GetDecimal();
				case PrimitiveTypes::Boolean: return GetBoolean() ? 1L : 0L;
				default: throw CreateDmlException("Unable to retrieve as numeric type.");
				}
			}

			/// <summary>
			/// Retrieves the value of the current node with conversion to a single-precision float.  An exception is thrown if the
			/// node is not an attribute or primitive of a compatible numeric type.
			/// </summary>
			/// <returns>The float form of the value.</returns>
			float GetAsSingle()
			{
				switch (GetPrimitiveType())
				{
				case PrimitiveTypes::Int: return (float)GetInt();
				case PrimitiveTypes::UInt: return (float)GetUInt();
				case PrimitiveTypes::Single: return (float)GetSingle();
				case PrimitiveTypes::Double: return (float)GetDouble();
				// case PrimitiveTypes::Decimal: return (float)GetDecimal();
				case PrimitiveTypes::Boolean: return GetBoolean() ? 1.0f : 0.0f;
				default: throw CreateDmlException("Unable to retrieve as numeric type.");
				}
			}

			/// <summary>
			/// Retrieves the value of the current node with conversion to a double-precision floating-point value.  An exception is thrown if the
			/// node is not an attribute or primitive of a compatible numeric type.
			/// </summary>
			/// <returns>The double form of the value.</returns>
			double GetAsDouble()
			{
				switch (GetPrimitiveType())
				{
				case PrimitiveTypes::Int: return (double)GetInt();
				case PrimitiveTypes::UInt: return (double)GetUInt();
				case PrimitiveTypes::Single: return (double)GetSingle();
				case PrimitiveTypes::Double: return (double)GetDouble();
				// case PrimitiveTypes::Decimal: return (double)GetDecimal();
				case PrimitiveTypes::Boolean: return GetBoolean() ? 1.0 : 0.0;
				default: throw CreateDmlException("Unable to retrieve as numeric type.");
				}
			}

			/// <summary>
			/// Retrieves the value of the current node with conversion to a string.  An exception is thrown if the
			/// node is not an attribute or primitive of a compatible type.
			/// </summary>
			/// <returns>The string representation of the value.</returns>
			string GetAsString()
			{
				switch (GetPrimitiveType())
				{
				case PrimitiveTypes::Int: return to_string(GetInt());
				case PrimitiveTypes::UInt: return to_string(GetUInt());
				case PrimitiveTypes::Single: return to_string(GetSingle());
				case PrimitiveTypes::Double: return to_string(GetDouble());
				// case PrimitiveTypes::Decimal: return to_string(GetDecimal());
				case PrimitiveTypes::Boolean: return to_string(GetBoolean());
				case PrimitiveTypes::DateTime: return to_string(GetDateTime());
				case PrimitiveTypes::String: return GetString();
				//case PrimitiveTypes::Array: 
						// if (ArrayType == ArrayTypes.U8) return Convert.ToBase64String(GetByteArray());
						// else throw CreateDmlException("Unable to retrieve as string type.");
				default: throw CreateDmlException("Unable to retrieve as string type.");
				}
			}

			#pragma endregion

			#pragma region "Navigation"

			/** Navigation **/

			/// <summary>
			/// CanSeek checks whether the underlying stream is capable of seeking.  If the stream can seek,
			/// the MoveOutOfContainer() operation is fast and the GetContext() and Seek...() methods are
			/// available.  If the stream cannot seek, the MoveOutOfContainer() operation is slower and the
			/// GetContext() and Seek...() methods will result in an exception.
			/// </summary>
			bool CanSeek() { return m_pReader->m_pStream->CanSeek(); }

			DmlContext* GetContainer() { return m_pContainer; }

			#if 0		// Should probably be avoided in this form now that ContentSize is a bit of a higher-level behavior.
			/// <summary>
			/// MoveOutOfContainer() permits the caller to navigate the reader out of the present container.
			/// In the case of a seekable, fixed-length container, this operation is very fast.  In other
			/// cases, the operation can be time consuming.
			/// </summary>
			/// <seealso>GetContainer->IsSizeKnown()</seealso>
			void MoveOutOfContainer()
			{
				if (m_pContainer->DataSize == UInt64_MaxValue || !m_pReader->m_pStream->CanSeek())
				{
					// We can parse until we see the end container marker.  We may also see sub-containers
					// within this one, so we have to keep track of what container it is we intend to close.
					DmlContext* pToFinish = m_pContainer;
					while (Read())
					{
						if (GetNodeType() == NodeTypes::EndContainer && m_pContainer == pToFinish) return;
					}
				}
				else
				{
					FinishNode();
					if (m_pContainer == nullptr || m_pContainer->OutOfBand) throw CreateDmlException("Mismatch between opening and closing of containers.");
					m_pReader->m_pStream->Seek(m_pContainer->StartPosition + (Int64)m_pContainer->DataSize, SeekOrigin::Begin);
					DmlContext* pParent = m_pContainer->m_pContainer;
					delete m_pContainer;
					m_pContainer = pParent;
					m_pAssociation = nullptr;
				}
			}
			#endif

			/// <summary>
			/// GetContext() is used with seekable streams in order to navigate.  The returned DmlContext
			/// can be used to return to the current read position, but can also be used in order to
			/// seek to a new position within the same container.  In order to seek in a DML tree, the 
			/// DmlReader must know what context the stream will have at the new position.  Context 
			/// identifies the current container and all higher-level containers in the tree.
			/// </summary>
			/// <returns>A DmlContext that can be used with the Seek..() methods.</returns>
			DmlContext GetContext() {
				FinishNode();
				DmlContext ret(*m_pContainer);
				ret.ContextPosition = m_pReader->m_pStream->GetPosition();
				return ret;
			}

			/// <summary>
			/// The Seek() method returns to a context retrieved by a GetContext() 
			/// call.  The Seek..() methods require a seekable stream.
			/// </summary>
			/// <param name="Context">Context to return to.</param>
			void Seek(const DmlContext& Context)
			{
				if (Context.ContextPosition == Int64_MaxValue)
					SeekAbsolute(Context, (UInt64)Context.StartPosition);
				else
					SeekAbsolute(Context, (UInt64)Context.ContextPosition);
			}

			// <summary>
			/// The SeekAbsolute() method repositions the DmlReader to the given
			/// Position relative to the beginning of the stream.  The Seek..()
			/// methods require a seekable stream.  The position must be part of 
			/// the container belonging to the Context.  That is, the seek must be 
			/// within the same container as the one when the GetContext() call was made.
			/// </summary>
			/// <param name="Context">Context retrieved by GetContext() while reading
			/// the container covering the position to seek to.</param>
			/// <param name="Position">Position to seek to, from beginning of stream.</param>
			void SeekAbsolute(const DmlContext& Context, UInt64 Position)
			{
				FinishNode();           // Clear out the current Association so it does not linger at the new location.
				m_pReader->m_pStream->Seek((Int64)Position, SeekOrigin::Begin);

				// Responsibility for freeing the m_pContainer object and any parent objects lies with DmlReader.  However,
				// portions of the container may be shared by the new context, therefore caution is required.  If there is a
				// common pointer value anywhere in the chain, then they must be pointing to the same place and the remainder
				// up the chain is identical - at that stage no further objects need be deleted.
				DmlContext* pOld = m_pContainer;				
				while (pOld != nullptr)
				{
					bool Retain = false;
					DmlContext* pNew = m_pContainer;
					while (pNew != nullptr)
					{
						if (pNew == pOld) { Retain = true; break; }
						pNew = pNew->m_pContainer;
					}
					if (Retain) break;
					DmlContext* pOldParent = pOld->m_pContainer;
					delete pOld;
					pOld = pOldParent;
				}

				m_pContainer = new DmlContext(Context);
			}

			/// <summary>
			/// The SeekRelative() method repositions the DmlReader to the given
			/// Offset relative to the Context position.  The Seek..() methods require
			/// a seekable stream.  The position must be within the same container as that
			/// belonging to the Context.  
			/// </summary>
			/// <seealso>GetRelativePosition().</seealso>
			/// <param name="Context">Context retrieved by GetContext() while reading
			/// the container covering the position to seek to.</param>
			/// <param name="Offset">Offset relative to the location where the Context
			/// was retrieved.</param>
			void SeekRelative(const DmlContext& Context, UInt64 Offset)
			{
				SeekAbsolute(Context, (UInt64)Context.ContextPosition + Offset);
			}

			/// <summary>
			/// SeekOutOfBand() can be used with seekable streams where data is contained
			/// beyond the end of the top-level container.  This data is out-of-band data
			/// because it is not part of the top-level container, which conceptually 
			/// includes all data in the DML stream.  Data is sometimes written "out-of-band"
			/// in order to make it easy to append data.  The seek occurs under the
			/// assumption that the out-of-band data belongs to the current DmlContext.
			/// That is, the data behaves as if it is part of the current container and tree.
			/// After the out-of-band data is read, Seek(Context) should be used to return
			/// to the previous location before sequential parsing can continue.
			/// </summary>
			/// <seealso>DmlWriter.WriteReservedSpace()</seealso>        
			/// <param name="Position">Position to seek, from beginning of stream.</param>
			/// <returns>The current context that can be used to return to the current
			/// location after the out-of-band data has been read.</returns>
			DmlContext SeekOutOfBand(UInt64 Position)
			{
				FinishNode();           // Clear out the current Association so it does not linger at the new location.
				DmlContext ret = GetContext();
				m_pReader->m_pStream->Seek((Int64)Position, SeekOrigin::Begin);
				
				m_pContainer->OutOfBand = true;
				return ret;
			}

			#pragma endregion

			#pragma region "High-level Header Parsing"

			/** High-Level Header Read Utility **/

		public:

			/// <summary>
			/// The ResourceResolve callback function retrieves or opens the URI specified and returns a
			/// Stream that can be used to parse the resource.  If the URI specifies an XML
			/// document, IsXml should be true.  If it specifies a DML document, IsXml should
			/// be false.
			/// </summary>
			/// <param name="Uri">The URI containing the requested resource.</param>
			/// <param name="IsXml">True if the URI points to an XML document.  False if the URI points to a DML document.</param>
			/// <returns>A smart pointer to a stream that can be used to access the resource.  ResourceResolve should throw an exception if 
			/// the resource cannot be retrieved.  The r_ptr object can take responsibility for freeing the Stream pointer if desired (see
			/// the r_ptr template class.)</returns>
			typedef r_ptr<Stream> (*ResourceResolve)(string Uri, bool& IsXml);

		private:

			Association BuildNewAssociation(UInt32 NewID, string NewName, string NewType)
			{
				PrimitiveTypes PT;
				ArrayTypes AT;

				if (StringToPrimitiveType(NewType, PT, AT)) return Association(NewID, NewName, PT, AT);
				// Might be an extended type.  We'll need to ask the extended codecs to try and identify it.
				/*
				foreach (IDmlReaderExtension Ext in Options.Extensions)
				{
					if (Ext.Identify(NewType) != 0)
						return new Association(NewID, NewName, PrimitiveTypes.Extension);
				}
				*/
				throw CreateDmlException("Unrecognized primitive type '" + NewType + "' requested.");
			}

			void ParseXmlTranslation(Translation& Into, xml::XmlElement& Xml, ResourceResolve ResolutionCallback = nullptr)
			{
				for (uint ii = 0; ii < Xml.Children.size(); ii++)
				{
					xml::XmlNode* pChildNode = Xml.Children[ii];
					if (!pChildNode->IsElement()) continue;
					xml::XmlElement* pChild = (xml::XmlElement*)pChildNode;

					if (IsEqualNoCase(pChild->LocalName, "node"))
					{
						if (!pChild->IsAttrPresent("id")) throw FormatException("Required missing attribute 'id' on node declaration.");
						if (!pChild->IsAttrPresent("name")) throw FormatException("Required missing attribute 'name' on node declaration.");
						if (!pChild->IsAttrPresent("type")) throw FormatException("Required missing attribute 'type' on node declaration.");
						UInt32 NewID = pChild->GetAttrAsUInt32("id");
						string NewName = pChild->GetAttribute("name");
						string NewType = pChild->GetAttribute("type");
						Into.Add(BuildNewAssociation(NewID, NewName, NewType));
						continue;
					}
					else if (IsEqualNoCase(pChild->LocalName, "container"))
					{
						if (!pChild->IsAttrPresent("id")) throw FormatException("Required missing attribute 'id' on container declaration.");
						if (!pChild->IsAttrPresent("name")) throw FormatException("Required missing attribute 'name' on container declaration.");
						UInt32 NewID = pChild->GetAttrAsUInt32("id");
						string NewName = pChild->GetAttribute("name");
						if (pChild->HasChildNodes())
						{
							Translation NewTranslation;
							try
							{
								ParseXmlTranslation(NewTranslation, *pChild, ResolutionCallback);
							}
							catch (std::exception& exc)
							{
								throw Exception(string(exc.what()) + "\n\twhile parsing local translation definition for <" + NewName + "> given in xml.");
							}
							Into.Add(Association(NewID, NewName, NewTranslation));
						}
						else Into.Add(Association(NewID, NewName, NodeTypes::Container));
						continue;
					}
					else if (IsEqualNoCase(pChild->LocalName, "xmlroot"))
					{
						// Ignore.						
						continue;
					}
					else if (IsEqualNoCase(pChild->LocalName, "dml:include-translation") || IsEqualNoCase(pChild->LocalName, "include-translation"))
					{
						string NewTranslationURN = pChild->GetAttribute("URN");
						if (NewTranslationURN.length() == 0) NewTranslationURN = pChild->GetAttribute("DML:URN");
						string NewTranslationURI = pChild->GetAttribute("URI");
						if (NewTranslationURI.length() == 0) NewTranslationURI = pChild->GetAttribute("DML:URI");
						if (NewTranslationURI.length() == 0)
							throw FormatException("Missing required attribute DML:URI on DML:Include-Translation directive.");

						try
						{
							ParseTranslation(Into, NewTranslationURI, NewTranslationURN, ResolutionCallback);
						}
						catch (std::exception& exc)
						{
							throw Exception(string(exc.what()) + "\n\twhile parsing external translation document at URI='" + NewTranslationURI + "'.");
						}
					}
					else if (IsEqualNoCase(pChild->LocalName, "dml:include-primitives") || IsEqualNoCase(pChild->LocalName, "include-primitives"))
					{
						string NewPrimitives = pChild->GetAttribute("Set");
						if (NewPrimitives.length() > 0)
						{
							PrimitiveSet ps(NewPrimitives, pChild->GetAttribute("Codec"));
							ps.CodecURI = pChild->GetAttribute("CodecURI");
							// TODO: Ignores configuration information as this implementation does not support extensions yet.

							AddPrimitiveSet(ps);
						}

						continue;
					}					
					else if (IsEqualNoCase(pChild->LocalName, "renumber"))
					{
						if (!pChild->IsAttrPresent("id")) throw FormatException("Required missing attribute 'id' on renumber directive.");
						if (!pChild->IsAttrPresent("new-id")) throw FormatException("Required missing attribute 'new-id' on renumber directive.");

						Into.Renumber(pChild->GetAttrAsUInt32("id"), pChild->GetAttrAsUInt32("new-id"));
						continue;
					}
					else throw FormatException("Unexpected node '" + pChild->LocalName + "' in translation document.");
				}
			}

			void ParseXmlTranslation(Translation& Into, Stream& Stream, string TranslationURN, ResourceResolve ResolutionCallback = nullptr)
			{				
				xml::XmlParser Parser;
				xml::XmlDocument* pDoc = Parser.Parse(Stream);
				xml::XmlElement* pRoot = pDoc->GetDocumentElement();
				if (pRoot == nullptr) return;
				if (!IsEqualNoCase(pRoot->LocalName, "DML:Translation") && !IsEqualNoCase(pRoot->LocalName, "Translation")) throw FormatException("Expected DML:Translation root element.");
				string StreamURN = pRoot->GetAttrAsString("DML:TranslationURN", "");
				if (StreamURN.length() == 0) StreamURN = pRoot->GetAttrAsString("TranslationURN", "");
				if (StreamURN.length() != 0 && TranslationURN.length() != 0)
				{
					if (!IsEqualNoCase(StreamURN, TranslationURN)) throw Exception("TranslationURN does not match retrieved resources.");
				}
								
				// Parse translation document body
				ParseXmlTranslation(Into, *pRoot, ResolutionCallback);

				delete pDoc;
			}

			/// <summary>Reads until an EndContainer marker is found.  If additional Container markers are found, recurses
			/// such that they are also skipped.</summary>
			void SkipContainer(DmlReader& Reader)
			{
				for (; ; )
				{
					if (!Reader.Read()) throw Reader.CreateDmlException("Unterminated container.");

					switch (Reader.GetNodeType())
					{
					case NodeTypes::EndContainer: return;
					case NodeTypes::Container: SkipContainer(Reader); continue;
					default: continue;
					}
				}
			}

			// Precondition: We must have already parsed the EndAttributes marker of the DML:Header or DML:Translation tag
			// and be ready to process the container.
			// Postcondition: Will have processed the EndContainer marker before returning.
			void ParseDmlTranslation(Translation& Into, DmlReader& Reader, ResourceResolve ResolutionCallback = nullptr)
			{
				// Parse translation document body
				for (; ; )
				{
					if (!Reader.Read()) throw Reader.CreateDmlException("Unterminated container.");

					switch (Reader.GetNodeType())
					{
					case NodeTypes::EndContainer: return;
					case NodeTypes::Comment: continue;
					case NodeTypes::Padding: continue;
					case NodeTypes::Container:
						{
							/** Parse XMLRoot element if found **/

							if (Reader.GetID() == dmltsl::tsl2::idXMLRoot)
							{
								// Skip over the XMLRoot element.
								for (; ; )
								{
									if (!Reader.Read()) throw Reader.CreateDmlException("Unterminated XMLRoot container in translation document.");
									if (Reader.GetNodeType() == NodeTypes::EndContainer) break;
								}
								continue;
							}

							/** Parse DML:Include-Translation container if found **/

							if (Reader.GetID() == dmltsl::tsl2::idDMLIncludeTranslation)
							{
								string NewTranslationURI;                                
								string NewTranslationURN;								

								for (; ; )
								{
									if (!Reader.Read()) throw Reader.CreateDmlException("Unterminated DML:Include-Translation directive in translation document.");

									switch (Reader.GetNodeType())
									{
									case NodeTypes::Comment: continue;
									case NodeTypes::Padding: continue;
									case NodeTypes::EndContainer: break;
									case NodeTypes::EndAttributes: continue;
									case NodeTypes::Primitive:
										switch (Reader.GetID())
										{
										case dmltsl::tsl2::idDML_URI: NewTranslationURI = Reader.GetString(); break;
										case dmltsl::tsl2::idDML_URN: NewTranslationURN = Reader.GetString(); break;
										default:
											throw Reader.CreateDmlException("Unrecognized primitive when parsing DML:Include-Translation directive in translation document.");
										}
										continue;
									default:
										throw Reader.CreateDmlException("Unexpected node type when parsing DML:Include-Translation directive in translation document.");
									}
									break;
								}

								if (NewTranslationURI.length() > 0 || NewTranslationURN.length() > 0)
								{
									try
									{
										ParseTranslation(Into, NewTranslationURI, NewTranslationURN, ResolutionCallback);								
									}
									catch (std::exception& exc)
									{
										throw Exception(string(exc.what()) + "\n\twhile parsing external translation document at URI='" + NewTranslationURI + "'.");
									}
								}

								continue;
							}

							/** Parse DML:Include-Primitives container if found **/

							if (Reader.GetID() == dmltsl::tsl2::idDMLIncludePrimitives)
							{
								string NewPrimitives;
								string NewCodec;
								string NewCodecURI;

								for (; ; )
								{
									if (!Reader.Read()) throw Reader.CreateDmlException("Unterminated DML:Include-Primitives directive in translation document.");

									switch (Reader.GetNodeType())
									{
									case NodeTypes::Comment: continue;
									case NodeTypes::Padding: continue;
									case NodeTypes::EndContainer: break;
									case NodeTypes::EndAttributes: 
										// TODO: Currently, we discard primitive configuration information because extensions are not supported.
										SkipContainer(Reader); 
										break;
									case NodeTypes::Primitive:
										switch (Reader.GetID())
										{
										case dmltsl::tsl2::idDMLSet: NewPrimitives = Reader.GetString(); break;
										case dmltsl::tsl2::idDMLCodec: NewCodec = Reader.GetString(); break;
										case dmltsl::tsl2::idDMLCodecURI: NewCodecURI = Reader.GetString(); break;
										default:
											throw Reader.CreateDmlException("Unrecognized primitive when parsing DML:Include directive in translation document.");
										}
										continue;
									default:
										throw Reader.CreateDmlException("Unexpected node type when parsing DML:Include directive in translation document.");
									}
									break;
								}

								if (NewPrimitives.length() > 0)
									AddPrimitiveSet(NewPrimitives, NewCodec, NewCodecURI);

								continue;
							}

							/** Parse a Node, Container, or Renumber container, possibly including local translation(s) **/

							string NewName;
							UInt32 NewID = 0; bool GotID = false;
							string NewType;
							UInt32 NewToID = 0; bool GotToID = false;
							Translation NewTranslation;
							if (Reader.GetID() == dmltsl::tsl2::idContainer) NewType = "container";
                            
							for (; ; )
							{
								if (!Reader.Read()) throw Reader.CreateDmlException("Unterminated container or node declaration in translation document.");

								switch (Reader.GetNodeType())
								{
								case NodeTypes::Comment: continue;
								case NodeTypes::Padding: continue;
								case NodeTypes::EndContainer: break;
								case NodeTypes::EndAttributes:
									if (IsEqual(NewType,"container"))
									{
										try
										{
											ParseDmlTranslation(NewTranslation, Reader, ResolutionCallback);     // Will parse until EndContainer is reached.										
										}
										catch (std::exception& exc)
										{
											throw Exception(string(exc.what()) + "\n\twhile parsing local translation definition for <" + NewName + "> given in dml.");
										}

										break;          // Because ParseDmlTranslation() ran until EndContainer was reached.
									}
									continue;
								case NodeTypes::Primitive:
									switch (Reader.GetID())
									{
									case dmltsl::tsl2::idDMLID: NewID = (uint)Reader.GetUInt(); GotID = true; break;
									case dmltsl::tsl2::idName: NewName = Reader.GetString(); break;
									case dmltsl::tsl2::idType: NewType = Reader.GetString(); break;
									case dmltsl::tsl2::idNewID: NewToID = (uint)Reader.GetUInt(); GotToID = true; break;
									default:
										throw Reader.CreateDmlException("Unrecognized primitive when parsing declaration in translation document.");
									}
									continue;
								default:
									throw Reader.CreateDmlException("Unexpected node type when parsing container or node declaration in translation document.");
								}
								break;
							}

							/** Process the Node, Container, or Renumber directive **/
                            
							if (Reader.GetID() == dmltsl::tsl2::idNode || Reader.GetID() == dmltsl::tsl2::idContainer)
							{
								if (!GotID || NewName.length() == 0 || NewType.length() == 0)
									throw Reader.CreateDmlException("Incomplete declaration in translation document.");

								if (compare_no_case(NewType, "container") == 0) {
									if (NewTranslation.GetCount() > 0)
										Into.Add(Association(NewID, NewName, NewTranslation));
									else
										Into.Add(Association(NewID, NewName, NodeTypes::Container));
								}
								else Into.Add(BuildNewAssociation(NewID, NewName, NewType));
								continue;
							}
							else if (Reader.GetID() == dmltsl::tsl2::idRenumber)
							{
								if (!GotID || !GotToID)
									throw Reader.CreateDmlException("Incomplete renumber directive in translation document.");

								Into.Renumber(NewID, NewToID);
								continue;
							}
							else throw Reader.CreateDmlException("Unrecognized directive in translation document.");
						}

					default:
						throw CreateDmlException("Unexpected or invalid node when parsing header or translation document.");
					}
				}
			}

			void ParseDmlTranslation(Translation& Into, r_ptr<Stream>&& Stream, string TranslationURN, ResourceResolve ResolutionCallback = nullptr)
			{
				DmlReader Reader = DmlReader::Create(std::move(Stream));
				Reader.ParseHeader(ResolutionCallback);

				// Find translation document top-level container            
				for (; ; )
				{
					if (!Reader.Read()) throw Reader.CreateDmlException("Expected DML:Translation element.");
					if (Reader.GetNodeType() == NodeTypes::Padding || Reader.GetNodeType() == NodeTypes::Comment) continue;
					if (Reader.GetNodeType() != NodeTypes::Container) throw Reader.CreateDmlException("Expected DML:Translation element.");
					if (Reader.GetID() != dmltsl::tsl2::idDMLTranslation) throw Reader.CreateDmlException("Expected DML:Translation document.");
					break;
				}

				// Parse translation document attributes
				for (; ; )
				{
					if (!Reader.Read()) throw Reader.CreateDmlException("Unterminated DML:Translation container.");
					if (Reader.GetNodeType() == NodeTypes::Padding || Reader.GetNodeType() == NodeTypes::Comment) continue;
					if (Reader.GetNodeType() == NodeTypes::EndContainer) return;          // Empty container                
					if (Reader.GetNodeType() == NodeTypes::EndAttributes) break;
					if (Reader.GetID() == dmltsl::tsl2::idDML_URN && TranslationURN.length() > 0)
					{
						if (to_lower(Reader.GetString()).compare(to_lower(TranslationURN)) != 0)
							throw Reader.CreateDmlException("DML:URN did not match the requested translation document's URN.");					
					}
				}

				// Parse translation document body
				ParseDmlTranslation(Into, Reader, ResolutionCallback);
			}

			void ParseTranslation(Translation& Into, string TranslationURI, string TranslationURN, ResourceResolve ResolutionCallback = nullptr)
			{
				if (TranslationURI.compare(dmltsl::dml3::urn) == 0) return;
				else if (TranslationURI.compare(dmltsl::tsl2::urn) == 0)
				{
					Into.Add(Translation::TSL2);
					return;
				}
				else if (ResolutionCallback == nullptr) throw Exception("Unable to retrieve DML Translation Document.");

				bool IsXml;
				r_ptr<Stream> ss = ResolutionCallback(TranslationURI, IsXml);
				if (IsXml) ParseXmlTranslation(Into, *ss, TranslationURN, ResolutionCallback);
				else ParseDmlTranslation(Into, r_ptr<Stream>::absolved(ss), TranslationURN, ResolutionCallback);
			}

		public:			

			/// <summary>
			/// Call ParseHeader() at the beginning of a stream to parse the DML Header.  The translation
			/// and primitives will be loaded into the DmlReader for further parsing.  Optional
			/// resolution can be provided to allow the DML header to retrieve any necessary translation
			/// documents.
			/// </summary>
			void ParseHeader(ResourceResolve ResolutionCallback = nullptr)
			{
				if (!Read() || GetID() != dmltsl::dml3::idDMLHeader) throw CreateDmlException("Expected DML Header");

				// Load header attributes				
				for (;;)
				{
					if (!Read()) throw CreateDmlException("Unterminated DML header.");
                
					switch (GetNodeType())
					{
					case NodeTypes::EndContainer: return;
					case NodeTypes::EndAttributes: break;
					case NodeTypes::Padding: continue;
					case NodeTypes::Comment: continue;
					case NodeTypes::Container: throw CreateDmlException("Containers are not permitted as DML attributes.\nInvalid use of container: " + GetName());
					case NodeTypes::Primitive:
						switch (GetID())
						{
						case dmltsl::dml3::idDMLDocType: continue;
						case dmltsl::dml3::idDMLVersion: continue;
						case dmltsl::dml3::idDMLReadVersion:
							{
								UInt64 ReadV = GetUInt();
								if (ReadV != dml::DMLReadVersion) throw CreateDmlException(string("DML Read version ") + to_string(ReadV) + string(" is not supported by this DML parser."));
								continue;
							}
						default:
							throw CreateDmlException("Unsupported DML attribute '" + GetName() + "' found in DML header.");
						}
					default: throw CreateDmlException("Unrecognized or invalid node type.");
					}
					break;
				}

				// Load header elements (directives)

				// Since we're now entering into translation language, this is identical code to parsing 
				// a DML:Translation container, and we use an identical function.
				ParseDmlTranslation(GlobalTranslation, *this, ResolutionCallback);
			}

			#pragma endregion

		private:

			#pragma region "State Management"

			/** State management **/

			void FinishNode()
			{
				try
				{
					if (m_pAssociation != nullptr)
					{
						switch (GetNodeType())
						{
						case NodeTypes::Primitive:
							switch (GetPrimitiveType())
							{
							case PrimitiveTypes::Array: SkipArray(); break;
							case PrimitiveTypes::Boolean: SkipBoolean(); break;
							case PrimitiveTypes::DateTime: SkipDateTime(); break;
								//case PrimitiveTypes.Decimal: SkipDecimal(); break;
							case PrimitiveTypes::Double: SkipDouble(); break;
							case PrimitiveTypes::Int: SkipInt(); break;
							case PrimitiveTypes::Matrix: SkipMatrix(); break;
							case PrimitiveTypes::Single: SkipSingle(); break;
							case PrimitiveTypes::String: SkipString(); break;
							case PrimitiveTypes::UInt: SkipUInt(); break;
							//case PrimitiveTypes::EncryptedDML: SkipEncryptedDml(); break;
							//case PrimitiveTypes::CompressedDML: SkipCompressedDml(); break;
							//case PrimitiveTypes::Extension: Association.DMLName.Extension.CloseNode(Association, Reader); break;                                
							default: throw CreateDmlException("No codec skip action available for current node.");
							}
							m_pAssociation = nullptr;
							break;

						case NodeTypes::Comment: 
							SkipComment();
							m_pAssociation = nullptr;
							break;

						case NodeTypes::Padding:
							SkipPadding();
							m_pAssociation = nullptr;
							break;

						case NodeTypes::Container: 
							m_pAssociation = nullptr;
							break;

						case NodeTypes::EndAttributes:
							IsAttribute = false;
							m_pAssociation = nullptr;
							break;

						case NodeTypes::EndContainer:
							{
								DmlContext* pParent = m_pContainer->m_pContainer;
								delete m_pContainer;
								m_pContainer = pParent;
								m_pAssociation = nullptr;
								break;
							}

						default: throw CreateDmlException("Unsupported node type at closure.");
						}
					}
				}
				catch (DmlException& dex) { throw dex; }
				catch (std::exception& ex) { throw CreateDmlException(ex.what()); }
			}

			void SkipInt() { GetInt(); }
			void SkipUInt() { GetUInt(); }
			void SkipBoolean() { GetBoolean(); }
			void SkipDateTime() { DiscardBytes(8); }
			void SkipSingle() { DiscardBytes(4); }
			void SkipDouble() { DiscardBytes(8); }

			void SkipString()
			{
				try
				{
					if (GetPrimitiveType() != PrimitiveTypes::String) throw CreateDmlException("Node type does not match Skip..() type.");
					UInt64 Length = m_pReader->ReadCompact64();
					DiscardBytes(Length);
					m_pAssociation = nullptr;                
				}
				catch (DmlException& dex) { throw dex; }
				catch (std::exception& ex) { throw CreateDmlException(ex.what()); }
			}

			void SkipArray()
			{
				if (GetArrayType() == ArrayTypes::Strings) { SkipStringArray(); return; }
				UInt64 Elements = m_pReader->ReadCompact64();
				UInt64 ElementSize;
				switch (GetArrayType())
				{
				case ArrayTypes::U8: ElementSize = 1; break;
				case ArrayTypes::U16: ElementSize = 2; break;
				case ArrayTypes::U24: ElementSize = 3; break;
				case ArrayTypes::U32: ElementSize = 4; break;
				case ArrayTypes::U64: ElementSize = 8; break;
				case ArrayTypes::I8: ElementSize = 1; break;
				case ArrayTypes::I16: ElementSize = 2; break;
				case ArrayTypes::I24: ElementSize = 3; break;
				case ArrayTypes::I32: ElementSize = 4; break;
				case ArrayTypes::I64: ElementSize = 8; break;
				case ArrayTypes::Singles: ElementSize = 4; break;
				case ArrayTypes::Doubles: ElementSize = 8; break;
				case ArrayTypes::DateTimes: ElementSize = 8; break;
				default: throw CreateDmlException("Unsupported array type.");
				}
				DiscardBytes(Elements * ElementSize);
			}

			void SkipMatrix()
			{			
				UInt64 Dimension0 = m_pReader->ReadCompact64();
				UInt64 Dimension1 = m_pReader->ReadCompact64();				
				UInt64 ElementSize;
				switch (GetArrayType())
				{
				case ArrayTypes::U8: ElementSize = 1; break;
				case ArrayTypes::U16: ElementSize = 2; break;
				case ArrayTypes::U24: ElementSize = 3; break;
				case ArrayTypes::U32: ElementSize = 4; break;
				case ArrayTypes::U64: ElementSize = 8; break;
				case ArrayTypes::I8: ElementSize = 1; break;
				case ArrayTypes::I16: ElementSize = 2; break;
				case ArrayTypes::I24: ElementSize = 3; break;
				case ArrayTypes::I32: ElementSize = 4; break;
				case ArrayTypes::I64: ElementSize = 8; break;
				case ArrayTypes::Singles: ElementSize = 4; break;
				case ArrayTypes::Doubles: ElementSize = 8; break;
				case ArrayTypes::DateTimes: ElementSize = 8; break;
				default: throw CreateDmlException("Unsupported matrix type.");
				}
				DiscardBytes(Dimension0 * Dimension1 * ElementSize);
			}

			void SkipStringArray()
			{
				if (GetPrimitiveType() != PrimitiveTypes::Array || GetArrayType() != ArrayTypes::Strings) throw CreateDmlException("Cannot read array of a different type.");
            
				UInt64 NStrings = m_pReader->ReadCompact64();
				for (UInt64 ii=0; ii < NStrings; ii++)
				{
					UInt64 NBytes = m_pReader->ReadCompact64();					
					DiscardBytes(NBytes);
				}
			}

			void SkipComment()
			{				
				if (GetNodeType() != NodeTypes::Comment) throw CreateDmlException("Node type does not match Skip..() type.");
				UInt64 Length = m_pReader->ReadCompact64();
				DiscardBytes(Length);				
			}			

			void SkipPadding()
			{				
				if (GetNodeType() != NodeTypes::Padding) throw CreateDmlException("Node type does not match Get..() type.");
				if (GetID() == dmltsl::dml3::idDMLPaddingByte) return;				
				UInt64 Length = m_pReader->ReadCompact64();
				DiscardBytes(Length);
			}			

			/** Translation management **/

			Translation* GetActiveTranslation()
			{
				if (m_pContainer != nullptr)
				{
					if (m_pContainer->m_pAssociation->pLocalTranslation != nullptr) return m_pContainer->m_pAssociation->pLocalTranslation;
					DmlContext* pIter = m_pContainer->m_pContainer;
					while (pIter != nullptr)
					{
						if (pIter->m_pAssociation->pLocalTranslation != nullptr) return pIter->m_pAssociation->pLocalTranslation;
						pIter = pIter->m_pContainer;
					}
				}
				return &GlobalTranslation;
			}

			#pragma endregion

			#pragma region "Low-Level Reading Utilities"

			/** Low-Level Reading Utilities **/
			
			r_ptr<Association> ReadIdentificationInformation()
			{
				try
				{
					UInt32 NameLen = m_pReader->ReadCompact32();
					string Name;
					Name.resize(NameLen);
					m_pReader->Read(&Name[0], NameLen);

					UInt32 TypeLen = m_pReader->ReadCompact32();					
					string Type;
					Type.resize(TypeLen);
					m_pReader->Read(&Type[0], TypeLen);					

					if (compare_no_case(Type, "container") == 0)
					{
						return r_ptr<Association>::responsible(new Association(dmltsl::dml3::idInlineIdentification, Name, NodeTypes::Container));
					}
					else
					{
						PrimitiveTypes PrimitiveType; ArrayTypes ArrayType;
						if (!StringToPrimitiveType(Type, PrimitiveType, ArrayType))
						{
							/**
							foreach (IDmlReaderExtension Ext in Options.Extensions)
							{
								uint TypeId = Ext.Identify(Type);
								if (TypeId != 0)
									return new Association(DmlTranslation.DML2.InlineIdentification.DMLID, new DmlName(Name, Ext, TypeId));
							}
							**/
							throw CreateDmlException("Dml type not recognized internally or by any registered extensions.");
						}
						return r_ptr<Association>::responsible(new Association(dmltsl::dml3::idInlineIdentification, Name, PrimitiveType, ArrayType));
					}
				}
				catch (std::exception& ex) { throw CreateDmlException(ex.what()); }
			}

			void DiscardBytes(Stream *pStream, UInt64 NBytes)
			{
				if (pStream->CanSeek()) pStream->Seek((Int64)NBytes, SeekOrigin::Current);
				else
				{
					static const int TrashSize = 4090;
					byte *pTrash = new byte [TrashSize];
					while (NBytes > 0)
					{
						int nToRead = TrashSize;
						if (NBytes < (UInt64)nToRead) nToRead = (int)NBytes;                    
						Int64 nRead = pStream->Read(pTrash, nToRead);
						if (nRead < nToRead) throw EndOfStreamException();
						NBytes -= (UInt64)nRead;
					}
				}
			}

			void DiscardBytes(UInt64 NBytes) { DiscardBytes(m_pReader->m_pStream.get(), NBytes); }

			#pragma endregion

			#pragma region "Error Management"

			/** Error Management **/
        						
			DmlException CreateDmlException(string Message) { return DmlException(Message + "\n" + GetErrorContext()); }

			string GetErrorContext()
			{
				string sb;				
				sb += "Dml Exception Context:\n";

				if (m_pAssociation != nullptr)
				{
					if (GetNodeType() != NodeTypes::Container)
					{
						// On the opening of a Container, the Association is the same as the Container, so avoid
						// duplicate presentation...
						if (m_pAssociation != nullptr) sb += S("\t") + AssociationToErrorString(*m_pAssociation, IsAttribute);
					}
				}
				DmlContext *pCont = m_pContainer;
				while (pCont != nullptr)
				{
					if (pCont->m_pAssociation != nullptr) sb += S("\t") + AssociationToErrorString(*(pCont->m_pAssociation), false);
					else sb += S("\t(Missing association)");
					pCont = pCont->m_pContainer;
				}

				return sb;
			}

			static string AssociationToErrorString(Association& Assoc, bool IsAttribute)
			{
				string Ident;

				if (Assoc.DMLID != UInt32_MaxValue && Assoc.DMLID != dmltsl::dml3::idInlineIdentification)
					Ident = "[" + to_hex_string(Assoc.DMLID) + "] ";
				else
					Ident = "";

				if (Assoc.Name.length() > 0)
				{
					string Name = Assoc.Name;
					switch (Assoc.NodeType)
					{                        
					case NodeTypes::Comment: Ident = Ident + "(Comment) "; break;
					case NodeTypes::Primitive: 
						if (IsAttribute)
							Ident = Ident + Name + "= "; 
						else
							Ident = Ident + "<" + Name + "> "; 
						break;
					case NodeTypes::Container: Ident = Ident + "<" + Name + "> "; break;
					case NodeTypes::EndContainer: Ident = Ident + "</" + Name + "> "; break;
					default:
					case NodeTypes::Unknown: Ident = Ident + Name + " (Unknown Type) "; break;
					}
				}
				else
				{
					switch (Assoc.NodeType)
					{
					case NodeTypes::Comment: Ident = Ident + "(Comment) "; break;
					case NodeTypes::Primitive: 
						if (IsAttribute)
							Ident = Ident + "(Attribute) ";
						else 
							Ident = Ident + "(Primitive) "; 
						break;
					case NodeTypes::Container: Ident = Ident + "(Container) "; break;
					case NodeTypes::EndContainer: Ident = Ident + "(End Container) "; break;
					default:
					case NodeTypes::Unknown: Ident = Ident + "(Unknown) "; break;
					}
				}            
            
				return Ident;
			}
			
			#pragma endregion

			#pragma region "Miscellaneous"

			/** Miscellaneous Helpers **/						

			DateTime FromNanoseconds(Int64 TimeValue)
			{
				DateTime Value = dml::ReferenceDate;
				Int64 Seconds = TimeValue / 1000000000ll;
				Int32 Nanoseconds = TimeValue % 1000000000ll;
				Value += TimeSpan(Seconds, Nanoseconds);
				return Value;
			}

			#pragma endregion
		};
	}
}

#endif	// __DmlReader_h__

//	End of DmlReader.h

