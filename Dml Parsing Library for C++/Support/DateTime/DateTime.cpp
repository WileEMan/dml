/**	DateTime.cpp
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
**/

#include "../Platforms/Platforms.h"

#if defined(_WINDOWS)
#include <Windows.h>
#elif defined(_LINUX)
#include <time.h>
#include <errno.h>
#endif

#include "DateTime.h"
#include "../Text/String.h"
#include "../Parsing/BaseTypeParsing.h"

namespace wb
{	
	/////////
	//	DateTime Constants
	//

		/** Definition of the time_t type in Win32:
			The number of seconds elapsed since midnight (00:00:00), January 1, 1970, coordinated universal time.

			Years divisible by 4:	+	492. (Not including year zero.)
			Years ending in 00:		-	19.	(Not including year zero.)
			Years divisible by 400:	+	4. (Not including year zero.)
			Year zero:				+	1 (A leap year.)
									--------
			Total Leap Years:			478. (Including year zero.)
			Total Non-Leap Years:	   1492.
		**/
	/*static*/ const Int64	DateTime::g_nOffsetForTimeT 
						= (time_constants::g_nSecondsPerLeapYear * 478ll) + (time_constants::g_nSecondsPerNonLeapYear * 1492ll);

		/** Definition of the FILETIME type in Win32:
			The number of 100-nanosecond intervals since January 1, 1601.
			The value will be converted to the number of seconds elapsed since January 1, 1601 before offseting.

			Years divisible by 4:	+	400. (Not including year zero.)
			Years ending in 00:		-	16.	(Not including year zero.)
			Years divisible by 400:	+	4. (Not including year zero.)
			Year zero:				+	1 (A leap year.)
									--------
			Total Leap Years:			389. (Including year zero.)
			Total Non-Leap Years:	   1212.
		**/
	/*static*/ const Int64  DateTime::g_nOffsetForFiletime
						= (time_constants::g_nSecondsPerLeapYear * 389ll) + (time_constants::g_nSecondsPerNonLeapYear * 1212ll);

	/*static*/ DateTime DateTime::Minimum = DateTime::GetMinimumValue();
	/*static*/ DateTime DateTime::Maximum = DateTime::GetMaximumValue();
	/*static*/ DateTime DateTime::Zero = DateTime(0,1,1, 0,0,0, DateTime::UTC);

	/////////
	//	DateTime Members
	//

	/*static*/ DateTime	DateTime::Now()
	{
	#	ifdef _WIN32
		SYSTEMTIME	st;
		GetLocalTime(&st);
		DateTime	now(st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds * 1000000, LocalTimeZone);
		return now;
	#	elif defined(_LINUX)
		return UtcNow().asLocalTime();		
	#	else
	#		error Platform-specific code is required for retrieving the current date and time as local time.
	#	endif
	}

	/*static*/ DateTime	DateTime::UtcNow()
	{
	#	ifdef _WIN32
		SYSTEMTIME	st;
		GetSystemTime(&st);
		DateTime	now(st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds * 1000000, UTC);
		return now;	
	#	elif defined(_LINUX)

		// Note: Program must be linked with -lrt (real-time library) for this support...
		// clock_gettime(), when using CLOCK_REALTIME, measures time relative to the Epoch, which appears to be in UTC and
		// is the same Epoch as time_t uses.
		timespec tp;
		if (clock_gettime(CLOCK_REALTIME, &tp) != 0) Exception::ThrowFromErrno(errno);
		DateTime	now;
		now.m_nSeconds = ((Int64)tp.tv_sec) + g_nOffsetForTimeT;
		now.m_nNanoseconds = tp.tv_nsec;
		now.m_nBias = 0;
		return now;

	#	else
	#		error Platform-specific code is required for retrieving the current date and time as UTC time.
	#	endif
	}

	/*static*/ int DateTime::GetLocalBias()
	{
		int	nBiasMinutes;
	#	ifdef _WIN32
		TIME_ZONE_INFORMATION	tzi;
		ZeroMemory( &tzi, sizeof(tzi) );
		switch( GetTimeZoneInformation( &tzi ) )
		{
		default:
		case TIME_ZONE_ID_UNKNOWN:	nBiasMinutes	= tzi.Bias; break;
		case TIME_ZONE_ID_STANDARD:	nBiasMinutes	= tzi.Bias + tzi.StandardBias; break;
		case TIME_ZONE_ID_DAYLIGHT:	nBiasMinutes	= tzi.Bias + tzi.DaylightBias; break;
		}
	#	elif defined(_LINUX)

		// Get GMT timezone offset...
		time_t t = time(NULL);
		struct tm lt = {0};
		localtime_r(&t, &lt);		
		return lt.tm_gmtoff;		// It even happens to be in seconds.  Not sure on portability.

	#	else
	#		error Platform-specific code is required for retrieving and calculating time zone bias information.
	#	endif

		assert( nBiasMinutes > -1440 && nBiasMinutes < 1440 );	// Possible range for bias is +/- 23.99 hours.

		return ( nBiasMinutes * 60 /* seconds/minute */ );
	}// End GetLocalBias()

