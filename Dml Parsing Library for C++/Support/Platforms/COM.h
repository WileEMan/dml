/*	COM.h
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)

	Component Object Model access (Windows only)
*/

#ifndef __WBCOM_h__
#define __WBCOM_h__

/** Table of contents / References **/

namespace wb {
	namespace runtime {		
	};
};

/** Dependencies **/

#include "Platforms.h"

#ifdef _WINDOWS

#include "../Text/String.h"
#include "../Collections/Vector.h"
#include "../Exceptions.h"
#include <comutil.h>

/** Content **/

namespace wb
{		
	namespace runtime
	{		
		/** COM Service (Windows only) **/

		// Initialize ThreadServices once per thread.  This should probably be wrapped into an
		// overall "Thread" class.
		class ThreadServices
		{
		public:
			ThreadServices(DWORD dwCoInit = COINIT_MULTITHREADED);
			~ThreadServices() { CoUninitialize(); }
		};

		template<typename T> class com_ptr
		{
			typedef T*		pointer;
			typedef T&		reference;
			pointer			m_pObj;			

		public:
			com_ptr() { m_pObj = nullptr; }
			com_ptr (pointer p) { m_pObj = p; }
			com_ptr (const com_ptr<T>& x) { m_pObj = x.m_pObj; m_pObj->AddRef(); }
			com_ptr (com_ptr<T>&& x) { m_pObj = x.m_pObj; x.m_pObj = nullptr; }
			com_ptr& operator= (const com_ptr<T>&& x) { free(); m_pObj = x.m_pObj; m_pObj->AddRef(); return *this; }
			com_ptr& operator= (com_ptr<T>&& x) { free(); m_pObj = x.m_pObj; x.m_pObj = nullptr; return *this; }

			bool operator== (pointer p) const { return (m_pObj == p); }
			bool operator!= (pointer p) const { return (m_pObj != p); }
			bool operator== (const com_ptr<T>& p) const { return (m_pObj == p.m_pObj); }
			bool operator!= (const com_ptr<T>& p) const { return (m_pObj != p.m_pObj); }

			operator bool() const { return (m_pObj != nullptr); }

			/** Cleanup **/

			void free()
			{
				if (m_pObj != nullptr) { m_pObj->Release(); m_pObj = nullptr; }				
			}

			~com_ptr() { free(); }

			/** Accessors **/
			
			void** attachment() { return (void**)&m_pObj; }
			pointer* operator&() { return &m_pObj; }
			pointer get() const { return m_pObj; }
			bool IsAssigned() const { return (m_pObj != nullptr); }
			pointer operator->() const { return m_pObj; }
			reference operator*() const { return *m_pObj; }

			template<typename Q> com_ptr<Q> as() 
			{
				if (m_pObj == nullptr) throw ArgumentNullException();
				com_ptr<Q> ret;
				HRESULT hr = m_pObj->QueryInterface(__uuidof(Q), ret.attachment());
				if (FAILED(hr)) return com_ptr<Q>();
				return ret;
			}
		};

		/** Implementations **/		

		inline ThreadServices::ThreadServices(DWORD dwCoInit)
		{
			DWORD dwStatus = ::CoInitializeEx(NULL, dwCoInit);
			if (dwStatus == S_OK || dwStatus == S_FALSE) return;
			throw Exception("Unable to intiailize COM on thread.");

			HRESULT hr = ::CoInitializeSecurity(
				NULL, 
				-1,                          // COM authentication
				NULL,                        // Authentication services
				NULL,                        // Reserved
				RPC_C_AUTHN_LEVEL_DEFAULT,   // Default authentication 
				RPC_C_IMP_LEVEL_IMPERSONATE, // Default Impersonation  
				NULL,                        // Authentication info
				EOAC_NONE,                   // Additional capabilities 
				NULL                         // Reserved
				);
			if (!SUCCEEDED(hr)) Exception::ThrowFromHRESULT(hr);
		}		
	}			
}

#endif	// _WINDOWS
#endif	// __WBCOM_h__

//	End of COM.h


