/*	DmlWriter.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
*/

#ifndef __DmlWriter_h__
#define __DmlWriter_h__

#include "../Support/Platforms/Platforms.h"
#include "../Support/Memory Management/Allocation.h"
#include "../Support/IO/FileStream.h"
#include "../Support/IO/EndianBinaryWriter.h"
#include "../Support/DateTime/DateTime.h"

namespace wb
{
	namespace dml
	{
		using namespace wb;
		using namespace wb::io;
		using namespace wb::memory;

		class DmlWriter
		{
			// m_pBase is not used directly except as being passed to m_pWriter.  However, we store it as a member in order
			// to take responsibility for deleting the object in certain cases.
			r_ptr<Stream> m_pBase;

			r_ptr<BinaryWriter>	m_pWriter;

		protected:			

			Codecs CommonCodec;
			Codecs ArrayCodec;

			DmlWriter()
			{
				CommonCodec = Codecs::NotLoaded;
				ArrayCodec = Codecs::NotLoaded;
			}			
			
			static DmlWriter Create(r_ptr<wb::io::Stream>&& Stream, DmlWriter Context)
			{
				DmlWriter ret;
				ret.m_pWriter = r_ptr<BinaryWriter>::responsible(new wb::io::BinaryWriter(std::move(Stream), false));
				ret.CommonCodec = Context.CommonCodec;
				ret.ArrayCodec = Context.ArrayCodec;
				return ret;
			}

			/// <summary>Throws a DmlException and provides any available context information to aid in troubleshooting.</summary>
			void ThrowDmlException(string Message)
			{
				throw DmlException(Message);
			}

			Int64 ToNanoseconds(const TimeSpan& Delta)
			{				
				Int64 Seconds = Delta.GetTotalSecondsNoRounding();
				if (Seconds >= 0)
				{
					// Maximum supported date range for this routine is the maximum 64-bit value for seconds less one second to account for nanoseconds up to +1 second.
					static const Int64 MaxSeconds = (Int64_MaxValue / time_constants::g_nNanosecondsPerSecond) - 1;
					if (Seconds >= MaxSeconds) throw NotSupportedException(S("Date value is outside codec-supported range."));
					return Seconds * time_constants::g_nNanosecondsPerSecond + Delta.GetNanoseconds();
				}
				else
				{
					// Minimum supported date range for this routine is the minimum 64-bit value for seconds plus one second to account for nanoseconds up to -1 second.
					static const Int64 MinSeconds = (Int64_MinValue / time_constants::g_nNanosecondsPerSecond) + 1;
					if (Seconds <= MinSeconds) throw NotSupportedException(S("Date value is outside codec-supported range."));
					return Seconds * time_constants::g_nNanosecondsPerSecond - Delta.GetNanoseconds();
				}
			}

			bool IsLEArray() 
			{ 
				switch (ArrayCodec)
				{
				case Codecs::LE: return true;
				case Codecs::BE: return false;
				default: throw DmlException(S("Array primitives not enabled at writer."));
				}
			}

		public:

			DmlWriter(DmlWriter&& mv)
				: m_pBase(std::move(mv.m_pBase)), m_pWriter(std::move(mv.m_pWriter)),
				CommonCodec(mv.CommonCodec), ArrayCodec(mv.ArrayCodec)
			{ }

			static DmlWriter Create(string Filename)
			{
				DmlWriter ret;
				ret.m_pBase = r_ptr<Stream>::responsible(new FileStream(Filename.c_str(), FileMode::Create));
				ret.m_pWriter = r_ptr<BinaryWriter>::responsible(new BinaryWriter(r_ptr<Stream>::absolved(ret.m_pBase), false));
				return ret;
			}

			static DmlWriter Create(r_ptr<Stream>&& stream)
			{
				DmlWriter ret;
				ret.m_pBase = std::move(stream);
				ret.m_pWriter = r_ptr<BinaryWriter>::responsible(new BinaryWriter(r_ptr<Stream>::absolved(ret.m_pBase), false));
				return ret;
			}

			void Close()
			{
				if (m_pBase != nullptr) m_pBase->Close();
				m_pWriter.release();
				m_pBase.release();
			}