	/*static*/ void DateTime::SetSystemTime(const DateTime& to)
	{
		#if defined(_WINDOWS)

		int nYear, nMonth, nDay, nHour, nMinute, nSecond, nNanosecond;

		if (!to.IsUTC()){
			DateTime   dtUTC = to.asUTC();
			dtUTC.Get(nYear, nMonth, nDay, nHour, nMinute, nSecond, nNanosecond);
		}
		else
			to.Get(nYear, nMonth, nDay, nHour, nMinute, nSecond, nNanosecond);		

		SYSTEMTIME  st;
		if (nYear < 0 || nYear > (Int32)UInt16_MaxValue) throw NotSupportedException(S("Cannot set system time year outside of 16-bit range."));
		st.wYear = (UInt16)nYear;
		st.wMonth = nMonth;
		st.wDay = nDay;
		st.wHour = nHour;
		st.wMinute = nMinute;
		st.wSecond = nSecond;		
		st.wMilliseconds = nNanosecond / 1000000;
		if (!::SetSystemTime(&st)) Exception::ThrowFromWin32(::GetLastError());

		#elif defined(_LINUX)

		time_t val = (time_t)to;
		if (stime(&val) == 0) return;
		Exception::ThrowFromErrno(errno);

		#endif
	}

#if 0
	void	DateTime::Set( int nYear, int nSeconds, int nBiasMinutes /*= UTC*/ )
	{
		m_nSeconds		= 0;		
		m_nNanoseconds	= 0;

			/** If nBiasMinutes is on automatic, determine the local bias **/
			/** Possible Improvement: Base on input date/time instead of on current date/time **/

		if( nBiasMinutes == (int)LocalTimeZone ) m_nBias	= GetLocalBias();
		else m_nBias	= ( nBiasMinutes * 60 /* seconds/minute */ );

		Add( nYear, /*Month=*/ 0, /*Day=*/ 0, /*Hour=*/ 0, /*Minute=*/ 0, (int)nSeconds );
	}
#endif

	void DateTime::Set(int nYear, int nMonth, int nDay, int nHour, int nMinute, int nSecond, int nNanoseconds /*= 0*/, int nBiasMinutes /*= LocalTimeZone*/)
	{
			/** Validate parameters **/

		assert (nMonth >= 1 && nMonth <= 12);
		assert (nDay >= 1 && nDay <= 31);
		assert (nHour >= 0 && nHour <= 23);
		assert (nMinute >= 0 && nMinute <= 59);
		assert (nSecond >= 0 && nSecond <= 59);
		assert (nNanoseconds >= 0 && nNanoseconds < time_constants::g_n32NanosecondsPerSecond);

			/** If nBiasMinutes is on automatic, determine the local bias **/
			/** Possible Improvement: Base on input date/time instead of on current date/time **/

		if( nBiasMinutes == (int)LocalTimeZone ) m_nBias	= GetLocalBias();
		else m_nBias	= ( nBiasMinutes * 60 /* seconds/minute */ );

			// Assertion: Valid range for bias is +/- 23.99 hours.
		assert( m_nBias > -(24 * 60 * 60) && m_nBias < (24 * 60 * 60) );

			/** Time of day and Day of month **/

			// Assertion: There are not that many days in that month, in a leap-year.
		assert( !IsLeapYear( nYear ) || nDay <= time_constants::g_tableDaysInMonthLY[nMonth] );		
			// Assertion: There are not that many days in that month, in a non-leap-year.
		assert( IsLeapYear( nYear ) || nDay <= time_constants::g_tableDaysInMonthNLY[nMonth] );		

		m_nSeconds	= (Int64)nSecond + (60 /*seconds/minute*/ * ((Int64)nMinute 	
						+ (60 /*minutes/hour*/ * ((Int64)nHour	+ (24 /*hours/day*/ * (Int64)(nDay - 1)) ))));

			/** Months **/

			/* Note: IsLeapYear(void) is not yet available because m_nSeconds is not yet completely calculated. */

		if (IsLeapYear( nYear ))
			m_nSeconds	+= (Int64)time_constants::g_tableDaysPastInYearLY[nMonth] * 60 * 60 * 24;
		else
			m_nSeconds	+= (Int64)time_constants::g_tableDaysPastInYearNLY[nMonth] * 60 * 60 * 24;

			/** Years **/

		UInt32	nAbsYear	= abs(nYear);	
		UInt32	nNumberOfLeapYears	= 0;
		//		-	The year zero was a leap year.
		//		Also, since we are counting year zero here and the later calculations count years *PAST*, we
		//		subtract one from our working year.
		if (nAbsYear){ nAbsYear --; nNumberOfLeapYears ++; }
		//		-	If the year is divisible by 4 but does not end in 00, then the year is a leap year, with 366 days. 
		//		-	If the year is not divisible by 4, then the year is a nonleap year, with 365 days. 
		//			Number of leap years = (Years divided by 4) rounded downward to integer
		//							less   (Years divided by 100) rounded downward to integer
		nNumberOfLeapYears			+= (nAbsYear / 4u) - (nAbsYear / 100u);	
		//		-	If the year ends in 00 but is not divisible by 400, then the year is a nonleap year, with 365 days. 
		//		-	If the year ends in 00 and is divisible by 400, then the year is a leap year, with 366 days. 
		//			Addition leap years  = (Years divided by 400) rounded downward to integer
		nNumberOfLeapYears			+= (nAbsYear / 400u);

		assert( (UInt32)abs(nYear) >= nNumberOfLeapYears );

			// Note: We intentionally revert to using abs(nYear) instead of nAbsYear now.  We need the 0th year back in.
		UInt32	nNumberOfNonLeapYears	= abs(nYear) - nNumberOfLeapYears;
	
	#if 0
	#	ifdef _DEBUG	/** Debugging: Verify the above calculations by integrating over the 'IsLeapYear()' function... */
		UInt32	nDebugNumberOfLeapYears = 0, nDebugNumberOfNonLeapYears = 0;
		if( nYear >= 0 ){
			for( int zi = 0; zi < nYear; zi ++ ) 
				if( IsLeapYear(zi) ) nDebugNumberOfLeapYears ++; else nDebugNumberOfNonLeapYears ++;
		} else {
			for( int zi = 0; zi > -nYear; zi -- ) 
				if( IsLeapYear(zi) ) nDebugNumberOfLeapYears ++; else nDebugNumberOfNonLeapYears ++;
		}
		assert( nDebugNumberOfLeapYears == nNumberOfLeapYears );
		assert( nDebugNumberOfNonLeapYears == nNumberOfNonLeapYears );
	#	endif
	#endif

		if( nYear < 0 )
		{
			m_nSeconds	-= (Int64)nNumberOfLeapYears * 60ll * 60ll * 24ll * 366ll;
			m_nSeconds	-= (Int64)nNumberOfNonLeapYears * 60ll * 60ll * 24ll * 365ll;
		}
		else
		{
			m_nSeconds	+= (Int64)nNumberOfLeapYears * 60ll * 60ll * 24ll * 366ll;
			m_nSeconds	+= (Int64)nNumberOfNonLeapYears * 60ll * 60ll * 24ll * 365ll;
		}

	#ifdef _DEBUG
		assert( (3/4) == 0 );					// Ensure that compiler rounds downward to integer.
		UInt32 nTrial3 = 3, nTrial4 = 4;
		assert( (nTrial3/nTrial4) == 0 );		// Ensure that hardware rounds downward to integer.
	#endif		

		m_nNanoseconds = nNanoseconds;

	}// End of DateTime::Set()

