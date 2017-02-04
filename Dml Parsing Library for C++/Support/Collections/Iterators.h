/*	Iterators.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
*/

#ifndef __WBIterators_h__
#define __WBIterators_h__

#include "../Platforms/Platforms.h"
#include "../Exceptions.h"
#include "../Text/String.h"
#include "../Collections/Pair.h"

#ifdef UseSTL
#include <iterator>

namespace wb
{
	struct input_iterator_tag : public std::input_iterator_tag {};
	struct output_iterator_tag : public std::output_iterator_tag {};
	struct forward_iterator_tag : public std::forward_iterator_tag {};
	struct bidirectional_iterator_tag : public std::bidirectional_iterator_tag {};
	struct random_access_iterator_tag : public std::random_access_iterator_tag {};

	template <class Category, class T, class Distance = std::ptrdiff_t, class Pointer = T*, class Reference = T&>
	struct iterator : std::iterator<Category,T,Distance,Pointer,Reference> { };
};

#else

namespace wb
{
	#if defined(_X86)
	typedef Int32 ptrdiff_t;
	#elif defined(_X64)
	typedef Int64 ptrdiff_t;
	#endif

	struct input_iterator_tag {};
	struct output_iterator_tag {};
	struct forward_iterator_tag {};
	struct bidirectional_iterator_tag {};
	struct random_access_iterator_tag {};

	template <class Category, class T, class Distance = ptrdiff_t, class Pointer = T*, class Reference = T&>
	struct iterator {
		typedef T         value_type;
		typedef Distance  difference_type;
		typedef Pointer   pointer;
		typedef Reference reference;
		typedef Category  iterator_category;
	};
}

#	endif

#endif	// __WBIterators_h__

//	End of Iterators.h

