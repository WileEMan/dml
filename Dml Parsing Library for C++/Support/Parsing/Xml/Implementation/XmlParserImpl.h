/////////
//	XmlParserImpl.h (Generation 4)
//	Copyright (C) 2010-2014 by Wiley Black
////

#ifndef __WBXmlParserImpl_v4_h__
#define __WBXmlParserImpl_v4_h__

#ifndef __WBXmlParser_v4_h__
#error	This header should be included only via XmlParser.h.
#endif

/** Dependencies **/

#include "../XmlParser.h"						// For Intellisense's benefit only.
#include "../../../Text/StringComparison.h"
#include "../../../IO/MemoryStream.h"

/** Content **/

namespace wb
{
	namespace xml
	{				
		/** XmlParser Implementation **/
		
		inline XmlParser::XmlParser()
		{
		}

		inline XmlParser::~XmlParser()
		{
		}

		inline string XmlParser::GetSource()
		{			
			if (CurrentSource.length() < 1) return "line " + std::to_string(CurrentLineNumber);
			return CurrentSource + ":" + std::to_string(CurrentLineNumber);
		}

		inline XmlDocument* XmlParser::Parse(wb::io::Stream& stream, const string& sSourceFilename)
		{
			// Optimization: Could avoid storing the whole thing in memory and parse as we go...
			wb::io::MemoryStream ms;
			wb::io::StreamToStream(stream, ms);
			ms.Seek(0, wb::io::SeekOrigin::End);
			ms.WriteByte(0);			// Add null-terminator
			ms.Rewind();
			return Parse((const char *)ms.GetDirectAccess(0), sSourceFilename);
		}

		inline XmlDocument* XmlParser::Parse(const char *psz, const string& sSourceFilename)
		{
			CurrentSource = sSourceFilename;
			CurrentLineNumber = 1;

			if (psz[0] != 0 && psz[1] != 0 && psz[2] != 0)
			{				
				if ((byte)psz[0] == 0xEF && (byte)psz[1] == 0xBB && (byte)psz[2] == 0xBF) 
					psz += 3; 														// UTF-8 BOM.  TODO: Respond to detected encoding.
			}			

			for (;; psz++)
			{
				if (*psz == 0) {
					if (CurrentSource.length() < 1) 
						throw ArgumentException("No XML content found.");
					else
						throw ArgumentException("No XML content found in " + CurrentSource + ".");
				}

				if (*psz == '<') {			
					if (*(psz+1) == '?'){
						if (!ParseXMLDeclaration(psz)) {
							throw FormatException("Invalid XML declaration format at " + GetSource() + ".");
						}
						continue;
					}
					break;
				}

				if (*psz == '\n') CurrentLineNumber++;

				if (!IsWhitespace(*psz))
				{
					throw FormatException("Expected XML opening tag at top-level at " + GetSource() + ".");
				}
			}

			XmlDocument *pDocument = new XmlDocument();
			try
			{
				pDocument->SourceLocation = sSourceFilename;
				ParseNode(psz, pDocument);
			}
			catch (std::exception&)
			{
				delete pDocument;
				throw;
			}
			return pDocument;
		}

		inline void XmlParser::SkipWhitespace(const char*& psz)
		{
			while (*psz && IsWhitespace(*psz)) {
				if (*psz == '\n') CurrentLineNumber++;
				psz++; 
			}
		}

		inline void XmlParser::ParseNode(const char *&psz, XmlNode *pNode)
		{
				// Assumes that all opening tags (if applicable) of the node have been parsed,
				// and that the pointer is at the beginning of node content.

				// Quits parsing at two possible locations:
				//	-	If pNode represents an XmlElement, then ParseNode() returns after the closing
				//		tag has been parsed (assuming no errors).
				//	-	If pNode does not represent an XmlElement, then ParseNode() returns at the
				//		end of the psz string.

				// On error, returns false and sets the last error string.

			//XmlNode	**ppNextChild = &(pNode->m_pFirstChild);

			for (;;)
			{
				if (*psz == 0)
				{
					if (pNode->IsElement())
					{
						throw FormatException("Badly formed XML (no closing tag for '" + pNode->ToString() + "' from " + pNode->SourceLocation + ")");
					}

					return;
				}

				if (*psz == '<') 
				{
					psz++;
					SkipWhitespace(psz);
					if (*psz == '/')
					{
						psz ++;
						string CloseTagName;
						if (!ParseClosingTag(psz, CloseTagName))
						{
							throw FormatException("Badly formed XML (closing tag '" + CloseTagName + "' at " + GetSource() + " is invalid)");
						}
						if (!pNode->IsElement())
						{
							throw FormatException("Badly formed XML (invalid closing tag '" + CloseTagName + "' found at " + GetSource() + " inside '" + pNode->ToString() + "' from " + pNode->SourceLocation + ")");
						}
						XmlElement *pElement = (XmlElement *)pNode;
						if (!IsEqual(CloseTagName, pElement->LocalName))
						{
							throw FormatException("Badly formed XML (mismatched closing tag '" + CloseTagName + "' at " + GetSource() + " found inside element '" + pElement->LocalName + "' from " + pElement->SourceLocation + ")");
						}
						return;
					}

					if (*psz == '!')
					{
						psz ++;
						if (*psz == '[' && StartsWith(psz + 1, "CDATA["))
						{
							psz += strlen("[CDATA[");
							XmlNode *pChild = ParseText(psz, true);
							if (!pChild) throw FormatException("Badly-formed XML, illegal CDATA format at " + GetSource() + ".");
							pNode->Children.push_back(pChild);
							continue;
						}
						if (*psz != '-' || *(psz+1) != '-') 
						{
							throw FormatException("Badly formed XML (invalid comment tag found at " + GetSource() + ").");
						}
						psz++; psz++;
						if (!ParseComment(psz))
						{
							throw FormatException("Badly formed XML (invalid comment found at " + GetSource() + ").");
						}
						continue;
					}

					XmlNode *pChild = ParseElement(psz);
					pNode->Children.push_back(pChild);
					continue;
				}

				if (IsWhitespace(*psz)) { 
					if (*psz == '\n') CurrentLineNumber++;
					psz++; continue; 
				}

				// A character other than <, whitespace, or end of string has been found 
				// inside the element.  Must be text!
				XmlNode *pChild = ParseText(psz, false);
				if (!pChild) { 
					throw FormatException("Badly-formed XML (text contains invalid character inside '" + pNode->ToString() + "')");
				}
				pNode->Children.push_back(pChild);
			}
		}