	int	DateTime::GetYearAndRemainder( UInt32& nRemainder ) const
	{											  
		/*	Terms I invented to simplify this:
			Leap-Set:			Units of 4-years with 0th year a leap-year.
			Non-Leap Set:		Units of 4-years with no leap-years.
			Leap Century:		Units of 100 years.  i.e. from year 0 to 99, inclusive. 
			Non-Leap Century:	Units of 100 years.  i.e. from year 100 to 199, inclusive. 
			Periods:			Units of 400 years.  i.e. from year 0 to 399, inclusive.

			Leap Century:
				Includes (25) leap-years (year zero counted, year 100 not counted.)
				(Year 100 is not even present since a century here is 0-99.)
				That's 75 non-leap-years.
				A leap century is 100 years.

			Non-Leap Century:
				Includes (24) leap-years (year zero not counted.)
				That's 76 non-leap-years.
				A non-leap century is 100 years.

			Periods:
				Includes (25+24+24+24) leap-years (year zero counted, years 100, 200, and 300 not counted).
				That's 97 leap-years in a period.
				That's 303 non-leap-years in a period.
				A period is 400 years.
				The second period (years 400-799) behaves identical to the first period, and so on.

			A "leap-century" has one extra leap-year/century from a "non-leap-century".
			A "leap-year" has one extra day from a "non-leap-year".

			Outline:
				Any one Period.
					Leap Century.
						25 Leap-Sets { Each 1 Leap-Year 3 Non-Leap-Years }
					3x Non-Leap Centuries.
						Each {
							1 Non-Leap Set (4 Non-Leap-Years)
							24 Leap-Sets { Each 1 Leap-Year 3 Non-Leap-Years }
						}

			Footnote:			An unsigned 32-bit integer can hold the number of seconds in 135+ years.
		*/
		static const Int64 nSecondsPerPeriod
#                               if 0
								= 60i64 /*seconds/minute*/ * 60i64 /*minutes/hour*/ * 24i64 /*hours/day*/
								* ( (365i64 /*days*/ * 303i64 /*non-leap-years/period*/)
								+ (366i64 /*days*/ * 97i64 /*leap-years/period*/) ) /*days/period*/;
#                               endif
										= 12622780800ll;

		static const UInt64 nSecondsPerLeapCentury
#                               if 0
								= 60i64 /*seconds/minute*/ * 60i64 /*minutes/hour*/ * 24i64 /*hours/day*/
								* ( (365i64 /*days*/ * 75i64 /*non-leap-years/leap-century*/)
								+ (366i64 /*days*/ * 25i64 /*leap-years/leap-century*/) ) /*days/period*/;
#                               endif
										= 3155760000ull;

		static const UInt64	nSecondsPerNonLeapCentury
#                               if 0
								= 60i64 /*seconds/minute*/ * 60i64 /*minutes/hour*/ * 24i64 /*hours/day*/
								* ( (365i64 /*days*/ * 76i64 /*non-leap-years/non-leap-century*/)
								+ (366i64 /*days*/ * 24i64 /*leap-years/non-leap-century*/) ) /*days/period*/;
#                               endif                                
										= 3155673600ull;

		static const UInt32	nSecondsPerLeapSet		
								= 60 /*seconds/minute*/ * 60 /*minutes/hour*/ * 24 /*hours/day*/
								* ( (365 /*days*/ * 3 /*non-leap-years/leap-set*/)
								+ (366 /*days*/ /*x 1 leap-years/leap-set*/) ) /*days/period*/;

		static const UInt32	nSecondsPerNonLeapSet
								= 60 /*seconds/minute*/ * 60 /*minutes/hour*/ * 24 /*hours/day*/
								* ( 365 /*days*/ * 4 /*non-leap-years/non-leap-set*/ )/*days/period*/;

		Int64 nYear = 0;
		UInt64 nRemain;
		if (m_nSeconds < 0) nRemain = (UInt64)(-m_nSeconds);
		else nRemain = (UInt64)m_nSeconds;

			/*
				Algorithm:
					Take off number of periods (and count 400 years for each).  
					This leaves us *starting* at a year which is evenly divisible by 400.  
				
					We now have as much as this:  
						Leap-Century (year 400.)
						3 x Non-Leap Centuries (years 500-799.)

				Data Types:  The maximum m_nSeconds value, Int64_MaxValue / nSecondsPerPeriod is 7.3069e+08,
					which is less than the maximum 32-bit signed integer value of 2.1475e+09.  Therefore, the
					maximum 64-bit value is still supported by capturing periods in a 32-bit value, however
					the maximum resulting year (after x400) is 2.9228e+11, which requires a 64-bit value.
			*/

		UInt32 nNumberOfWholePeriods = (UInt32)(nRemain / nSecondsPerPeriod);		/* Gives units of 'periods' */
		nYear += nNumberOfWholePeriods * 400ll;
		nRemain -= (Int64)nNumberOfWholePeriods * nSecondsPerPeriod;

			/* 
				Algorithm B:
				------------
					Any whole leap-centuries?
					Yes.	Count off 1 whole leap-century if present.
							Count off remaining whole non-leap-centuries (up to 2 possible.)
							Any whole non-leap sets?
							Yes.	Count off 1 whole non-leap-set if present.
									Continue at algorithm C below.
							No.		Count off remaining whole non-leap-years.
									Done.

					No.		Continue at algorithm C below.
			*/

		if( nRemain >= nSecondsPerLeapCentury )
		{
				/* "Yes" codition in above algorithm */

				/* Count off whole leap-century */
			nYear += 100;
			nRemain -= nSecondsPerLeapCentury;

				/* Count off whole non-leap-centuries (up to 2 whole are possible, 1 partial.) */
			assert (nRemain < (2 * nSecondsPerNonLeapCentury) + nSecondsPerNonLeapCentury);
			while( nRemain >= nSecondsPerNonLeapCentury )
			{
				nYear += 100;
				nRemain -= nSecondsPerNonLeapCentury;
			}

				/* Left in remain:  0 to 99 years, in a non-leap-century */
			assert (nRemain <= (UInt64)UInt32_MaxValue);
			nRemainder	= (UInt32)nRemain;				// Switch to 32-bit arithmetic.

				/* Count off 1 whole non-leap-set if present */
			if( nRemainder >= nSecondsPerNonLeapSet ){
				nYear += 4;
				nRemainder -= nSecondsPerNonLeapSet;
			}
			else 
			{
					/* Count off remaining non-leap-years if present.
						Since we are in a partial non-leap-set, 3 whole are possible, and 1 partial.
					*/
				assert( nRemainder < (3 * time_constants::g_nSecondsPerNonLeapYear) + time_constants::g_nSecondsPerNonLeapYear );
				UInt32 nNumberOfNonLeapYears	= nRemainder / time_constants::g_nSecondsPerNonLeapYear;
				nYear += nNumberOfNonLeapYears;
				nRemainder -= nNumberOfNonLeapYears * time_constants::g_nSecondsPerNonLeapYear;

					// The remainder should contain no more than 1 year's worth of seconds.
				assert( nRemainder <= time_constants::g_nSecondsPerLeapYear );

					// The internal representation can support years beyond the 32-bit range, but the public interface does not.
				assert (nYear >= Int32_MinValue && nYear <= Int32_MaxValue);

				if (m_nSeconds >= 0) return (int)nYear;
				else return (int)-nYear;
			}

				/* Next:	Count off remaining leap-sets if present.
							Since we are in a non-leap-century, and we've counted off one, 23 are possible.
							1 partial is possible. */
			assert (nRemainder < (23 * nSecondsPerLeapSet) + nSecondsPerLeapSet);
		}
		else { 
			/* Else, next:	Count off remaining leap-sets if present.
							Since we are in a leap-century, 25 are possible.  1 partial is possible. */
			assert (nRemain < (25 * nSecondsPerLeapSet) + nSecondsPerLeapSet); 

				/* Left in remain:  0 to 99 years, in a leap-century */
			assert (nRemain <= (UInt64)UInt32_MaxValue);
			nRemainder	= (UInt32)nRemain;				// Switch to 32-bit arithmetic.
		}

			/**
				Algorithm C
				-----------
					Count off remaining whole leap-sets if present.
					Continue at Algorithm D below.
			**/

			/* Count off remaining leap-sets if present.			
			*/
		UInt32 nNumberOfLeapSets	= nRemainder / nSecondsPerLeapSet;
		nYear += nNumberOfLeapSets * 4;
		nRemainder -= nNumberOfLeapSets * nSecondsPerLeapSet;	

			/**
				Algorithm D
				-----------
					Is there a whole leap-year?
					Yes.	Count off 1 leap-year.
							Count off remaining whole non-leap-years.
							Done.
					No.		Done.
			**/

			/* Count off whole leap-year if present. */
		if( nRemainder >= time_constants::g_nSecondsPerLeapYear )
		{
			nYear ++;
			nRemainder -= time_constants::g_nSecondsPerLeapYear;	

				/* Count off remaining non-leap-years if present.
					Since we are in a leap-set, and we've counted off one, 2 are possible.
					1 partial is possible.
				*/
			assert (nRemainder < (2 * time_constants::g_nSecondsPerNonLeapYear) + time_constants::g_nSecondsPerNonLeapYear);
			UInt32 nNumberOfNonLeapYears	= nRemainder / time_constants::g_nSecondsPerNonLeapYear;
			nYear += nNumberOfNonLeapYears;
			nRemainder -= nNumberOfNonLeapYears * time_constants::g_nSecondsPerNonLeapYear;
		}

			// The remainder should contain no more than 1 year's worth of seconds.
		assert (nRemainder <= time_constants::g_nSecondsPerLeapYear);

			// The internal representation can support years beyond the 32-bit range, but the public interface does not.
		assert (nYear >= Int32_MinValue && nYear <= Int32_MaxValue);

		if (m_nSeconds >= 0) return (int)nYear;
		else return (int)-nYear;

	}// End of GetYearAndRemainder()

