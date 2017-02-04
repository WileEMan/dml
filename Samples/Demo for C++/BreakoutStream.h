/////////
//	BreakoutStream.h
//
//	Provides the BreakoutStream class, which provides a wb::io::Stream compatible Stream wrapper
//  that records a text log of all bytes read and written on the stream as well as position seeks.
//  The text log can be read with GetBreakout() and cleared with ClearBreakout().  
//
//	The BreakoutStream is particularly useful for breaking out the bytes in a binary file and displaying
//  them alongside the processing that is happening on those bytes.
////

#ifndef __BreakoutStream_h__
#define __BreakoutStream_h__

using namespace wb;
using namespace wb::io;
using namespace wb::memory;

class BreakoutStream : public wb::io::Stream
{
	r_ptr<wb::io::Stream>	m_pBase;
	string	CurrentBreakout;

public:	
	/** Initialization **/
	BreakoutStream() { }
	BreakoutStream(r_ptr<wb::io::Stream>&& Base) : m_pBase(std::move(Base)) { }
	BreakoutStream(const BreakoutStream& cp) { throw Exception("Cannot copy a BreakoutStream object."); }
	BreakoutStream& operator=(const BreakoutStream& cp) { throw Exception("Cannot copy a BreakoutStream object."); }
	BreakoutStream(BreakoutStream&& mv) { operator=(mv); }
	BreakoutStream& operator=(BreakoutStream&& mv) 
	{
		m_pBase = std::move(mv.m_pBase);
		CurrentBreakout = std::move(mv.CurrentBreakout);
		return *this;
	}

	/** Breakout Access & Control **/
	void ClearBreakout() { CurrentBreakout.clear(); }
	const string& GetBreakout() const { return CurrentBreakout; }

	/** Implementation **/
	bool CanRead() override { return m_pBase->CanRead(); }
	bool CanWrite() override { return m_pBase->CanWrite(); }
	bool CanSeek() override { return m_pBase->CanSeek(); }

	int ReadByte() override 
	{
		int ch = m_pBase->ReadByte();
		if (ch == -1) return -1;
		CurrentBreakout = CurrentBreakout + " " + to_hex_string((UInt8)ch);
		return ch;
	}

	Int64 Read(void* pBuffer, Int64 nLength) override 
	{
		Int64 nBytes = m_pBase->Read(pBuffer, nLength);
		for (Int64 ii=0; ii < nBytes; ii++)
			CurrentBreakout = CurrentBreakout + " " + to_hex_string((UInt8)((byte*)pBuffer + ii));
		return nBytes;
	}

	void WriteByte(byte ch) override 
	{
		CurrentBreakout += " ";
		CurrentBreakout += to_hex_string(ch);
		m_pBase->WriteByte(ch);
	}

	void Write(const void *pBuffer, Int64 nLength) override 
	{
		byte* pbBuffer = (byte*)pBuffer;
		for (Int64 ii=0; ii < nLength; ii++)
			CurrentBreakout = CurrentBreakout + " " + to_hex_string((UInt8)(pbBuffer + ii));
		m_pBase->Write(pBuffer, nLength);
	}
	
	void Seek(Int64 offset, SeekOrigin origin) override 
	{
		switch (origin)
		{
		case SeekOrigin::Begin: CurrentBreakout += " ->[" + to_hex_string((UInt64)offset) + "]"; break;
		case SeekOrigin::Current: CurrentBreakout += " ->[+" + to_hex_string((UInt64)offset) + "]"; break;
		case SeekOrigin::End: CurrentBreakout += " ->[End+(" + to_hex_string((UInt64)offset) + ")]"; break;
		}
		m_pBase->Seek(offset, origin);
	}

	Int64 GetPosition() const override { return m_pBase->GetPosition(); }
	Int64 GetLength() const override { return m_pBase->GetLength(); }

	void Flush() override { m_pBase->Flush(); }
	void Close() override { m_pBase->Close(); }
};

#endif	// __BreakoutStream_h__

//	End of BreakoutStream.h