			/// <summary>
			/// Call AddPrimitiveSet() to attempt to enable a primitive set that is required for the DML document to
			/// be written.  If a primitive set is not supported, an exception will be thrown.  The AddPrimitiveSet()
			/// call does not cause anything to be emitted to the DML stream, it only enables support for the requested
			/// primitive codec.
			/// </summary>
			/// <param name="Set">Primitive set required.</param>
			void AddPrimitiveSet(string SetName, string Codec)
			{
				SetName = to_lower(SetName);
				Codec = to_lower(Codec);
				if (SetName.compare(S("base")) == 0) return;
				if (SetName.compare(S("common")) == 0)
				{				
					if (Codec.compare(S("le")) == 0) { CommonCodec = Codecs::LE; return; }
					if (Codec.compare(S("be")) == 0) { CommonCodec = Codecs::BE; return; }
					ThrowDmlException(S("Common primitive set codec is not recognized by writer."));
				}
				if (SetName.compare(S("ext-precision")) == 0) ThrowDmlException(S("Extended precision floating-point not supported by writer."));
				if (SetName.compare(S("decimal-float")) == 0) ThrowDmlException(S("Base-10 (Decimal) floating-point not supported by writer."));
				if (SetName.compare(S("arrays")) == 0)
				{
					if (Codec.compare(S("le")) == 0) { ArrayCodec = Codecs::LE; return; }
					if (Codec.compare(S("be")) == 0) { ArrayCodec = Codecs::BE; return; }
					ThrowDmlException(S("Array/Matrix primitive set codec is not recognized by writer."));
				}						
				if (SetName.compare(S("decimal-array")) == 0) ThrowDmlException(S("Decimal floating-point arrays are not supported by writer."));
				if (SetName.compare(S("dml-ec1")) == 0)
				{
					ThrowDmlException(S("dml-ec1 primitive set codec not supported by writer."));		// TODO: Implement dml-ec1 v1.
					//if (Codec.compare(S("v1")) == 0) return;
					//ThrowDmlException(S("Dml-EC primitive set codec not recognized by writer."));                            
				}
				ThrowDmlException(S("Primitive set not recognized by writer."));
			}

			void AddPrimitiveSet(const PrimitiveSet& Set)
			{
				AddPrimitiveSet(Set.Set, Set.Codec);
			}

			/** Encoding Size Predictors **/

			/// <summary>
			/// PredictNodeHeadSize() predicts the encoded size of a node's head.  PredictNodeHeadSize() 
			/// assumes that the caller would use WriteStartNode() to write the node.
			/// </summary>
			/// <param name="ID">The ID which would be written</param>
			/// <returns>The exact size, in bytes, required to write the node head using DmlWriter.</returns>
			static UInt64 PredictNodeHeadSize(UInt32 ID)
			{
				return (UInt64)BinaryWriter::SizeCompact32(ID);
			}

			/// <summary>
			/// PredictNodeHeadSize() predicts the encoded size of a node's head when using inline identification.
			/// </summary>
			/// <param name="Name">The name which would be written.</param>
			/// <returns>The exact size, in bytes, required to write the node head using DmlWriter.</returns>
			static UInt64 PredictNodeHeadSize(const char *pszName, const char *pszNodeType)
			{
				size_t NameLength = strlen(pszName);
				size_t NodeTypeLength = strlen(pszNodeType);
				if (NameLength > UInt32_MaxValue || NodeTypeLength > UInt32_MaxValue)
					throw ArgumentOutOfRangeException("Name and type length must fit within 32-bit length.");				
				return
					(UInt64)(
						BinaryWriter::SizeCompact32(dmltsl::dml3::idInlineIdentification)
						+ BinaryWriter::SizeCompact32((UInt32)NameLength)
						+ NameLength
						+ BinaryWriter::SizeCompact32((UInt32)NodeTypeLength)
						+ NodeTypeLength
						);
			}

			/// <summary>
			/// PredictNodeHeadSize() predicts the encoded size of a node's head when using inline identification.
			/// </summary>
			/// <param name="Name">The name which would be written.</param>
			/// <param name="PrimitiveType">A non-array primitive type for the node.</param>
			/// <param name="ArrayType">The array type for the node, or ArrayTypes::Unknown if not applicable.</param>
			/// <returns>The exact size, in bytes, required to write the node head using DmlWriter.</returns>
			static UInt64 PredictNodeHeadSize(const char *pszName, PrimitiveTypes PrimitiveType, ArrayTypes ArrayType = ArrayTypes::Unknown)
			{
				return PredictNodeHeadSize(pszName, PrimitiveTypeToString(PrimitiveType, ArrayType));
			}

			/// <summary>
			/// PredictContainerHeadSize() predicts the encoded size of a container's head with no size provided.
			/// </summary>
			/// <param name="ID">The DMLID of the container to be written.</param>
			/// <returns>The exact size, in bytes, required to write the container head using DmlWriter.</returns>
			static UInt64 PredictContainerHeadSize(UInt32 ID)
			{
				return PredictNodeHeadSize(ID);
			}

			/// <summary>
			/// PredictContainerHeadSize() predicts the encoded size of a container's head with no size provided
			/// when using inline identification.
			/// </summary>
			/// <param name="Name">The name which would be written.</param>
			/// <returns>The exact size, in bytes, required to write the container head using DmlWriter.</returns>
			static UInt64 PredictContainerHeadSize(const char *pszName)
			{
				return PredictNodeHeadSize(pszName, "container");
			}

			/// <summary>
			/// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
			/// encode the primitive's content, not including the node head.
			/// </summary>
			/// <param name="Value">The value to be represented in the primitive.</param>
			/// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
			static UInt64 PredictPrimitiveSize(UInt64 Value) { return (UInt64)BinaryWriter::SizeCompact64(Value); }

			/// <summary>
			/// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
			/// encode the primitive's content, not including the node head.
			/// </summary>
			/// <param name="Value">The value to be represented in the primitive.</param>
			/// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
			static UInt64 PredictPrimitiveSize(Int64 Value) { return (UInt64)BinaryWriter::SizeCompactS64(Value); }