		inline XmlElement *XmlParser::ParseElement(const char *&psz)
		{
			#ifdef _DEBUG
			const char *pszAtStart = psz;
			#endif

				// The < character may optionally have already been parsed.

			if (*psz == '<') psz++;
			SkipWhitespace(psz);
			if (*psz == '/') return NULL;

			string strName;
			while (*psz)
			{
				if (IsWhitespace(*psz)) break;
				if (*psz == '>' || *psz == '/') break;
				strName += *psz;
				psz++;
			}

			if (*psz == 0) {
				throw FormatException("Improperly terminated XML tag (missing closing >) on tag '" + strName + "' at " + GetSource() + ".");
			}

			XmlElement *pElement = new XmlElement();
			try
			{
				pElement->SourceLocation = GetSource();
				pElement->LocalName = strName;

				SpecialTag Special;
				if (!ParseAttributes(psz, pElement, Special))
				{
					#ifdef _DEBUG
					throw FormatException("Improperly formatted XML tag '" + strName + "' at " + GetSource() + ".  Context: \n" + string(pszAtStart).substr(0,100).c_str());
					#else
					throw FormatException("Improperly formatted XML tag '" + strName + "' at " + GetSource() + ".");
					#endif
				}

				if (Special == OpenAndClose) return pElement;	

				SkipWhitespace(psz);
				ParseNode(psz, pElement);
				return pElement;
			}
			catch (std::exception&) { delete pElement; throw; }
		}

		inline bool XmlParser::ParseAttributes(const char *&psz, XmlElement *pElement, SpecialTag& Special)
		{
				// We assume that the <tag-name characters have already been parsed.
				// We will return one character past the > character (assuming no errors).
				// Returns false on error, but does not set the last error.

			Special = Ordinary;

			SkipWhitespace(psz);

			string	Name;
			string	Current;
			string	Code;			

			bool bParsingAttrName = false, bParsingAttrValue = false, bParsingString = false, bParsingEscapeCode = false;
			for (; *psz != 0; psz++)
			{
				if (*psz == '\n') CurrentLineNumber++;

				if (bParsingString)
				{
					if (*psz == '\"') { bParsingString = false; bParsingEscapeCode = false; Code.clear(); continue; }

					if (bParsingEscapeCode) {
						if (*psz == ';') {
							bParsingEscapeCode = false;					
							if (IsEqualNoCase(Code, "quot")) Current += '\"';
							else if (IsEqualNoCase(Code, "amp")) Current += '&';
							else if (IsEqualNoCase(Code, "apos")) Current += '\'';
							else if (IsEqualNoCase(Code, "lt")) Current += '<';
							else if (IsEqualNoCase(Code, "gt")) Current += '>';
							Code.clear();
							continue;
						}

						Code += *psz;
						continue;
					}

					if (*psz == '&') { bParsingEscapeCode = true; continue; }

					Current += *psz;
					continue;
				}
				else if (*psz == '\"') { bParsingString = true; continue; }

				if (bParsingAttrName)
				{
					if (*psz == '=') { Name = Current; Current.clear(); bParsingAttrName = false; bParsingAttrValue = true; continue; }
					if (IsWhitespace(*psz)) continue;
					if (*psz == '>') { psz++; return false; }
					if (*psz == '/') return false;			
					Current += *psz;
					continue;
				}

				if (bParsingAttrValue)
				{
					if (IsWhitespace(*psz) || *psz == '>' || *psz == '/' || *psz == '?')
					{
						bParsingAttrValue = false;
						XmlAttribute *pNewAttr = new XmlAttribute();
						pNewAttr->Name = Name;
						pNewAttr->Value = Current;				
						pElement->Attributes.push_back(pNewAttr);

						if (*psz == '>') { psz ++; return true; }

						if (*psz == '/' || *psz == '?')
						{
							if (*psz == '/') Special = OpenAndClose; else Special = Declaration;
							psz ++;
							SkipWhitespace(psz);
							if (*psz != '>') return false;
							psz ++;
							return true;			
						}

						Name.clear();
						Current.clear();
						continue;
					}

					Current += *psz;
					return false;
				}

				if (IsWhitespace(*psz)) continue;
				if (*psz == '=') return false;
				if (*psz == '>') { psz ++; return true; }

				if (*psz == '/' || *psz == '?')
				{
					if (*psz == '/') Special = OpenAndClose; else Special = Declaration;
					psz ++;
					SkipWhitespace(psz);
					if (*psz != '>') return false;
					psz ++;
					return true;			
				}		

				bParsingAttrName = true;
				Current += *psz;
			}

			return false;
		}

