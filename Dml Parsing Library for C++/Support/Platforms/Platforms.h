/*	Platform.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
*/

#ifndef __WbPlatform_h__
#define __WbPlatform_h__

#if (!defined(UseSTL) && !defined(NoSTL))
#	error Please define either UseSTL or NoSTL preprocessor macros to indicate whether the standard template library is in use.
#endif

#if (defined(UseSTL) && defined(NoSTL))
#	error Only one of UseSTL or NoSTL can be defined.  Please review Dml_Configuration.harderr
#endif

#include <math.h>
#include <limits.h>
#if (defined(_MSC_VER))
#include <tchar.h>
typedef unsigned long uint;
#endif
#if (defined(__GNUC__))
#include <sys/types.h>			// For size_t.  Also defines uint.
#endif
#include <float.h>				// For DBL_MIN and DBL_MAX.

typedef unsigned char byte;
typedef unsigned char UInt8;
typedef unsigned short UInt16;
typedef unsigned long UInt32;
typedef unsigned long long UInt64;

typedef signed char Int8;
typedef signed short Int16;
typedef signed long Int32;
typedef signed long long Int64;

#define UInt8_MinValue		0u
#define UInt16_MinValue		0u
#define UInt32_MinValue		0ul
#define UInt64_MinValue		0ull

#define UInt8_MaxValue		UCHAR_MAX
#define UInt16_MaxValue		USHRT_MAX
#define UInt32_MaxValue		ULONG_MAX
#define UInt64_MaxValue		ULLONG_MAX

#define Int8_MinValue		SCHAR_MIN
#define Int16_MinValue		SHRT_MIN
#define Int32_MinValue		LONG_MIN
#define Int64_MinValue		LLONG_MIN

#define Int8_MaxValue		SCHAR_MAX
#define Int16_MaxValue		SHRT_MAX
#define Int32_MaxValue		LONG_MAX
#define Int64_MaxValue		LLONG_MAX

#define Float_MaxValue		FLT_MAX
#define Double_MaxValue		DBL_MAX

#define Float_MinValue		FLT_MIN
#define Double_MinValue		DBL_MIN

static const size_t size_t_MaxValue = (size_t)-1;

#if defined(__GNUC__)
	// Define a single macro to calculate GNUC version.  For example, GCC 3.2.0 will yield GCC_VERSION 30200.
#define GCC_VERSION (__GNUC__ * 10000 + __GNUC_MINOR__ * 100 + __GNUC_PATCHLEVEL__)
#endif

#if (defined(_MSC_VER))
#define override 
#endif

#if (defined(_MSC_VER) && (_MSC_VER > 1800)) || (defined(GCC_VERSION) && (GCC_VERSION >= 40600))
	// Not sure on exact versions.  Also there is confusion between whether the compiler truly supports
	// constexpr or at least recognizes the keyword without error (which is the goal here).  The constexpr_please
	// macro uses constexpr if the compiler supports it, or evaluates to nothing when not supported.  Due to
	// compiler recognition with poor support, but the limitation that the C++ standard forbids applying macros
	// to recognized keywords, the base keyword can't be used.
#define constexpr_please	constexpr
#else
#define constexpr_please
#endif

#if (defined(_MSC_VER) && (_MSC_VER < 1700)) || (defined(GCC_VERSION) && (GCC_VERSION < 40600))
#define noexcept
#endif

#if defined(_WIN32) || defined(_WIN64)
#define _WINDOWS
#else
#define _LINUX
#endif

#if defined(PrimaryModule) || defined(DmlPrimaryModule)
#if defined(UseSTL) && defined(_WINDOWS) && defined(_MSC_VER)
	#if defined(UNICODE)
		#pragma message("Compiling using Visual C++ in Windows with STL, UNICODE enabled.")
	#else
		#pragma message("Compiling using Visual C++ in Windows with STL.")
	#endif
#elif defined(NoSTL) && defined(_WINDOWS) && defined(_MSC_VER)
	#if defined(UNICODE)
		#pragma message("Compiling using Visual C++ in Windows without STL, UNICODE enabled.")
	#else
		#pragma message("Compiling using Visual C++ in Windows without STL.")
	#endif
#endif
#endif

#ifdef GCC_VERSION
    #define MaybeUnused __attribute__((used))
#else
	#define MaybeUnused
#endif

/** Compatibility with older compiler and standards **/

#if (defined(GCC_VERSION) && GCC_VERSION < 40700)
#define override 
#endif

#if defined(UseSTL) && ((defined(_MSC_VER) && _MSC_VER >= 1600) || (defined(GCC_VERSION) && GCC_VERSION >= 40600))
	#include <cstddef>			// For nullptr_t
	namespace wb { typedef std::nullptr_t nullptr_t; }
