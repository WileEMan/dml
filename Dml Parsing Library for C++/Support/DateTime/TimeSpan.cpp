/**	TimeSpan.cpp
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
**/
  
#include <stdlib.h>
#include "../Platforms/Platforms.h"
#include "../Exceptions.h"
#include "../Text/String.h"
#include "../Parsing/BaseTypeParsing.h"
#include "TimeSpan.h"

namespace wb
{		
	///*static*/ const TimeSpan TimeSpan::Invalid(Int64_MaxValue, Int32_MaxValue);
	///*static*/ const TimeSpan TimeSpan::Zero(0, 0);

	bool TimeSpan::TryParse(const char *lpsz, TimeSpan& Value)
	{
		size_t iDivider;
		string	str( lpsz );
		bool Negative = false;

			/* Hours:Minutes:Seconds field */						

		while (*lpsz == ' ' || *lpsz == '\t') lpsz++;
		if (*lpsz == '-') Negative = true;

		Int64	nA;
		if (!Int64_TryParse(str.c_str(), wb::NumberStyles::Integer, nA)) return false;
		nA = abs(nA);			// Will be accounted for by "Negative" flip, which also handles the case of 0 hours but still negative.

		iDivider	= str.find(':');
		if (iDivider == string::npos){
			
			Value.m_nElapsedSeconds = nA;
			if (Negative) Value.m_nElapsedSeconds = -Value.m_nElapsedSeconds;

			iDivider = str.find('.');
			if (iDivider == string::npos) { Value.m_nElapsedNanoseconds = 0; return true; }

			str = str.substr(iDivider);
			double nano;
			if (!Double_TryParse(str.c_str(), wb::NumberStyles::Float, nano)) return false;
			
			Value.m_nElapsedNanoseconds = (Int32)(nano / time_constants::g_dSecondsPerNanosecond);
			if (Value.m_nElapsedSeconds < 0) Value.m_nElapsedNanoseconds = -Value.m_nElapsedNanoseconds;
			return true;
		}

			/* Minutes:Seconds field */
		
		str	= str.substr(iDivider + 1);

		Int64	nB;
		if (!Int64_TryParse(str.c_str(), wb::NumberStyles::Integer, nB)) return false;
		
		iDivider	= str.find(':');
		if( iDivider == string::npos ){ 

			Value.m_nElapsedSeconds = (nA * time_constants::g_nSecondsPerMinute) + nB;
			if (Negative) Value.m_nElapsedSeconds = -Value.m_nElapsedSeconds;

			iDivider = str.find('.');
			if (iDivider == string::npos) {	Value.m_nElapsedNanoseconds = 0; return true; }

			str = str.substr(iDivider);
			double nano;
			if (!Double_TryParse(str.c_str(), wb::NumberStyles::Float, nano)) return false;
			
			Value.m_nElapsedNanoseconds = (Int32)(nano / time_constants::g_dSecondsPerNanosecond);
			if (Value.m_nElapsedSeconds < 0) Value.m_nElapsedNanoseconds = -Value.m_nElapsedNanoseconds;
			return true;
		}

			/* Seconds field */

		str	= str.substr(iDivider + 1);		

		Int64	nC;
		if (!Int64_TryParse(str.c_str(), wb::NumberStyles::Integer, nC)) return false;
		Value.m_nElapsedSeconds = (nA * time_constants::g_nSecondsPerHour) + (nB * time_constants::g_nSecondsPerMinute) + nC;
		if (Negative) Value.m_nElapsedSeconds = -Value.m_nElapsedSeconds;
		
		iDivider = str.find('.');
		if (iDivider == string::npos) {	Value.m_nElapsedNanoseconds = 0; return true; }

		str = str.substr(iDivider);
		double nano;
		if (!Double_TryParse(str.c_str(), wb::NumberStyles::Float, nano)) return false;
			
		Value.m_nElapsedNanoseconds = (Int32)(nano / time_constants::g_dSecondsPerNanosecond);
		if (Value.m_nElapsedSeconds < 0) Value.m_nElapsedNanoseconds = -Value.m_nElapsedNanoseconds;
		return true;
	}

	/*static*/ TimeSpan TimeSpan::Parse(const char* psz)
	{
		TimeSpan ret;
		if (!TryParse(psz, ret))
			throw FormatException(S("Unable to parse time span."));
		return ret;
	}	

