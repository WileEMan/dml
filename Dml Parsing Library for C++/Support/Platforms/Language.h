/////////
//  Language.h
//  Copyright (C) 1999-2002, 2014 by Wiley Black
/////////
//  Provides enhancements and conveniences within the C++ language.
/////////

#ifndef __WBLanguage_h__
#define __WBLanguage_h__

//#include "../Common.h"

/** AddEnumFlagSupport(EnumType,FlagType) is used to define the operators &, |, ^, ~, &=, |=, ^=, ==, and != for an enum class.  Once defined,
	this enables the use of the enum class in a "flag style" without typecasts.  For example:

	enum class MyFlags : UInt32
	{
		Alpha	= 0x1,
		Beta	= 0x2,
		Gamma	= 0x4
	}

	AddEnumFlagSupport(MyFlags, UInt32);

	void ExampleUse()
	{
		MyFlags value = MyFlags::Beta | MyFlags::Gamma;
		if (value & MyFlags::Gamma != 0) printf("Gamma present!\n");
	}

	Use of an embedded typename is supported.  For example, AddEnumFlagSupport(MyClass::MyFlags, UInt16) is allowed.
**/

#if defined(GCC_VERSION) && (GCC_VERSION < 40400)	

	/** We're using emulated strongly-typed enum support (enum class) via the macros provided in Platforms.h.  The emulation
		is less restrictive than the C++ standard, and automatic conversion to integral types seems to cover it. **/

#define AddEnumFlagSupport(EnumType,FlagType)				\
	inline EnumType& operator&=(EnumType& x, int y) { x = static_cast<EnumType>(static_cast<FlagType>(x) & static_cast<FlagType>(y)); return x; }		\
	inline EnumType& operator|=(EnumType& x, int y) { x = static_cast<EnumType>(static_cast<FlagType>(x) | static_cast<FlagType>(y)); return x; }		\
	inline EnumType& operator^=(EnumType& x, int y) { x = static_cast<EnumType>(static_cast<FlagType>(x) ^ static_cast<FlagType>(y)); return x; }	

#else

	/** Using language's native strongly-typed enum support **/
#define AddEnumFlagSupport(EnumType,FlagType)			\
	inline EnumType	operator&(EnumType x, EnumType y) { return static_cast<EnumType>(static_cast<FlagType>(x) & static_cast<FlagType>(y)); }	\
	inline EnumType	operator|(EnumType x, EnumType y) { return static_cast<EnumType>(static_cast<FlagType>(x) | static_cast<FlagType>(y)); }	\
	inline EnumType	operator^(EnumType x, EnumType y) { return static_cast<EnumType>(static_cast<FlagType>(x) ^ static_cast<FlagType>(y)); }	\
	inline EnumType	operator~(EnumType x) { return static_cast<EnumType>(~static_cast<FlagType>(x)); }	\
	inline EnumType& operator&=(EnumType& x, EnumType y) { x = x & y; return x; }		\
	inline EnumType& operator|=(EnumType& x, EnumType y) { x = x | y; return x; }		\
	inline EnumType& operator^=(EnumType& x, EnumType y) { x = x ^ y; return x; }		\
	inline bool operator==(const EnumType& x, int y) { return static_cast<FlagType>(x) == y; }		\
	inline bool operator!=(const EnumType& x, int y) { return static_cast<FlagType>(x) != y; }	

#endif

#endif	// __WBLanguage_h__

//  End of Language.h
