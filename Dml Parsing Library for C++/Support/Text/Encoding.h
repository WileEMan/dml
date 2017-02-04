/*	Encoding.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
*/

#ifndef __WBEncoding_h__
#define __WBEncoding_h__

/** Table of contents / References **/

namespace wb {
	namespace text {
		class Encoding;
		class UTF8Encoding;
	};
};

/** Dependencies **/

#include "../Platforms/Platforms.h"
#include "../Text/String.h"

//#include <cctype>
//#include <cwctype>
//#include <string>	

/** Content **/

namespace wb
{		
	namespace text
	{

		/** Encodings **/

		class Encoding
		{
		public:		
			virtual uint GetCodePage() const = 0;

			static const Encoding& Default;
			static UTF8Encoding UTF8;
		};	

		class UTF8Encoding : Encoding
		{
		public:		
			uint GetCodePage() const override { return 65001; }
		};
	}

	/** Conversions **/
	
	inline string to_string(const string& str) { return str; }
	inline wstring to_wstring(const wstring& str) { return str; }

	string to_string(const wstring& str, const text::Encoding& UsingEncoding = text::Encoding::Default);
	wstring to_wstring(const string& str, const text::Encoding& UsingEncoding = text::Encoding::Default);

	#if defined(UNICODE) || defined(_MBCS)
	inline wstring to_osstring(const string& str) { return to_wstring(str); }
	inline wstring to_osstring(const wstring& str) { return str; }
	#else
	inline string to_osstring(const string& str) { return str; }
	inline string to_osstring(const wstring& str) { return to_string(str); }
	#endif
}

#endif	// __WBString_h__

//	End of String.h


