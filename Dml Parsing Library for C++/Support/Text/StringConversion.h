/////////
//  StringConversion.h
//  Copyright (C) 2000-2010, 2014 by Wiley Black
//	Author(s):
//		Wiley Black			TheWiley@gmail.com
//
/////////
//	Revision History:
//		August, 2014
//			Merged and organized into new C++ General Library.
//
//		July, 2010
//			Reformed into a conversion-only class to offer similarity to .NET structure.
//
//		November, 2004
//			Renamed to CStringEx.h and .cpp so as to avoid conflicts with the CString.h
//			header included as part of the STL in Borland C++ Builder.
//
//		May 19, 2001
//			Modified CString to support += operations.  This required redefining the
//			whole class, with two different variables (m_nStrAlloc and m_nStrLen) instead
//			of just one, as it had previously been using (m_nStrLen).  Now the class
//			could allocate more memory than needed, keeping a small 'buffer' for
//			adding new data. 
//
//		May 25, 2001
//			Moved many functions to the new CString.cpp file, making them out-of-line.
//
//		March 2019
//			Heavy re-write to unify a lot of the conversions into some core functions with
//			more flexibility and wrappers that configure.  Also fixed some issues with extreme
//			cases and unit tested under more conditions.
/////////

#ifndef __WBStringConversion_h__
#define __WBStringConversion_h__

/** Dependencies **/

//#include "../Common.h"
#include "../Text/String.h"
#include "../Parsing/BaseTypeParsing.h"
#include <climits>
// We rely on std::numeric_limits<T>::max() and min() here, so can't have the macros defined.
#ifdef max
#undef max
#undef min
#endif

/** Content **/

namespace wb
{	
	/** Convert class, offering more detailed conversion capabilities in the .NET styling **/
	// See also to_string() methods offered in String.h.

	class Convert
	{
	public:

		struct Format 
		{
			// MinChars: the minimum number of characters to display.  If fewer than MinChars are required to convert to text, then the string is padded with whitespace
			// on the left.  To avoid padding, use MinChars of zero.
			int MinChars;

			// MaxChars: the maximum number of characters to display.  If the number cannot fit in a string with MaxChars or fewer, than the number is saturated at the
			// largest (or most negative) number that will fit within MaxChars.  If commas are being displayed, they do count as characters for this constraint.
			int MaxChars;

			// AlwaysShowSign: forces the display of a + sign for positive values of the number.  This character will count as one character for the purposes of fitting
			// within MaxChars.
			bool AlwaysShowSign;

			/** Only applicable for floating-point values **/

			// MaxDecimals: maximum number of digits after the radix point (decimal point) to output.  If zero, no decimal place is shown.  Digits after the decimal 
			// place are only displayed if the integral portion fits within MaxChars, and decimal places are sacrificed before integral digits to make the text fit.
			int MaxDecimals;

			// RailInfinities: if true, saturate infinities at the largest finite number that fits in MaxChars.  If false, output "+inf" or "-inf" padded to MinChars.  
			// Note: NaN always outputs "NaN".
			bool RailInfinities;		

			// Some additional defaults:
			static Format DefaultSingleFormat() { Format ff; ff.MaxDecimals = 8; return ff; }

			Format() : MinChars(0), MaxChars(60), AlwaysShowSign(false), MaxDecimals(16), RailInfinities(false) { }
		};

	private:

			// This private version also makes available the # of characters
			// consumed in the reading of the floating-point number.  
		static double ToDouble(const char *psz, int& iIndex, bool& bSuccess);

		static bool IsWhitespace(char ch);
		static bool IsHexDigit(char ch);

		static string Reverse(const string& str);

		template<bool WithCommas>
		static string Generic_ToString(double dValue, Format format);

		template<bool WithCommas, int MaxCharsForType, int MaxCharsForPositive, typename ValType>
		static string Generic_ToString(ValType nValue, Format format, string LowestNumber = "0");

	public:

			/** Additional Formatting Functions **/

