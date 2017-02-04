/**	TimeSpan.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
**/

/*	A class for representing elapsed times.

	The time is stored in the seconds.  It represents the *actual*
	time elapsed.  Effects such as leap years are not represented in
	the time span, however, this class interacts with the DateTime
	class to incorporate such effects.
*/
/////////

#ifndef __TimeSpan_h__
#define __TimeSpan_h__

#include <math.h>
#include <stdlib.h>
#include <assert.h>
#include "../Text/String.h"
#include "TimeConstants.h"

namespace wb
{
	class TimeSpan
	{
	protected:

		/** If m_nElapsedSeconds is negative, then m_nElapsedNanoseconds will be stored negative as well.  The two would be "added" to
			accomplish the total time.  Use IsNegative() to check for negative spans, since m_nElapsedSeconds could be zero when
			m_nElapsedNanoseconds is non-zero negative. **/

		Int64	m_nElapsedSeconds;
		Int32	m_nElapsedNanoseconds;		

		friend class DateTime;

	public:

		/** TimeSpan() constructors
			Parameters do not have constraints.  I.e., minutes do not need to be 1-60.  This allows
			the creation of an object such as:
				TimeSpan( 0, 48, 0, 0 ) for 48 hours.
		**/

		TimeSpan();
		TimeSpan( const TimeSpan& );		
		TimeSpan( Int64 nDays, int nHours, int nMinutes, int nSeconds, int nNanoseconds = 0 );
		TimeSpan( Int64 nElapsedSeconds, Int32 nElapsedNanoseconds );
	#	ifdef _MFC
		TimeSpan( const CTimeSpan& );
	#	endif

		static const TimeSpan Invalid;
		static const TimeSpan Zero;

		// All Get...() calls (except GetTotal...() and GetApproxTotal...() calls) will return the absolute time.  For example, if 
		// the time span represents -2 hours, then GetDays() = 0, GetHours() = 2, so forth, and IsNegative() is true.

			// Get...() calls return the number of units, less any larger units, rounded downward.
			// For example:  119 seconds -> GetMinutes() = 1, GetSeconds() = 59.			
		bool	IsNegative() const;
		Int64	GetDays() const;
		Int32	GetHours() const;
		Int32	GetMinutes() const;
		Int32	GetSeconds() const;
		Int32	GetNanoseconds() const;

			// This call is slightly faster than the individual Get...() calls.
		void	Get( Int64& nDays, Int32& nHours, Int32& nMinutes, Int32& nSeconds ) const;
		void	Get( Int64& nDays, Int32& nHours, Int32& nMinutes, Int32& nSeconds, Int32& nNanoseconds ) const;

			// Note: The GetTotal...() functions round to the nearest whole number.  For example, 119 seconds -> 2 minutes.
		Int64	GetTotalDays() const;
		Int64	GetTotalHours() const;
		Int64	GetTotalMinutes() const;
		Int64	GetTotalSeconds() const;

			// These GetTotal...() functions will throw an exception if the returned value cannot fit in an Int64 value.  For
			// nanoseconds this occurs with intervals longer than about 290 years, and is longer for the remaining calls.
		Int64	GetTotalMilliseconds() const;
		Int64	GetTotalMicroseconds() const;

			// These GetTotal...() functions use no rounding.  GetTotalNanoseconds will throw an exception on intervals longer
			// than about 290 years.
		Int64	GetTotalSecondsNoRounding() const;
		Int64	GetTotalNanoseconds() const;

			// Since CDateTimeSpan objects are not affixed to a calender date, these functions provide approximations only.	
			// Get..Total..() calls are rounded.  Other Get..() calls always round down.
		Int64	GetApproxYears() const;
		Int32	GetApproxMonths() const;
		Int32	GetApproxDays() const;
		Int64	GetApproxTotalYears() const;
		Int64	GetApproxTotalMonths() const;	

			/** fromString()
				Attempts to parse a string into an elapsed time.  Returns Invalid if a valid format was not
				recognized and parsed successfully.  The following are examples of supported formats:

					HH:MM:SS
					MM:SS
					SS
					HH:MM:SS text
					MM:SS text
					SS text

				where:
					SS represents seconds, and may contain any number of decimal digits.
			**/
		static bool TryParse(const schar*, TimeSpan&);
		static TimeSpan Parse(const schar*);

			/** asExactString()
				Returns string as "XX days H:MM:SS.sss hours", always including seconds.
				Returns string as "H:MM:SS.sss hours", when less than a day elapsed.

				If Precision is zero then the ".sss" component is omitted.  Otherwise Precision controls the number
				of digits after the decimal place, up to a maximum of 9.
			**/

