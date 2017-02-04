/**	Dml.cpp
	Copyright (C) 2014 by Wiley Black (TheWiley@gmail.com)
**/

#ifdef _WIN32
#include <Windows.h>
#endif

#define DmlPrimaryModule
#include "../Dml.h"

#include "DmlReader.h"
#include "DmlWriter.h"

namespace wb
{
	namespace dml
	{
		/*static*/ DateTime ReferenceDate(2001, 1, 1, 0, 0, 0, 0, DateTime::UTC);        		

		const char* PrimitiveTypeToString(PrimitiveTypes Type, ArrayTypes ArrayType)
        {
            switch (Type)
            {
			case PrimitiveTypes::Boolean: return "boolean";
			case PrimitiveTypes::DateTime: return "datetime";
			case PrimitiveTypes::Single: return "single";
			case PrimitiveTypes::Double: return "double";
			case PrimitiveTypes::Int: return "int";
			case PrimitiveTypes::UInt: return "uint";
			case PrimitiveTypes::String: return "string";                
                // ext-precision types
                // decimal-float types
			case PrimitiveTypes::Decimal: return "decimal128";
			case PrimitiveTypes::CompressedDML: return "compressed-dml";
			case PrimitiveTypes::EncryptedDML: return "encrypted-dml";
                // array-types
			case PrimitiveTypes::Array: 
				switch (ArrayType)
				{
				case ArrayTypes::U8: return "array-U8";
				case ArrayTypes::U16: return "array-U16";
				case ArrayTypes::U24: return "array-U24";
				case ArrayTypes::U32: return "array-U32";
				case ArrayTypes::U64: return "array-U64";
				case ArrayTypes::I8: return "array-I8";
				case ArrayTypes::I16: return "array-I16";
				case ArrayTypes::I24: return "array-I24";
				case ArrayTypes::I32: return "array-I32";
				case ArrayTypes::I64: return "array-I64";
				case ArrayTypes::Singles: return "array-SF";
				case ArrayTypes::Doubles: return "array-DF";
				case ArrayTypes::DateTimes: return "array-DT";
				case ArrayTypes::Strings: return "array-S";

				case ArrayTypes::Decimals: return "array-10F";

				default: throw ArgumentException("Not a recognized DML array type.");
				}
			case PrimitiveTypes::Matrix: 
				switch (ArrayType)
				{
				case ArrayTypes::U8: return "matrix-U8";
				case ArrayTypes::U16: return "matrix-U16";
				case ArrayTypes::U24: return "matrix-U24";
				case ArrayTypes::U32: return "matrix-U32";
				case ArrayTypes::U64: return "matrix-U64";
				case ArrayTypes::I8: return "matrix-I8";
				case ArrayTypes::I16: return "matrix-I16";
				case ArrayTypes::I24: return "matrix-I24";
				case ArrayTypes::I32: return "matrix-I32";
				case ArrayTypes::I64: return "matrix-I64";
				case ArrayTypes::Singles: return "matrix-SF";
				case ArrayTypes::Doubles: return "matrix-DF";
				case ArrayTypes::DateTimes: return "matrix-DT";
				case ArrayTypes::Strings: return "matrix-S";

				case ArrayTypes::Decimals: return "matrix-10F";

				default: throw ArgumentException("Not a recognized DML matrix type.");
				}
            default: return NULL;
            }                        
        }

		const char* NodeTypeToString(NodeTypes NodeType, PrimitiveTypes Type, ArrayTypes ArrayType)
		{
			switch (NodeType)
			{
			case NodeTypes::Composite: return "Composite";
			case NodeTypes::Structural: return "Structural";
			default:
			case NodeTypes::Unknown: return "Unknown";
			case NodeTypes::Container: return "Container";
			case NodeTypes::Primitive: return PrimitiveTypeToString(Type, ArrayType);
			case NodeTypes::EndAttributes: return "EndAttributes";
			case NodeTypes::EndContainer: return "EndContainer";
			case NodeTypes::Comment: return "Comment";
			case NodeTypes::Padding: return "Padding";				
			}
		}

