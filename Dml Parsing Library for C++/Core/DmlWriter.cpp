/**	DmlWriter.cpp
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
**/

#ifdef _WIN32
#include <Windows.h>
#endif

#include "../Dml.h"

namespace wb
{
	namespace dml
	{
		/*static*/ UInt64 DmlWriter::PaddingNodeHeadSize = (UInt64)BinaryWriter::SizeCompact32(dmltsl::dml3::idDMLPadding);
	}
}

//	End of DmlWriter.cpp

