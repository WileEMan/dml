/*	EndianBinaryReader.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
*/

#ifndef __EndianBinaryReader_h__
#define __EndianBinaryReader_h__

#include "../Platforms/Platforms.h"
#include "Streams.h"
#include "../Memory Management/Allocation.h"

namespace wb
{
	namespace io
	{
		class BinaryReader
		{			
			bool IsPlatformLittleEndian;

			static bool TestPlatformLittleEndian()
			{
				union {
					UInt32 i;
					char c[4];
				} bint = {0x01020304};

				return (bint.c[0] == 1);
			}

			void ReversedRead(byte *pBuffer, Int64 nLength)
			{
				int ch;
				while (nLength > 0) { 
					nLength--; 
					ch = m_pStream->ReadByte();
					if (ch < 0) throw EndOfStreamException();
					pBuffer[nLength] = ch;
				}
			}

			void EndianRead(byte *pBuffer, Int64 nLength)
			{
				if (IsLittleEndian == IsPlatformLittleEndian) Read(pBuffer, nLength);
				else ReversedRead(pBuffer, nLength);
			}

		public:

			memory::r_ptr<Stream> m_pStream;

			/** Initialization and cleanup **/

			BinaryReader(memory::r_ptr<Stream>&& Stream, bool LittleEndianStream)
				:	
				IsPlatformLittleEndian(TestPlatformLittleEndian()),
				m_pStream(Stream),				
				IsLittleEndian(LittleEndianStream)
			{
			}

			/*
			BinaryReader(Stream* pStream, bool LittleEndianStream)
				:									
				IsPlatformLittleEndian(TestPlatformLittleEndian()),
				m_pStream(pStream),
				IsLittleEndian(LittleEndianStream)
			{
			}
			*/
		
			/**
			std::ostream* DetachStream()
			{
				std::ostream* ret = m_pWriter;
				m_pWriter = nullptr;
				return ret;
			}
			**/

			~BinaryReader() { m_pStream = nullptr; }

			/// <summary>IsLittleEndian indicates that the stream is operating in little (true) or big (false) endian. </summary>
			bool IsLittleEndian;

			/*** Elementary readers ***/

			byte ReadByte() 
			{ 
				int ch = m_pStream->ReadByte();  
				if (ch < 0) throw EndOfStreamException();
				return (byte)ch;
			}

			void Read(byte *pBuffer, Int64 nLength) 
			{ 
				Int64 nRead = m_pStream->Read(pBuffer, nLength); 
				if (nRead == nLength) return;
				if (nRead < 0) throw IOException();
				throw EndOfStreamException();
			}

			void Read(char *pBuffer, Int64 nLength) { Read((byte *)pBuffer, nLength); }

			/** Compact-Integer readers (Endian-Independent) **/

			UInt32 ReadCompact32()
			{
				UInt32 ret = 0;
				byte ch = ReadByte();

				if ((ch & 0x80) == 0x80)
					ret = ((UInt32)ch & 0x7F);
				else if ((ch & 0xC0) == 0x40)
				{
					ret = ((UInt32)ch & 0x3F) << 8;
					ret |= ((UInt32)ReadByte());
				}
				else if ((ch & 0xE0) == 0x20)
				{
					ret |= ((UInt32)ch & 0x1F) << 16;
					ret |= ((UInt32)ReadByte()) << 8;
					ret |= ((UInt32)ReadByte());
				}
				else if ((ch & 0xF0) == 0x10)
				{
					ret |= ((UInt32)ch & 0x0F) << 24;
					ret |= ((UInt32)ReadByte()) << 16;
					ret |= ((UInt32)ReadByte()) << 8;
					ret |= ((UInt32)ReadByte());
				}
				else if (ch == 0x08)
				{
					ret |= ((UInt32)ReadByte()) << 24;
					ret |= ((UInt32)ReadByte()) << 16;
					ret |= ((UInt32)ReadByte()) << 8;
					ret |= ((UInt32)ReadByte());
				}
				else
				{
					if (m_pStream->CanSeek())
						throw Exception("Invalid Compact-32 encoding sequence at position [" + to_string(m_pStream->GetPosition()) + "].");
					else
						throw FormatException("Invalid Compact-32 encoding sequence.");
				}
				return ret;
			}