			/// <summary>
			/// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
			/// encode the primitive's content, not including the node head.
			/// </summary>
			/// <param name="Value">The value to be represented in the primitive.</param>
			/// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
			static UInt64 PredictPrimitiveSize(bool Value) { return 1; }

			/// <summary>
			/// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
			/// encode the primitive's content, not including the node head.
			/// </summary>
			/// <param name="Value">The value to be represented in the primitive.</param>
			/// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
			static UInt64 PredictPrimitiveSize(DateTime Value) { return 8; }

			/// <summary>
			/// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
			/// encode the primitive's content, not including the node head.
			/// </summary>
			/// <param name="Value">The value to be represented in the primitive.</param>
			/// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
			static UInt64 PredictPrimitiveSize(float Value) { return 4; }

			/// <summary>
			/// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
			/// encode the primitive's content, not including the node head.
			/// </summary>
			/// <param name="Value">The value to be represented in the primitive.</param>
			/// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
			static UInt64 PredictPrimitiveSize(double Value) { return 8; }

			/// <summary>
			/// PredictPrimitiveSize() provides the number of bytes of data that will be required to 
			/// encode the primitive's content, not including the node head.
			/// </summary>
			/// <param name="Value">The value to be represented in the primitive.</param>
			/// <returns>The number of bytes of data required to represent the primitive's content in DML encoding.</returns>
			static UInt64 PredictPrimitiveSize(string Value)
			{
				UInt64 ByteCount = Value.length();
				return (UInt64)BinaryWriter::SizeCompact64(ByteCount) + ByteCount;
			}

			/// <summary>
			/// PredictCommentSize() provides the number of bytes of data that will be required to 
			/// encode the comment's content, not including the node head.
			/// </summary>
			/// <param name="Value">The text to be placed in the comment.</param>
			/// <returns>The number of bytes of data required to represent the comment's content in DML encoding.</returns>        
			static UInt64 PredictCommentSize(string Value)
			{
				UInt64 ByteCount = Value.length();
				return (UInt64)BinaryWriter::SizeCompact64((UInt64)ByteCount) + (UInt64)ByteCount;
			}

			// <summary>
			/// PredictPaddingSize() provides the number of bytes of data that will be required to 
			/// encode the padding's content, not including the node head.  PredictPaddingSize()
			/// assumes that a multi-byte padding node will be utilized.  For single-byte padding,
			/// the content size is zero.
			/// </summary>
			/// <param name="Length">The length of padding that would be encoded.</param>
			/// <returns>The number of bytes of data required to represent the padding's content in DML encoding.</returns>        
			static UInt64 PredictPaddingSize(UInt64 PaddingLength)
			{
				return (UInt64)BinaryWriter::SizeCompact64(PaddingLength) + PaddingLength;
			}

			/** Low-Level Data Format **/

			/// <summary>
			/// WriteStartNode() writes the head of a DML node.  It does not write the node's 
			/// data.
			/// </summary>        
			/// <param name="ID">DMLID to write into the node head.</param>			
			void WriteStartNode(UInt32 ID)
			{
				assert (ID != dmltsl::dml3::idInlineIdentification);		// This overload cannot be used with inline identification.
				m_pWriter->WriteCompact32(ID);
			}

			/// <summary>
			/// This overload of WriteStartNode() writes the DML node head using inline identification.  
			/// The data content is not written.
			/// </summary>
			/// <param name="Name">Text name of the node.  Must be compatible with XML naming conventions.</param>
			/// <param name="Type">Node type.</param>
			void WriteStartNode(string Name, string NodeType)
			{
				if (Name.length() > UInt32_MaxValue || NodeType.length() > UInt32_MaxValue)
					throw ArgumentOutOfRangeException();
				m_pWriter->WriteCompact32(dmltsl::dml3::idInlineIdentification);
				m_pWriter->WriteCompact32((UInt32)Name.length());
				m_pWriter->Write(Name.c_str(), Name.length());
				m_pWriter->WriteCompact32((UInt32)NodeType.length());            
				m_pWriter->Write(NodeType.c_str(), NodeType.length());
			}

			/** Container Writers **/

			/// <summary>
			/// This overload of WriteStartContainer() writes the container head.  The 
			/// data content is not written.
			/// </summary>
			/// <param name="ID">DMLID of the container.</param>
			void WriteStartContainer(UInt32 ID) { WriteStartNode(ID); }

			/// <summary>
			/// This overload of WriteStartContainer() writes the container head using inline
			/// identification.  The data content is not written.
			/// </summary>
			/// <param name="Name">Text name of the element.</param>
			void WriteStartContainer(string Name)
			{
				WriteStartNode(Name, "container");
			}

			/// <summary>
			/// This overload of WriteStartContainer() writes the container head using the 
			/// identification information provided in an Association type.  The data 
			/// content is not written.
			/// </summary>
			/// <param name="Identity">Identification of the element.</param>
			void WriteStartContainer(const Association& Identity)
			{
				if (Identity.NodeType != NodeTypes::Container)
					throw DmlException(S("Only container node types can be written using WriteStartContainer()."));
				if (Identity.IsInlineIdentification())
					WriteStartNode(Identity.Name, "container");
				else
					WriteStartNode(Identity.DMLID);
			}