	int		DateTime::GetMonthFromRemainder( UInt32& nRemainder, int nYear, bool bLeapYear ) const
	{
		assert( (bLeapYear || nRemainder < time_constants::g_nSecondsPerNonLeapYear) );
		assert( (!bLeapYear || nRemainder < time_constants::g_nSecondsPerLeapYear) );

		if( nYear < 0 ){
				// At this point, flip the remainder within 1 year.  For example,
				// January 10th, 1 B.C. will have a remainder that corresponds to
				// December 21st, 1 B.C. if the years are negative.  So if we
				// subtract from seconds-in-1-year, we correct for this.
			if( bLeapYear ) nRemainder = time_constants::g_nSecondsPerLeapYear - nRemainder;
			else nRemainder = time_constants::g_nSecondsPerNonLeapYear - nRemainder;
		}
	
		 /** Perform a binary search **/

		int nMonth	= 6, nAbsDelta = 3;
		for(;;)
		{
			if( nMonth <= 1 ) return nMonth;

			if( bLeapYear )
			{
				if( nMonth >= 12 ){ 
					nRemainder -= time_constants::g_tableSecondsPastInLeapYear[nMonth];
					return nMonth; 
				}

				if( nRemainder >= time_constants::g_tableSecondsPastInLeapYear[nMonth] ){
					if( nRemainder < time_constants::g_tableSecondsPastInLeapYear[nMonth+1] ){
						nRemainder -= time_constants::g_tableSecondsPastInLeapYear[nMonth];
						return nMonth;
					}
					else nMonth += nAbsDelta;
				}
				else nMonth -= nAbsDelta;
			}
			else
			{
				if( nMonth >= 12 ){ 
					nRemainder -= time_constants::g_tableSecondsPastInNonLeapYear[nMonth];
					return nMonth; 
				}

				if( nRemainder >= time_constants::g_tableSecondsPastInNonLeapYear[nMonth] ){
					if( nRemainder < time_constants::g_tableSecondsPastInNonLeapYear[nMonth+1] ){
						nRemainder -= time_constants::g_tableSecondsPastInNonLeapYear[nMonth];
						return nMonth;
					}
					else nMonth += nAbsDelta;
				}
				else nMonth -= nAbsDelta;
			}

			nAbsDelta >>= 1;
			if( !nAbsDelta ) nAbsDelta = 1;
		}
	}// End of GetMonthFromRemainder()


