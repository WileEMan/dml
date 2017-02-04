/**	DateTime.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
**/

/*
	A class for representing dates and times.

	- Signed 128-bit integer implementation supports any time with nanosecond precision.  
	- Time zone representation.  Time always stored internally as UTC, but a bias is represented which
		allows support for any specified time zone.
	- The data content is comprised of:
		- Number of seconds elapsed since epoch (signed 64-bit).
		- Number of nanoseconds elapsed from second (unsigned 32-bit).
		- Bias (in seconds) from UTC.	
	- The epoch is year zero, time zero.

	Conversions:

		To/From UTC/local-time-zone:

			// Note the extra zero on the end of the following contructor call (bias parameter.)
		DateTime	dtUTC( 1980, 1, 21, 0, 0, 0, 0 );			// Jan 21st, 1980 at midnight in UTC time.
		DateTime	dtAsLocalTime = dtUTC.asLocalTime();		// Now in local time.
		DateTime	dtBackAgain = dtAsLocalTime.asUTC();		// Jan 21st, 1980 at midnight in UTC time.
		assert( dtUTC == dtBackAgain );

		To/From an MFC CTime:

		DateTime	dtJan21_1980( 1980, 1, 21, 0, 0, 0 );		// Jan 21st, 1980 at midnight in local time zone.
		time_t		timeValue	= (time_t)dtJan21_1980;			// Uses the time_t conversion.  Now in UTC.
		CTime		tmJan21_1980( dtJan21_1980 );				// Uses the time_t conversion.  CTime in local time.
		assert( timeValue == tmJan21_1980.GetTime() );			// CTime's internal representation is the time_t.
		DateTime	dtBackAgain( tmJan21_1980 );				// Uses the CTime constructor.  Remains local time.
		assert( dtBackAgain == dtJan21_1980 );

		To/From a time_t:
			* See known issues.

		DateTime	dtJan21_1980( 1980, 1, 21, 0, 0, 0 );		// Jan 21st, 1980 at midnight in local time zone.
		time_t		timeJan21_1980_UTC	= (time_t)dtJan21_1980;	// Uses the time_t conversion.  Becomes UTC time.
		DateTime	dtJan21_1980_UTC( timeJan21_1980_UTC );		// Now in UTC time.

	Implementation:

		Platform requires support for 64-bit signed integers.

		Represents date/time as a 64-bit signed integer as the number of seconds
		elapsed since midnight (00:00:00), January 1, 0.  This counts year zero
		as a year.  Negative numbers of greater magnitude (i.e. -500 vs -5) are
		earlier in a timeline (i.e. -31M would be 1 B.C. and -63M would be 2 B.C.)

		Gregorian Calendar Rules Applied:
		-	If the year is divisible by 4 but does not end in 00, then the year is a leap year, with 366 days. 
			Examples: 1996, 2004. 
		-	If the year is not divisible by 4, then the year is a nonleap year, with 365 days. 
			Examples: 2001, 2002, 2003, 2005. 
		-	If the year ends in 00 but is not divisible by 400, then the year is a nonleap year, with 365 days. 
			Examples: 1900, 2100. 
		-	If the year ends in 00 and is divisible by 400, then the year is a leap year, with 366 days. 
			Examples: 1600, 2000, 2400. 
		-	The year zero was a leap year.
		-	Months:			Jan, Mar, May, Jul, Aug, Nov, Dec have 31 days.
		-	Months:			        Apr, Jun,      Sep, Oct   have 30 days.
		-	Non-Leap Year:  Feb has 28 days.  Year has 365 days.
		-	Leap Year:		Feb has 29 days.  Year has 366 days.		

	Limitations:

		Projection of times into different eras is not performed.  For example, calculating a time value
		in an era when daylight savings time was not applied will provide the time on today's Gregorian
		calendar with today's daylight svaings rules, but will not correct for differences from the 
		era.  Some examples can be identified:

		- Eras with different daylight savings time rules may result in inaccurate time zone biases.
		- Eras using different calendars.  For example, a date prior to 1582 would have been specified
		  on the Julian calendar, however the adjustment will not be applied by DateTime automatically.
		- Calculating a time with a bias set by daylight savings time on (off) and then adding or subtracting
		  a change in time to a period when daylight savings time would be off (on).  The original bias
		  will be retained.

		The bias for "local times" is calculated with respect to the current system bias, not the bias that
		would apply at a different date.
*/
/////////

#ifndef __DateTime_h__
#define __DateTime_h__

/////////
//	Dependencies
//

#if (defined(_MSC_VER) && !defined(_INC_WINDOWS))
	#error Include Windows.h before this header.
#endif

#include "../Platforms/Platforms.h"
#include "../Exceptions.h"
#include "../Text/String.h"
#include "TimeConstants.h"

#if (defined(_MSC_VER))
#include <sys\types.h>
#include <stdlib.h>
#include <assert.h>
#include <float.h>
#include <time.h>
#endif

#ifndef _WINDOWS
typedef struct _FILETIME {
  UInt32 dwLowDateTime;
  UInt32 dwHighDateTime;
} FILETIME, *PFILETIME;
#endif

namespace wb
{
	class TimeSpan;

	class DateTime
	{
			// Relative offset for the time_t type (both are in units of seconds but at different initial times.)
		static const Int64 g_nOffsetForTimeT;

			// Relative offset for the FILETIME type
		static const Int64 g_nOffsetForFiletime;

	protected:

		Int64	m_nSeconds;				// Time, in seconds, since epoch (Zero).
		UInt32	m_nNanoseconds;			// Nanoseconds after m_nSeconds.
		Int32	m_nBias;				// The bias (in seconds) from UTC.  UTC (in seconds) = m_nSeconds - m_nBias.

			// The Get...Remainder() functions always operate on positive remainders.  This makes 
			// sense because only years can be negative.  At the 'year-to-month' remainder
			// transition, negative years cause the remainder to be subtracted out of 1 year.

			// GetYearAndRemainder() uses remainder as an output only.
			// For years B.C., the return year will be negative but the remainder
			// will be positive (absolute value).
		int		GetYearAndRemainder( UInt32& nRemainder ) const;

			// GetMonthFromRemainder() uses remainder as both an input and an output.
			// For years B.C., the remainder returned from GetMonthFromRemainder() will have
			// been subtracted from 1 year.
		int		GetMonthFromRemainder( UInt32& nRemainder, int nYear, bool bLeapYear ) const;

			// GetDaysFromRemainder() uses remainder as both an input and an output.
			// It returns the number of WHOLE days contained in the input remainder.
			// For day-of-month presentation, add one to the return value.
		int		GetDaysFromRemainder( UInt32& nRemainder ) const;

			// These Get...FromRemainder() uses remainder as both an input and an output.
			// It returns (and removes) the number of WHOLE units contained in the remainder.
			// The remainder of the call to GetMinutesFromRemainder() is the number of seconds.
		int		GetHoursFromRemainder( UInt32& nRemainder ) const;
		int		GetMinutesFromRemainder( UInt32& nRemainder ) const;

	public:

		enum { 
			UTC				= 0,
			LocalTimeZone	= Int32_MaxValue
		};

		enum {
			January	= 1,
			February,
			March,
			April,
			May,
			June,
			July,
			August,
			September,
			October,
			November,
			December
		};

		enum DayOfWeek {
			Sunday = 0,
			Monday,
			Tuesday,
			Wednesday,
			Thursday,
			Friday,
			Saturday
		};

			/** For example, for January returns 31.  
				The last day of January is always January 31st. **/
		static int GetDaysInMonth( int nMonth, bool bLeapYear );
		static int GetDaysInMonth( int nMonth, int nYear );

			/** Day specified as (1-31) **/
			/** Month specified as (1-12) **/
			/** Year specified as (-200 billion to +200 billion) **/
			/** Hour specified as (0-23) **/
			/** Minutes and seconds specified as (0-59) **/
			/** Bias is the bias from UTC time, in minutes (+/- 1439 or the special value 'LocalTimeZone') **/
			/** UTC = time/date-specified + Bias **/
			/** e.g. For a time/date specified in U.S. Mountain Standard Time, outside DST, the Bias would be +(7 * 60). **/				
		DateTime(int nYear, int nMonth, int nDay, int nHour, int nMinute, int nSecond, int nNanosecond = 0, int nBiasMinutes = LocalTimeZone);
		DateTime(const DateTime&);
		DateTime(DateTime&&);
		DateTime(FILETIME);
		//DateTime( const SYSTEMTIME&, int nBiasMinutes = LocalTimeZone );
		DateTime(time_t);				// Note: The value will be in UTC time.  Conversions are possible, see usage.
		DateTime();	

		int			GetYear() const;
		int			GetMonth() const;
		int			GetDay() const;
		int			GetHour() const;		// Returns the hour as (0-23)	
		int			GetMinute() const;
		int			GetSecond() const;

		int			GetHourIn12HourFormat() const;
		bool		IsAM() const;
		bool		IsPM() const;

		void		GetDayOfWeek( DayOfWeek& nValue ) const;
		string		GetDayOfWeek() const;

		string		GetMonthAsString() const;

			/**	Now() returns a DateTime object representing the date/time at the time of the function call in local time.
				UtcNow() returns a DateTime object representing the date/time at the time of the function call in UTC.				
			**/
		static DateTime		Now();
		static DateTime		UtcNow();
		// static DateTime		Zero();			// Returns the date time corresponding to midnight, January 1, 0000 UTC.

			/** GetCurrentDate()
				Returns a CDateTime object corresponding to midnight of the current day in UTC.
			**/
		static DateTime		GetCurrentDate();

			/** SetSystemTime()
				Sets the current system time to match the DateTime object's value. **/
		static void			SetSystemTime(const DateTime& to);

			// The following Get() calls are much less computationally expensive than individual Get...() calls.
		void	Get(int& nYear, int& nMonth, int& nDay, int& nHour, int& nMinute, int& nSecond, int& nNanosecond) const;
		void	Get(int& nYear, int& nMonth, int& nDay, int& nHour, int& nMinute, int& nSecond) const;
		void	GetDate(int& nYear, int& nMonth, int& nDay) const;
		void	GetTime(int& nHours, int& nMinutes, double& dSeconds) const;
		void	GetTime(int& nHours, int& nMinutes, int& nSeconds) const;

		bool	IsLeapYear() const;							// Returns true if the current year is a leap year.
		static bool IsLeapYear(int nYear);				// Returns true if 'nYear' is a leap year.

			/** GetSecondsIntoYear()
				GetSecondsIntoYear() returns the number of true seconds elapsed from the beginning of the year.
				Use this (probably after converting to UTC) in combination with GetYear() as one method of
				disassembling a DateTime value.
			**/
		UInt32	GetSecondsIntoYear() const;		