			/** The numeric formatting functions are a little faster than printf() style functions
				since they don't have to parse the format string (%5.6f, for example).  

				nMaxChars will always be the maximum number of characters displayed.  If the
				value to be displayed cannot be shown in 'nMaxChars' characters, the value will 
				be constrained to fit.  For example, if you are trying to display 12345 in 4 
				characters, the number shown will be 9999 instead.  

				Decimal places are the lower priority.  That is, if a number would have to be
				truncated to a smaller number to display it with decimal places, then it will
				not be truncated, fewer or no decimal places will be shown instead.  For example,
				if trying to display 1234.67 in 5 characters, it will be shown as 1234 instead.
				If you are trying to show 1234.67 in 6 characters, it would be shown as 1234.7
				instead.

				'WithCommas' functions operate the same way, adding commas between every 3 digits
				which appear before the decimal place.

				Use 'nMaxChars' of zero to ensure the entire integral value is displayed.  
				The floating-point formatting functions stop when nothing but zeros remain
				to be displayed.
			**/

			/** Double-precision floating-point to string conversions **/
		static string ToString(double dValue, Format format = Format());
		static string ToStringWithCommas(double dValue, Format format = Format());

			/** Single-precision floating-point to string conversions **/
		static string ToString(float fValue, Format format = Format::DefaultSingleFormat());
		static string ToStringWithCommas(float fValue, Format format = Format::DefaultSingleFormat());

			/** Integer to string conversions **/		
		static string ToString(Int64 nValue, Format format = Format());
		static string ToStringWithCommas(Int64 nValue, Format format = Format());
		static string ToString(Int32 nValue, Format format = Format());
		static string ToStringWithCommas(Int32 nValue, Format format = Format());		
		static string ToString(int nValue, Format format = Format()) { return ToString((Int32)nValue, format); }
		static string ToStringWithCommas(int nValue, Format format = Format()) { return ToStringWithCommas((Int32)nValue, format); }	

			/** Unsigned Integer to string conversions **/
		static string ToString(UInt64 nValue, Format format = Format());
		static string ToStringWithCommas(UInt64 nValue, Format format = Format());
		static string ToString(UInt32 nValue, Format format = Format());
		static string ToStringWithCommas(UInt32 nValue, Format format = Format());			
		static string ToString(unsigned int nValue, Format format = Format()) { return ToString(nValue, format); }
		static string ToStringWithCommas(unsigned int nValue, Format format = Format()) { return ToStringWithCommas(nValue, format); }
	
			/** Boolean to string conversion **/
		static const char *ToString(bool Value);
	};

	/** Implementation **/

	inline string Convert::Reverse(const string& str)
	{
		string ret;
		for (size_t ii = str.length() - 1; ii > 0; ii--) ret += str[ii];
		if (str.length() > 0) ret += str[0];
		return ret;
	}

	inline string Convert::ToString(float fValue, Format format){
		return ToString(double(fValue), format);
	}

	inline string Convert::ToStringWithCommas(float fValue, Format format){
		return ToStringWithCommas(double(fValue), format);
	}

	inline const char *Convert::ToString(bool Value) { if (Value) return "true"; else return "false"; }	

