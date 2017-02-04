/*	Matrix.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
*/

#ifndef __WBMatrix_h__
#define __WBMatrix_h__

#include <string.h>
#include <stdlib.h>
#include "Collections/Vector.h"

namespace wb
{
	template < class T > class matrix : protected vector<T>
	{
	//public:
		typedef T					value_type;
		typedef value_type&			reference;
		typedef const value_type&	const_reference;
		typedef vector<T>			base;

		size_t		m_nColumns;
		// Rows can be computed as m_nLength / m_nColumns.
		
		#if 0
		static void copy(value_type* pDst, value_type* pSrc, size_t count)
		{
			for (size_t ii=0; ii < count; ii++) pDst[ii] = pSrc[ii];
		}

		static void fill(value_type* pDst, const value_type& val, size_t count)
		{
			for (size_t ii=0; ii < count; ii++) pDst[ii] = val;
		}

		// mm() - Matrix Multiply
		template<class T> friend static wb::matrix<T> mm(const wb::matrix<T>& a, const wb::matrix<T>& b);

		// em() - Elementwise Matrix Multiply
		template<class T> friend static wb::matrix<T> em(const wb::matrix<T>& a, const wb::matrix<T>& b);
		#endif

	public:

		explicit matrix ()
			: m_nColumns(0)
		{
		}

		explicit matrix (size_t rows, size_t columns)
			: base(rows*columns), m_nColumns(columns)
		{
		}
		
        matrix (size_t rows, size_t columns, const value_type& val)
			: base(rows*columns, val), m_nColumns(columns)
		{
		}

		matrix (const matrix& cp)
			: base(cp), m_nColumns(cp.columns())
		{
		}
		
		matrix (matrix&& mv) : base(mv), m_nColumns(mv.m_nColumns)
		{
		}
		
		matrix& operator= (const matrix& cp)
		{
			base::operator=(cp);
			m_nColumns = cp.m_nColumns;
		}

		matrix& operator= (matrix&& mv)
		{
			base::operator=(mv);
			m_nColumns = mv.m_nColumns;
		}

		size_t size() const { return base::size(); }
		size_t rows() const { return size() / m_nColumns; }
		size_t columns() const { return m_nColumns; }

		void resize(size_t nRows, size_t nColumns)
		{
			base::resize(nRows * nColumns);
			m_nColumns = nColumns;
		}

		reference at (size_t iRow, size_t iCol) 
		{
			#ifdef _DEBUG
			assert (((iRow * m_nColumns) + iCol) < size());
			#endif
			return base::operator[](((iRow * m_nColumns) + iCol)); 
		}

		const_reference at (size_t iRow, size_t iCol) const
		{
			#ifdef _DEBUG
			assert (((iRow * m_nColumns) + iCol) < size());
			#endif
			return base::operator[](((iRow * m_nColumns) + iCol)); 
		}

		reference operator() (size_t iRow, size_t iCol) 
		{
			#ifdef _DEBUG
			assert (((iRow * m_nColumns) + iCol) < size());
			#endif
			return base::operator[](((iRow * m_nColumns) + iCol)); 
		}

		const_reference operator() (size_t iRow, size_t iCol) const 
		{
			#ifdef _DEBUG
			assert (((iRow * m_nColumns) + iCol) < size());
			#endif
			return base::operator[](((iRow * m_nColumns) + iCol));
		}
	};	
}

#endif	// __WBMatrix_h__

//	End of Matrix.h