	string TimeSpan::ToString(int Precision /*= 9*/) const
	{
		Int64 nDays;
		Int32 nHours, nMinutes, nSeconds, nNanoseconds;
		Get(nDays, nHours, nMinutes, nSeconds, nNanoseconds);		
			// Note: Get(...) returns all positive values.  Must call IsNegative() to
			// find negative cases.

		if (Precision < 1)
		{
			if (nNanoseconds > 500000000) 
			{
				nSeconds ++;
				if (nSeconds >= 60) 
				{
					nSeconds = 0; nMinutes ++;
					if (nMinutes >= 60) 
					{
						nMinutes = 0; nHours ++;
						if (nHours >= 24)
						{
							nHours = 0; nDays ++;
						}
					}
				}
			}
			else if (nNanoseconds < -500000000) 
			{
				nSeconds --;
				if (nSeconds <= -60) 
				{
					nSeconds = 0; nMinutes --;
					if (nMinutes <= -60) 
					{
						nMinutes = 0; nHours --;
						if (nHours <= -24)
						{
							nHours = 0; nDays --;
						}
					}
				}
			}
		
			char tmp[64];
			#ifdef _MSC_VER
			if( nHours < 24 )
				sprintf_s(tmp, S("%d:%02d:%02d hours"), nHours, nMinutes, nSeconds );
			else
				sprintf_s(tmp, S("%lld days %d:%02d:%02d hours"), nDays, nHours, nMinutes, nSeconds );			
			#else
			if( nHours < 24 )
				sprintf(tmp, S("%ld:%02ld:%02ld hours"), nHours, nMinutes, nSeconds );
			else
				sprintf(tmp, S("%lld days %ld:%02ld:%02ld hours"), nDays, nHours, nMinutes, nSeconds );			
			#endif
			if (IsNegative()) return string(S("-")) + tmp;
			else return tmp;
		}
		else
		{
			double dSeconds = (double)nSeconds + (double)nNanoseconds * time_constants::g_dSecondsPerNanosecond;

			char tmp[64];
			#ifdef _MSC_VER
			if( abs(nHours) < 24 )
				sprintf_s(tmp, S("%d:%02d:%02.*f hours"), nHours, nMinutes, Precision, dSeconds );
			else
				sprintf_s(tmp, S("%lld days %d:%02d:%02.*f hours"), nDays, nHours, nMinutes, Precision, dSeconds );
			#else
			if( abs(nHours) < 24 )
				sprintf(tmp, S("%ld:%02ld:%02.*f hours"), nHours, nMinutes, Precision, dSeconds );
			else
				sprintf(tmp, S("%lld days %ld:%02ld:%02.*f hours"), nDays, nHours, nMinutes, Precision, dSeconds );
			#endif
			if (IsNegative()) return string(S("-")) + tmp;
			else return tmp;
		}

	}// End of ToString()			