			void WriteEndAttributes()
			{
				WriteStartNode(dmltsl::dml3::idDMLEndAttributes);
			}

			void WriteEndContainer()
			{
				WriteStartNode(dmltsl::dml3::idDMLEndContainer);
			}

			/** Base Primitive Writers: By DMLID **/        			

			void Write(UInt32 ID, UInt8 Value) { Write(ID, (UInt64)Value); }
			void Write(UInt32 ID, Int8 Value) { Write(ID, (Int64)Value); }
			void Write(UInt32 ID, UInt16 Value) { Write(ID, (UInt64)Value); }
			void Write(UInt32 ID, Int16 Value) { Write(ID, (Int64)Value); }
			void Write(UInt32 ID, UInt32 Value) { Write(ID, (UInt64)Value); }
			void Write(UInt32 ID, Int32 Value) { Write(ID, (Int64)Value); }
			void Write(UInt32 ID, UInt64 Value) { WriteStartNode(ID); m_pWriter->WriteCompact64(Value); }
			void Write(UInt32 ID, Int64 Value) { WriteStartNode(ID); m_pWriter->WriteCompactS64(Value); }
			void Write(UInt32 ID, bool Value) { 
				WriteStartNode(ID); 
				m_pWriter->Write((byte)(Value ? 1 : 0)); 
			}
			void Write(UInt32 ID, string Value) {
				WriteStartNode(ID); m_pWriter->WriteCompact64(Value.length()); m_pWriter->Write(Value.c_str(), Value.length());
			}			
			void Write(UInt32 ID, const char* Value) {
				Int64 len = strlen(Value);
				WriteStartNode(ID); m_pWriter->WriteCompact64(len); m_pWriter->Write(Value, len);
			}
			void Write(UInt32 ID, byte* pData, Int64 nLength) {
				WriteStartNode(ID); m_pWriter->WriteCompact64(nLength); m_pWriter->Write(pData, nLength);
			}			

			/** Base Primitive Writers: By Inline Identification **/			

			void Write(string Name, UInt8 Value) { Write(Name, (UInt64)Value); }
			void Write(string Name, Int8 Value) { Write(Name, (Int64)Value); }
			void Write(string Name, UInt16 Value) { Write(Name, (UInt64)Value); }
			void Write(string Name, Int16 Value) { Write(Name, (Int64)Value); }
			void Write(string Name, UInt32 Value) { Write(Name, (UInt64)Value); }
			void Write(string Name, Int32 Value) { Write(Name, (Int64)Value); }
			void Write(string Name, UInt64 Value) { WriteStartNode(Name, "uint"); m_pWriter->WriteCompact64(Value); }
			void Write(string Name, Int64 Value) { WriteStartNode(Name, "int"); m_pWriter->WriteCompactS64(Value); }
			void Write(string Name, bool Value) { WriteStartNode(Name, "boolean"); m_pWriter->Write((byte)(Value ? 1 : 0)); }
			void Write(string Name, string Value) {
				WriteStartNode(Name, "string"); m_pWriter->WriteCompact64(Value.length()); m_pWriter->Write(Value.c_str(), Value.length());
			}
			void Write(string Name, const char* Value) {
				Int64 len = strlen(Value);
				WriteStartNode(Name, "string"); m_pWriter->WriteCompact64(len); m_pWriter->Write(Value, len);
			}
			void Write(string Name, byte* pData, Int64 nLength) {
				WriteStartNode(Name, "array-U8"); m_pWriter->WriteCompact64(nLength); m_pWriter->Write(pData, nLength);
			}

			/** Base Primitive Writers: By Association **/

			void Write(const Association& Identity, UInt8 Value) { if (Identity.IsInlineIdentification()) Write(Identity.Name,(UInt64)Value); else Write(Identity.DMLID,(UInt64)Value); }
			void Write(const Association& Identity, Int8 Value) { if (Identity.IsInlineIdentification()) Write(Identity.Name,(Int64)Value); else Write(Identity.DMLID,(Int64)Value); }
			void Write(const Association& Identity, UInt16 Value) { if (Identity.IsInlineIdentification()) Write(Identity.Name,(UInt64)Value); else Write(Identity.DMLID,(UInt64)Value); }
			void Write(const Association& Identity, Int16 Value) { if (Identity.IsInlineIdentification()) Write(Identity.Name,(Int64)Value); else Write(Identity.DMLID,(Int64)Value); }
			void Write(const Association& Identity, UInt32 Value) { if (Identity.IsInlineIdentification()) Write(Identity.Name,(UInt64)Value); else Write(Identity.DMLID,(UInt64)Value); }
			void Write(const Association& Identity, Int32 Value) { if (Identity.IsInlineIdentification()) Write(Identity.Name,(Int64)Value); else Write(Identity.DMLID,(Int64)Value); }
			void Write(const Association& Identity, UInt64 Value) { if (Identity.IsInlineIdentification()) Write(Identity.Name,Value); else Write(Identity.DMLID,Value); }
			void Write(const Association& Identity, Int64 Value) { if (Identity.IsInlineIdentification()) Write(Identity.Name,Value); else Write(Identity.DMLID,Value); }
			void Write(const Association& Identity, bool Value) { if (Identity.IsInlineIdentification()) Write(Identity.Name,Value); else Write(Identity.DMLID,Value); }
			void Write(const Association& Identity, string Value) { 
				if (Identity.IsInlineIdentification()) 
					Write(Identity.Name,Value); 
				else 
					Write(Identity.DMLID,Value); 
			}			
			void Write(const Association& Identity, const char *Value) { 
				if (Identity.IsInlineIdentification()) 
					Write(Identity.Name,Value); 
				else 
					Write(Identity.DMLID,Value); 
			}			
			void Write(const Association& Identity, byte* pData, Int64 nLength) { if (Identity.IsInlineIdentification()) Write(Identity.Name,pData,nLength); else Write(Identity.DMLID,pData,nLength); }			