		ArrayTypes SuffixToArrayType(string TypeSuffix)
        {
			// TypeSuffix = to_lower(TypeSuffix);			// Unnecessary because StringToPrimitiveType() has already done this.
			if (TypeSuffix.compare("u8")	== 0) return ArrayTypes::U8;
			else if (TypeSuffix.compare("u16")	== 0) return ArrayTypes::U16;
			else if (TypeSuffix.compare("u32")	== 0) return ArrayTypes::U32;
			else if (TypeSuffix.compare("u64")	== 0) return ArrayTypes::U64;
			else if (TypeSuffix.compare("i8")	== 0) return ArrayTypes::I8;
			else if (TypeSuffix.compare("i16")	== 0) return ArrayTypes::I16;
			else if (TypeSuffix.compare("i32")	== 0) return ArrayTypes::I32;
			else if (TypeSuffix.compare("i64")	== 0) return ArrayTypes::I64;
			else if (TypeSuffix.compare("sf")	== 0) return ArrayTypes::Singles;
			else if (TypeSuffix.compare("df")	== 0) return ArrayTypes::Doubles;
			else if (TypeSuffix.compare("dt")	== 0) return ArrayTypes::DateTimes;
			else if (TypeSuffix.compare("s")	== 0) return ArrayTypes::Strings;
			else if (TypeSuffix.compare("10f")	== 0) return ArrayTypes::Decimals;
			else throw FormatException("Not a recognized type indicator.");
        }

		bool StringToPrimitiveType(string TypeStr, PrimitiveTypes& Type, ArrayTypes& ArrayType)
        {
			TypeStr = to_lower(TypeStr);

            ArrayType = ArrayTypes::Unknown;
            
            if (TypeStr.find("array-") != string::npos)
            {
                Type = PrimitiveTypes::Array;
                ArrayType = SuffixToArrayType(TypeStr.substr(strlen("array-")));
                return true;
            }
            else if (TypeStr.find("matrix-") != string::npos)
            {
                Type = PrimitiveTypes::Matrix;
                ArrayType = SuffixToArrayType(TypeStr.substr(strlen("matrix-")));
                return true;
            }
			else if (TypeStr.compare("int") == 0) { Type = PrimitiveTypes::Int; return true; }
			else if (TypeStr.compare("uint") == 0) { Type = PrimitiveTypes::UInt; return true; }
			else if (TypeStr.compare("boolean") == 0) { Type = PrimitiveTypes::Boolean; return true; }
			else if (TypeStr.compare("single") == 0) { Type = PrimitiveTypes::Single; return true; }
			else if (TypeStr.compare("double") == 0) { Type = PrimitiveTypes::Double; return true; }
			else if (TypeStr.compare("datetime") == 0) { Type = PrimitiveTypes::DateTime; return true; }
			else if (TypeStr.compare("string") == 0) { Type = PrimitiveTypes::String; return true; }
				// ext-precision types
				// decimal-float types
			else if (TypeStr.compare("decimal128") == 0) { Type = PrimitiveTypes::Decimal; return true; }
			else if (TypeStr.compare("compressed-dml") == 0) { Type = PrimitiveTypes::CompressedDML; return true; }
			else if (TypeStr.compare("encrypted-dml") == 0) { Type = PrimitiveTypes::EncryptedDML; return true; }
			else return false;
        }
	}

	namespace dmltsl
	{
		namespace tsl2
		{
			/** TSL2 Predefined Translation **/

			Association DMLTranslation(dmltsl::tsl2::idDMLTranslation, "DML:Translation", NodeTypes::Container);

			Association URN(dmltsl::tsl2::idDML_URN, "DML:URN", PrimitiveTypes::String);			
			Association IncludeTranslation(dmltsl::tsl2::idDMLIncludeTranslation, "DML:Include-Translation", NodeTypes::Container);
			Association URI(dmltsl::tsl2::idDML_URI, "DML:URI", PrimitiveTypes::String);						

			Association IncludePrimitives(dmltsl::tsl2::idDMLIncludePrimitives, "DML:Include-Primitives", NodeTypes::Container);
			Association Set(dmltsl::tsl2::idDMLSet, "DML:Set", PrimitiveTypes::String);
			Association Codec(dmltsl::tsl2::idDMLCodec, "DML:Codec", PrimitiveTypes::String);			
			Association CodecURI(dmltsl::tsl2::idDMLCodecURI, "DML:CodecURI", PrimitiveTypes::String);
			