		string ToString(int Precision = 9) const;
		string ToShortString() const;		// Returns string as "YY months", showing only highest-level unit.

			/** Operations **/

		bool operator==( const TimeSpan& ) const;
		bool operator!=( const TimeSpan& ) const;
		bool operator<=( const TimeSpan& ) const;
		bool operator<( const TimeSpan& ) const;
		bool operator>=( const TimeSpan& ) const;
		bool operator>( const TimeSpan& ) const;

		TimeSpan operator+( const TimeSpan& ) const;
		TimeSpan operator-( const TimeSpan& ) const;

		const TimeSpan& operator=( const TimeSpan& );
	};

	/** Non-class operations **/

	inline string to_string(const TimeSpan& Value) { return Value.ToString(); }

	/////////
	//  Inline functions
	//

	inline TimeSpan::TimeSpan() { m_nElapsedSeconds = 0; m_nElapsedNanoseconds = 0; }
	inline TimeSpan::TimeSpan( const TimeSpan& cp ) : m_nElapsedSeconds(cp.m_nElapsedSeconds), m_nElapsedNanoseconds(cp.m_nElapsedNanoseconds) { }
	inline TimeSpan::TimeSpan(Int64 nElapsedSeconds, Int32 nElapsedNanoseconds) {
		m_nElapsedSeconds = nElapsedSeconds;
		m_nElapsedNanoseconds = nElapsedNanoseconds; 
		assert ((m_nElapsedNanoseconds > -1000000000 && m_nElapsedNanoseconds < 1000000000) 
			|| (m_nElapsedSeconds == Int64_MaxValue && m_nElapsedNanoseconds == Int32_MaxValue));
	}
	inline TimeSpan::TimeSpan(Int64 nDays, int nHours, int nMinutes, int nSeconds, int nNanoseconds /*= 0*/) {
		m_nElapsedSeconds =  (nDays * time_constants::g_nSecondsPerDay) + ((Int64)nHours * time_constants::g_nSecondsPerHour) 
						  + ((Int64)nMinutes * time_constants::g_nSecondsPerMinute) + (Int64)nSeconds;
		m_nElapsedNanoseconds = abs(nNanoseconds);
		if (m_nElapsedSeconds < 0) m_nElapsedNanoseconds = -m_nElapsedNanoseconds;
		assert (m_nElapsedNanoseconds > -1000000000 && m_nElapsedNanoseconds < 1000000000);
	}
	#ifdef _MFC
	inline TimeSpan::TimeSpan( const CTimeSpan& tmSpan ){ m_nElapsedSeconds = tmSpan.GetTotalSeconds(); m_nElapsedNanoseconds = 0; }
	#endif	

	/** If less than one second is stored, then m_nElapsedSeconds will be zero but m_nElapsedNanoseconds can still be negative.  Therefore,
		we must check both. **/
	inline bool		TimeSpan::IsNegative() const { return (m_nElapsedSeconds < 0 || m_nElapsedNanoseconds < 0); }

	inline Int64	TimeSpan::GetDays() const {
			// GetDays() is the same as GetTotalDays() except that this version rounds downward.
		return abs(m_nElapsedSeconds) /*seconds*/ / time_constants::g_nSecondsPerDay /*seconds/day*/;
	}

	inline Int32	TimeSpan::GetHours() const { return (Int32)(abs(m_nElapsedSeconds) % time_constants::g_nSecondsPerDay) / time_constants::g_n32SecondsPerHour; }
	inline Int32	TimeSpan::GetMinutes() const { return (Int32)(abs(m_nElapsedSeconds) % time_constants::g_nSecondsPerHour) / time_constants::g_n32SecondsPerMinute; }
	inline Int32	TimeSpan::GetSeconds() const { return (Int32)(abs(m_nElapsedSeconds) % time_constants::g_nSecondsPerMinute); }	
	inline Int32	TimeSpan::GetNanoseconds() const { return abs(m_nElapsedNanoseconds); }	
	
	// Note: We disregard the nanoseconds in the following calculations.  The only effect is to nudge the rounding by 0.5 seconds, which only matters if
	// the total days, hours, or minutes value was directly on a half-unit.
	inline Int64	TimeSpan::GetTotalDays() const { return Round64(m_nElapsedSeconds /*seconds*/ / time_constants::g_dSecondsPerDay /*seconds/day*/ ); }
	inline Int64	TimeSpan::GetTotalHours() const { return Round64(m_nElapsedSeconds /*seconds*/ / time_constants::g_dSecondsPerHour /*seconds/hour*/ ); }
	inline Int64	TimeSpan::GetTotalMinutes() const { return Round64(m_nElapsedSeconds /*seconds*/ / time_constants::g_dSecondsPerMinute /*seconds/minute*/ ); }

