/*	Streams.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
*/

#ifndef __WBStreams_h__
#define __WBStreams_h__

#include "../Platforms/Platforms.h"
#include <fcntl.h>
#include <errno.h>
#if defined(_MSC_VER)
#include <io.h>
#include <tchar.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <share.h>
#elif defined(_LINUX)
#include <sys/types.h>
#include <unistd.h>
#endif

#include "../Exceptions.h"

namespace wb
{
	namespace io
	{		
		enum_class_start(SeekOrigin, int)
		{
			Begin,
			Current,
			End
		}
		enum_class_end(SeekOrigin);		

		/// <summary>Stream provides the virtual (abstract) base class for any streams of data.  Streams can include memory, file, or network
		/// data.  Stream cannot be instantiated directly, and instead a concrete class such as FileStream or MemoryStream should be used.
		/// Streams provide read, write, and seek capabilities where the underlying stream supports it (query CanRead(), CanWrite(), and
		/// CanSeek() if needed).  Reads can be performed one byte at a time with ReadByte(), watching for a -1 to indicate end of stream,
		/// or can be performed as blocks using the Read() call.  Similarly, writes are supported through WriteByte() or Write() for a
		/// block.  Seeking, length, and position calls are also available (if CanSeek() returns true).
		class Stream
		{
		public:
			virtual ~Stream() { Close(); }

			virtual bool CanRead() { return false; }
			virtual bool CanWrite() { return false; }
			virtual bool CanSeek() { return false; }

			/// <summary>Reads one byte from the stream and advances to the next byte position, or returns -1 if at the end of stream.</summary>			
			virtual int ReadByte() { throw NotSupportedException(); }	

			/// <returns>The number of bytes read into the buffer.  This can be less than the requested nLength if not that many bytes are
			/// available, or zero if the end of the stream has been reached.</returns>
			virtual Int64 Read(void *pBuffer, Int64 nLength) 
			{ 
				int ch;
				byte* pDstBuffer = (byte*)pBuffer;
				for (Int64 count = 0; count < nLength; count++)
				{
					ch = ReadByte();
					if (ch < 0) return count;
					*pDstBuffer = (byte)ch; pDstBuffer++;
				}
				return nLength;
			}

			virtual void WriteByte(byte ch) { throw NotSupportedException(); }
			virtual void Write(const void *pBuffer, Int64 nLength) 
			{ 
				byte* pb = (byte *)pBuffer; 
				while (nLength--) WriteByte(*pb++); 
			}

			virtual Int64 GetPosition() const { throw NotSupportedException(); }
			virtual Int64 GetLength() const { throw NotSupportedException(); }
			virtual void Seek(Int64 offset, SeekOrigin origin) { throw NotSupportedException(); }

			virtual void Flush() { }
			virtual void Close() { }
		};				

		/// <summary>Convenience routine that reads the remainder of Source into the Destination stream.  If called
		/// from the start of the Source stream, then this provides a quick way to copy the entirity of one stream
		/// to another, including streams of different types.</summary>
		inline void StreamToStream(Stream& Source, Stream& Destination)
		{
			byte buffer[4096];
			for (;;)
			{
				Int64 nBytes = Source.Read(buffer, sizeof(buffer));
				if (nBytes == 0) return;			
				Destination.Write(buffer, nBytes);
			}
		}

		/// <summary>Reads the remainder of the Source stream into a std::string.</summary>
		inline string ReadToEnd(Stream& Source)
		{
			string ret;
			byte buffer[4096];
			for (;;)
			{
				Int64 nBytes = Source.Read(buffer, sizeof(buffer) - 1);
				if (nBytes == 0) return ret;
				ret.append((const char *)buffer, (int)nBytes);
			}
		}

		/// <summary>Writes an entire std::string to a stream.</summary>
		inline void StringToStream(const string& Source, Stream& Dest)
		{			
			size_t remaining = Source.length();			
			size_t position = 0;
			byte buffer[4096];
			while (remaining > 0)
			{
				int used = 0;
				for (; used < 4096 && remaining > 0; used++, position++, remaining--) buffer[used] = Source.at(position);
				Dest.Write(buffer, used);
			}
		}
	}
}

#endif	// __WBStreams_h__

//	End of Streams.h