	void	DateTime::AddMonths( int nMonths /*= 1*/ )
	{
		UInt32 nRemainder;
		int nYear = GetYearAndRemainder( nRemainder );
		bool bLeapYear = IsLeapYear( nYear );
		int nThisMonth = GetMonthFromRemainder( nRemainder, nYear, bLeapYear );

		if (nMonths > 0)
		{
			while (nMonths --)
			{
				if (nThisMonth == 12)
				{
					if (bLeapYear)
						m_nSeconds += time_constants::g_tableSecondsInMonthLY[nThisMonth];
					else
						m_nSeconds += time_constants::g_tableSecondsInMonthNLY[nThisMonth];

					nYear ++;
					nThisMonth = 1;
					bLeapYear = IsLeapYear(nYear);
					continue;
				}

				if (bLeapYear)
					m_nSeconds += time_constants::g_tableSecondsInMonthLY[nThisMonth];
				else
					m_nSeconds += time_constants::g_tableSecondsInMonthNLY[nThisMonth];
				nThisMonth ++;
			}
		}
		else
		{
			nMonths = abs(nMonths);
			while (nMonths --)
			{
				if (nThisMonth == 1)
				{
					if (bLeapYear)
						m_nSeconds -= time_constants::g_tableSecondsInMonthLY[nThisMonth];
					else
						m_nSeconds -= time_constants::g_tableSecondsInMonthNLY[nThisMonth];

					nYear --;
					nThisMonth = 12;
					bLeapYear = IsLeapYear(nYear);
					continue;
				}

				if( bLeapYear )
					m_nSeconds -= time_constants::g_tableSecondsInMonthLY[nThisMonth];
				else
					m_nSeconds -= time_constants::g_tableSecondsInMonthNLY[nThisMonth];
				nThisMonth --;
			}
		}
	}// End of AddMonths()

	void DateTime::AddYears( int nYears /*= 1*/ )
	{
		if (nYears >= 0)
		{
			while (nYears --)
			{
				if (IsLeapYear()) 
					m_nSeconds	+= time_constants::g_nSecondsPerLeapYear;
				else
					m_nSeconds	+= time_constants::g_nSecondsPerNonLeapYear;
			}
		}
		else
		{
			nYears = abs(nYears);
			while (nYears --)
			{
				if (IsLeapYear()) 
					m_nSeconds	-= time_constants::g_nSecondsPerLeapYear;
				else
					m_nSeconds	-= time_constants::g_nSecondsPerNonLeapYear;
			}
		}
	}// End of AddYears()

	/////////
	//	String Functions
	//

	string getWord( const string& str )
	{
		string word;
		for (size_t ii = 0; ii < str.length(); ii++) if( str[ii] == ' ' ) return word; else word += str[ii];
		return word;
	}