			// For range of parameters, see matching constructor.		
		void	Set(int nYear, int nMonth, int nDay, int nHour, int nMinute, int nSecond, int nNanoseconds = 0, int nBiasMinutes = LocalTimeZone);
		void	Set(time_t);
		void	Set(UInt64 Seconds, UInt32 Nanoseconds, int nBiasSeconds /*= 0 for UTC*/);

			// The Add...() functions can accept negative values.
			// The Add...() functions can accept values beyond one unit (for example, you can add 3600 seconds.)
			// Effects such as leap-years are automatically accounted for.
		void	AddTime( int nHours, int nMinutes, double dSeconds );	// Adds up to 24 hours to the value of this object.
		void	AddTime( int nHours, int nMinutes, int nSeconds );		// Adds up to 24 hours to the value of this object.
		void	AddDays( int nDays = 1 );		// Adds N days (24-hours ea) to the value of this object.
		void	AddMonths( int nMonths = 1 );	// Adds N month (28,29,30, or 31 days ea) to the value of this object.
		void	AddYears( int nYears = 1 );		// Adds N year (365 or 366 days ea) to the value of this object.
		void	Add( int nYears, int nMonths, int nDays, int nHours, int nMinutes, double dSeconds );
		void	Add( int nYears, int nMonths, int nDays, int nHours, int nMinutes, int nSeconds );	

			/** Operations **/
	
		bool operator==( const DateTime& ) const;
		bool operator!=( const DateTime& ) const;
		bool operator<=( const DateTime& ) const;
		bool operator<( const DateTime& ) const;
		bool operator>=( const DateTime& ) const;
		bool operator>( const DateTime& ) const;

		const DateTime& operator=( const DateTime& );
		const DateTime& operator+=( const TimeSpan& timeSpan );
		const DateTime& operator-=( const TimeSpan& timeSpan );
		DateTime operator+( const TimeSpan& tmSpan ) const;
		DateTime operator-( const TimeSpan& tmSpan ) const;
		TimeSpan operator-( const DateTime& ) const;			

			/** Conversions **/

		operator time_t() const;				// Automatically returns UTC time based on the given bias.
		operator tm() const;					// Returns local time zone time.
		operator bool() const;					// Returns true if time is initialized.	
		bool operator!() const;	

		DateTime	asUTC() const;				// Converts the time/date value to UTC.
		DateTime	asLocalTime() const;		// Converts the time/date value to the local time zone.

		bool    IsUTC() const { return m_nBias == 0; }  // Returns true if the object is an UTC representation

		string Format( const char *lpszFormatStr ) const;
		string asLongString() const;											// Returns string as "HH:MM:SS x.m. on Weekday, Month XX, YYYY", 12-hr clock
		string asPresentationString( bool bSeconds = true ) const;				// Returns string as "HH:MM:SS x.m. Weekday, Month XX, YYYY", 12-hr clock
		string asMediumString( bool bSeconds = true, bool b24Hr = true, bool bYear = true ) const;		// Returns string as "Wkd Mon XX[ YYYY] HH:MM[:SS][ XM]"
		string asShortString() const;											// Returns string as "Mon XX YY HH:MM:SS", 24-hr clock
		string asNumericString() const;											// Returns string as "DD.MM.YYYY HH:MM:SS", 24-hr clock
		string asMilString() const;												// Returns string as "DDMonYY HH:MM:SS", 24-hr clock
		string asDateString( bool bYear = true ) const;							// Returns string as "Weekday, Month XX[, YYYY]"
		string asTimeString( bool bSeconds = true, bool b24Hr = true ) const;	// Returns string as "HH:MM[:SS]" or "HH:MM[:SS] XM"
		string asDebugString() const;											// Returns string as "DD.MM.YYYY HH.MM.SS", 24-hr clock
		string asInternetString() const;										// Returns string as "Wkd, DD Mnt YYYY HH:MM:SS GMT", 24-hr clock (see RFC 822 & 1123)	
		string asISO8601(int Precision = 6) const;								// Returns string as "YYYY-MM-DDTHH:MM:SS.ssssss[+/-HH:MM or Z]" as in ISO 8601.

		string		ToString() const;							// Same as 'asISO8601()'.  Recommended format for storage/transmission.
		static bool	TryParse(const char*, DateTime&);			// Reads the 'asInternetString()' (always UTC) or 'asPresentationString()' (assumes local time) formats.
		static DateTime Parse(const char*);

		FILETIME ToFILETIME() const;
		// void	asSystemtime( SYSTEMTIME& ) const;

			// Returns the number of seconds since Midnight (00:00:00) on Year zero.
		Int64	GetSeconds() const { return m_nSeconds; }

			// Returns the number of nanoseconds after the "seconds" value.
		Int32	GetNanoseconds() const { return m_nNanoseconds; }

			// Returns the number of seconds difference between UTC time and the value.  UTC = Value - Bias.
			// This is specified with the commonly used time zone convention.  For example, Arizona would have
			// a m_nBias value of -7h (-25200 seconds).
		Int32	GetBias() const { return m_nBias; }

			// Returns the number of seconds since Midnight (00:00:00) on Year zero after converting to UTC time.
		Int64	GetUTCSeconds() const { return m_nSeconds - m_nBias; }

			// Returns the number of seconds since Midnight (00:00:00) on Year zero after converting to local time.		
		Int64	GetLocalSeconds() const { return (m_nSeconds - (Int64)m_nBias + (Int64)GetLocalBias()); }

			// Returns the bias, in seconds, applied when working in the local time zone.  UTC = Value + Bias.
		static int	GetLocalBias();