#else
	#if ((defined(_MSC_VER) && _MSC_VER < 1600) || (defined(GCC_VERSION) && GCC_VERSION < 40600))
	#define nullptr	(0)
	#endif
	namespace wb { typedef void* nullptr_t; }
#endif

/** Provide emulation via macro for strongly-type enums (enum class) when not supported **/
#if defined(GCC_VERSION) && (GCC_VERSION < 40400)	
	#define enum_class_start(x,type)					\
		class x {										\
			typedef type ty;							\
			ty Value;									\
		public:											\
			enum Values
	#define enum_class_end(x)	;						\
			x() { Value = 0; }							\
			x(const x& val) { Value = val.Value; }		\
			x(Values val) { Value = (ty)val; }			\
			explicit x(int val) { Value = (ty)val; }	\
			x& operator=(const x& val) { Value = val.Value; return *this; }		\
			x& operator=(Values val) { Value = (ty)val; return *this; }			\
			bool operator==(int b) const { return (ty)Value == (ty)b; }			\
			bool operator!=(int b) const { return (ty)Value != (ty)b; }			\
			bool operator==(const x& b) const { return Value == b.Value; }		\
			bool operator!=(const x& b) const { return Value != b.Value; }		\
			operator ty() const { return Value; }				\
		};		
	#define enum_type(x)					x::Values
	#define UsingEnumClassEmulation
#else
	#define enum_class_start(x,type)		enum class x : type
	#define enum_class_end(x)				;
	#define enum_type(x)					x
#endif

#ifdef UseSTL
#include <utility>
#else
namespace std
{
	template <class T> struct remove_reference      {typedef T type;};
	template <class T> struct remove_reference<T&>  {typedef T type;};
	template <class T> struct remove_reference<T&&> {typedef T type;};

	template<class T> inline typename remove_reference<T>::type&& move(T&& _Arg)
	{
		return ((typename remove_reference<T>::type&&)_Arg);
	}

	template <class T> inline T&& forward(typename remove_reference<T>::type& _Arg)
	{ return (static_cast<T&&>(_Arg)); }

	template <class T> inline T&& forward(typename remove_reference<T>::type&& _Arg)
	{ return (static_cast<T&&>(_Arg)); }

	template<class T> inline void swap(T& a, T& b)
	{
		T tmp = std::move(a); a = std::move(b); b = std::move(tmp);		
	}
}
#endif

#if defined(_LINUX)
#	include <endian.h>
#	define htonll(x) htobe64(x)
#	define ntohll(x) be64toh(x)
#elif defined(_FreeBSD) || defined(_NetBSD)
#	include <sys/endian.h>
#	define htonll(x) htobe64(x)
#	define ntohll(x) be64toh(x)
#elif defined(_WINDOWS)
	// htonll() provided natively.
#else
#	error Platform support required.
#endif

namespace wb
{
	inline UInt16 SwapEndian(UInt16 val) { return (val << 8) | (val >> 8); }
	inline UInt32 SwapEndian(UInt32 val) { return (val << 24) | ((val <<  8) & 0x00ff0000) | ((val >>  8) & 0x0000ff00) | ((val >> 24) & 0x000000ff); }
	inline UInt64 SwapEndian(UInt64 val) { 
		return (val << 56) | ((val << 40) & 0x00ff000000000000ull) | ((val << 24) & 0x0000ff0000000000ull) | ((val << 8) & 0x000000ff00000000ull)
			| ((val >> 8) & 0x00000000ff000000) | ((val >> 24) & 0x0000000000ff0000ull) | ((val >> 40) & 0x000000000000ff00ull) | ((val >> 56) & 0x00000000000000ffull);
	}
	inline Int16 SwapEndian(Int16 val) { return SwapEndian((UInt16)val); }
	inline Int32 SwapEndian(Int32 val) { return SwapEndian((UInt32)val); }
	inline Int64 SwapEndian(Int64 val) { return SwapEndian((UInt64)val); }
	inline float SwapEndian(float val) { return (float)SwapEndian((UInt32)val); }
	inline double SwapEndian(double val) { return (double)SwapEndian((UInt64)val); }	

	inline Int32	Round32(double dVal){ return (fmod(dVal, 1.0) >= 0.5) ? (((Int32)dVal) + 1) : ((Int32)dVal); }
	inline Int64	Round64(double dVal){ return (fmod(dVal, 1.0) >= 0.5) ? (((Int64)dVal) + 1) : ((Int64)dVal); }	
}

#endif	// __WbPlatform_h__

//	End of Platform.h