		inline bool XmlParser::ParseClosingTag(const char *&psz, string& CloseTagName)
		{
				// We assume that the </ characters have already been parsed.
				// We will return one character past the > character (assuming no errors).
				// Returns false on error, but does not set the last error.

			SkipWhitespace(psz);			

			CloseTagName = "";

			for (; *psz != 0; psz++)
			{		
				if (IsWhitespace(*psz)) {
					if (*psz == '\n') CurrentLineNumber++;
					psz++;
					continue;
				}
				if (*psz == '>') { psz++; return true; }
				CloseTagName += *psz;
			}

			return false;
		}

		inline XmlText *XmlParser::ParseText(const char *&psz, bool CDATA)
		{
				// Assumes that psz currently points to the first text character.
				// Returns on the first non-text character, which is usually < if there are no errors,
				// although it could be additional text if we are parsing CDATA.
				// Returns NULL on error but does not set the last error string.			

			XmlText *pText = new XmlText();
			pText->SourceLocation = GetSource();

			if (!CDATA)
			{
				string Code;
				bool bParsingEscapeCode = false;

				for (; *psz != 0; psz++)
				{
					if (*psz == '\n') CurrentLineNumber++;
					if (*psz == '<') return pText;
					/** Microsoft's XML Parser apparently allows a > character given no < opener, and they even use 
						it in some Visual Studio/MSBuild property files (xml).  I'm not sure how I feel about it, but
						I need to parse Microsoft's XML files so guess I'm going with it.
					if (*psz == '>')
					{
						delete pText;
						return NULL;
					}
					**/

					if (bParsingEscapeCode) {
						if (*psz == ';') {
							bParsingEscapeCode = false;				
							if (IsEqualNoCase(Code, "quot")) pText->Text += '\"';
							else if (IsEqualNoCase(Code, "amp")) pText->Text += '&';
							else if (IsEqualNoCase(Code, "apos")) pText->Text += '\'';
							else if (IsEqualNoCase(Code, "lt")) pText->Text += '<';
							else if (IsEqualNoCase(Code, "gt")) pText->Text += '>';
							Code.clear();
							continue;
						}

						Code += *psz;
						continue;
					}

					if (*psz == '&') { bParsingEscapeCode = true; continue; }

					pText->Text += *psz;
				}
			}
			else	// CDATA Parsing...
			{				
				for (; *psz != 0; psz++) 
				{
					if (*psz == '\n') CurrentLineNumber++;
					if (*psz == ']' && *(psz+1) == ']' && *(psz+2) == '>') { psz += 3; return pText; }
					pText->Text += *psz;
				}
				throw FormatException("Unterminated CDATA block.");
			}

			return pText;
		}

		inline bool XmlParser::ParseXMLDeclaration(const char *&psz)
		{
				// Assumes that we have not yet parsed the <? characters, but have verified they are present.
				// Returns one character past the closing > character (assuming no errors).
				// Returns false on error, but does not set the last error.		

			if (*psz != '<') return false;
			psz ++;
			if (*psz != '?') return false;
			psz ++;

			/** Note: ParseAttributes() is fully setup to handle the rest of the
				XML Declaration tag, including the special ? at the end.  We just
				aren't using it at the moment. **/

			for (; *psz != 0; psz++)
			{
				if (*psz == '\n') CurrentLineNumber++;
				if (*psz == '>')
				{
					psz++; return true;
				}
			}

			return false;
		}

		inline bool XmlParser::ParseComment(const char *&psz)
		{
				// Assumes we have already parsed the <!-- characters.
				// Returns one character past the closing --> characters (assuming no errors).
				// Returns false on error, but does not set the last error.

			int nMatch = 0;
			for (; *psz != 0; psz++)
			{
				if (*psz == '\n') CurrentLineNumber++;
				if (*psz == '-') { nMatch ++; continue; }
				if (*psz == '>' && nMatch == 2) { psz++; return true; }
				nMatch = 0;
			}
			return false;
		}
	}
}

#endif	// __WBXmlParserImpl_v4_h__

//	End of XmlParserImpl.h

