/////////
//	XML Parser (Generation 4)
//	Copyright (C) 2010-2014 by Wiley Black
////
//	A simplified XML Parser which operates in a single-pass and
//	uses a minimal dependency set.  The parser utilizes linked lists
//	instead of dynamic arrays, however this feature is hidden 
//	from user code for usability.  The class hierarchy is designed
//	to be somewhat similar to the .NET heirarchy.
//
//	These features could be added to the parser, however at present
//	the following XML features are not implemented and generate an
//	error:
//		- XML Namespaces
//		- Partial/characterwise message parsing 
//
//	Some features not available in the XML specification include:
//		- Multiple top-level elements permitted (automatically) if needed.
//			This will manifest as the XmlDocument object containing multiple
//			children.
//
//	Some features which are included:
//		- Support for comment parsing (comments are discarded)
////

#ifndef __WBXmlParser_v4_h__
#define __WBXmlParser_v4_h__

/** Table of Contents **/

namespace wb
{
	namespace xml
	{
		class XmlParser;
	}
}

/** Dependencies **/

#include "Xml.h"
#include "../../IO/Streams.h"

/** Content **/

namespace wb
{
	namespace xml
	{
		class XmlParser
		{
			enum SpecialTag
			{
				Ordinary,
				OpenAndClose,
				Declaration
			};

			void ParseNode(const char *&psz, XmlNode* pNode);
			XmlElement* ParseElement(const char *&psz);
			bool ParseAttributes(const char *&psz, XmlElement* pElement, SpecialTag& Special);
			bool ParseClosingTag(const char *&psz, string &CloseTagName);
			bool ParseXMLDeclaration(const char *&psz);
			bool ParseComment(const char *&psz);
			XmlText* ParseText(const char *&psz, bool CDATA);

			bool IsWhitespace(char ch) { return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r'; }

		public:

			XmlParser();
			~XmlParser();

			// The returned XmlElement has been allocated with new and should be delete'd when done.
			// An exception is thrown on error.
			XmlDocument *Parse(const char *psz);

			// The returned XmlElement has been allocated with new and should be delete'd when done.
			// An exception is thrown on error.
			XmlDocument *Parse(wb::io::Stream& stream);
		};
	}
}

/** Late dependencies **/

#include "Implementation/XmlParserImpl.h"

#endif	// __WBXmlParser_h__

//	End of XmlParser.h