	/*static*/ bool DateTime::TryParse(const char* psz, DateTime& Value)
	{
		string str = psz;

		/**
				Sun, 06 Nov 1994 08:49:37 GMT		; (1) RFC 822, updated by RFC 1123
				Sunday, 06-Nov-94 08:49:37 GMT		; (2) RFC 850, obsoleted by RFC 1036
				Sun Nov  6 08:49:37 1994			; (3) ANSI C's asctime() format
				1:13:15 a.m. Sunday June 8, 2005	; (4) asPresentationString() format
				1994-11-05T08:15:30-05:00			; (5a) ISO 8601 with a bias
				1994-11-05T08:15:30Z				; (5b) ISO 8601 with UTC
			
				012345678901234567890123456789
		**/

		// Only formats 1 (returned by ToString()), 2, 4, and 5 are currently supported.

		if (str.find(S("a.m.")) != string::npos || str.find(S("p.m.")) != string::npos || str.find(S("A.M.")) != string::npos || str.find(S("P.M.")) != string::npos 
			|| str.find(S("pm")) != string::npos || str.find(S("am")) != string::npos || str.find(S("PM")) != string::npos || str.find(S("AM")) != string::npos)
		{
				// Format 4:   01:13:15 a.m. Sunday June 08, 2005

			size_t iIndex;

				// Read hours...
			Int32 nHour;
			if (!Int32_TryParse(str.c_str(), NumberStyles::Integer, nHour)) return false;
			if( str[1] == ':' ) iIndex = 2;
			else if( str[2] == ':' ) iIndex = 3;
			else return false;

				// Read minutes...
			if( iIndex+2 >= str.length() ) return false;
			Int32 nMinute;
			if (!Int32_TryParse(str.substr(iIndex, 2).c_str(), NumberStyles::Integer, nMinute)) return false;
			iIndex += 2;

				// Read seconds, if present
			Int32 nSecond = 0;
			if( iIndex >= str.length() ) return false;
			if( str[iIndex] == ':' ){
				iIndex ++;
				if( iIndex+2 >= str.length() ) return false;
				if (!Int32_TryParse(str.substr(iIndex, 2).c_str(), NumberStyles::Integer, nSecond)) return false;
				iIndex += 2;
			}
			while( iIndex < str.length() && str[iIndex] == ' ' ) iIndex++;		// Skip whitespace

				// Read seconds & am/pm...
			bool bMorning;
			if( iIndex+5 >= str.length() ) return false;
			if( to_lower(str.substr(iIndex,4)).compare(S("a.m.")) == 0 ){ bMorning = true; iIndex += 5; }
			else if( to_lower(str.substr(iIndex,4)).compare(S("p.m.")) == 0 ){ bMorning = false; iIndex += 5; }
			else if( to_lower(str.substr(iIndex,2)).compare(S("am")) == 0 ){ bMorning = true; iIndex += 3; }
			else if( to_lower(str.substr(iIndex,2)).compare(S("pm")) == 0 ){ bMorning = false; iIndex += 3; }
			else return false;

				// Skip over weekday...
			if( iIndex >= str.length() ) return false;
			string	strWeekday = getWord(str.substr(iIndex));
			iIndex += strWeekday.length() + 1;

				// Read month...
			int nMonth;
			if( iIndex >= str.length() ) return false;
			string strMonth = to_lower(getWord(str.substr(iIndex)));
			if( strMonth.compare(S("january")) == 0 )			nMonth = 1;
			else if( strMonth.compare(S("february")) == 0 )	nMonth = 2;
			else if( strMonth.compare(S("march")) == 0 )		nMonth = 3;
			else if( strMonth.compare(S("april")) == 0 )		nMonth = 4;
			else if( strMonth.compare(S("may")) == 0 )			nMonth = 5;
			else if( strMonth.compare(S("june")) == 0 )		nMonth = 6;
			else if( strMonth.compare(S("july")) == 0 )		nMonth = 7;
			else if( strMonth.compare(S("august")) == 0 )		nMonth = 8;
			else if( strMonth.compare(S("september")) == 0 )	nMonth = 9;
			else if( strMonth.compare(S("october")) == 0 )		nMonth = 10;
			else if( strMonth.compare(S("november")) == 0 )	nMonth = 11;
			else if( strMonth.compare(S("december")) == 0 )	nMonth = 12;
			else return false;
			iIndex += strMonth.length() + 1;

				// Read day of month and comma
			if( iIndex+2 >= str.length() ) return false;
			Int32 nDay;
			if (!Int32_TryParse(str.substr(iIndex,2).c_str(), NumberStyles::Integer, nDay)) return false;			
			while( iIndex < str.length() && str[iIndex] != ',' ) iIndex ++;
			if( iIndex+1 >= str.length() || str[iIndex] != ',' ) return false;
			iIndex ++;
			if( iIndex+1 >= str.length() || str[iIndex] != ' ' ) return false;
			iIndex ++;

				// Read year
			Int32 nYear;
			if (!Int32_TryParse(str.substr(iIndex).c_str(), NumberStyles::Integer, nYear)) return false;			

				// Apply modifiers
			if( !bMorning ){
				if( nHour != 12 ) nHour += 12;		// 12 p.m. becomes '12' on tweny-four hour clock.  All other p.m. hours get +12.
			}	
			else {
				if( nHour == 12 ) nHour = 0;		// 12 a.m. becomes '0' on twenty-four hour clock.  All other a.m. hours are unmodified.
			}

			if( ( nMonth >= 1 && nMonth <= 12 )
			 && ( nDay >= 1 && nDay <= 31 )
			 && ( nHour >= 0 && nHour <= 23 )
			 && ( nMinute >= 0 && nMinute <= 59 )
			 && ( nSecond >= 0 && nSecond <= 59 ) )
			{
					// Presentation time is always assumed to be given in the local time zone.

				Value = DateTime(nYear, nMonth, nDay, nHour, nMinute, nSecond, 0, LocalTimeZone);
				return true;
			}
			else return false;
		}
		else if(str.find(S("GMT")) != string::npos || str.find(S("UTC")) != string::npos)
		{
				// Sun, 06 Nov 1994 08:49:37 GMT		; (1) RFC 822, updated by RFC 1123
				// Sunday, 06-Nov-94 08:49:37 GMT		; (2) RFC 850, obsoleted by RFC 1036
				// Sun, 06 Nov 1994 08:49:37 UTC		; Supported variant of (1)
				// Sunday, 06-Nov-94 08:49:37 UTC		; Supported variant of (2)

			size_t iIndex = str.find(',');
			if( iIndex == string::npos ) return false;

			while (iIndex < str.length() && (str[iIndex] == ',' || isspace(str[iIndex]))) iIndex++;
			if (iIndex >= str.length()) return false;
			if (!isdigit(str[iIndex])) return false;

			Int32 nDay;
			if (!Int32_TryParse(str.substr(iIndex).c_str(), NumberStyles::Integer, nDay)) return false;
			while (iIndex < str.length() && !isalpha(str[iIndex])) iIndex++;
			if (iIndex >= str.length()) return false;

				// Note that extended month names (i.e. January) will also work since the first 3 letters are
				// always the abreviation.
			int nMonth;
			string strMonth = to_lower(str.substr(iIndex,3));
			if( strMonth.compare(S("jan")) == 0 ) nMonth = 1;
			else if( strMonth.compare(S("feb")) == 0 ) nMonth = 2;
			else if( strMonth.compare(S("mar")) == 0 ) nMonth = 3;
			else if( strMonth.compare(S("apr")) == 0 ) nMonth = 4;
			else if( strMonth.compare(S("may")) == 0 ) nMonth = 5;
			else if( strMonth.compare(S("jun")) == 0 ) nMonth = 6;
			else if( strMonth.compare(S("jul")) == 0 ) nMonth = 7;
			else if( strMonth.compare(S("aug")) == 0 ) nMonth = 8;
			else if( strMonth.compare(S("sep")) == 0 ) nMonth = 9;
			else if( strMonth.compare(S("oct")) == 0 ) nMonth = 10;
			else if( strMonth.compare(S("nov")) == 0 ) nMonth = 11;
			else if( strMonth.compare(S("dec")) == 0 ) nMonth = 12;
			else return false;

				// Skip past spaces, dashes, and month name until we get to the year.
			while (iIndex < str.length() && !isnumeric(str[iIndex])) iIndex++;
			if (iIndex >= str.length()) return false;
			Int32 nYear;
			if (!Int32_TryParse(str.substr(iIndex).c_str(), NumberStyles::Integer, nYear)) return false;

			if (nYear < 100){
				DateTime dtNow = DateTime::Now(); 
				Int32 nCurrentYear = dtNow.GetYear();
				Int32 nCurrent2Year = nCurrentYear % 100ll;
				Int32 nCurrentCentury = (nCurrentYear - nCurrent2Year);
				nYear = nCurrentCentury + nYear;
			}

				// Skip to the whitespace following the year.
			while (iIndex < str.length() && !isspace(str[iIndex])) iIndex++;
			if (iIndex >= str.length()) return false;
				// Skip to the first numeric following the whitespace following the year.
			while (iIndex < str.length() && !isnumeric(str[iIndex])) iIndex++;
			if (iIndex >= str.length()) return false;

			str = str.substr(iIndex);

				// 08:49:37 GMT
				// 012345678901

			string strGMT = str.substr(9,3);
			if (str.length() < 12 || (strGMT.compare(S("GMT")) != 0 && strGMT.compare(S("UTC")) != 0)
			 || str[2] != ':' || str[5] != ':' ) return false;

			string strHour = str.substr( 0, 2 );
			string strMin  = str.substr( 3, 2 );
			string strSec  = str.substr( 6, 2 );

			Int32 nHour, nMinute, nSecond;
			if (!Int32_TryParse(strHour, NumberStyles::Integer, nHour)
			 || !Int32_TryParse(strMin, NumberStyles::Integer, nMinute)
			 || !Int32_TryParse(strSec, NumberStyles::Integer, nSecond)) return false;

			if (( nMonth >= 1 && nMonth <= 12 )
			 && ( nDay >= 1 && nDay <= 31 )
			 && ( nHour >= 0 && nHour <= 23 )
			 && ( nMinute >= 0 && nMinute <= 59 )
			 && ( nSecond >= 0 && nSecond <= 59 ))
			{
					// This format always stores time in GMT (a.k.a. UTC or Zulu) time.

				Value = DateTime(nYear, nMonth, nDay, nHour, nMinute, nSecond, 0, UTC);
				return true;
			}
			else return false;		
		}	
		else if (str.length() >= 10 && str[4] == '-' && str[7] == '-' )
		{
				// 012345678901234567890123456
				// 1994-11-05T08:15:30-05:00			; (5a) ISO 8601 with a bias
				// 1994-11-05T08:15:30Z					; (5b) ISO 8601 with UTC
				// 1994-11-05T08:15:30.123456-05:00		; (5c) ISO 8601 variant with a bias
				// 1994-11-05T08:15:30.123456Z			; (5d) ISO 8601 variant with UTC

			string strYear = str.substr(0,4);
			string strMonth = str.substr(5,2);
			string strDay = str.substr(8,2);

			Int32 nYear, nMonth, nDay;
			if (!Int32_TryParse(strYear, NumberStyles::Integer, nYear)
			 || !Int32_TryParse(strMonth, NumberStyles::Integer, nMonth)
			 || !Int32_TryParse(strDay, NumberStyles::Integer, nDay)) return false;			
			if( nMonth < 1 || nMonth > 12 || nDay < 1 || nDay > 31 ) return false;

			Int32 nHour = 0, nMinute = 0;
			double dSecond = 0.0;
			Int32 nBiasMinutes = 0;

			if (str.length() >= 16 && str[10] == 'T' && str[13] == ':' )
			{
				string strHour = str.substr(11,2);
				string strMinute = str.substr(14,2);

				if (!Int32_TryParse(strHour, NumberStyles::Integer, nHour)
				 || !Int32_TryParse(strMinute, NumberStyles::Integer, nMinute)) return false;				
				if( nHour < 0 || nHour > 23 || nMinute < 0 || nMinute > 59) return false;

				if (str.length() >= 18)
				{
					size_t nSecondDigits = 0;
					while (17+nSecondDigits < str.length() && (isdigit(str[17+nSecondDigits]) || str[17+nSecondDigits] == '.')) nSecondDigits++;

					if (!nSecondDigits) return false;
					string strSecond = str.substr(17,nSecondDigits);
					if (!Double_TryParse(strSecond, NumberStyles::Float, dSecond)) return false;					
					if (dSecond < 0.0 || dSecond > 60.0) return false;

					if (str.length() > 17+nSecondDigits){
						if (str[17+nSecondDigits] == 'Z' ) nBiasMinutes = 0;
						else if( str[17+nSecondDigits] == '-' || str[17+nSecondDigits] == '+' )
						{
							if (str.length() >= 17+nSecondDigits+3)
							{
								string strBiasHours	= str.substr(17+nSecondDigits,3);

								Int32 nBiasHours;
								if (!Int32_TryParse(strBiasHours, NumberStyles::Integer, nBiasHours)) return false;								

								if (str.length() > 17+nSecondDigits+3 && str[17+nSecondDigits+3] == ':')
								{
									string strBiasMinutes	= str.substr(17+nSecondDigits+4,2);
									
									if (!Int32_TryParse(strBiasMinutes, NumberStyles::Integer, nBiasMinutes)) return false;

									if (nBiasHours >= 0) nBiasMinutes = nBiasHours * 60 + nBiasMinutes;
									else nBiasMinutes = nBiasHours * 60 - nBiasMinutes;
								}
							}
							else return false;
						}
						else return false;
					}
				}
			}

			Int32 nSecond = (Int32)dSecond;
			Int32 nNanosecond = (Int32)(fmod(dSecond, 1.0) / time_constants::g_dSecondsPerNanosecond);

			Value = DateTime(nYear, nMonth, nDay, nHour, nMinute, nSecond, nNanosecond, nBiasMinutes);
			return true;
		}
		else if( str.length() == 24 && str[3] == ' ' && str[7] == ' ' && str[10] == ' ' && str[19] == ' ' 
			&& str[13] == ':' && str[16] == ':' )
		{
				// Sun Nov  6 08:49:37 1994			; (3) ANSI C's asctime() format
				// 012345678901234567890123
				//			 1		   2

			int nMonth;
			string strMonth = to_lower(str.substr(4,3));
			if( strMonth.compare(S("jan")) == 0 ) nMonth = 1;
			else if( strMonth.compare(S("feb")) == 0 ) nMonth = 2;
			else if( strMonth.compare(S("mar")) == 0 ) nMonth = 3;
			else if( strMonth.compare(S("apr")) == 0 ) nMonth = 4;
			else if( strMonth.compare(S("may")) == 0 ) nMonth = 5;
			else if( strMonth.compare(S("jun")) == 0 ) nMonth = 6;
			else if( strMonth.compare(S("jul")) == 0 ) nMonth = 7;
			else if( strMonth.compare(S("aug")) == 0 ) nMonth = 8;
			else if( strMonth.compare(S("sep")) == 0 ) nMonth = 9;
			else if( strMonth.compare(S("oct")) == 0 ) nMonth = 10;
			else if( strMonth.compare(S("nov")) == 0 ) nMonth = 11;
			else if( strMonth.compare(S("dec")) == 0 ) nMonth = 12;
			else return false;

			string strDay	= str.substr(8,2);
			string strHour	= str.substr(11,2);
			string strMin	= str.substr(14,2);
			string strSec   = str.substr(17,2);
			string strYear  = str.substr(20);

			Int32 nDay, nYear, nHour, nMinute, nSecond;
			if (!Int32_TryParse(strDay, NumberStyles::Integer, nDay)
			 || !Int32_TryParse(strYear, NumberStyles::Integer, nYear)
			 || !Int32_TryParse(strHour, NumberStyles::Integer, nHour)
			 || !Int32_TryParse(strMin, NumberStyles::Integer, nMinute)
			 || !Int32_TryParse(strSec, NumberStyles::Integer, nSecond)) return false;
			
			if (( nMonth >= 1 && nMonth <= 12 )
			 && ( nDay >= 1 && nDay <= 31 )
			 && ( nHour >= 0 && nHour <= 23 )
			 && ( nMinute >= 0 && nMinute <= 59 )
			 && ( nSecond >= 0 && nSecond <= 59 ))
			{
					// This format always stores time in local time.

				Value = DateTime(nYear, nMonth, nDay, nHour, nMinute, nSecond, 0, LocalTimeZone);
				return true;
			}
			else return false;
		}
		else
		{
			return false;					// Unrecognized time format.
		}

	}// End of DateTime::TryParse()

	/*static*/ DateTime DateTime::Parse(const char* psz)
	{
		DateTime ret;
		if (!TryParse(psz, ret)) 
			throw FormatException(S("Unable to parse date/time."));
		return ret;
	}
}

//	End of DateTime.cpp