		static DateTime Minimum;					// Retrieves the lowest allowed DateTime value.
		static DateTime Maximum;					// Retrieves the highest allowed DateTime value.
		static DateTime Zero;						// Retrieves a zero DateTime value.  Often better than using "Minimum" to avoid rollovers.

		static DateTime GetMinimumValue();
		static DateTime GetMaximumValue();
	};

	#define DateTime_MinValue (DateTime::Minimum)
	#define DateTime_MaxValue (DateTime::Maximum)

	/** Non-class operations **/

	inline string to_string(const DateTime& Value) { return Value.ToString(); }
}

//	Late dependency
#include "TimeSpan.h"

namespace wb
{
	/////////
	//	Inline functions
	//	

	inline DateTime::DateTime(){ m_nSeconds = 0ull; m_nNanoseconds = 0; m_nBias = 0; }
	inline DateTime::DateTime(const DateTime& cp){ m_nSeconds = cp.m_nSeconds; m_nNanoseconds = cp.m_nNanoseconds; m_nBias = cp.m_nBias; }
	inline DateTime::DateTime(DateTime&& cp){ m_nSeconds = cp.m_nSeconds; m_nNanoseconds = cp.m_nNanoseconds; m_nBias = cp.m_nBias; }
	inline DateTime::DateTime(time_t nValue){ Set( nValue ); }
	inline DateTime::DateTime(int nYear, int nMonth, int nDay, int nHour, int nMinute, int nSecond, int nNanosecond /*= 0*/, int nBias /*= LocalTimeZone*/)
	{
		Set( nYear, nMonth, nDay, nHour, nMinute, nSecond, nNanosecond, nBias );
	}	
	inline DateTime::DateTime(FILETIME ft)
	{
		UInt64	iFiletime = ((UInt64)ft.dwLowDateTime) | ((UInt64)ft.dwHighDateTime << 32);
			// in units of 0.0000001 seconds (100 nanosecond units)
			// 50000000 in units of 0.0000001 seconds = 5.0 seconds, so /10000000 = 5 seconds
		static const UInt64 PerSecond = 10000000ull;
		m_nSeconds = (iFiletime / PerSecond) + g_nOffsetForFiletime;
		m_nNanoseconds = iFiletime % PerSecond;
		m_nBias	= 0;
	}
	inline FILETIME DateTime::ToFILETIME() const
	{
		static const UInt64 PerSecond = 10000000ull;
		DateTime UTC = asUTC();
		UInt64	iFiletime = UTC.m_nSeconds - g_nOffsetForFiletime;
		iFiletime *= PerSecond;
		iFiletime += UTC.m_nNanoseconds / 100ull;
		FILETIME ret;
		ret.dwLowDateTime = (UInt32)(iFiletime);
		ret.dwHighDateTime = (UInt32)(iFiletime >> 32ull);
		return ret;
	}
#	if 0
	inline DateTime::DateTime( const SYSTEMTIME& st, int nBias /*= LocalTimeZone*/ )
	{
		Set( st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, nBias );
	}
	inline void DateTime::asSystemtime( SYSTEMTIME& st ) const
	{
		int nYear, nMonth, nDay, nHour, nMinute, nSecond;
		Get( nYear, nMonth, nDay, nHour, nMinute, nSecond );
		st.wYear = nYear;
		st.wMonth = nMonth;
		st.wDay = nDay;
		st.wHour = nHour;
		st.wMinute = nMinute;
		st.wSecond = nSecond;
		st.wMilliseconds = 0;
	}
#	endif	

	inline /*static*/ bool DateTime::IsLeapYear(int nYear)
	{
			/** Determine if current year is a leap-year **/

			/** Test Cases
				----------
				year & 3			Year 0 & 3 = 0 (true)
				(A)					Year 99 & 3 = 3 (false)
									Year 100 & 3 = 0 (true)
									Year 2000 & 3 = 0 (true)

				year % 100			Year 0 % 100 = 0 (false)
				(B)					Year 99 % 100 = 99 (true)
									Year 100 % 100 = 0 (false)
									Year 2000 % 100 = 0 (false)

				!(year % 400)		Year 0 % 400 = 0 (true)
				(C)					Year 99 % 400 = 99 (false)
									Year 100 % 400 = 100 (false)
									Year 2000 % 400 = 0 (true)

				A && (B || C)		Year 0: A=true, B=false, C=true ==> true (Leap-Year)
									Year 99: A=false, B=true, C=false ==> false (Non-Leap-Year)
									Year 100: A=true, B=false, C=false ==> false (Non-Leap-Year)
									Year 2000: A=true, B=false, C=true ==> true (Leap-Year)
			**/

		return ((abs(nYear) & 3) == 0) && (((abs(nYear) % 100) != 0) || (((abs(nYear) % 400) == 0)));
	}

	inline bool	DateTime::IsLeapYear() const { return IsLeapYear( GetYear() ); }

	inline int DateTime::GetDaysFromRemainder( UInt32& nRemainder ) const {
		int nDays = nRemainder / time_constants::g_n32SecondsPerDay;
		nRemainder -= nDays * time_constants::g_n32SecondsPerDay;
		return nDays;
	}

	inline int DateTime::GetHoursFromRemainder( UInt32& nRemainder ) const {
		int nHours = nRemainder / time_constants::g_n32SecondsPerHour;
		nRemainder -= nHours * time_constants::g_n32SecondsPerHour;
		return nHours;
	}

	inline int DateTime::GetMinutesFromRemainder( UInt32& nRemainder ) const {
		int nMinutes = nRemainder / 60;
		nRemainder -= nMinutes * 60;
		return nMinutes;
	}

