/*	EndianBinaryWriter.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
*/

#ifndef __EndianBinaryWriter_h__
#define __EndianBinaryWriter_h__

#include "../Platforms/Platforms.h"
#include "Streams.h"
#include "../Memory Management/Allocation.h"

namespace wb
{
	namespace io
	{
		class BinaryWriter
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

			void ReversedWrite(byte *pBuffer, int nLength)
			{
				while (nLength > 0) { nLength--; m_pStream->WriteByte(pBuffer[nLength]); }
			}

			void EndianWrite(byte *pBuffer, int nLength)
			{
				if (IsLittleEndian == IsPlatformLittleEndian) Write(pBuffer, nLength);
				else ReversedWrite(pBuffer, nLength);
			}

		public:

			memory::r_ptr<Stream>	m_pStream;

			/** Initialization and cleanup **/

			BinaryWriter(memory::r_ptr<Stream>&& Stream, bool LittleEndianStream)
				:	
				IsPlatformLittleEndian(TestPlatformLittleEndian()),
				m_pStream(Stream),
				IsLittleEndian(LittleEndianStream)
			{
			}

			/*
			BinaryWriter(Stream* pStream, bool LittleEndianStream)
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

			~BinaryWriter() { m_pStream = nullptr; }

			/// <summary>IsLittleEndian indicates that the stream is operating in little (true) or big (false) endian. </summary>
			bool IsLittleEndian;

			/*** Elementary writers ***/

			void Write(byte Value) { m_pStream->WriteByte(Value); }
			void Write(const byte *pBuffer, Int64 nLength) { m_pStream->Write(pBuffer, nLength); }
			void Write(const char *pBuffer, Int64 nLength) { m_pStream->Write((const byte *)pBuffer, nLength); }

			/*** Compact-Integer writers (Endian-independent) ***/

			static int SizeCompact32(UInt32 Value)
			{
				if (Value <= 0x7F) return 1;
				else if (Value <= 0x3FFF) return 2;
				else if (Value <= 0x1FFFFF) return 3;
				else if (Value <= 0x0FFFFFFF) return 4;
				else return 5;
			}

			void WriteCompact32(UInt32 Value)
			{
				if (Value <= 0x7F)
					Write((byte)(0x80 | Value));
				else if (Value <= 0x3FFF)
				{
					Write((byte)(0x40 | (Value >> 8)));
					Write((byte)Value);
				}
				else if (Value <= 0x1FFFFF)
				{
					Write((byte)(0x20 | (Value >> 16)));
					Write((byte)(Value >> 8));
					Write((byte)Value);
				}
				else if (Value <= 0x0FFFFFFF)
				{
					Write((byte)(0x10 | (Value >> 24)));
					Write((byte)(Value >> 16));
					Write((byte)(Value >> 8));
					Write((byte)Value);
				}
				else
				{
					Write((byte)(0x08));
					Write((byte)(Value >> 24));
					Write((byte)(Value >> 16));
					Write((byte)(Value >> 8));
					Write((byte)Value);
				}
			}

			static int SizeCompact64(UInt64 Value)
			{
				if (Value <= 0x7Full) return 1;
				else if (Value <= 0x3FFFull) return 2;
				else if (Value <= 0x1FFFFFull) return 3;
				else if (Value <= 0x0FFFFFFFull) return 4;
				else if (Value <= 0x07FFFFFFFFull) return 5;
				else if (Value <= 0x03FFFFFFFFFFull) return 6;
				else if (Value <= 0x01FFFFFFFFFFFFull) return 7;
				else if (Value <= 0x00FFFFFFFFFFFFFFull) return 8;
				else return 9;
			}