			/** Common Primitive Writers: By DMLID **/			

			void Write(UInt32 ID, float Value)
			{
				switch (CommonCodec)
				{
				case Codecs::LE: WriteStartNode(ID); m_pWriter->IsLittleEndian = true; m_pWriter->Write(Value); return;
				case Codecs::BE: WriteStartNode(ID); m_pWriter->IsLittleEndian = false; m_pWriter->Write(Value); return;
				default: throw DmlException("Common primitive set not selected.");
				}
			}

			void Write(UInt32 ID, double Value)
			{
				switch (CommonCodec)
				{
				case Codecs::LE: WriteStartNode(ID); m_pWriter->IsLittleEndian = true; m_pWriter->Write(Value); return;
				case Codecs::BE: WriteStartNode(ID); m_pWriter->IsLittleEndian = false; m_pWriter->Write(Value); return;
				default: throw DmlException("Common primitive set not selected.");
				}
			}        
        
			void Write(UInt32 ID, DateTime Date)
			{				
				Int64 DateValue = ToNanoseconds(Date - dml::ReferenceDate);

				switch (CommonCodec)
				{
				case Codecs::LE: WriteStartNode(ID); m_pWriter->IsLittleEndian = true; m_pWriter->Write(DateValue); return;
				case Codecs::BE: WriteStartNode(ID); m_pWriter->IsLittleEndian = false; m_pWriter->Write(DateValue); return;
				default: throw DmlException("Common primitive set not selected.");
				}
			}

			/** Common Primitive Writers: By Inline Identification **/			

			void Write(string Name, float Value)
			{
				switch (CommonCodec)
				{
				case Codecs::LE: WriteStartNode(Name, "single"); m_pWriter->IsLittleEndian = true; m_pWriter->Write(Value); return;
				case Codecs::BE: WriteStartNode(Name, "single"); m_pWriter->IsLittleEndian = false; m_pWriter->Write(Value); return;
				default: throw DmlException("Common primitive set not selected.");
				}
			}

			void Write(string Name, double Value)
			{
				switch (CommonCodec)
				{
				case Codecs::LE: WriteStartNode(Name, "double"); m_pWriter->IsLittleEndian = true; m_pWriter->Write(Value); return;
				case Codecs::BE: WriteStartNode(Name, "double"); m_pWriter->IsLittleEndian = false; m_pWriter->Write(Value); return;
				default: throw DmlException("Common primitive set not selected.");
				}
			}

			void Write(string Name, DateTime Date)
			{
				Int64 DateValue = ToNanoseconds(Date - dml::ReferenceDate);

				switch (CommonCodec)
				{
				case Codecs::LE: WriteStartNode(Name, "datetime"); m_pWriter->IsLittleEndian = true; m_pWriter->Write(DateValue); return;
				case Codecs::BE: WriteStartNode(Name, "datetime"); m_pWriter->IsLittleEndian = false; m_pWriter->Write(DateValue); return;
				default: throw DmlException("Common primitive set not selected.");
				}
			}        

			/** Common Primitive Writers: By Association **/			

			void Write(const Association& Identity, float Value) { if (Identity.IsInlineIdentification()) Write(Identity.Name,Value); else Write(Identity.DMLID,Value); }		
			void Write(const Association& Identity, double Value) { if (Identity.IsInlineIdentification()) Write(Identity.Name,Value); else Write(Identity.DMLID,Value); }        
			void Write(const Association& Identity, DateTime Value) { if (Identity.IsInlineIdentification()) Write(Identity.Name,Value); else Write(Identity.DMLID,Value); }

			/** Array Writers: By DMLID **/

			void Write(UInt32 ID, char* pData, Int64 nLength) { Write(ID, (byte*)pData, nLength); }

			void Write(UInt32 ID, Int16* pArray, Int64 nElements) { Write(ID, (UInt16*)pArray, nElements); }
			void Write(UInt32 ID, UInt16* pArray, Int64 nElements)
			{
				WriteStartNode(ID);
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}