	inline int DateTime::GetYear() const { UInt32 nRemainder; return GetYearAndRemainder(nRemainder); }
	inline int DateTime::GetMonth() const { 
		UInt32 nRemainder; int nYear = GetYearAndRemainder(nRemainder); 
		return GetMonthFromRemainder( nRemainder, nYear, IsLeapYear(nYear) );
	}

	inline int DateTime::GetDay() const { 
		UInt32 nRemainder; int nYear = GetYearAndRemainder(nRemainder);
		GetMonthFromRemainder( nRemainder, nYear, IsLeapYear(nYear) );
		return GetDaysFromRemainder( nRemainder ) + 1;
	}

	inline int DateTime::GetHour() const { 
		UInt32 nRemainder; int nYear = GetYearAndRemainder(nRemainder);
		GetMonthFromRemainder( nRemainder, nYear, IsLeapYear(nYear) );
		GetDaysFromRemainder( nRemainder );
		return GetHoursFromRemainder( nRemainder );
	}

	inline int DateTime::GetMinute() const { 
		UInt32 nRemainder; int nYear = GetYearAndRemainder(nRemainder);
		GetMonthFromRemainder( nRemainder, nYear, IsLeapYear(nYear) );
		GetDaysFromRemainder( nRemainder );
		GetHoursFromRemainder( nRemainder );
		return GetMinutesFromRemainder( nRemainder );
	}

	inline int DateTime::GetSecond() const { 
		UInt32 nRemainder; int nYear = GetYearAndRemainder(nRemainder);
		GetMonthFromRemainder( nRemainder, nYear, IsLeapYear(nYear) );
		GetDaysFromRemainder( nRemainder );
		GetHoursFromRemainder( nRemainder );
		GetMinutesFromRemainder( nRemainder );
		return (int)nRemainder;
	}

	inline int DateTime::GetHourIn12HourFormat() const { 
		int Hour = GetHour(); 
		if (Hour == 0) return 12;
		if (Hour == 12) return 12;
		if (Hour >= 12) Hour -= 12;
		return Hour;
	}

	inline bool	DateTime::IsAM() const {
		int Hour = GetHour();
		return (Hour < 12);
	}

	inline bool	DateTime::IsPM() const {
		int Hour = GetHour();
		return (Hour >= 12);
	}

	inline void DateTime::Get(int& nYear, int& nMonth, int& nDay, int& nHour, int& nMinute, int& nSecond, int& nNanosecond) const {
		UInt32 nRemainder; 
		nYear = GetYearAndRemainder(nRemainder);
		nMonth = GetMonthFromRemainder( nRemainder, nYear, IsLeapYear(nYear) );
		nDay = GetDaysFromRemainder( nRemainder ) + 1;
		nHour = GetHoursFromRemainder( nRemainder );
		nMinute = GetMinutesFromRemainder( nRemainder );
		nNanosecond = m_nNanoseconds;
	}

	inline void DateTime::Get(int& nYear, int& nMonth, int& nDay, int& nHour, int& nMinute, int& nSecond) const {
		UInt32 nRemainder; 
		nYear = GetYearAndRemainder(nRemainder);
		nMonth = GetMonthFromRemainder( nRemainder, nYear, IsLeapYear(nYear) );
		nDay = GetDaysFromRemainder( nRemainder ) + 1;
		nHour = GetHoursFromRemainder( nRemainder );
		nMinute = GetMinutesFromRemainder( nRemainder );
		nSecond = (int)nRemainder;
	}

	inline void DateTime::GetDate(int& nYear, int& nMonth, int& nDay) const {
		UInt32 nRemainder; 
		nYear = GetYearAndRemainder(nRemainder);
		nMonth = GetMonthFromRemainder( nRemainder, nYear, IsLeapYear(nYear) );
		nDay = GetDaysFromRemainder( nRemainder ) + 1;
	}

	inline void	DateTime::GetTime(int& nHours, int& nMinutes, double& dSeconds) const {
		UInt32 nRemainder; 
		int nYear = GetYearAndRemainder(nRemainder);
		GetMonthFromRemainder( nRemainder, nYear, IsLeapYear(nYear) );
		GetDaysFromRemainder( nRemainder );
		nHours = GetHoursFromRemainder( nRemainder );
		nMinutes = GetMinutesFromRemainder( nRemainder );
		dSeconds = (int)nRemainder;
		dSeconds += (double)m_nNanoseconds * time_constants::g_dSecondsPerNanosecond;
	}

	inline void	DateTime::GetTime(int& nHours, int& nMinutes, int& nSeconds) const {
		UInt32 nRemainder; 
		int nYear = GetYearAndRemainder(nRemainder);
		GetMonthFromRemainder( nRemainder, nYear, IsLeapYear(nYear) );
		GetDaysFromRemainder( nRemainder );
		nHours = GetHoursFromRemainder( nRemainder );
		nMinutes = GetMinutesFromRemainder( nRemainder );
		nSeconds = (int)nRemainder;
	}

	inline void DateTime::GetDayOfWeek(DayOfWeek& dow) const
	{
		if( m_nSeconds <= time_constants::g_n32SecondsPerDay )
		{
			int nDays = (int)(1 - (m_nSeconds / time_constants::g_n32SecondsPerDay)) - 1;
			dow = (DayOfWeek)(6 - (nDays % 7));
		}
		else
		{
				/* 1. Calculate # of days since Midnight, January 1st, Year 0 */
			int		nDays	= (int)(m_nSeconds / time_constants::g_n32SecondsPerDay) - 1;
			dow = (DayOfWeek)(nDays % 7);
		}

		assert( ((int)dow) >= 0 && ((int)dow) <= 6 );
	}