			void WriteCompact64(UInt64 Value)
			{            
				if (Value <= 0x7Full)
					Write((byte)(0x80 | (byte)Value));
				else if (Value <= 0x3FFFull)
				{
					Write((byte)(0x40 | (byte)(Value >> 8)));
					Write((byte)Value);
				}
				else if (Value <= 0x1FFFFFull)
				{
					Write((byte)(0x20 | (byte)(Value >> 16)));
					Write((byte)(Value >> 8));
					Write((byte)Value);
				}
				else if (Value <= 0x0FFFFFFFull)
				{
					Write((byte)(0x10 | (Value >> 24)));
					Write((byte)(Value >> 16));
					Write((byte)(Value >> 8));
					Write((byte)Value);
				}
				else if (Value <= 0x07FFFFFFFFull)
				{
					Write((byte)(0x08 | (Value >> 32)));
					Write((byte)(Value >> 24));
					Write((byte)(Value >> 16));
					Write((byte)(Value >> 8));
					Write((byte)Value);
				}
				else if (Value <= 0x03FFFFFFFFFFull)
				{
					Write((byte)(0x04 | (Value >> 40)));
					Write((byte)(Value >> 32));
					Write((byte)(Value >> 24));
					Write((byte)(Value >> 16));
					Write((byte)(Value >> 8));
					Write((byte)Value);
				}
				else if (Value <= 0x01FFFFFFFFFFFFull)
				{
					Write((byte)(0x02 | (Value >> 48)));
					Write((byte)(Value >> 40));
					Write((byte)(Value >> 32));
					Write((byte)(Value >> 24));
					Write((byte)(Value >> 16));
					Write((byte)(Value >> 8));
					Write((byte)Value);
				}
				else if (Value <= 0x00FFFFFFFFFFFFFFull)
				{
					Write((byte)(0x01 | (Value >> 56)));
					Write((byte)(Value >> 48));
					Write((byte)(Value >> 40));
					Write((byte)(Value >> 32));
					Write((byte)(Value >> 24));
					Write((byte)(Value >> 16));
					Write((byte)(Value >> 8));
					Write((byte)Value);
				}
				else             
				{
					Write((byte)(0x00));
					Write((byte)(Value >> 56));
					Write((byte)(Value >> 48));
					Write((byte)(Value >> 40));
					Write((byte)(Value >> 32));
					Write((byte)(Value >> 24));
					Write((byte)(Value >> 16));
					Write((byte)(Value >> 8));
					Write((byte)Value);
				}
			}

			static int SizeCompactS64(Int64 Value)
			{
				// See WriteCompactS64() for commentary.
            
				if (Value >= 0)
				{
					if (Value <= 0x3Fll) return 1;
					else if (Value <= 0x1FFFll) return 2;
					else if (Value <= 0x0FFFFFll) return 3;
					else if (Value <= 0x07FFFFFFll) return 4;
					else if (Value <= 0x03FFFFFFFFll) return 5;
					else if (Value <= 0x01FFFFFFFFFFll) return 6;
					else if (Value <= 0x00FFFFFFFFFFFFll) return 7;
					else if (Value <= 0x007FFFFFFFFFFFFFll) return 8;
					else return 9;                
				}
				else // Value < 0..
				{
					if (Value >= -64ll) return 1;
					else if (Value >= -8192ll) return 2;
					else if (Value >= -1048576ll) return 3;
					else if (Value >= -134217728ll) return 4;
					else if (Value >= -17179869184ll) return 5;
					else if (Value >= -2199023255552ll) return 6;
					else if (Value >= -281474976710656ll) return 7;
					else if (Value >= -36028797018963968ll) return 8;
					else return 9;
				}
			}