			UInt64 ReadCompact64()
			{
				UInt64 ret = 0;
				byte ch = ReadByte();

				if ((ch & 0x80) == 0x80) ret = ((UInt64)ch & 0x7F);
				else if ((ch & 0xC0) == 0x40)
				{
					ret = ((UInt64)ch & 0x3F) << 8;
					ret |= ((UInt64)ReadByte());
				}
				else if ((ch & 0xE0) == 0x20)
				{
					ret = ((UInt64)ch & 0x1F) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
				}
				else if ((ch & 0xF0) == 0x10)
				{
					ret = ((UInt64)ch & 0x0F) << 24;
					ret |= ((UInt64)ReadByte()) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
				}
				else if ((ch & 0xF8) == 0x08)
				{
					ret = ((UInt64)ch & 0x07) << 32;
					ret |= ((UInt64)ReadByte()) << 24;
					ret |= ((UInt64)ReadByte()) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
				}
				else if ((ch & 0xFC) == 0x04)
				{
					ret = ((UInt64)ch & 0x03) << 40;
					ret |= ((UInt64)ReadByte()) << 32;
					ret |= ((UInt64)ReadByte()) << 24;
					ret |= ((UInt64)ReadByte()) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
				}
				else if ((ch & 0xFE) == 0x02)
				{
					ret = ((UInt64)ch & 0x01) << 48;
					ret |= ((UInt64)ReadByte()) << 40;
					ret |= ((UInt64)ReadByte()) << 32;
					ret |= ((UInt64)ReadByte()) << 24;
					ret |= ((UInt64)ReadByte()) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
				}
				else if (ch == 0x01)
				{
					ret = ((UInt64)ReadByte()) << 48;
					ret |= ((UInt64)ReadByte()) << 40;
					ret |= ((UInt64)ReadByte()) << 32;
					ret |= ((UInt64)ReadByte()) << 24;
					ret |= ((UInt64)ReadByte()) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
				}
				else if (ch == 0)
				{
					ret = ((UInt64)ReadByte()) << 56;
					ret |= ((UInt64)ReadByte()) << 48;
					ret |= ((UInt64)ReadByte()) << 40;
					ret |= ((UInt64)ReadByte()) << 32;
					ret |= ((UInt64)ReadByte()) << 24;
					ret |= ((UInt64)ReadByte()) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
				}
				else throw Exception();     // This exception would indicate a logic flaw in this routine.
            
				return ret;
			}

			Int64 ReadCompactS64()
			{
				UInt64 ret = 0;
				byte ch = ReadByte();

				if ((ch & 0x80) == 0x80) return SignExtend(((UInt64)ch & 0x7F), 7);
				else if ((ch & 0xC0) == 0x40)
				{
					ret = ((UInt64)ch & 0x3F) << 8;
					ret |= ((UInt64)ReadByte());
					return SignExtend(ret, 14);
				}
				else if ((ch & 0xE0) == 0x20)
				{
					ret = ((UInt64)ch & 0x1F) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
					return SignExtend(ret, 21);
				}
				else if ((ch & 0xF0) == 0x10)
				{
					ret = ((UInt64)ch & 0x0F) << 24;
					ret |= ((UInt64)ReadByte()) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
					return SignExtend(ret, 28);
				}
				else if ((ch & 0xF8) == 0x08)
				{
					ret = ((UInt64)ch & 0x07) << 32;
					ret |= ((UInt64)ReadByte()) << 24;
					ret |= ((UInt64)ReadByte()) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
					return SignExtend(ret, 35);
				}
				else if ((ch & 0xFC) == 0x04)
				{
					ret = ((UInt64)ch & 0x03) << 40;
					ret |= ((UInt64)ReadByte()) << 32;
					ret |= ((UInt64)ReadByte()) << 24;
					ret |= ((UInt64)ReadByte()) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
					return SignExtend(ret, 42);
				}
				else if ((ch & 0xFE) == 0x02)
				{
					ret = ((UInt64)ch & 0x01) << 48;
					ret |= ((UInt64)ReadByte()) << 40;
					ret |= ((UInt64)ReadByte()) << 32;
					ret |= ((UInt64)ReadByte()) << 24;
					ret |= ((UInt64)ReadByte()) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
					return SignExtend(ret, 49);
				}
				else if (ch == 0x01)
				{
					ret = ((UInt64)ReadByte()) << 48;
					ret |= ((UInt64)ReadByte()) << 40;
					ret |= ((UInt64)ReadByte()) << 32;
					ret |= ((UInt64)ReadByte()) << 24;
					ret |= ((UInt64)ReadByte()) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
					return SignExtend(ret, 56);
				}
				else if (ch == 0)
				{
					ret = ((UInt64)ReadByte()) << 56;
					ret |= ((UInt64)ReadByte()) << 48;
					ret |= ((UInt64)ReadByte()) << 40;
					ret |= ((UInt64)ReadByte()) << 32;
					ret |= ((UInt64)ReadByte()) << 24;
					ret |= ((UInt64)ReadByte()) << 16;
					ret |= ((UInt64)ReadByte()) << 8;
					ret |= ((UInt64)ReadByte());
					return (long)ret;
				}
				else throw Exception();     // This exception would indicate a logic flaw in this routine.            
			}

			Int64 SignExtend(UInt64 Value, int nBits)
			{
				// See ReadI() for similar explanation...            
				UInt64 mask = (1UL << (nBits - 1));
				if ((Value & mask) != 0UL)
				{
					UInt64 extendmask = UInt64_MaxValue << nBits;
					return (Int64)(Value | extendmask);
				}
				return (Int64)Value;
			}