	// Now we consider nanoseconds...
	inline Int64	TimeSpan::GetTotalSeconds() const { 
		if (m_nElapsedSeconds >= 0)
			return (m_nElapsedNanoseconds > (time_constants::g_n32NanosecondsPerSecond/2)) ? (m_nElapsedSeconds + 1) : (m_nElapsedSeconds);
		else
			return (m_nElapsedNanoseconds > -(time_constants::g_n32NanosecondsPerSecond/2)) ? (m_nElapsedSeconds) : (m_nElapsedSeconds - 1);
	}

	inline Int64	TimeSpan::GetTotalSecondsNoRounding() const { return m_nElapsedSeconds; }

	inline Int64	TimeSpan::GetTotalMilliseconds() const { 
		static const Int64 MSPerS = 1000ll;
		static const Int64 MaxS = (Int64_MaxValue / MSPerS);
		if (abs(m_nElapsedSeconds) + 1 > MaxS)
			throw ArgumentOutOfRangeException("Cannot retrieve total milliseconds on time spans longer than " + to_string(MaxS) + " seconds.");
		if (m_nElapsedSeconds >= 0)
			return (m_nElapsedSeconds * MSPerS) + Round64(m_nElapsedNanoseconds / 1000000.0);
		else 
			return (m_nElapsedSeconds * MSPerS) - Round64(m_nElapsedNanoseconds / 1000000.0);
	}

	inline Int64	TimeSpan::GetTotalMicroseconds() const { 
		static const Int64 USPerS = 1000000ll;
		static const Int64 MaxS = (Int64_MaxValue / USPerS);
		if (abs(m_nElapsedSeconds) + 1 > MaxS)
			throw ArgumentOutOfRangeException("Cannot retrieve total microseconds on time spans longer than " + to_string(MaxS) + " seconds.");
		if (m_nElapsedSeconds >= 0)
			return (m_nElapsedSeconds * USPerS) + Round64(m_nElapsedNanoseconds / 1000.0);
		else 
			return (m_nElapsedSeconds * USPerS) - Round64(m_nElapsedNanoseconds / 1000.0);
	}

	inline Int64	TimeSpan::GetTotalNanoseconds() const { 
		static const Int64 NSPerS = 1000000000ll;
		static const Int64 MaxS = (Int64_MaxValue / NSPerS);
		if (abs(m_nElapsedSeconds) + 1 > MaxS)
			throw ArgumentOutOfRangeException("Cannot retrieve total nanoseconds on time spans longer than " + to_string(MaxS) + " seconds.");
		if (m_nElapsedSeconds >= 0)
			return (m_nElapsedSeconds * NSPerS) + (Int64)m_nElapsedNanoseconds;
		else 
			return (m_nElapsedSeconds * NSPerS) - (Int64)m_nElapsedNanoseconds;
	}

			// Since TimeSpan objects are not affixed to a calender date, these functions provide approximations only.

	inline Int64	TimeSpan::GetApproxTotalYears() const { return Round64(m_nElapsedSeconds /*seconds*/ / time_constants::g_dApproxSecondsPerYear /*seconds/year*/ ); }
	inline Int64	TimeSpan::GetApproxTotalMonths() const { return Round64(m_nElapsedSeconds /*seconds*/ / time_constants::g_dApproxSecondsPerMonth /*seconds/month*/ ); }

	inline Int64	TimeSpan::GetApproxYears() const { return abs(m_nElapsedSeconds) /*seconds*/ / time_constants::g_nApproxSecondsPerYear /*seconds/year*/; }
	inline Int32	TimeSpan::GetApproxMonths() const { return (Int32)(abs(m_nElapsedSeconds) % time_constants::g_nApproxSecondsPerYear) / time_constants::g_n32ApproxSecondsPerMonth; }
	inline Int32	TimeSpan::GetApproxDays() const { return (Int32)(abs(m_nElapsedSeconds) % time_constants::g_nApproxSecondsPerMonth) / time_constants::g_n32SecondsPerDay; }