			void Write(UInt32 ID, Int32* pArray, Int64 nElements) { Write(ID, (UInt32*)pArray, nElements); }
			void Write(UInt32 ID, UInt32* pArray, Int64 nElements)
			{
				WriteStartNode(ID);
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}

			void Write(UInt32 ID, Int64* pArray, Int64 nElements) { Write(ID, (UInt64*)pArray, nElements); }
			void Write(UInt32 ID, UInt64* pArray, Int64 nElements)
			{
				WriteStartNode(ID);
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}
			
			void Write(UInt32 ID, float* pArray, Int64 nElements)
			{
				WriteStartNode(ID);
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}

			void Write(UInt32 ID, double* pArray, Int64 nElements)
			{
				WriteStartNode(ID);
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}

			void Write(UInt32 ID, DateTime* pArray, int nElements)
			{		
				WriteStartNode(ID);
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); 
				Int64* pValues = new Int64[nElements];
				try
				{
					for (Int64 ii=0; ii < nElements; ii++) pValues[ii] = ToNanoseconds(pArray[ii] - dml::ReferenceDate);
					m_pWriter->Write(pValues, nElements);
				}
				catch (std::exception&) { delete[] pValues; throw; }
				delete[] pValues;
			}

			void Write(UInt32 ID, string *pArray, Int64 nStrings)
			{            
				WriteStartNode(ID);
				m_pWriter->WriteCompact64(nStrings);
				for (Int64 ii=0; ii < nStrings; ii++)				
				{
					string& s = pArray[ii];					
					m_pWriter->WriteCompact64(s.length());
					m_pWriter->Write(s.c_str(), s.length());
				}
			}

			/** Array Writers: By Inline Identification **/

			void Write(string Name, const UInt16* pArray, Int64 nElements)
			{
				WriteStartNode(Name, "array-U16");
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}
			void Write(string Name, const UInt32* pArray, Int64 nElements)
			{
				WriteStartNode(Name, "array-U32");
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}
			void Write(string Name, const UInt64* pArray, Int64 nElements)
			{
				WriteStartNode(Name, "array-U64");
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}
			void Write(string Name, const char* pArray, Int64 nElements)
			{
				WriteStartNode(Name, "array-I8");
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}
			void Write(string Name, const Int16* pArray, Int64 nElements)
			{
				WriteStartNode(Name, "array-I16");
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}
			void Write(string Name, const Int32* pArray, Int64 nElements)
			{
				WriteStartNode(Name, "array-I32");
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}
			void Write(string Name, const Int64* pArray, Int64 nElements)
			{
				WriteStartNode(Name, "array-I64");
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}
			void Write(string Name, const float* pArray, Int64 nElements)
			{
				WriteStartNode(Name, "array-SF");
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}
			void Write(string Name, const double* pArray, Int64 nElements)
			{
				WriteStartNode(Name, "array-DF");
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pArray, nElements);
			}
			void Write(string Name, const DateTime* pArray, int nElements)
			{		
				WriteStartNode(Name, "array-DT");
				m_pWriter->WriteCompact64(nElements);
				m_pWriter->IsLittleEndian = IsLEArray(); 
				Int64* pValues = new Int64[nElements];
				try
				{
					for (Int64 ii=0; ii < nElements; ii++) pValues[ii] = ToNanoseconds(pArray[ii] - dml::ReferenceDate);
					m_pWriter->Write(pValues, nElements);
				}
				catch (std::exception&) { delete[] pValues; throw; }
				delete[] pValues;
			}			
			void Write(string Name, const string *pArray, Int64 nStrings)
			{            
				WriteStartNode(Name, "array-S");
				m_pWriter->WriteCompact64(nStrings);
				for (Int64 ii=0; ii < nStrings; ii++)				
				{
					const string& s = pArray[ii];					
					m_pWriter->WriteCompact64(s.length());
					m_pWriter->Write(s.c_str(), s.length());
				}
			}

			/** Array Writers: By Association **/

			template<typename T> void Write(const Association& Identity, const T* pData, Int64 nLength) { if (Identity.IsInlineIdentification()) Write(Identity.Name,pData,nLength); else Write(Identity.DMLID,pData,nLength); }

			/** Matrix Writers: By DMLID **/