			void WriteCompactS64(Int64 Value)
			{
				// Template:
				// Positive values must be <= 01111111 (127) for 8-bits.
				//                            011111111 (255) for 9-bits.
				//                            (2^(n-1))-1 for n-bits.
				// Negative values must be >= -128 for 8-bits.
				//                            -256 for 9-bits.
				//                            -(2^(n-1)) for n-bits.

				if (Value >= 0)
				{
					if (Value <= 0x3Fll)                  // 7-bits, top-bit zero
						Write((byte)(0x80 | Value));
					else if (Value <= 0x1FFF)           // 14-bits, top-bit zero
					{
						Write((byte)(0x40 | (Value >> 8)));
						Write((byte)Value);
					}
					else if (Value <= 0x0FFFFFll)         // 21-bits
					{
						Write((byte)(0x20 | (Value >> 16)));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
					else if (Value <= 0x07FFFFFFll)
					{
						Write((byte)(0x10 | (Value >> 24)));
						Write((byte)(Value >> 16));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
					else if (Value <= 0x03FFFFFFFFll)
					{
						Write((byte)(0x08 | (Value >> 32)));
						Write((byte)(Value >> 24));
						Write((byte)(Value >> 16));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
					else if (Value <= 0x01FFFFFFFFFFll)
					{
						Write((byte)(0x04 | (Value >> 40)));
						Write((byte)(Value >> 32));
						Write((byte)(Value >> 24));
						Write((byte)(Value >> 16));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
					else if (Value <= 0x00FFFFFFFFFFFFll)
					{
						Write((byte)(0x02 | (Value >> 48)));
						Write((byte)(Value >> 40));
						Write((byte)(Value >> 32));
						Write((byte)(Value >> 24));
						Write((byte)(Value >> 16));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
					else if (Value <= 0x007FFFFFFFFFFFFFll)
					{
						Write((byte)(0x01));
						Write((byte)(Value >> 48));
						Write((byte)(Value >> 40));
						Write((byte)(Value >> 32));
						Write((byte)(Value >> 24));
						Write((byte)(Value >> 16));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
					else
					{
						Write((byte)(0x00));
						Write((byte)(Value >> 56));
						Write((byte)(Value >> 48));
						Write((byte)(Value >> 40));
						Write((byte)(Value >> 32));
						Write((byte)(Value >> 24));
						Write((byte)(Value >> 16));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
				}
				else // Value < 0..
				{
					// (Value >= -2^N) where N are the number of bits available for representation.  Referencing
					// the 2's complement format, the top-bit must be a one for this negative number and is
					// excluded from N.  For example, the first template is 1xxx xxxx, showing 7-bits, but N=6.
					if (Value >= -64ll)                   // 6-bits + top-bit one
						Write((byte)(0x80 | (Value & 0x7F)));
					else if (Value >= -8192ll)            // 13-bits + top-bit one
					{
						Write((byte)(0x40 | ((Value >> 8) & 0x3F)));
						Write((byte)Value);
					}
					else if (Value >= -1048576ll)         // 20-bits + top-bit one
					{
						Write((byte)(0x20 | ((Value >> 16) & 0x1F)));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
					else if (Value >= -134217728ll)       // 27-bits + top-one
					{
						Write((byte)(0x10 | ((Value >> 24) & 0x0F)));
						Write((byte)(Value >> 16));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
					else if (Value >= -17179869184ll)     // 34-bits + top-one
					{
						Write((byte)(0x08 | ((Value >> 32) & 0x07)));
						Write((byte)(Value >> 24));
						Write((byte)(Value >> 16));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
					else if (Value >= -2199023255552ll)   // 41-bits + top-one
					{
						Write((byte)(0x04 | ((Value >> 40) & 0x03)));
						Write((byte)(Value >> 32));
						Write((byte)(Value >> 24));
						Write((byte)(Value >> 16));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
					else if (Value >= 281474976710656ll)  // 48-bits + top-one
					{
						Write((byte)(0x02 | ((Value >> 48) & 0x01)));
						Write((byte)(Value >> 40));
						Write((byte)(Value >> 32));
						Write((byte)(Value >> 24));
						Write((byte)(Value >> 16));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
					else if (Value >= 36028797018963968ll)    // 55-bits + top-one
					{
						Write((byte)(0x01));
						Write((byte)(Value >> 48));
						Write((byte)(Value >> 40));
						Write((byte)(Value >> 32));
						Write((byte)(Value >> 24));
						Write((byte)(Value >> 16));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
					else    // 64-bits
					{
						Write((byte)(0x00));
						Write((byte)(Value >> 56));
						Write((byte)(Value >> 48));
						Write((byte)(Value >> 40));
						Write((byte)(Value >> 32));
						Write((byte)(Value >> 24));
						Write((byte)(Value >> 16));
						Write((byte)(Value >> 8));
						Write((byte)Value);
					}
				}
			}

			/** Scalar Primitive Writers **/

			void Write(Int16 Value) { Write((UInt16)Value); }
			void Write(UInt16 Value)
			{
				if (IsLittleEndian)
				{
					Write((byte)(Value)); Write((byte)(Value >> 8));
				}
				else
				{
					Write((byte)(Value >> 8)); Write((byte)(Value));
				}
			}

			void Write(float Value) { EndianWrite((byte *)&Value, sizeof(Value)); }
			void Write(Int32 Value) { EndianWrite((byte *)&Value, sizeof(Value)); }
			void Write(UInt32 Value) { EndianWrite((byte *)&Value, sizeof(Value)); }
			void Write(double Value) { EndianWrite((byte *)&Value, sizeof(Value)); }
			void Write(Int64 Value) { EndianWrite((byte *)&Value, sizeof(Value)); }
			void Write(UInt64 Value) { EndianWrite((byte *)&Value, sizeof(Value)); }
			void WriteUI24(UInt32 Value) { EndianWrite((byte *)&Value, 3); }

			/**** 1-D Array Writers ****/			

			void Write(const UInt16* pData, Int64 nElements)
			{
				if (IsLittleEndian == IsPlatformLittleEndian) Write((byte *)pData, nElements * sizeof(pData[0]));
				else
				{
					byte *pRawData = (byte *)pData;					
					byte ch1, ch2;
					for (int ii=0; ii < nElements; ii++)
					{
						ch1 = *pRawData++; ch2 = *pRawData++;
						Write(ch2); Write(ch1);
					}
				}
			}

			void Write(const Int16* pData, Int64 nElements) { Write((UInt16*)pData, nElements); }

			void Write(const UInt32* pData, Int64 nElements)
			{
				if (IsLittleEndian == IsPlatformLittleEndian) Write((byte*)pData, nElements * sizeof(pData[0]));
				else
				{
					byte *pRawData = (byte *)pData;					
					byte ch1, ch2, ch3, ch4;
					for (int ii=0; ii < nElements; ii++)
					{
						ch1 = *pRawData++; ch2 = *pRawData++; ch3 = *pRawData++; ch4 = *pRawData++;
						Write(ch4); Write(ch3); Write(ch2); Write(ch1);
					}
				}
			}

			void Write(const Int32* pData, Int64 nElements) { Write((UInt32*)pData, nElements); }			
			void Write(const float* pData, Int64 nElements) { Write((UInt32*)pData, nElements); }

			void Write(const UInt64* pData, Int64 nElements)
			{
				if (IsLittleEndian == IsPlatformLittleEndian) Write((byte*)pData, nElements * sizeof(pData[0]));
				else
				{
					byte *pRawData = (byte *)pData;
					byte ch1, ch2, ch3, ch4, ch5, ch6, ch7, ch8;
					for (int ii=0; ii < nElements; ii++)
					{
						ch1 = *pRawData++; ch2 = *pRawData++; ch3 = *pRawData++; ch4 = *pRawData++;
						ch5 = *pRawData++; ch6 = *pRawData++; ch7 = *pRawData++; ch8 = *pRawData++;
						Write(ch8); Write(ch7); Write(ch6); Write(ch5);
						Write(ch4); Write(ch3); Write(ch2); Write(ch1);
					}
				}
			}

			void Write(const Int64* pData, Int64 nElements) { Write((UInt64*)pData, nElements); }
			void Write(const double* pData, Int64 nElements) { Write((UInt64*)pData, nElements); }
		};
	}
}

#endif	// __EndianBinaryWriter_h__

//	End of EndianBinaryWriter.h

