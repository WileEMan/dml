/*	Exceptions.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)

	Example Usage:

		try
		{
			// Execute some code...
			if (ThingsGoBadly) throw NetworkException("An example of a specific exception.");
			// Call some other code that might throw a std::exception...
		}
		catch (wb::NetworkException& ex)
		{
			printf("A network error occurred:\n    %s\n", ex.GetMessageW().c_str());
			printf("Press any key to exit.\n");
			_getch();
		}
		catch (std::exception& ex)
		{
			printf("Exception occurred:\n    %s\n", ex.what());
			printf("Press any key to exit.\n");
			_getch();
		}
*/

#ifndef __WBExceptions_h__
#define __WBExceptions_h__

#ifdef UseSTL
#include <stdexcept>
#endif
#include "Platforms/Platforms.h"
#include "Text/String.h"
#include "Text/Encoding.h"

#ifdef _WINDOWS
#include <comutil.h>
#include <comdef.h>
#endif

#undef GetMessage			// Avoid conflict with MSVC macro.

#ifndef UseSTL
namespace std
{
	// Although we will not be throwing any std::exception's from our code, defining it
	// allows us to catch std::exception's whether using STL or not.
	class exception
	{   // base of all library exceptions
	public:
		exception() { msg = ""; }
		exception(const char * const &_msg) { msg = _msg; }		
		exception(const exception& cp) { msg = cp.msg; }
		exception& operator=(const exception& cp) { msg = cp.msg; return *this; }
		virtual ~exception() { }
		virtual const char * what() const throw() { return msg; }

	private:
		const char * msg;		
	};
}
#endif

namespace wb
{	
	class Exception 
		: public std::exception
	{
	protected:
		// Although the Microsoft implementation of std::exception includes a message,
		// this allows portability.  Also, we may not be compiling in UseSTL mode.
		string Message;

		#if 0
		void CopyFrom(const std::exception &right) { 
#			if UseSTL
			std::exception::operator=(right);
#			endif
			Message = right.what();
		}
		#endif

	public:
		Exception() { }
		Exception(const char * const &message) { Message = message; }
		Exception(const string& message) { Message = message; }
		Exception(const Exception &right) { Message = right.Message; }
		Exception(Exception&& from) { Message = std::move(from.Message); }
		Exception& operator=(const Exception &right) { 
			std::exception::operator=(right);
			Message = right.Message; 
			return *this; 
		}
		~Exception() throw() { }		
		virtual string GetMessage() const { return Message; }
		inline string GetMessageA() const { return Message; }		// In case the MSVC macros interfere.
		inline string GetMessageW() const { return Message; }		// In case the MSVC macros interfere.
		const char *what() const throw() override { return Message.c_str(); }

		static void ThrowFromErrno(int FromErrno);
		#if defined(_WINDOWS)
		static void ThrowFromWin32(UInt32 LastError);
		static void ThrowFromHRESULT(HRESULT LastError);
		//template<class T> static void ThrowFromHRESULT(HRESULT LastError, T* pObject);	
		#endif
	};

#	define DeclareGenericException(ExceptionClass,DefaultMessage)			\
	class ExceptionClass : public Exception {				\
		public:		\
			ExceptionClass() : Exception(DefaultMessage) { }			\
			ExceptionClass(const char * const &message) : Exception(message) { }	\
			ExceptionClass(const string& message) : Exception(message) { }	\
			ExceptionClass(const ExceptionClass &right) : Exception(right) { }	\
			ExceptionClass(ExceptionClass&& from) : Exception(from) { }	\
			ExceptionClass& operator=(const ExceptionClass &right) { Exception::operator=(right); return *this; }		\
	}
	
	DeclareGenericException(ArgumentException, S("An invalid argument was supplied."));
	DeclareGenericException(ArgumentNullException, S("Argument provided was null.")); 
	DeclareGenericException(ArgumentOutOfRangeException, S("Out of range."));	
	DeclareGenericException(DirectoryNotFoundException, S("Directory not found."));
	DeclareGenericException(EndOfStreamException, S("Unexpected end of stream."));
	DeclareGenericException(FileNotFoundException, S("File not found."));
	DeclareGenericException(IndexOutOfRangeException, S("Index outside of valid range."));
	DeclareGenericException(IOException, S("An I/O error occurred."));	
	DeclareGenericException(UnauthorizedAccessException, S("Unauthorized access."));	
	DeclareGenericException(NotSupportedException, S("Operation not supported."));	
	DeclareGenericException(NotImplementedException, S("Operation not implemented."));	
	DeclareGenericException(OutOfMemoryException, S("Out of memory."));
	DeclareGenericException(FormatException, S("Invalid format."));	
	DeclareGenericException(TimeoutException, S("A timeout has occurred."));	
	DeclareGenericException(NetworkException, S("Network communication failure."));

	#ifdef _WINDOWS
	class COMException : public Exception {			
	public:
		HRESULT Code;

