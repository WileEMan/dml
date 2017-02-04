#include "Dml.h"

using namespace wb::dml;

void DmlWriterExample1()
{
	UInt16 PixelValues[9] = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

	DmlWriter Writer = DmlWriter::Create("FileExample.dml");
	Writer.AddPrimitiveSet("arrays", "le");
	Writer.WriteHeader();

	Writer.WriteStartContainer("Metadata");
	Writer.WriteEndAttributes();
	Writer.WriteStartContainer("Patch");
	Writer.Write("Center-X", (UInt64)52);
	Writer.Write("Center-Y", (UInt64)391);
	Writer.WriteEndAttributes();
	Writer.Write("PixelData", PixelValues, 3, 3);
	Writer.WriteEndContainer();
	Writer.WriteEndContainer();
	Writer.Close();
}