			/** Scalar primitive readers **/

			UInt16 ReadUInt16()
			{
				if (IsLittleEndian)
					return (UInt16)((UInt16)ReadByte() | ((UInt16)ReadByte() << 8));
				else           
					return (UInt16)(((UInt16)ReadByte() << 8) | (UInt16)ReadByte());
			}

			UInt32 ReadUInt24()
			{
				if (IsLittleEndian)
					return (UInt32)ReadByte() | ((UInt32)ReadByte() << 8) | ((UInt32)ReadByte() << 16);
				else
					return ((UInt32)ReadByte() << 16) | ((UInt32)ReadByte() << 8) | (UInt32)ReadByte();
			}

			UInt32 ReadUInt32()
			{
				if (IsLittleEndian)
					return (UInt32)ReadByte() | ((UInt32)ReadByte() << 8) | ((UInt32)ReadByte() << 16) | ((UInt32)ReadByte() << 24);            
				else            
					return ((UInt32)ReadByte() << 24) | ((UInt32)ReadByte() << 16) | ((UInt32)ReadByte() << 8) | (UInt32)ReadByte();            
			}

			UInt64 ReadUInt64()
			{
				if (IsLittleEndian)            
					return (UInt64)ReadByte() | ((UInt64)ReadByte() << 8) | ((UInt64)ReadByte() << 16) | ((UInt64)ReadByte() << 24)
						| ((UInt64)ReadByte() << 32) | ((UInt64)ReadByte() << 40) | ((UInt64)ReadByte() << 48) | ((UInt64)ReadByte() << 56);            
				else            
					return ((UInt64)ReadByte() << 56) | ((UInt64)ReadByte() << 48) | ((UInt64)ReadByte() << 40) | ((UInt64)ReadByte() << 32)
						| ((UInt64)ReadByte() << 24) | ((UInt64)ReadByte() << 16) | ((UInt64)ReadByte() << 8) | (UInt64)ReadByte();            
			}

			Int8 ReadSByte() { return (Int8)ReadByte(); }
			Int16 ReadInt16() { return (Int16)ReadUInt16(); }
			Int32 ReadInt32() { return (Int32)ReadUInt32(); }
			Int64 ReadInt64() { return (Int64)ReadUInt64(); }

			/// <summary>
			/// ReadInt24() reads a signed 24-bit value from the stream.  The
			/// value is sign-extended to be properly represented in the 32-bit
			/// return value.
			/// </summary>
			/// <returns>A 32-bit representation of the 24-bit stream value</returns>
			Int32 ReadInt24()
			{
				// See ReadI() for logic description.
				UInt32 ret = ReadUInt24();
				UInt32 mask = 0x800000UL;
				if ((ret & mask) != 0UL)
				{
					UInt32 extendmask = 0xFF000000UL;
					return (Int32)(ret | extendmask);
				}
				return (Int32)ret;
			}

			float ReadSingle() { float ret; EndianRead((byte*)&ret, sizeof(ret)); return ret; }
			double ReadDouble() { double ret; EndianRead((byte*)&ret, sizeof(ret)); return ret; }
			
			/** 1-D Array Primitives **/

			void Read(UInt16* pBuffer, Int64 Elements)
			{
				Read((byte *)pBuffer, Elements * sizeof(pBuffer[0]));
				if (IsLittleEndian != IsPlatformLittleEndian)
					for (int ii=0; ii < Elements; ii++) pBuffer[ii] = SwapEndian(pBuffer[ii]);
			}

			void Read(UInt32* pBuffer, Int64 Elements)
			{
				Read((byte *)pBuffer, Elements * sizeof(pBuffer[0]));
				if (IsLittleEndian != IsPlatformLittleEndian)
					for (int ii=0; ii < Elements; ii++) pBuffer[ii] = SwapEndian(pBuffer[ii]);
			}

			void Read(UInt64* pBuffer, Int64 Elements)
			{
				Read((byte *)pBuffer, Elements * sizeof(pBuffer[0]));
				if (IsLittleEndian != IsPlatformLittleEndian)
					for (int ii=0; ii < Elements; ii++) pBuffer[ii] = SwapEndian(pBuffer[ii]);
			}

			void Read(Int16* pBuffer, Int64 Elements) { return Read((UInt16*)pBuffer, Elements); }
			void Read(Int32* pBuffer, Int64 Elements) { return Read((UInt32*)pBuffer, Elements); }
			void Read(Int64* pBuffer, Int64 Elements) { return Read((UInt64*)pBuffer, Elements); }
			void Read(float* pBuffer, Int64 Elements) { return Read((UInt32*)pBuffer, Elements); }
			void Read(double* pBuffer, Int64 Elements) { return Read((UInt64*)pBuffer, Elements); }
		};
	}
}

#endif	// __EndianBinaryReader_h__

//	End of EndianBinaryReader.h