			Association Container(dmltsl::tsl2::idContainer, "Container", NodeTypes::Container);
			Association Node(dmltsl::tsl2::idNode, "Node", NodeTypes::Container);        
			Association Name(dmltsl::tsl2::idName, "name", PrimitiveTypes::String);
			Association ID(dmltsl::tsl2::idDMLID, "id", PrimitiveTypes::UInt);
			Association Type(dmltsl::tsl2::idType, "type", PrimitiveTypes::String);
			Association Usage(dmltsl::tsl2::idUsage, "usage", PrimitiveTypes::String);
			Association Renumber(dmltsl::tsl2::idRenumber, "Renumber", NodeTypes::Container);
			Association NewID(dmltsl::tsl2::idNewID, "new-id", PrimitiveTypes::UInt);

			Association XMLRoot(dmltsl::tsl2::idXMLRoot, "XMLRoot", NodeTypes::Container);

			Translation CreateTSL2()
			{
				Translation TSL2;

				TSL2.Add(DMLTranslation);

				TSL2.Add(URN);
				TSL2.Add(IncludeTranslation);
				TSL2.Add(URI);

				TSL2.Add(IncludePrimitives);
				TSL2.Add(Set);
				TSL2.Add(Codec);				
				TSL2.Add(CodecURI);
								
				TSL2.Add(Container);
				TSL2.Add(Node);
				TSL2.Add(Name);
				TSL2.Add(ID);
				TSL2.Add(Type);
				TSL2.Add(Usage);
				TSL2.Add(Renumber);
				TSL2.Add(NewID);

				TSL2.Add(XMLRoot);

				return TSL2;
			}
		}

		namespace dml3
		{
			/** DML3 Predefined Translation **/

			Association Fragment(71619779, "DML:Fragment", NodeTypes::Structural);
			
			Association Version(dmltsl::dml3::idDMLVersion, "DML:Version", PrimitiveTypes::UInt);
			Association ReadVersion(dmltsl::dml3::idDMLReadVersion, "DML:ReadVersion", PrimitiveTypes::UInt);
			Association DocType(dmltsl::dml3::idDMLDocType, "DML:DocType", PrimitiveTypes::String);

			Association Header(dmltsl::dml3::idDMLHeader, "DML:Header", tsl2::CreateTSL2());
			
			Association ContentSize(dmltsl::dml3::idDMLContentSize, "DML:ContentSize", PrimitiveTypes::UInt);

			Association InlineIdentification(dmltsl::dml3::idInlineIdentification, "DML:InlineIdentification", NodeTypes::Structural);
			Association Comment(dmltsl::dml3::idDMLComment, "DML:Comment", NodeTypes::Comment);
			Association Padding(dmltsl::dml3::idDMLPadding, "DML:Padding", NodeTypes::Padding);
			Association PaddingByte(dmltsl::dml3::idDMLPaddingByte, "DML:PaddingByte", NodeTypes::Padding);			
			Association EndAttributes(dmltsl::dml3::idDMLEndAttributes, "DML:EndAttributes", NodeTypes::EndAttributes);
			Association EndContainer(dmltsl::dml3::idDMLEndContainer, "DML:EndContainer", NodeTypes::EndContainer);

			Translation CreateDML3()
			{
				Translation DML3;
			
				DML3.Add(Version);
				DML3.Add(ReadVersion);
				DML3.Add(DocType);

				DML3.Add(Header);
								
				DML3.Add(ContentSize);
				DML3.Add(Comment);				
				
					/**
						* These entries are handled explicitly by DmlReader::Read() and 
						* need not be automatically recognized:
					InlineIdentification,
					Padding,
					PaddingByte,
					EndAttributes,
					EndContainer,
						*/

				return DML3;
			}
		}		
	}

	namespace dml
	{
		Translation Translation::DML3 = dmltsl::dml3::CreateDML3();					
		Translation Translation::TSL2 = dmltsl::tsl2::CreateTSL2();
	}
}

//	End of Dml.cpp