	string TimeSpan::ToApproxString() const
	{
			// Returns string as "XX Days", showing only highest-level unit.

		string Sign = S("");
		if (IsNegative()) Sign = S("-");

		char tmp[64];
		Int64 nTotalDays	= abs((int)GetTotalDays());

		if( nTotalDays > 90ll)
		{
			if( GetApproxTotalYears() >= 2ll )			// Note: We allow months up to 24...
			{
				#ifdef _MSC_VER
				sprintf_s(tmp, S("%lld years"), GetApproxTotalYears() );
				#else
				sprintf(tmp, S("%lld years"), GetApproxTotalYears() );
				#endif
				return Sign + tmp;
			}
			else 
			{
				#ifdef _MSC_VER
				sprintf_s(tmp, S("%lld months"), GetApproxTotalMonths() );
				#else
				sprintf(tmp, S("%lld months"), GetApproxTotalMonths() );
				#endif
				return Sign + tmp;
			}
		}
		else	// Else( less than 90 days )
		{
			if( nTotalDays >= 2ll )					// Note: We allow hours up to 48...
			{
				#ifdef _MSC_VER
				sprintf_s(tmp, S("%lld days"), Round64(GetTotalDays()) );
				#else
				sprintf(tmp, S("%lld days"), Round64(GetTotalDays()) );
				#endif
				return tmp;
			}
			else if( abs(GetTotalMinutes()) > 90 )			// Note: We allow minutes up to 90...
			{
				#ifdef _MSC_VER
				sprintf_s(tmp, S("%lld hours"), Round64(GetTotalHours()) );
				#else
				sprintf(tmp, S("%lld hours"), Round64(GetTotalHours()) );
				#endif
				return tmp;
			}
			else 
			{
				if( abs(GetTotalSeconds()) > 90 ){					// Note: We allow seconds up to 90...

					#ifdef _MSC_VER
					sprintf_s(tmp, S("%lld minutes"), Round64(GetTotalMinutes()) );
					#else
					sprintf(tmp, S("%lld minutes"), Round64(GetTotalMinutes()) );
					#endif
					return tmp;
				}
			
				double TotalSeconds = GetTotalSeconds();
				if (abs(TotalSeconds) > 30){
					#ifdef _MSC_VER
					sprintf_s(tmp, S("%lld seconds"), Round64(GetTotalSeconds()) );
					#else
					sprintf(tmp, S("%lld seconds"), Round64(GetTotalSeconds()) );
					#endif
					return tmp;
				}				

				if (abs(TotalSeconds) > 5){
					#ifdef _MSC_VER
					sprintf_s(tmp, S("%.1f seconds"), TotalSeconds);
					#else
					sprintf(tmp, S("%.1f seconds"), TotalSeconds);
					#endif
					return tmp;
				}

				if (abs(TotalSeconds) > 0.030){
					#ifdef _MSC_VER
					sprintf_s(tmp, S("%.3f seconds"), TotalSeconds);
					#else
					sprintf(tmp, S("%.3f seconds"), TotalSeconds);
					#endif
					return tmp;
				}

				if (abs(TotalSeconds) >= 0.000030){
					#ifdef _MSC_VER
					sprintf_s(tmp, S("%.6f seconds"), TotalSeconds);
					#else
					sprintf(tmp, S("%.6f seconds"), TotalSeconds);
					#endif
					return tmp;
				}

					#ifdef _MSC_VER
				sprintf_s(tmp, S("%lld nanoseconds"), GetTotalNanoseconds());
					#else
				sprintf(tmp, S("%lld nanoseconds"), GetTotalNanoseconds());
					#endif
				return tmp;
			}
				}
	}// End of ToApproxString()

	string TimeSpan::ToShortString(int Precision /*= 3*/) const
	{
			// Returns string as "XX Days", showing only highest-level unit.			

		string Sign = S("");
		if (IsNegative()) Sign = S("-");

		char tmp[64];
		Int64 nTotalDays	= abs((int)GetTotalDays());

		if( nTotalDays >= 2ll )					// Note: We allow hours up to 48...
		{
			#ifdef _MSC_VER
			sprintf_s(tmp, S("%.*f days"), Precision, GetTotalDays() );
			#else
			sprintf(tmp, S("%.*f days"), Precision, GetTotalDays() );
			#endif
			return tmp;
		}
		else if( abs(GetTotalMinutes()) > 90 )			// Note: We allow minutes up to 90...
		{
					#ifdef _MSC_VER
			sprintf_s(tmp, S("%.*f hours"), Precision, GetTotalHours() );
					#else
			sprintf(tmp, S("%.*f hours"), Precision, GetTotalHours() );
					#endif
			return tmp;
				}
		else 
		{
			if( abs(GetTotalSeconds()) > 90 ){					// Note: We allow seconds up to 90...

					#ifdef _MSC_VER
				sprintf_s(tmp, S("%.*f minutes"), Precision, GetTotalMinutes() );
					#else
				sprintf(tmp, S("%.*f minutes"), Precision, GetTotalMinutes() );
					#endif
				return tmp;
				}

			double TotalSeconds = GetTotalSeconds();			
			if (abs(TotalSeconds) >= 0.000030){
				#ifdef _MSC_VER
				sprintf_s(tmp, S("%.*f seconds"), Precision, TotalSeconds);
				#else
				sprintf(tmp, S("%.*f seconds"), Precision, TotalSeconds);
				#endif
				return tmp;
			}

			#ifdef _MSC_VER
			sprintf_s(tmp, S("%lld nanoseconds"), GetTotalNanoseconds());
			#else
			sprintf(tmp, S("%lld nanoseconds"), GetTotalNanoseconds());
			#endif
			return tmp;
		}
	}// End of ToShortString()
}

//	End of TimeSpan.cpp


