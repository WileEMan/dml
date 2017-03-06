/**	Encoding.cpp
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
**/

#include "../Platforms/Platforms.h"

#ifdef _WINDOWS
#include <Windows.h>
#else
#include <stdlib.h>
#endif

#include "Encoding.h"
#include "../Exceptions.h"

typedef wchar_t wchar;

namespace wb
{
	namespace text
	{		
		/*static*/ UTF8Encoding Encoding::UTF8;
		/*static*/ const Encoding& Encoding::Default = *(Encoding *)&Encoding::UTF8;		
	}

	string to_string(const wstring& str, const text::Encoding& UsingEncoding /*= Encoding::Default*/)
	{
		size_t sz_full = str.length();
		if (sz_full > Int32_MaxValue) throw ArgumentOutOfRangeException("Exceeded maximum supported string length for this conversion.");
		int sz = (int)sz_full;
		#if defined(_WINDOWS)
			int nd = WideCharToMultiByte(UsingEncoding.GetCodePage(), 0, &str[0], sz, NULL, 0, NULL, NULL);
			string ret(nd, 0);
			int w = WideCharToMultiByte(UsingEncoding.GetCodePage(), 0, &str[0], sz, &ret[0], nd, NULL, NULL);
			if (w != sz) {				
				throw Exception(S("Invalid size written during wide string to multibyte string conversion."));
			}
			return ret;
		#else
			const wchar_t* p = str.c_str();
			char* tp = new char[sz];
			size_t w = wcstombs(tp, p, sz);
			if (w != (size_t)sz) {
				delete[] tp;				
				throw Exception(S("Invalid size written during wide string to multibyte string conversion."));
			}
			string ret(tp);
			delete[] tp;
			return ret;
		#endif
	}
		
	wstring to_wstring(const string& str, const text::Encoding& UsingEncoding /*= Encoding::Default*/)
	{
		#if defined(_WINDOWS)
			size_t sz_full = str.length();
			if (sz_full > Int32_MaxValue) throw ArgumentOutOfRangeException("Exceeded maximum supported string length for this conversion.");
			int sz = (int)sz_full;
			int nd = MultiByteToWideChar(UsingEncoding.GetCodePage(), 0, &str[0], sz, NULL, 0);
			wstring ret(nd, 0);
			int w = MultiByteToWideChar(UsingEncoding.GetCodePage(), 0, &str[0], sz, &ret[0], nd);
			if (w != sz) {
				throw Exception(S("Invalid size written during wide string to multibyte string conversion."));
			}
			return ret;
		#else
			const char* p = str.c_str();
			size_t len = str.length();
			size_t sz = len * sizeof(wchar);
			wchar* tp = new wchar[sz];
			size_t w = mbstowcs(tp, p, sz);
			if (w != len) {
				delete[] tp;
				throw Exception(S("Invalid size written during wide string to multibyte string conversion."));
			}
			wstring ret(tp);
			delete[] tp;
			return ret;
		#endif
	}
}

//	End of Encoding.cpp
