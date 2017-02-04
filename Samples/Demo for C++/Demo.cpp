/////////
//	Demo.cpp
//	A demonstration and diagnostic for the DML Parsing Library for C++.
//	Author(s):	Wiley Black
////

/////////
//	Dependencies
//

#include <stdio.h>
#include <conio.h>

#define PrimaryModule												// This line must be present in just one compilation unit, before Dml.h.
#include "../../Dml Parsing Library for C++/Dml.h"					// This line can be present in all compilation units.

#include "BreakoutStream.h"

using namespace wb;
using namespace wb::dml;

void print_tabs(int count) { while(count--) printf("  "); }

/////////
//	Configuration
//

//#define Run_Diagnostics		1

/////////
//	DML Translation Demo
//

const char* pszSampleTranslation =
	"<DML:Translation DML:Version=\"2\" DML:URN=\"urn:dml:demo1\">\n"
	"    <DML:Include-Primitives DML:Set=\"common\" DML:Codec=\"le\" />\n"
	"    <Container id=\"1\" name=\"Solar-System\">\n"
	"      <Node id=\"1\" name=\"name\" type=\"string\" />\n"
	"      <Container id=\"2\" name=\"Star\" />\n"
	"      <Container id=\"3\" name=\"Planet\">\n"
	"        <Node id=\"3\" name=\"radius-in-km\" type=\"double\" />\n"
	"        <Container id=\"4\" name=\"Moon\" />\n"
	"      </Container>\n"
	"    </Container>\n"
	"</DML:Translation>";

r_ptr<wb::io::Stream> Get_Sample_Translation()
{
	/** Create a simple little artificial translation in XML **/

	wb::io::MemoryStream* pTranslationStream = new wb::io::MemoryStream();
	pTranslationStream->Write(pszSampleTranslation, strlen(pszSampleTranslation));
	pTranslationStream->Seek(0, wb::io::SeekOrigin::Begin);
	return r_ptr<wb::io::Stream>::responsible(pTranslationStream);
}

// The sample translation above can be written to an XML file and used by the Dml Editor to generate the Demo1Translation.h
// and Demo1Translation.cpp source files.  Code-based versions of the translation are not required, but they can make DML
// code easier and more readable.

#include "Demo1Translation.h"

/////////
//	DML Writing Demo
//

const char* pszDML_Filename = "Sample.dml";

void DML_Writer_Demo()
{		
	DmlWriter Writer = DmlWriter::Create(pszDML_Filename);		
	for (int ii = 0; ii < Demo1Translation::NumOfRequiredPrimitiveSets; ii++) Writer.AddPrimitiveSet(Demo1Translation::RequiredPrimitiveSets[ii]);
	Writer.WriteHeader(Demo1Translation::urn, Demo1Translation::urn, "Solar-System");
	Demo1Translation Demo1;
	
	Writer.WriteComment("A comment between header and content.");	// Write a DML comment.  Typically ignored by the parser.

	Writer.WriteStartContainer(Demo1.Solar_System);					// Write <Solar-System> in DML		
	Writer.Write(Demo1.Solar_System.name, "Home");					// Add Name="Home" attribute
	Writer.WriteEndAttributes();									// <Solar-System> has no attributes, but it does have elements.

	Writer.WriteStartContainer(Demo1.Solar_System.Star);			// Write <Star>
	Writer.Write(Demo1.Solar_System.name, "Sol");					// Add Name="Sol" attribute
	Writer.WriteEndContainer();										// Write </Star>

	Writer.WriteStartContainer(Demo1.Solar_System.Planet);			// Write <Planet>
	Writer.Write(Demo1.Solar_System.name, "Mercury");				// Add Name="Mercury" attribute
	Writer.Write(Demo1.Solar_System.Planet.radius_in_km, 2439.7);	// Add radius-in-km=2439.7 attribute, a double primitive	
	Writer.WriteEndContainer();

	Writer.WriteStartContainer(Demo1.Solar_System.Planet);			// Write <Planet>
	Writer.Write(Demo1.Solar_System.name, "Venus");					// Add Name="Venus" attribute
	Writer.Write(Demo1.Solar_System.Planet.radius_in_km, 6051.8);	// Add radius-in-km=6051.8 attribute, a double primitive	
	Writer.WriteEndContainer();

	Writer.WriteStartContainer(Demo1.Solar_System.Planet);			// Write <Planet>
	Writer.Write(Demo1.Solar_System.name, "Earth");					// Add Name="Earth" attribute
	Writer.Write(Demo1.Solar_System.Planet.radius_in_km, 6371.0);	// Add radius-in-km=6371.0 attribute, a double primitive	
	Writer.WriteEndAttributes();
	Writer.WriteStartContainer(Demo1.Solar_System.Planet.Moon);		// Write <Moon>
	Writer.WriteEndContainer();										// Write </Moon>
	Writer.WriteEndContainer();										// Write </Planet>

	Writer.WriteComment("We'll omit the rest of the solar system for brevity.");

	Writer.WriteEndContainer();										// Write </Solar-System>	

	printf("DML file written: %s\n", pszDML_Filename);
}