	inline string DateTime::GetDayOfWeek() const
	{
			/* 1. Calculate # of days since Midnight, January 1st, Year 0 */
		int		nDays	= (int)(m_nSeconds / time_constants::g_n32SecondsPerDay) - 1;
			/* Jan 1, 0 was a Saturday it would seem.  (Since the Gregorian calender
				doesn't actually extend back that far without corrections, this is 
				only partially true.) */
		switch( nDays % 7 ){ 
		default:	assert(1);
		case Sunday: return string(S("Sunday"));
		case Monday: return string(S("Monday"));
		case Tuesday: return string(S("Tuesday"));
		case Wednesday: return string(S("Wednesday"));
		case Thursday: return string(S("Thursday"));
		case Friday: return string(S("Friday"));
		case Saturday: return string(S("Saturday"));
		}
	}

	inline string DateTime::GetMonthAsString() const {
		switch( GetMonth() )
		{
		case January:	return S("January");
		case February:	return S("February");
		case March:		return S("March");
		case April:		return S("April");
		case May:		return S("May");
		case June:		return S("June");
		case July:		return S("July");
		case August:	return S("August");
		case September:	return S("September");
		case October:	return S("October");
		case November:	return S("November");
		case December:	return S("December");
		default:		return S("Undefined");
		}
	}

	inline UInt32 DateTime::GetSecondsIntoYear() const {
		UInt32 nRet;
		/*int nYear =*/ GetYearAndRemainder( nRet );
		return nRet;
	}

	inline void	DateTime::AddTime(int nHours, int nMinutes, int nSeconds){
		m_nSeconds += (Int64)nSeconds	+ (60 /*seconds/minute*/ * ((Int64)nMinutes
						+ (60 /*minutes/hour*/ * (Int64)nHours) ) );
	}

	inline void	DateTime::AddDays(int nDays /*= 1*/){ m_nSeconds += (Int64)(time_constants::g_n32SecondsPerDay * nDays); }

	inline void DateTime::Add(int nYears, int nMonths, int nDays, int nHours, int nMinutes, int nSeconds){
		if (nYears) AddYears( nYears );
		if (nMonths) AddMonths( nMonths );
		AddDays( nDays );
		AddTime( nHours, nMinutes, nSeconds );
	}		
	
	inline /*static*/ DateTime	DateTime::GetCurrentDate(){
		int nYear; int nMonth, nDay;
		DateTime   dtNow = Now();
		dtNow.GetDate(nYear, nMonth, nDay);
		return DateTime(nYear, nMonth, nDay, 0, 0, 0, UTC);
	}
	// inline /*static*/ DateTime DateTime::Zero(){ return DateTime(0,1,1, 0,0,0, UTC); }

		/** Operations **/

	inline bool DateTime::operator==( const DateTime& dt ) const { return m_nNanoseconds == dt.m_nNanoseconds && GetUTCSeconds() == dt.GetUTCSeconds(); }
	inline bool DateTime::operator!=( const DateTime& dt ) const { return m_nNanoseconds != dt.m_nNanoseconds || GetUTCSeconds() != dt.GetUTCSeconds(); }
	inline bool DateTime::operator<=( const DateTime& dt ) const { Int64 ThisUTCSeconds = GetUTCSeconds(), ThatUTCSeconds = dt.GetUTCSeconds(); return (ThisUTCSeconds < ThatUTCSeconds || (ThisUTCSeconds == ThatUTCSeconds && m_nNanoseconds <= dt.m_nNanoseconds)); }
	inline bool DateTime::operator<( const DateTime& dt ) const { Int64 ThisUTCSeconds = GetUTCSeconds(), ThatUTCSeconds = dt.GetUTCSeconds(); return (ThisUTCSeconds < ThatUTCSeconds || (ThisUTCSeconds == ThatUTCSeconds && m_nNanoseconds < dt.m_nNanoseconds)); }
	inline bool DateTime::operator>=( const DateTime& dt ) const { Int64 ThisUTCSeconds = GetUTCSeconds(), ThatUTCSeconds = dt.GetUTCSeconds(); return (ThisUTCSeconds > ThatUTCSeconds || (ThisUTCSeconds == ThatUTCSeconds && m_nNanoseconds >= dt.m_nNanoseconds)); }
	inline bool DateTime::operator>( const DateTime& dt ) const { Int64 ThisUTCSeconds = GetUTCSeconds(), ThatUTCSeconds = dt.GetUTCSeconds(); return (ThisUTCSeconds > ThatUTCSeconds || (ThisUTCSeconds == ThatUTCSeconds && m_nNanoseconds > dt.m_nNanoseconds)); }

	inline const DateTime& DateTime::operator=( const DateTime& cp ){ 
		m_nSeconds = cp.m_nSeconds;
		m_nNanoseconds = cp.m_nNanoseconds;
		m_nBias	 = cp.m_nBias;
		return *this;
	}

	inline const DateTime& DateTime::operator+=( const TimeSpan& tmSpan ) { 
		Int32 NewNanoseconds = (Int32)m_nNanoseconds + tmSpan.m_nElapsedNanoseconds;
		if (NewNanoseconds < 0) { m_nSeconds --; NewNanoseconds += time_constants::g_n32NanosecondsPerSecond; } 
		else if (NewNanoseconds >= time_constants::g_n32NanosecondsPerSecond) { m_nSeconds ++; NewNanoseconds -= time_constants::g_n32NanosecondsPerSecond; }
		m_nNanoseconds = NewNanoseconds;
		m_nSeconds += tmSpan.m_nElapsedSeconds;
		return *this;
	}

