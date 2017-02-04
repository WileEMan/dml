/*	Vector.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
*/

#ifndef __WBVector_h__
#define __WBVector_h__

#ifdef UseSTL
#include <vector>
namespace wb
{
	//using namespace std;

	template < class T, class Alloc = std::allocator<T> > class vector : public std::vector<T,Alloc>	
	{ 
		typedef std::vector<T,Alloc> base;
		typedef Alloc allocator_type;
		typedef T value_type;
		typedef typename std::vector<T,Alloc>::size_type size_type;

	public:
		explicit vector (const allocator_type& alloc = allocator_type()) : base(alloc) { }
		explicit vector (size_type n, const value_type& val = value_type(), 
			const allocator_type& alloc = allocator_type()) : base(n,val,alloc) { }
		template <class InputIterator> vector (InputIterator first, InputIterator last, 
			const allocator_type& alloc = allocator_type()) : base(first, last, alloc) { }
		vector (const vector& x) : base(x) { }
	};
}
#else

#include <stdio.h>
#include <string.h>
#include <ctype.h>
#include <assert.h>

namespace wb
{
	template < class T > class vector
	{
	public:
		typedef T					value_type;
		typedef value_type&			reference;
		typedef const value_type&	const_reference;

	protected:
		value_type*		m_pContent;
		size_t			m_nLength;
		size_t			m_nCapacity;

		void Allocate(size_t nNewCapacity)
		{			
			if (m_pContent != nullptr) delete[] m_pContent;			
			m_pContent = new T[nNewCapacity];
			m_nLength = 0;
			m_nCapacity = nNewCapacity;
		}

		static void DoCopy(value_type* pDst, value_type* pSrc, size_t count)
		{
			for (size_t ii=0; ii < count; ii++) pDst[ii] = pSrc[ii];
		}

		static void DoMove(value_type* pDst, value_type* pSrc, size_t count)
		{
			for (size_t ii=0; ii < count; ii++) pDst[ii] = std::move(pSrc[ii]);
		}

		static void DoFill(value_type* pDst, const value_type& val, size_t count)
		{
			for (size_t ii=0; ii < count; ii++) pDst[ii] = val;
		}

	public:

		explicit vector ()
			: m_pContent(nullptr), m_nLength(0), m_nCapacity(0)
		{
		}

		explicit vector (size_t n)
			: m_pContent(nullptr), m_nLength(0), m_nCapacity(0)
		{
			Allocate(n);			
			m_nLength = n;
		}
		
        vector (size_t n, const value_type& val)
			: m_pContent(nullptr), m_nLength(0), m_nCapacity(0)
		{
			Allocate(n);
			DoFill(m_pContent, val, n);			
			m_nLength = n;
		}

		vector (const vector& cp)
			: m_pContent(nullptr), m_nLength(0), m_nCapacity(0)
		{
			Allocate(cp.m_nLength);
			DoCopy(m_pContent, cp.m_pContent, cp.m_nLength);
			m_nLength = cp.m_nLength;
		}
		
		vector (vector&& mv)			
		{
			m_pContent = mv.m_pContent; mv.m_pContent = nullptr;
			m_nLength = mv.m_nLength; mv.m_nLength = 0;
			m_nCapacity = mv.m_nCapacity; mv.m_nCapacity = 0;
		}

		~vector()
		{			
			if (m_pContent != nullptr) delete[] m_pContent;
			m_pContent = nullptr;
			m_nLength = m_nCapacity = 0;
		}
		
		vector& operator= (const vector& cp)
		{
			if (m_nCapacity < cp.m_nLength) Allocate(cp.m_nLength);
			DoCopy(m_pContent, cp.m_pContent, cp.m_nLength);			
			m_nLength = cp.m_nLength;
		}

		vector& operator= (vector&& mv)
		{			
			if (m_pContent != nullptr) delete[] m_pContent;
			m_pContent = mv.m_pContent; mv.m_pContent = nullptr;
			m_nLength = mv.m_nLength; mv.m_nLength = 0;
			m_nCapacity = mv.m_nCapacity; mv.m_nCapacity = 0;
		}

		size_t size() const { return m_nLength; }

		reference operator[] (size_t n) 
		{ 
			#ifdef _DEBUG
			assert (n < m_nLength);
			#endif
			return m_pContent[n]; 
		}

		const_reference operator[] (size_t n) const 
		{
			#ifdef _DEBUG
			assert (n < m_nLength);
			#endif
			return m_pContent[n];
		}

		void push_back (const value_type& val)
		{
			if (m_nLength + 1 > m_nCapacity) reserve(m_nLength + 1);
			m_pContent[m_nLength] = val;
			m_nLength++;
		}

		void push_back (value_type&& val)
		{
			if (m_nLength + 1 > m_nCapacity) reserve(m_nLength + 1);
			m_pContent[m_nLength] = val;
			m_nLength++;
		}

		typedef size_t iterator;
		iterator begin () { return iterator(0); }

		iterator insert (iterator position, const value_type& val)
		{
			resize(m_nLength + 1);
			assert (position >= 0);
			assert (position < m_nLength);
			iterator iRelocate = position;
			while (iRelocate < m_nLength - 1)
				m_pContent[iRelocate+1] = std::move(m_pContent[iRelocate]);
			m_pContent[position] = val;
			return position;
		}

		void erase (iterator position)
		{
			assert (position < m_nLength);
			// [0] [1] [2] [3] length = 4
			// erase [2]
			// [0] [1] [3->2] length = 3
			while (position < m_nLength - 1)
				m_pContent[position] = std::move(m_pContent[position + 1]);
			m_nLength--;
			// We are not deallocating any elements, so there is no need to call the destructor.
		}

		void reserve(size_t nNewCapacity)
		{
			if (nNewCapacity <= m_nCapacity) return;

			if (sizeof(value_type) > 32 && nNewCapacity < 8) nNewCapacity = 8;
			else if (nNewCapacity < 64) nNewCapacity = 64;
			else if (nNewCapacity < 256) nNewCapacity = 256;
			else if (nNewCapacity < 2048) nNewCapacity = 2048;
			else if (nNewCapacity < 65536) nNewCapacity = 65536;
			else if (nNewCapacity < 1048576) nNewCapacity = ((nNewCapacity - 1) & ~0xFFFF) + 0x10000;		// Round up to nearest 64K block size.
			else nNewCapacity = ((nNewCapacity - 1) & ~0xFFFFF) + 0x100000;									// Round up to nearest 1M block size.
			m_nCapacity = nNewCapacity;

			value_type* pNewContent = new value_type [m_nCapacity];
			DoMove(pNewContent, m_pContent, m_nLength);
			// Since we did a move instead of a copy, I don't think we are obligated to call the destructors,
			// however delete[] will do this automatically.  We could possibly use the non-array delete as
			// an optimization here, but this is the safest approach.			
			if (m_pContent != nullptr) delete[] m_pContent;
			m_pContent = pNewContent;
		}

		void resize(size_t nNewSize)
		{
			if (nNewSize > m_nCapacity) reserve(nNewSize);
			m_nLength = nNewSize;
		}

		void clear()
		{
			m_nLength = 0;
		}
	};
}

#endif

#endif	// __WBVector_h__

//	End of Vector.h