		COMException() : Exception("An unrecognized COM error has occurred.") { Code = 0; }			
		COMException(const COMException &right) : Exception(right) { Code = right.Code; }
		COMException(COMException&& from) : Exception(from) { Code = from.Code; }
		COMException& operator=(const COMException &right) { Exception::operator=(right); Code = right.Code; return *this; }
		COMException(HRESULT ErrorCode) : Exception()
		{
			char tmp[256];
			sprintf_s(tmp, sizeof(tmp), "COM Error 0x%08X has occurred.", ErrorCode);
			Message = tmp;
			Code = ErrorCode;
		}
		COMException(const string& message, HRESULT ErrorCode) : Exception(message) { Code = ErrorCode; }
	};		
	#endif

	/** Implementations **/

#if 0
	template<typename T> inline /*static*/ void Exception::ThrowFromHRESULT(HRESULT LastError, T* pObject)
	{
		using namespace wb::runtime;

		/*
		switch (LastError)
		{				
		default: break;
		}
		*/
	
		#if 1
		HRESULT hr;
		string description;
		string source;
		string helpfile;

		com_ptr<IErrorInfo> pErrInfo; 		
		hr = ::GetErrorInfo(0, &pErrInfo);
		if(hr != S_OK) throw COMException(LastError);

		BSTR bstrDescription;
		hr = pErrInfo->GetDescription(&bstrDescription);
		if(FAILED(hr)) throw COMException(LastError);
		if (bstrDescription != NULL)
		{
			_bstr_t wrapperDesc(bstrDescription, false);
			description = to_string(wstring((const wchar_t*)wrapperDesc));		
		}

		BSTR bstrSource;
		hr = pErrInfo->GetSource(&bstrSource);
		if(FAILED(hr)) throw COMException(LastError);
		if (bstrSource != NULL)
		{
			_bstr_t wrapperSource(bstrSource, false);
			source = to_string(wstring((const wchar_t*)wrapperSource));
		}

		BSTR bstrHelpFile;
		hr = pErrInfo->GetHelpFile(&bstrHelpFile);
		if(FAILED(hr)) throw COMException(LastError);
		if (bstrHelpFile != NULL)
		{
			_bstr_t wrapperHelpFile(bstrHelpFile, false);
			helpfile = to_string(wstring((const wchar_t*)wrapperHelpFile));
		}		

		if (description.length() == 0)
		{
			UInt32 Win32Code = 0;
			if ((hr & 0xFFFF0000) == MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, 0)) 
			{
				Win32Code = HRESULT_CODE(hr);

				TCHAR*	pszBuffer;
				if (::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_IGNORE_INSERTS, 
					nullptr, Win32Code, 
					MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR)&pszBuffer, sizeof(TCHAR*), nullptr) != 0
					&& pszBuffer != nullptr)
				{
					osstring os_msg = pszBuffer;
					::LocalFree(pszBuffer);
					description = to_string(os_msg);
				}
			}
		}

		char tmp[256];
		sprintf_s(tmp, sizeof(tmp), "COM Error 0x%08X has occurred", LastError);
		string msg = tmp;
		if (description.length() > 0) msg = msg + ": " + description; else msg = msg + ".";
		if (source.length() > 0) msg = msg + "\nSource: " + source;
		if (helpfile.length() > 0) msg = msg + "\nHelp file: " + helpfile;

		throw COMException(msg, LastError);
		#else
		
		if (pObject != nullptr)
		{
			com_ptr<ISupportErrorInfo> pSupportErrorInfo;
			HRESULT hr = pObject->QueryInterface(__uuidof(ISupportErrorInfo), pSupportErrorInfo.attachment());
			if (SUCCEEDED(hr))
			{
				hr = pSupportErrorInfo->InterfaceSupportsErrorInfo(__uuidof(T));
				if (hr == S_OK)
				{
					// This interface supports rich errors.  

					com_ptr<IErrorInfo> pErrInfo;
					hr = ::GetErrorInfo(0, &pErrInfo);
					if (hr == S_OK && pErrInfo != nullptr)
					{
						_com_error err(LastError, pErrInfo.get());	
						string msg;
						msg = to_string(err.ErrorMessage());
						if ((const wchar_t*)err.Description() != nullptr) msg = msg + "Description: " + to_string((const wchar_t*)err.Description());
						if ((const wchar_t*)err.Source() != nullptr) msg = msg + " Source: " + to_string((const wchar_t*)err.Source());								
						throw COMException(msg, LastError);
					}
				}
			}
		}

		_com_error err(LastError);
		string msg;
		if ((const wchar_t*)err.Description() != nullptr) msg = msg + "Description: " + to_string((const wchar_t*)err.Description());
		if ((const wchar_t*)err.Source() != nullptr) msg = msg + " Source: " + to_string((const wchar_t*)err.Source());
		msg = msg + " Additional: " + to_string(err.ErrorMessage());
		throw COMException(msg, LastError);

		#endif
	}		
#endif	
}

#endif	// __WBExceptions_h__

//	End of Exceptions.h