	inline const DateTime& DateTime::operator-=( const TimeSpan& tmSpan ) {
		Int32 NewNanoseconds = (Int32)m_nNanoseconds - tmSpan.m_nElapsedNanoseconds;
		if (NewNanoseconds < 0) { m_nSeconds --; NewNanoseconds += time_constants::g_n32NanosecondsPerSecond; }
		else if (NewNanoseconds >= time_constants::g_n32NanosecondsPerSecond) { m_nSeconds ++; NewNanoseconds -= time_constants::g_n32NanosecondsPerSecond; }
		m_nNanoseconds = NewNanoseconds;
		m_nSeconds -= tmSpan.m_nElapsedSeconds;
		return *this;
	}

	inline DateTime DateTime::operator+( const TimeSpan& tmSpan ) const {
		DateTime ret(*this);
		ret += tmSpan;		
		return ret;
	}

	inline DateTime DateTime::operator-( const TimeSpan& tmSpan ) const {
		DateTime ret(*this);
		ret -= tmSpan;
		return ret;
	}

	inline TimeSpan DateTime::operator-( const DateTime& b ) const 
	{ 
		TimeSpan ret;		
		// 20 and 400 nanoseconds minus 15 and 300 nanoseconds:		delta = 5 seconds and 100 nanoseconds
		// 20 and 300 nanoseconds minus 15 and 400 nanoseconds:		delta = 4 seconds and 999,999,900 nanoseconds
		// 15 and 300 nanoseconds minus 20 and 400 nanoseconds:		delta = -5 seconds and -100 nanoseconds
		// 15 and 400 nanoseconds minus 20 and 300 nanoseconds:		delta = -4 seconds and -999,999,900 nanoseconds
		ret.m_nElapsedSeconds = GetUTCSeconds() - b.GetUTCSeconds();
		ret.m_nElapsedNanoseconds = m_nNanoseconds - b.m_nNanoseconds;
		if (ret.m_nElapsedSeconds >= 0)
		{
			if (ret.m_nElapsedNanoseconds < 0) { ret.m_nElapsedSeconds --; ret.m_nElapsedNanoseconds += time_constants::g_n32NanosecondsPerSecond; }
			return ret;
		}
		else
		{		
			if (ret.m_nElapsedNanoseconds > 0) { ret.m_nElapsedSeconds ++; ret.m_nElapsedNanoseconds -= time_constants::g_n32NanosecondsPerSecond; }
			return ret;
		}
	}		

		/** Conversions **/	

	inline DateTime::operator time_t() const
	{
		Int64 nValueAsUTC = GetUTCSeconds();
		if (nValueAsUTC < g_nOffsetForTimeT) return 0;
		return (time_t)(nValueAsUTC - g_nOffsetForTimeT);
	}

	inline void	DateTime::Set(time_t nInput){ 
		m_nSeconds		= ((UInt64)nInput) + g_nOffsetForTimeT;
		m_nNanoseconds	= 0;
		m_nBias			= 0;						// time_t values are specified in UTC time.		
	}

	inline void DateTime::Set(UInt64 nSeconds, UInt32 nNanoseconds, int nBias){
		m_nSeconds		= nSeconds;
		m_nNanoseconds	= nNanoseconds;
		m_nBias			= nBias;
	}

	inline DateTime	DateTime::asUTC() const {
		DateTime	ret;
		ret.m_nSeconds		= GetUTCSeconds();
		ret.m_nNanoseconds	= m_nNanoseconds;
		ret.m_nBias			= 0;
		return ret;
	}

	inline DateTime	DateTime::asLocalTime() const {
		DateTime	ret;
		ret.m_nSeconds		= GetLocalSeconds();
		ret.m_nNanoseconds	= m_nNanoseconds;
		ret.m_nBias			= GetLocalBias();
		return ret;
	}

	inline DateTime::operator tm() const
	{
		tm tmRet;
		memset(&tmRet, 0, sizeof(tmRet));
		UInt32 nRemainder; 
		int nYear = GetYearAndRemainder(nRemainder);
		if (nYear < 1900) throw Exception(S("tm structure cannot represent date range."));
		tmRet.tm_year	= nYear - 1900;
		tmRet.tm_mon    = GetMonthFromRemainder( nRemainder, nYear, IsLeapYear(nYear) ) - 1;
		tmRet.tm_mday	= GetDaysFromRemainder( nRemainder ) + 1;
		tmRet.tm_hour	= GetHoursFromRemainder( nRemainder );
		tmRet.tm_min	= GetMinutesFromRemainder( nRemainder );
		tmRet.tm_sec	= (int)nRemainder;
		DayOfWeek dow;
		GetDayOfWeek( dow );
		tmRet.tm_wday	= (int)dow;
		assert( tmRet.tm_wday >= 0 && tmRet.tm_wday <= 6 );
		return tmRet;
	}	

	inline string	DateTime::Format( const char* lpszFormatStr ) const
	{
		char str[256];
		tm tmValue	= operator tm();
		assert( tmValue.tm_wday >= 0 && tmValue.tm_wday <= 6 );
		strftime(str, 256, lpszFormatStr, &tmValue);
		return str;
	}