	inline bool Convert::IsWhitespace(char ch){ return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r'; }
	inline bool Convert::IsHexDigit(char ch){ return (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'); }		

	template<bool WithCommas>
	inline /*static*/ string Convert::Generic_ToString(double dValue, Format format)
	{
		string ret;
		int ii, jj;

			// This algorithm generates the string in reverse, and the last step is to reverse the string.

		if (format.MaxChars <= 0 || format.MaxChars > 60) format.MaxChars = 60;		
		if (format.MinChars < 0) format.MinChars = 0;
		if (format.MinChars > 65535) format.MinChars = 65535;
		if (format.MaxDecimals < 0) format.MaxDecimals = 0;
		if (format.MaxDecimals > 65535) format.MaxDecimals = 65535;
		
		// Note: I tested that in Visual C++ 2012 you can convert quiet NaN, signalling NaN, +inf, and -inf back and forth between double and
		// float and they retain their special values correctly.
		static_assert(std::numeric_limits<float>::is_iec559, "IEEE 754 required");		// Required as this function is written.

		if (_isnan(dValue)) 
		{
			ret = "NaN";
			while ((int)ret.length() < format.MinChars) ret = S(" ") + ret; 
			return ret;
		}

		if (!format.RailInfinities && !_finite(dValue))
		{
			if (dValue > 0.0) ret = "+inf"; else ret = "-inf";
			while ((int)ret.length() < format.MinChars) ret = S(" ") + ret; 
			return ret;
		}

		// If RailInfinities is true, then it should trigger on the MaxChars saturation rules below and proceed normally.
		
		if (std::abs(dValue) < std::numeric_limits<double>::min())
		{
			// Convert denormalized numbers to be exactly zero so that they don't trigger any unexpected behaviors later.
			dValue = 0.0;
		}

		bool bNegative = (dValue < 0.0);

		int nChars = 0;
		if( bNegative ){
			if (format.MaxChars == 1 && !format.AlwaysShowSign) { dValue = 0.0; bNegative = false; }
			else {
				nChars ++;
				dValue = -dValue;			
			}
		}
		else if( format.AlwaysShowSign ) nChars ++;		

		bool MaxedOut = false;
		
		double dMaxValue = 1.0 - ::std::numeric_limits<double>::epsilon();
		// See ToStringWithCommas(UInt64) for explanation.
		jj = 0;
		for (ii = 0; ii < format.MaxChars - nChars; ii++) 
		{			
			if (!WithCommas || jj < 3) { dMaxValue *= 10.0; jj++; } else jj = 0;
		}
		double dMaxValueWith1Decimal = dMaxValue / 100.0;
		if( dValue >= dMaxValue ) dValue = dMaxValue;
		if( dValue >= dMaxValueWith1Decimal 
			|| dMaxValueWith1Decimal < 1.0 ) format.MaxDecimals = 0;
	
		if( format.MaxDecimals ){
				// Loop, reducing nMaxDecimals until we have few enough decimal places for the non-decimal
				// part to fit alright.  Keeping in mind that nMaxValueWithNDecimal is the maximum value with
				// N decimal places *based on the maximum number of characters allowed, format.MaxChars*.  We already
				// know that at least 1 decimal will fit, so we begin there.  That is also why we reduce
				// 'dMaxValueWith1Decimal' before the if() check, and why we pre-decrement nMaxDecimals.
			int nUseDecimals = 1;
			while( -- format.MaxDecimals ){
				dMaxValueWith1Decimal /= 10.0;					// Now 'MaxValueWith N Decimals'.  e.g. 9.99
				if( dValue >= dMaxValueWith1Decimal				// e.g. if( 10000.0 >= 9.99 ) break
					|| dMaxValueWith1Decimal < 1.0 ) break; else nUseDecimals ++; 
			}
			format.MaxDecimals = nUseDecimals;
		}

		double dTestValue = dValue * pow( 10.0, double(format.MaxDecimals) );			
		if( fmod( dTestValue, 1.0 ) >= 0.5 ) dTestValue += 1.0;			// Round upward	
		dTestValue = dTestValue / pow(10.0, double(format.MaxDecimals));
		if (dTestValue > dMaxValue) MaxedOut = true;		
		
		dValue *= pow( 10.0, double(format.MaxDecimals) );
		if (!MaxedOut)
		{
			if( fmod( dValue, 1.0 ) >= 0.5 ) dValue += 1.0;			// Round upward	
		}		
		
		jj = 0;
		while (nChars < format.MaxChars)
		{
			UInt32 nDigit = (UInt32)fmod( dValue, 10.0 );			// Note: Integer conversion always rounds down.
			ret += (char)(nDigit + '0');
			nChars ++;
			dValue /= 10.0;

			// We're going in reverse, so we're writing out the decimal places first...
			if( format.MaxDecimals ){
				format.MaxDecimals --;
				if( !format.MaxDecimals ){				
					ret += S(".");
					nChars ++;									// Count off an extra character for the decimal place
					if( dValue < 1.0 ){							// If nothing above the decimal, make sure					
						ret += S("0");							// that a zero gets shown before the decimal.
						nChars ++;
						break;
					}
				}
			}
			else {
				if( dValue < 1.0 ) break;						// No more characters to show (except zeros)
				if (WithCommas)
				{
					if (jj == 3){
						ret += S(",");
						nChars ++;
						jj = 0;
					}
					else jj++;
				}
			}
		}

		if( bNegative ) ret += S("-");
		else if( format.AlwaysShowSign ) ret += S("+");

		assert (ret.length() == nChars);
		while( nChars < format.MinChars ) { ret += S(" "); nChars++; }

		return Reverse(ret);
	}

	inline string Convert::ToStringWithCommas(double dValue, Format format)
	{
		return Generic_ToString<true>(dValue, format);
	}

	inline string Convert::ToString(double dValue, Format format)
	{
		return Generic_ToString<false>(dValue, format);		
	}

	// Take advantage of overloading and enable_if to prevent the compiler from ever trying to generate "Value = -Value" for an unsigned type, which is a warning or error.
	// The second case, for unsigned values, should never be called anyway, but the compiler doesn't seem to realize that without some overloading help.
	template<typename ValType> static ValType NegateIfSigned(ValType Value, typename std::enable_if<std::is_signed<ValType>::value, ValType>::type * = nullptr) { return -Value; }		
	template<typename ValType> static ValType NegateIfSigned(ValType Value, typename std::enable_if<!std::is_signed<ValType>::value, ValType>::type * = nullptr) { return Value; }		

	template<typename ValType> static bool IsNegative(ValType Value, typename std::enable_if<std::is_signed<ValType>::value, ValType>::type * = nullptr) { return (Value < 0); }		
	template<typename ValType> static bool IsNegative(ValType Value, typename std::enable_if<!std::is_signed<ValType>::value, ValType>::type * = nullptr) { return false; }			

	template<bool WithCommas, int MaxCharsForType, int MaxCharsForPositive, typename ValType>
	inline string Convert::Generic_ToString(ValType nValue, Format format, string LowestNumber)
	{
		string ret;
		int ii, jj;

		const bool Signed = std::is_signed<ValType>();

			// This algorithm generates the string in reverse, and the last step is to reverse the string.		
		
		if (format.MaxChars <= 0 || format.MaxChars >= MaxCharsForType) format.MaxChars = MaxCharsForType;
		if (format.MinChars < 0) format.MinChars = 0;
		if (format.MinChars > 65535) format.MinChars = 65535;

		bool bNegative = IsNegative(nValue);		// (nValue < 0);   Done this way to help the compiler.

		int nChars = 0;
		if (Signed && bNegative) {
			if (format.MaxChars == 1 && !format.AlwaysShowSign) { nValue = (ValType)0; bNegative = false; }
			else
			{
				nChars++; 
				nValue = NegateIfSigned(nValue); // nValue = -nValue;	Done this way to help the compiler figure things out.
			}
		}
		else if (format.AlwaysShowSign) nChars++;				

		// We don't want to overflow counting this.

		// For Example, Int64, WithCommas:
		//	Max positive 64-bit signed integer:  9,223,372,036,854,775,807 has 19 digits and 6 commas.  MaxCharsForPositive = 25.
		//	Max negative 64-bit signed integer: -9,223,372,036,854,775,808 has 19 digits and 6 commas, plus the sign character.  MaxCharsForType = 26.  
		//
		//	In the Int64 case, format.MaxChars would default/max out to 26 (MaxCharsForType).  At this point, we've forced the value 
		//	positive (and noted a negative in the separate bNegative boolean) and noted a +/- prefix character if applicable.  
		//
		//		If the value is positive without AlwaysShowSign:	nChars = 0, MaxCharsForPositive = 25, MaxChars = 26.
		//		If the value is positive with AlwaysShowSign:		nChars = 1, MaxCharsForPositive = 25, MaxChars = 26.
		//		If the value is negative:							nChars = 1, MaxCharsForPositive = 25, MaxChars = 26.
		//
		//	Next, consider whether we will correctly saturate in each case if the caller requests one fewer MaxChars.  In the
		//	positive case without AlwaysShowSign, the caller would have to request two fewer MaxChars to saturate.

		if (format.MaxChars - nChars < MaxCharsForPositive)
		{
			// Special case: the lowest signed integral value can't be negated to a positive number, so we still have the lowest negative
			// number in this case.  Since we triggered the if statement and are at the lowest negative value, we are gauranteed to saturate
			// here but can't because we're still negative.  Since we're going to saturate and cap the number anyway, we can cheat and increment
			// by one to get off this special case value.
			if (Signed && nValue == std::numeric_limits<ValType>::lowest()) nValue = NegateIfSigned(nValue + 1);

			ValType nOverMaxValue = 1;
			// After ii = 0, nMaxChars = 1+: nOverMaxValue   = 10
			// After ii = 1, nMaxChars = 2+: nOverMaxValue   = 100
			// After ii = 2, nMaxChars = 3+: nOverMaxValue   = 1,000
			// After ii = 3, nMaxChars = 4+: nOverMaxValue   = 1,000 *
			// After ii = 4, nMaxChars = 5+: nOverMaxValue   = 10,000
			// After ii = 5, nMaxChars = 6+: nOverMaxValue   = 100,000
			// After ii = 6, nMaxChars = 7+: nOverMaxValue   = 1,000,000 
			// After ii = 7, nMaxChars = 8+: nOverMaxValue   = 1,000,000 *
			// After ii = 8, nMaxChars = 9+: nOverMaxValue   = 10,000,000
			// After ii = 9, nMaxChars = 10+: nOverMaxValue  = 100,000,000 
			// After ii = 10, nMaxChars = 11+: nOverMaxValue = 1,000,000,000
			// After ii = 11, nMaxChars = 12+: nOverMaxValue = 1,000,000,000 *
			//  * cases to skip multiplying by x10 when using commas.
			jj = 0;
			for (ii = 0; ii < format.MaxChars - nChars; ii++) 
			{
				if (!WithCommas || jj < 3) { nOverMaxValue *= 10; jj++; } else jj = 0;
			}
			if (nValue >= nOverMaxValue) nValue = nOverMaxValue - 1;
		}

		if (Signed && nValue == std::numeric_limits<ValType>::lowest())
		{
			// Special case: the lowest value of signed integral types can't be negated to a positive number.  We've already made it past the
			// saturation for MaxChars step, and we're still wanting to output that special character, so there's nothing for it but to output
			// that special number.
			ret = LowestNumber;
			while ((int)ret.length() < format.MinChars) ret = S(" ") + ret;
			return ret;
		}
		else
		{
			// Generate reversed result
			jj = 0;
			while (nChars <= MaxCharsForType)
			{
				UInt32 nDigit = (UInt32)(nValue % 10ull);
				ret += (char)(nDigit + '0');
				nChars ++;
				nValue /= 10ull;
			
				if (nValue < 1) break;						// No more characters to show
				if (WithCommas)
				{
					if (jj == 3) { ret += S(","); jj = 0; nChars++; } else jj++;			
				}
			}		
			assert ((int)ret.length() <= format.MaxChars);

			if( bNegative ) ret += S("-");
			else if (format.AlwaysShowSign) ret += S("+");
			assert (ret.length() == nChars);

			while((int)ret.length() < format.MinChars) ret += S(" ");
			return Reverse(ret);
		}		
	}

	inline string Convert::ToStringWithCommas(UInt64 nValue, Format format)
	{		
		// Max positive 64-bit unsigned signed integer: 18,446,744,073,709,551,615 has 20 digits and 6 commas.  MaxCharsForType = MaxCharsForPositive = 26.

		return Generic_ToString<true, 26, 26, UInt64>(nValue, format);
	}
	
	inline string Convert::ToStringWithCommas(Int64 nValue, Format format)
	{
		// Max positive 64-bit signed integer:  9,223,372,036,854,775,807 has 19 digits and 6 commas.  MaxCharsForPositive = 25.
		// Max negative 64-bit signed integer: -9,223,372,036,854,775,808 has 19 digits and 6 commas, plus the sign character.  MaxCharsForType = 26.  

		return Generic_ToString<true, 26, 25, Int64>(nValue, format, "-9,223,372,036,854,775,808");
	}		

	inline string Convert::ToString(UInt64 nValue, Format format)
	{
		return Generic_ToString<false, 20, 20, UInt64>(nValue, format);
	}
		
	inline string Convert::ToString(Int64 nValue, Format format)
	{
		return Generic_ToString<false, 20, 19, Int64>(nValue, format, "-9223372036854775808");
	}

	inline string Convert::ToStringWithCommas(Int32 nValue, Format format)
	{
		// Max positive 32-bit signed integer:  2,147,483,647 has 10 digits and 3 commas.  MaxCharsForPositive = 13.
		// Max negative 32-bit signed integer: -2,147,483,648 has 10 digits and 3 commas, plus the sign character.  MaxCharsForType = 14.
		return Generic_ToString<true, 14, 13, Int32>(nValue, format, "-2,147,483,648");
	}
		
	inline string Convert::ToString(Int32 nValue, Format format)
	{		
		return Generic_ToString<false, 11, 10, Int32>(nValue, format, "-2147483648");
	}

	inline string Convert::ToStringWithCommas(UInt32 nValue, Format format)
	{
		// Max positive 32-bit unsigned integer:  4,294,967,295 has 10 digits and 3 commas.  MaxCharsForType = MaxCharsForPositive = 13.
		return Generic_ToString<true, 13, 13, UInt32>(nValue, format);
	}
		
	inline string Convert::ToString(UInt32 nValue, Format format)
	{
		return Generic_ToString<false, 10, 10, UInt32>(nValue, format);
	}		

	inline string ToLower(string str) { string ret = str; std::transform(ret.begin(), ret.end(), ret.begin(), ::tolower); return ret; }
	inline string ToUpper(string str) { string ret = str; std::transform(ret.begin(), ret.end(), ret.begin(), ::toupper); return ret; }	

	#if 0
	#ifdef _DEBUG	
	inline void StringConversion_UnitTest()
	{
		using namespace std;		

		cout << "Unsigned 64:" << endl;
		cout << "0 = " << wb::Convert::ToStringWithCommas((UInt64)0) << endl;	
		cout << "1 = " << wb::Convert::ToStringWithCommas((UInt64)1) << endl;	
		cout << "5 = " << wb::Convert::ToStringWithCommas((UInt64)5) << endl;	
		cout << "10 = " << wb::Convert::ToStringWithCommas((UInt64)10) << endl;	
		cout << "1,000 = " << wb::Convert::ToStringWithCommas((UInt64)1000) << endl;	
		cout << "68,719,476,735 = " << wb::Convert::ToStringWithCommas((UInt64)68719476735ull) << endl;	
		cout << "68,719,476,735 maxed at 6 characters = " << wb::Convert::ToStringWithCommas((UInt64)68719476735ull, 0, 6) << endl;
		cout << endl;

		cout << "Signed 64:" << endl;
		cout << "0 = " << wb::Convert::ToStringWithCommas((Int64)0) << endl;	
		cout << "1 = " << wb::Convert::ToStringWithCommas((Int64)1) << endl;	
		cout << "5 = " << wb::Convert::ToStringWithCommas((Int64)5) << endl;	
		cout << "10 = " << wb::Convert::ToStringWithCommas((Int64)10) << endl;	
		cout << "1,000 = " << wb::Convert::ToStringWithCommas((Int64)1000) << endl;	
		cout << "68,719,476,735 = " << wb::Convert::ToStringWithCommas((Int64)68719476735ll) << endl;	
		cout << "68,719,476,735 maxed at 6 characters = " << wb::Convert::ToStringWithCommas((Int64)68719476735ll, 0, 6) << endl;	
		cout << "-1 = " << wb::Convert::ToStringWithCommas((Int64)-1) << endl;	
		cout << "-5 = " << wb::Convert::ToStringWithCommas((Int64)-5) << endl;	
		cout << "-10 = " << wb::Convert::ToStringWithCommas((Int64)-10) << endl;	
		cout << "-1,000 = " << wb::Convert::ToStringWithCommas((Int64)-1000) << endl;	
		cout << "-68,719,476,735 = " << wb::Convert::ToStringWithCommas((Int64)-68719476735ll) << endl;	
		cout << "-68,719,476,735 maxed at 6 characters = " << wb::Convert::ToStringWithCommas((Int64)68719476735ll, 0, 6) << endl;
		cout << endl;

		cout << "Double:" << endl;
		cout << "0 = " << wb::Convert::ToStringWithCommas((double)0) << endl;	
		cout << "1 = " << wb::Convert::ToStringWithCommas((double)1) << endl;	
		cout << "5 = " << wb::Convert::ToStringWithCommas((double)5) << endl;	
		cout << "10 = " << wb::Convert::ToStringWithCommas((double)10) << endl;	
		cout << "1,000 = " << wb::Convert::ToStringWithCommas((double)1000) << endl;	
		cout << "68,719,476,735.0 = " << wb::Convert::ToStringWithCommas((double)68719476735.0) << endl;	
		cout << "68,719,476,735.0 maxed at 6 characters = " << wb::Convert::ToStringWithCommas((double)68719476735.0, 6) << endl;
		cout << "68,719,476,735.123456789 maxed at 10 characters = " << wb::Convert::ToStringWithCommas((double)68719476735.123456789, 10) << endl;
		cout << "-68,719,476,735.123456789 maxed at 10 characters = " << wb::Convert::ToStringWithCommas((double)-68719476735.123456789, 10) << endl;
		cout << endl;
	}
	#endif
	#endif
}

#endif  // __StringConversion_h__

//  End of StringConversion.h