/////////
//	DML Reading Demo
//

r_ptr<Stream> ResourceResolve(string Uri, bool& IsXml)
{
	if (IsEqualNoCase(Uri, Demo1Translation::urn))
	{
		// Return a stream containing the XML form of the translation document given above.
		IsXml = true; 
		return Get_Sample_Translation();
	}
	throw FileNotFoundException("Unable to retrieve requested URI.");
}

int BreakoutColumns = 30, BreakoutDivider = 4;
void PrintBreakout(BreakoutStream& Stream)
{
	string Text = Stream.GetBreakout();
	Stream.ClearBreakout();
	for (;;)
	{
		// TODO: Should make sure we're splitting on whitespace...

		if (Text.length() > (size_t)BreakoutColumns)
		{
			printf("%s", Text.substr(0, BreakoutColumns).c_str());
			Text = Text.substr(BreakoutColumns);
			for (int ii=0; ii < BreakoutDivider; ii++) printf(" ");
			printf("\n");
		}
		else if (Text.length() == BreakoutColumns)
		{
			printf("%s", Text.c_str());
			for (int ii=0; ii < BreakoutDivider; ii++) printf(" ");
			return;
		}
		else
		{
			printf("%s", Text.c_str());
			for (int ii=Text.length(); ii < BreakoutColumns + BreakoutDivider; ii++) printf(" ");
			return;
		}
	}
}

void DML_Container_Reader_Demo(BreakoutStream& Stream, DmlReader& Reader, int Depth = 0)
{	
	bool NeedClosingTag = false;
	while (Reader.Read())
	{
		switch (Reader.GetNodeType())
		{
		case NodeTypes::Container: 
			PrintBreakout(Stream);
			print_tabs(Depth);
			printf("<%s \n", Reader.GetName().c_str());
			DML_Container_Reader_Demo(Stream, Reader, Depth + 1);			
			if (Depth == 0) return;
			break;
		case NodeTypes::EndAttributes:
			PrintBreakout(Stream);
			NeedClosingTag = true;
			print_tabs(Depth);
			printf(">\n"); 
			break;
		case NodeTypes::EndContainer: 
			PrintBreakout(Stream);
			if (!NeedClosingTag) 
			{
				print_tabs(Depth);
				printf("/>\n");
			}
			else
			{
				print_tabs(Depth - 1);
				printf("</%s>\n", Reader.GetContainer()->GetName().c_str()); 
			}
			return;
		case NodeTypes::Comment:
			{
				string Text = Reader.GetComment();
				PrintBreakout(Stream);
				print_tabs(Depth);
				printf("<!-- %s -->\n", Text.c_str());
			}
			break;
		case NodeTypes::Primitive:
			{
				string Name = Reader.GetName();
				string Value;
				switch (Reader.GetPrimitiveType())
				{
				case PrimitiveTypes::Boolean: Value = to_string(Reader.GetBoolean()); break;
				case PrimitiveTypes::Int: Value = to_string(Reader.GetInt()); break;
				case PrimitiveTypes::UInt: Value = to_string(Reader.GetUInt()); break;
				case PrimitiveTypes::String: Value = Reader.GetString(); break;
				case PrimitiveTypes::Single: Value = to_string(Reader.GetSingle()); break;
				case PrimitiveTypes::Double: Value = to_string(Reader.GetDouble()); break;
				default: Value = "Nondisplay primitive."; break;
				}

				PrintBreakout(Stream);

				if (Reader.IsAttribute)
				{
					print_tabs(Depth);
					printf("%s=\"%s\" \n", Name.c_str(), Value.c_str());					
				}
				else
				{
					print_tabs(Depth + 1);
					printf("<%s>%s</%s>\n", Name.c_str(), Value.c_str(), Name.c_str());
				}
			}
		}
	}
}