	inline string	DateTime::asPresentationString( bool bSeconds /*= true*/ ) const { return bSeconds ? Format( S("%I:%M:%S %p %A %B %d, %Y") ) : Format( S("%I:%M %p %A %B %d, %Y") ); }
	inline string	DateTime::asLongString() const { return Format( S("%I:%M:%S %p on %A, %B %d, %Y") ); }

	inline string	DateTime::asMediumString( bool bSeconds, bool b24Hr, bool bYear ) const { 
		if( bYear )
		{
			if( b24Hr )
			{
				if( bSeconds )	return Format( S("%a %b %d %Y %H:%M:%S") ); 
				else			return Format( S("%a %b %d %Y %H:%M") ); 
			}
			else
			{
				if( bSeconds )	return Format( S("%a %b %d %Y %I:%M:%S %p") ); 
				else			return Format( S("%a %b %d %Y %I:%M %p") ); 
			}
		}
		else
		{
			if( b24Hr )
			{
				if( bSeconds )	return Format( S("%a %b %d %H:%M:%S") ); 
				else			return Format( S("%a %b %d %H:%M") ); 
			}
			else
			{
				if( bSeconds )	return Format( S("%a %b %d %I:%M:%S %p") ); 
				else			return Format( S("%a %b %d %I:%M %p") ); 
			}
		}
	}

	inline string	DateTime::asShortString() const { return Format( S("%b %d %y %H:%M:%S") ); }    
	inline string	DateTime::asNumericString() const { return Format( S("%d.%m.%Y %H:%M:%S") ); }
	inline string	DateTime::asMilString() const { return Format( S("%d%b%y %H:%M:%S") ); }

	inline string	DateTime::asDateString( bool bYear /*= true*/ ) const { 
		if( bYear )
			return Format( S("%A %B %d, %Y") ); 
		else
			return Format( S("%A %B %d") ); 
	}

	inline string	DateTime::asTimeString( bool bSeconds /*= true*/, bool b24Hr /*= true*/ ) const { 
		if( bSeconds ){
			if( b24Hr ) return Format( S("%H:%M:%S") ); 
			else		return Format( S("%I:%M:%S %p") );
		}
		else {
			if( b24Hr ) return Format( S("%H:%M") ); 
			else		return Format( S("%I:%M %p") );
		}
	}

	inline string	DateTime::asDebugString() const { return Format( S("%d.%m.%Y %H.%M.%S") ); }
	inline string	DateTime::asInternetString() const { 
		return asUTC().Format(S("%a, %d %b %Y %H:%M:%S GMT")); 
	}
	inline string	DateTime::ToString() const { return asISO8601(9); }

	inline string	DateTime::asISO8601(int Precision /*= 6*/) const { 

		// Equivalent to:	 string strA = Format(S("%Y-%m-%dT%H:%M:"));
		// but supports dates beyond the 1900-67435 year range.
		int nYear, nMonth, nDay, nHour, nMinute, nSecond;
		Get(nYear, nMonth, nDay, nHour, nMinute, nSecond);
		char pszA[64];
		#if defined(_MSC_VER)
		sprintf_s(pszA, S("%d-%02d-%02dT%02d:%02d:"), nYear, nMonth, nDay, nHour, nMinute);
		#else
		sprintf(pszA, S("%d-%02d-%02dT%02d:%02d:"), nYear, nMonth, nDay, nHour, nMinute);
		#endif		
		string strA = pszA;

		char pszB[64];
		double dSecond = (double)nSecond + (double)m_nNanoseconds * time_constants::g_dSecondsPerNanosecond;
		#if defined(_MSC_VER)
		sprintf_s(pszB, S("%0*.*f"), (Precision + 3), Precision, dSecond);
		#else
		sprintf(pszB, S("%0*.*f"), (Precision + 3), Precision, dSecond);
		#endif

		if( abs(GetBias()) > 60 )
		{
			int nBias = GetBias();		// In seconds...
			int nBiasHours = nBias / 3600;
			nBias = nBias % 3600;		// Remaining seconds...
			UInt32 nBiasMinutes = abs( nBias / 60 );
			char pszC[64];
			#if defined(_MSC_VER)
			sprintf_s(pszC, S("%+03d:%02u"), nBiasHours, nBiasMinutes);
			#else
			sprintf(pszC, S("%+03d:%02u"), nBiasHours, (uint)nBiasMinutes);
			#endif
			return strA + pszB + pszC;
		}
		else return strA + pszB + S("Z");	
	}

			/** For example, for January returns 31. **/
	inline /*static*/ int DateTime::GetDaysInMonth( int nMonth, bool bLeapYear ){
		assert( nMonth >= 1 && nMonth <= 12 );
		return bLeapYear ? time_constants::g_tableDaysInMonthLY[nMonth] : time_constants::g_tableDaysInMonthNLY[nMonth];
	}
	inline /*static*/ int DateTime::GetDaysInMonth( int nMonth, int nYear ){ return GetDaysInMonth( nMonth, IsLeapYear(nYear) ); }

	/*static*/ inline DateTime DateTime::GetMinimumValue() { DateTime ret; ret.m_nSeconds = Int64_MinValue; ret.m_nNanoseconds = 0; ret.m_nBias = 0; return ret; }
	/*static*/ inline DateTime DateTime::GetMaximumValue() { DateTime ret; ret.m_nSeconds = Int64_MaxValue; ret.m_nNanoseconds = 0; ret.m_nBias = 0; return ret; }
}

#endif	// __DateTime_h__

//	End of DateTime.h