			void Write(UInt32 ID, const byte* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(ID);
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(UInt32 ID, const UInt16* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(ID);
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(UInt32 ID, const UInt32* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(ID);
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(UInt32 ID, const UInt64* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(ID);
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(UInt32 ID, const char* pMatrix, int nRows, int nColumns) { Write(ID, (UInt8*)pMatrix, nRows, nColumns); }			
			void Write(UInt32 ID, const Int16* pMatrix, int nRows, int nColumns) { Write(ID, (UInt16*)pMatrix, nRows, nColumns); }			
			void Write(UInt32 ID, const Int32* pMatrix, int nRows, int nColumns) { Write(ID, (UInt32*)pMatrix, nRows, nColumns); }			
			void Write(UInt32 ID, const Int64* pMatrix, int nRows, int nColumns) { Write(ID, (UInt64*)pMatrix, nRows, nColumns); }			
			
			void Write(UInt32 ID, const float* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(ID);
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(UInt32 ID, const double* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(ID);
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			/** Matrix Writers: By Inline Identification **/

			void Write(string Name, const byte* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(Name, "matrix-U8");
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(string Name, const UInt16* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(Name, "matrix-U16");
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(string Name, const UInt32* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(Name, "matrix-U32");
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(string Name, const UInt64* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(Name, "matrix-U64");
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(string Name, const char* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(Name, "matrix-I8");
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(string Name, const Int16* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(Name, "matrix-I16");
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(string Name, const Int32* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(Name, "matrix-I32");
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(string Name, const Int64* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(Name, "matrix-I64");
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(string Name, const float* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(Name, "matrix-SF");
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			void Write(string Name, const double* pMatrix, int nRows, int nColumns)
			{
				WriteStartNode(Name, "matrix-DF");
				m_pWriter->WriteCompact64(nColumns);
				m_pWriter->WriteCompact64(nRows);
				m_pWriter->IsLittleEndian = IsLEArray(); m_pWriter->Write(pMatrix, nRows * nColumns);
			}

			/** Matrix Writers: By Association **/

			template<typename T> void Write(const Association& Identity, const T* pData, int nRows, int nColumns) { if (Identity.IsInlineIdentification()) Write(Identity.Name,pData,nRows,nColumns); else Write(Identity.DMLID,pData,nRows,nColumns); }

			/** Reserved Space Writers **/

			static UInt64 PaddingNodeHeadSize;

			bool CanSeek() { return m_pWriter->m_pStream->CanSeek(); }
			Int64 GetPosition() { return m_pWriter->m_pStream->GetPosition(); }
			void Seek(Int64 Position) { m_pWriter->m_pStream->Seek(Position, SeekOrigin::Begin);  }

			/// <summary>
			/// WriteReservedSpace() emits a sequence of padding into the stream of length 'ReservedSpace'.
			/// In seekable streams, the sequence may be overwritten by DML later, although another WriteReserveSpace()
			/// call must be made at the end of the rewrite to "re-reserve" any portion not utilized in
			/// the rewrite.
			/// </summary>
			/// <param name="ReservedSpace">The number of bytes of reserved space to emit.  The node head
			/// will be part of the reserved space.  For example, a ReservedSpace value of 1 will result in
			/// a single-byte padding node.</param>
			void WriteReservedSpace(UInt64 ReservedSpace)
			{
				if (ReservedSpace < 8)
				{
					for (int ii = 0; ii < (int)ReservedSpace; ii++) m_pWriter->Write((byte)dmltsl::dml3::WholeDMLPaddingByte);
				}
				else
				{
					/** Multi-byte padding nodes include a data size value which is itself encoded
					 *  as Compact-64.  A Compact-64's encoded size is variable, which makes hitting
					 *  an intended size a bit tricky.  We'll emit some single-byte paddings to round 
					 *  it out prior to writing the multi-byte padding.
					 */
					UInt64 DataSize = ReservedSpace - PaddingNodeHeadSize - 1ull;
					UInt64 SizeSize = (UInt64)BinaryWriter::SizeCompact64(DataSize);
					while (PaddingNodeHeadSize + SizeSize + DataSize > ReservedSpace)
					{
						DataSize--;
						SizeSize = (UInt64)BinaryWriter::SizeCompact64(DataSize);
					}
					while (PaddingNodeHeadSize + SizeSize + DataSize < ReservedSpace)
					{
						// Emit some single-byte padding to round it out.
						m_pWriter->Write((byte)dmltsl::dml3::WholeDMLPaddingByte);
						ReservedSpace--;
					}
					WriteStartNode(dmltsl::dml3::idDMLPadding);
					ReservedSpace -= PaddingNodeHeadSize;
					m_pWriter->WriteCompact64(DataSize);
					ReservedSpace -= SizeSize;

					static const int BufferSize = 4090;
					byte* pBuffer = new byte [BufferSize];
					try
					{
						memset(pBuffer, 0, BufferSize);
						while (ReservedSpace > 0)
						{
							UInt64 WriteSize = ReservedSpace;
							if (WriteSize > (UInt64)BufferSize) WriteSize = BufferSize;
							m_pWriter->Write(pBuffer, WriteSize);
							ReservedSpace -= WriteSize;
						}
					}
					catch (std::exception&) { delete[] pBuffer; throw; }
					delete[] pBuffer;
				}
			}

			/** Misc. Writers **/

			/// <summary>
			/// WriteComment() can be used to emit a comment node.
			/// </summary>
			/// <param name="Text">Text to incorporate into comment.</param>
			void WriteComment(string Text)
			{				
				WriteStartNode(dmltsl::dml3::idDMLComment);
				m_pWriter->WriteCompact64(Text.length());
				m_pWriter->Write(Text.c_str(), Text.length());
			}

#			if 0
			/// <summary>
			/// Call WriteStartExtension() to write an extended primitive node.  The WriteStartExtension()
			/// method writes the node head with the given parameters, then returns an EndianBinaryWriter
			/// which can be used to write the node content.
			/// </summary>
			/// <param name="ID">DMLID of the node.</param>
			/// <param name="MultipurposeBit">Value of the multipurpose bit for the node.</param>
			/// <returns>An EndianBinaryWriter which can be used to write the node's content.</returns>
			public EndianBinaryWriter WriteStartExtension(UInt32 ID, bool MultipurposeBit)
			{
				WriteStartNode(ID, MultipurposeBit);
				return Writer;
			}

			/// <summary>
			/// Call WriteStartExtension() to write an extended primitive node.  The WriteStartExtension()
			/// method writes the node head with the given parameters, then returns an EndianBinaryWriter
			/// which can be used to write the node content.  This overload generates an inline identification
			/// node.
			/// </summary>
			/// <param name="Name">XML-Compatible name of the node.</param>
			/// <param name="NodeType">Type string identifying the node primitive.</param>
			/// <param name="MultipurposeBit">Value of the multipurpose bit for the node.</param>
			/// <returns>An EndianBinaryWriter which can be used to write the node's content.</returns>
			public EndianBinaryWriter WriteStartExtension(string Name, string NodeType, bool MultipurposeBit)
			{
				WriteStartNode(Name, NodeType, MultipurposeBit);
				return Writer;
			}
#			endif

			/** Header Writers **/

			/// <summary>
			/// WriteHeader() emits the DML header that must begin every DML document.  Although a header is required on a
			/// DML document, it can be generated directly using other DmlWriter calls.  WriteHeader() is a convenience
			/// function only.  Include-Primitives references are added to the header for any sets have been successfully 
			/// added to the DmlWriter using the AddPrimitiveSet() call before calling WriteHeader().
			/// </summary>
			/// <param name="TranslationURI">URI providing the DML Translation document for the format.  Can be an empty string if the document will use
			/// only inline identification.</param>
			/// <param name="TranslationURN">URN providing verification of the DML Translation for this content.  Can be an empty string.</param>
			/// <param name="DocType">Text describing the file format.  Recommended to match to the DML top-level container name.  Can be an empty string.</param>			
			void WriteHeader(string TranslationURI = S(""), string TranslationURN = S(""), string DocType = S(""))
			{
				WriteStartContainer(dmltsl::dml3::idDMLHeader);
				Write(dmltsl::dml3::idDMLVersion, DMLVersion);
				Write(dmltsl::dml3::idDMLReadVersion, DMLReadVersion);
				if (DocType.length() > 0) Write(dmltsl::dml3::idDMLDocType, DocType);
				WriteEndAttributes();

				if (TranslationURI.length() > 0)
				{
					WriteStartContainer(dmltsl::tsl2::idDMLIncludeTranslation);
					Write(dmltsl::tsl2::idDML_URI, TranslationURI);
					if (TranslationURN.length() > 0) Write(dmltsl::tsl2::idDML_URN, TranslationURN);
					WriteEndContainer();
				}				

				if (CommonCodec != Codecs::NotLoaded)
				{
					WriteStartContainer(dmltsl::tsl2::idDMLIncludePrimitives);
					Write(dmltsl::tsl2::idDMLSet, "common");
					switch (CommonCodec)
					{
					case Codecs::LE: Write(dmltsl::tsl2::idDMLCodec, "le"); break;
					case Codecs::BE: Write(dmltsl::tsl2::idDMLCodec, "be"); break;
					default: throw NotSupportedException();
					}
					WriteEndContainer();
				}

				if (ArrayCodec != Codecs::NotLoaded)
				{
					WriteStartContainer(dmltsl::tsl2::idDMLIncludePrimitives);
					Write(dmltsl::tsl2::idDMLSet, "arrays");
					switch (ArrayCodec)
					{
					case Codecs::LE: Write(dmltsl::tsl2::idDMLCodec, "le"); break;
					case Codecs::BE: Write(dmltsl::tsl2::idDMLCodec, "be"); break;
					default: throw NotSupportedException();
					}
					WriteEndContainer();
				}

				/*
				if (PrimitiveSets != nullptr)
				{
					for (int ii=0; ii < nPrimitiveSets; ii++)
					{
						PrimitiveSet& ps = PrimitiveSets[ii];

						// Write the <DML:Include-Primitives /> directive to the header.
						WriteStartContainer(dmltsl::tsl2::idDMLIncludePrimitives);
						Write(dmltsl::tsl2::idDMLSet, ps.Set);
						if (ps.Codec.length() > 0) Write(dmltsl::tsl2::idDMLCodec, ps.Codec);						
						if (ps.CodecURI.length() > 0) Write(dmltsl::tsl2::idDMLCodecURI, ps.CodecURI);
						WriteEndContainer();

						// Also enable the primitive set in the DmlWriter.
						AddPrimitiveSet(ps.Set, ps.Codec);
					}
				}
				*/
				WriteEndContainer();				
			}
		};
	}
}

#endif	// __DmlWriter_h__

//	End of DmlWriter.h