	inline void		TimeSpan::Get(Int64& nDays, Int32& nHours, Int32& nMinutes, Int32& nSeconds) const 
	{
			// This function uses the fast that multiplication is slightly faster than division as an optimization technique.
			// It also is slightly faster because less 64-bit arithmetic is required having retained the remainders.
		nDays = abs(m_nElapsedSeconds) /*seconds*/ / time_constants::g_nSecondsPerDay /*seconds/day*/;
		// assert(abs( (Int64)(m_nElapsedSeconds - (nDays * time_constants::g_nSecondsPerDay)) ) < Int32_MaxValue);
		Int32 nRemainder = (Int32)(abs(m_nElapsedSeconds) - (nDays * time_constants::g_nSecondsPerDay));

		assert( nRemainder < time_constants::g_nSecondsPerDay );
		nHours = nRemainder / time_constants::g_n32SecondsPerHour;
		nRemainder -= (nHours * time_constants::g_n32SecondsPerHour);

		assert( nRemainder < time_constants::g_n32SecondsPerHour );
		nMinutes = nRemainder / time_constants::g_n32SecondsPerMinute;
		nRemainder -= (nMinutes * time_constants::g_n32SecondsPerMinute);

		assert( nRemainder < time_constants::g_n32SecondsPerMinute );
		nSeconds = nRemainder;

	}// End of Get()

	inline void		TimeSpan::Get(Int64& nDays, Int32& nHours, Int32& nMinutes, Int32& nSeconds, Int32& nNanoseconds) const 
	{
		Get(nDays, nHours, nMinutes, nSeconds);
		nNanoseconds = abs(m_nElapsedNanoseconds);
	}			

	inline bool TimeSpan::operator==( const TimeSpan& span ) const { return m_nElapsedSeconds == span.m_nElapsedSeconds && m_nElapsedNanoseconds == span.m_nElapsedNanoseconds; }
	inline bool TimeSpan::operator!=( const TimeSpan& span ) const { return m_nElapsedSeconds != span.m_nElapsedSeconds || m_nElapsedNanoseconds != span.m_nElapsedNanoseconds; }
	inline bool TimeSpan::operator<=( const TimeSpan& span ) const { return m_nElapsedSeconds < span.m_nElapsedSeconds || (m_nElapsedSeconds == span.m_nElapsedSeconds && m_nElapsedNanoseconds <= span.m_nElapsedNanoseconds); }
	inline bool TimeSpan::operator<( const TimeSpan& span ) const { return m_nElapsedSeconds < span.m_nElapsedSeconds || (m_nElapsedSeconds == span.m_nElapsedSeconds && m_nElapsedNanoseconds < span.m_nElapsedNanoseconds); }
	inline bool TimeSpan::operator>=( const TimeSpan& span ) const { return m_nElapsedSeconds > span.m_nElapsedSeconds || (m_nElapsedSeconds == span.m_nElapsedSeconds && m_nElapsedNanoseconds >= span.m_nElapsedNanoseconds); }
	inline bool TimeSpan::operator>( const TimeSpan& span ) const { return m_nElapsedSeconds > span.m_nElapsedSeconds || (m_nElapsedSeconds == span.m_nElapsedSeconds && m_nElapsedNanoseconds > span.m_nElapsedNanoseconds); }	

	inline TimeSpan TimeSpan::operator+( const TimeSpan& span ) const 
	{ 
		Int32 NewNanoseconds = m_nElapsedNanoseconds + span.m_nElapsedNanoseconds;
		if (NewNanoseconds >= time_constants::g_n32NanosecondsPerSecond) return TimeSpan(m_nElapsedSeconds + span.m_nElapsedSeconds + 1, NewNanoseconds - time_constants::g_n32NanosecondsPerSecond);
		else if (NewNanoseconds <= -time_constants::g_n32NanosecondsPerSecond) return TimeSpan(m_nElapsedSeconds + span.m_nElapsedSeconds - 1, NewNanoseconds + time_constants::g_n32NanosecondsPerSecond);
		else return TimeSpan(m_nElapsedSeconds + span.m_nElapsedSeconds, NewNanoseconds); 
	}
	inline TimeSpan TimeSpan::operator-( const TimeSpan& span ) const 
	{ 
		Int32 NewNanoseconds = m_nElapsedNanoseconds - span.m_nElapsedNanoseconds;
		if (NewNanoseconds >= time_constants::g_n32NanosecondsPerSecond) return TimeSpan(m_nElapsedSeconds - span.m_nElapsedSeconds + 1, NewNanoseconds - time_constants::g_n32NanosecondsPerSecond);
		else if (NewNanoseconds <= -time_constants::g_n32NanosecondsPerSecond) return TimeSpan(m_nElapsedSeconds - span.m_nElapsedSeconds - 1, NewNanoseconds + time_constants::g_n32NanosecondsPerSecond);
		else return TimeSpan(m_nElapsedSeconds - span.m_nElapsedSeconds, NewNanoseconds); 
	}

	inline const TimeSpan& TimeSpan::operator=( const TimeSpan& cp ){ m_nElapsedSeconds = cp.m_nElapsedSeconds; m_nElapsedNanoseconds = cp.m_nElapsedNanoseconds; return *this; }
}

#endif	// __TimeSpan_h__

//	End of TimeSpan.h