void DML_Reader_Demo()
{
	DmlReader::ParsingOptions Options;
	Options.DiscardComments = false;			// Retain comments so we can display them.

	printf("Reading DML File: %s\n", pszDML_Filename);
	printf("Binary readout of the file is shown on left.\n");
	printf("XML display of the file is shown on right.\n");
	printf("\n");
	wb::io::FileStream Source(pszDML_Filename, wb::io::FileMode::Open);
	BreakoutStream SourceBreakout(r_ptr<wb::io::Stream>::absolved(Source));
	DmlReader HeaderReader = wb::dml::DmlReader::Create(r_ptr<Stream>::absolved(SourceBreakout), Options);	

	printf("[Start of DML Header]\n");
	DML_Container_Reader_Demo(SourceBreakout, HeaderReader);
	printf("[End of DML Header]\n");

	// Rewind and parse header so that we have the translation loaded.  This time, don't display it.
	// What follows is a lot more representative of the way you would actually, typically, parse a DML document.
	DmlReader Reader = wb::dml::DmlReader::Create(r_ptr<Stream>::absolved(SourceBreakout), Options);	
	SourceBreakout.Seek(0, wb::io::SeekOrigin::Begin);	

	// ParseHeader() will process the document's DML:Header.  This should point the document to the Demo1Translation's URN.
	// That URN, in turn, will be passed to our ResourceResolve() callback function, which will provide the XML version of
	// the translation.  ParseHeader() will parse this XML and generate a translation that is applied to the Reader and is
	// used as its translation look-up table for the remainder of the parsing.
	Reader.ParseHeader(ResourceResolve);
	SourceBreakout.ClearBreakout();	
	
	printf("\n");
	printf("[Start of DML Content]\n");
	DML_Container_Reader_Demo(SourceBreakout, Reader);
	printf("[End of DML document]\n");
}

/////////
//	Various Diagnostics
//

void Run_Dictionary_Diagnostic()
{
	typedef unordered_map<UInt32, string> map_type;
	map_type mapping(8);
	typedef pair<UInt32, string> KeyValuePair;

	mapping.insert(KeyValuePair(8, "Eight"));
	mapping.insert(KeyValuePair(9, "Nine"));
	mapping.insert(KeyValuePair(15, "Fifteen"));
	mapping.insert(KeyValuePair(47, "Forty-Seven"));
	mapping.insert(KeyValuePair(83, "Eighty-Three"));

	printf("All entries: ");
	for (map_type::iterator all = mapping.begin(); all != mapping.end(); all++)
	{
		printf("%s ", all->second.c_str());
	}
	printf("\n\n");

	printf("By bucket: \n");
	for (size_t iBucket = 0; iBucket < mapping.bucket_count(); iBucket++)
	{
		printf("Bucket #%d: ", iBucket);
		for (map_type::local_iterator bucket = mapping.begin(iBucket); bucket != mapping.end(iBucket); bucket++)
		{
			printf("%s ", bucket->second.c_str());
		}
		printf("\n");
	}
	printf("End of list.\n");
}

/////////
//	Main Demo
//

void main()
{	
	try
	{			
		using namespace wb::dml;

		printf("Sample Translation in XML ----------------\n");
		printf("%s\n", pszSampleTranslation);
		printf("\n\n");

		printf("DML Writer Demo --------------------------\n");
		DML_Writer_Demo();
		printf("\n\n");

		printf("DML Reader Demo --------------------------\n");
		DML_Reader_Demo();
		printf("\n\n");

		#ifdef Run_Diagnostics

		printf("Dictionary Diagnostics -------------------\n");
		Run_Dictionary_Diagnostic();
		printf("\n\n");

		#endif
	}
	catch (Exception e)
	{
		printf("\nError:  %s\n\n\n", e.GetMessage().c_str());
	}
	
	printf("Press any key to exit...");
	_getch();
}
