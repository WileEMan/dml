/////////
//	XmlImpl.h (Generation 4)
//	Copyright (C) 2010-2014 by Wiley Black
////

#ifndef __WBXmlImpl_v4_h__
#define __WBXmlImpl_v4_h__

#ifndef __WBXml_v4_h__
#error	This header should be included only via Xml.h.
#endif

/** Dependencies **/

#include <assert.h>
#include "../../../Text/StringComparison.h"
#include "../../../Text/StringConversion.h"
#include "../../BaseTypeParsing.h"
#include "../Xml.h"					// For Intellisense, but otherwise has no effect.

/** Content **/

namespace wb
{
	namespace xml
	{
		/** XmlWriterOptions implementation **/

		inline XmlWriterOptions::XmlWriterOptions(XmlWriterOptions& cp)
		{
			IncludeContent = cp.IncludeContent;
			Indentation = cp.Indentation;
			AllowSingleTags = cp.AllowSingleTags;
			EscapeAttributeWhitespace = cp.EscapeAttributeWhitespace;
		}

		inline XmlWriterOptions::XmlWriterOptions(XmlWriterOptions&& cp)
		{
			IncludeContent = cp.IncludeContent;
			Indentation = cp.Indentation;
			AllowSingleTags = cp.AllowSingleTags;
			EscapeAttributeWhitespace = cp.EscapeAttributeWhitespace;
		}

		inline XmlWriterOptions::XmlWriterOptions(bool AndContent)
		{
			IncludeContent = AndContent;
			Indentation = 0;
			AllowSingleTags = true;
			EscapeAttributeWhitespace = false;
		}

		/** XmlNode Implementation **/

		inline XmlNode::XmlNode()
		{
		}

		inline XmlNode::~XmlNode()
		{
			for (size_t ii=0; ii < Children.size(); ii++) delete Children[ii];
			Children.clear();
		}		

		inline /*static*/ string XmlNode::Escape(const XmlWriterOptions& Options, const string& str)
		{
			string ret;
			for (size_t ii=0; ii < str.length(); ii++)
			{				
				if (!Options.EscapeAttributeWhitespace)
				{
					switch (str[ii])
					{
					case '\"': ret += "&quot;"; continue;
					case '&': ret += "&amp;"; continue;
					case '\'': ret += "&apos;"; continue;
					case '<': ret += "&lt;"; continue;
					case '>': ret += "&gt;"; continue;
					default: ret += str[ii]; continue;
					}
				}
				else
				{
					switch (str[ii])
					{
					case '\"': ret += "&quot;"; continue;
					case '&': ret += "&amp;"; continue;
					case '\'': ret += "&apos;"; continue;
					case '<': ret += "&lt;"; continue;
					case '>': ret += "&gt;"; continue;
					case ' ': ret += "&#x20;"; continue;
					case '\t': ret += "&#x9;"; continue;
					case '\n': ret += "&#xA;"; continue;
					case '\r': ret += "&#xD;"; continue;
					default: ret += str[ii]; continue;
					}
				}
			}
			return ret;
		}

		inline /*static*/ string XmlNode::Escape(const JsonWriterOptions& Options, const string& str)
		{
			string ret;
			for (size_t ii=0; ii < str.length(); ii++)
			{				
				switch (str[ii])
				{
				case '\"': ret += "\\\""; continue;
				case '\\': ret += "\\\\"; continue;
				case '/': ret += "\\/"; continue;
				case '\b': ret += "\\b"; continue;
				case '\f': ret += "\\f"; continue;
				case '\n': ret += "\\n"; continue;
				case '\r': ret += "\\r"; continue;
				case '\t': ret += "\\t"; continue;
					// TODO: Can use \u to escape a Unicode character.
				default: ret += str[ii]; continue;
				}
			}
			return ret;
		}

		inline /*static*/ void XmlNode::Indent(const XmlWriterOptions& Options, string& OnString)
		{
			for (int ii=0; ii < Options.Indentation; ii++) OnString += '\t';
		}

		inline /*static*/ void XmlNode::Indent(const JsonWriterOptions& Options, string& OnString)
		{
			for (int ii=0; ii < Options.Indentation; ii++) OnString += '\t';
		}

		inline string XmlNode::ToString(XmlWriterOptions Options)
		{
			if (Options.IncludeContent)
			{
				string ret;
				//int nSinceLF = 0;
				for (size_t ii=0; ii < Children.size(); ii++)
				{					
					XmlNode* pChild = Children[ii];
					string substr = pChild->ToString(Options);
					ret += substr;
					//nSinceLF += substr.length();
					//if (nSinceLF > 40) { ret += '\n'; Indent(Options, ret); nSinceLF = 0; }
					// ret += '\n';					
				}
				return ret;
			}
			else return "XmlNode";
		}

		inline string XmlNode::ToJson(JsonWriterOptions Options)
		{
			throw NotImplementedException("Conversion to Json value for this node was not implemented.");
		}

		inline string XmlNode::ToJsonValue(JsonWriterOptions Options)
		{
			throw NotImplementedException("Conversion to Json value for this node was not implemented.");
		}

		inline vector<XmlElement*> XmlNode::ChildElements()
		{
			vector<XmlElement*> ret;
			for (size_t ii=0; ii < Children.size(); ii++)
				if (Children[ii]->IsElement()) ret.push_back((XmlElement*)Children[ii]);
			return ret;
		}

		inline XmlElement *XmlNode::FindChild(const char *pszTagName)
		{
			for (size_t ii=0; ii < Children.size(); ii++)
			{			
				if (Children[ii]->IsElement())
				{
					XmlElement *pElement = (XmlElement *)Children[ii];
					if (IsEqual(pElement->LocalName, pszTagName)) return pElement;
				}
			}
			return NULL;
		}

		inline XmlElement *XmlNode::FindNthChild(const char *pszTagName, int N)
		{
			int Matches = 0;
			for (size_t ii=0; ii < Children.size(); ii++)
			{
				if (Children[ii]->IsElement())
				{
					XmlElement *pElement = (XmlElement *)Children[ii];
					if (IsEqual(pElement->LocalName, pszTagName))
					{
						if (Matches == N) return pElement;				
						Matches ++;
					}
				}
			}
			return nullptr;
		}

		inline XmlNode *XmlNode::InsertChild(int at_index, XmlNode *pNewChild)
		{	
			try
			{
				Children.insert(Children.begin() + at_index, pNewChild);
			}
			catch (...) { delete pNewChild; throw; }
			return pNewChild;
		}

		inline XmlNode *XmlNode::InsertChild(int at_index, std::unique_ptr<XmlNode>&& pNewChild)
		{
			XmlNode* pNode = pNewChild.release();
			try
			{
				Children.insert(Children.begin() + at_index, pNode);
			}
			catch (...) { delete pNode; throw; }
			return pNode;
		}

		inline XmlNode *XmlNode::AppendChild(XmlNode *pNewChild)
		{	
			try
			{
				Children.push_back(pNewChild);
			}
			catch (...) { delete pNewChild; throw; }
			return pNewChild;
		}

		inline XmlNode *XmlNode::AppendChild(std::unique_ptr<XmlNode>&& pNewChild)
		{
			XmlNode* pNode = pNewChild.release();
			try
			{
				Children.push_back(pNode);
			}
			catch (...) { delete pNode; throw; }
			return pNode;
		}

		inline void XmlNode::RemoveChild(XmlNode *pChild)
		{
			for (size_t ii=0; ii < Children.size(); ii++)
			{
				if (Children[ii] == pChild)
				{
					Children.erase(Children.begin() + ii);

					// We assume there is only one instance of pChild in
					// the list.  Anything else is improper construction.
					return;
				}
			}
		}		

		/** XmlDocument Implementation **/
		
		inline string XmlDocument::ToString(XmlWriterOptions Options)
		{		
			if (!Options.IncludeContent) return "XmlDocument";
			return XmlNode::ToString(Options);
		}		

		inline string XmlDocument::ToJson(JsonWriterOptions Options)
		{			
			string ret;
			Indent(Options, ret);
			ret += "{\n";
			Options.Indentation++;			

			// ret += GetDocumentElement()->ToJson(Options);			
			Indent(Options, ret);
			ret += '\"';
			ret += Escape(Options, GetDocumentElement()->LocalName);
			ret += "\": ";
			if (Options.TopLevelAsArray) ret += "[ ";
			ret += GetDocumentElement()->ToJsonValue(Options);
			if (Options.TopLevelAsArray) ret += " ]";

			ret += "\n";
			Options.Indentation--;
			Indent(Options, ret);
			ret += "}\n";
			return ret;
		}

		inline XmlElement *XmlDocument::CreateElement(const char *pszLocalName)
		{
			XmlElement *pNewElement = new XmlElement();
			pNewElement->LocalName = pszLocalName;
			return pNewElement;
		}

		inline XmlElement *XmlDocument::GetDocumentElement() { 
			if (!Children.size()) return NULL; 
			if (!Children[0]->IsElement()) return NULL;
			return (XmlElement *)Children[0];
		}

		inline XmlNode* XmlDocument::DeepCopy() 
		{ 
			XmlDocument* pRet = new XmlDocument();
			try
			{
				pRet->SourceLocation = this->SourceLocation;
				for (size_t ii=0; ii < Children.size(); ii++)
				{
					XmlNode* pCopy = Children[ii]->DeepCopy();
					pRet->Children.push_back(pCopy);
				}
				return pRet;
			}		
			catch (...) { delete pRet; throw; }
		}		

		/** XmlElement Implementation **/

		inline XmlElement::XmlElement()
		{			
		}

		inline XmlElement::XmlElement(const string& LocalName)
		{
			this->LocalName = LocalName;
		}

		inline XmlElement::~XmlElement()
		{
			// The XmlElement is responsible for deleting all its attributes.
			for (size_t ii=0; ii < Attributes.size(); ii++) delete Attributes[ii];
			Attributes.clear();
		}

		inline XmlAttribute *XmlElement::FindAttribute(const string& AttrName) const
		{
			for (size_t ii=0; ii < Attributes.size(); ii++)
			{
				if (IsEqual(Attributes[ii]->Name, AttrName)) return Attributes[ii];
			}
			return NULL;
		}

		inline void XmlElement::AddStringAsAttr(const char *pszAttrName, const char *pszValue)
		{
			XmlAttribute *pNewAttr = new XmlAttribute;
			pNewAttr->Name = pszAttrName;
			pNewAttr->Value = pszValue;
			Attributes.push_back(pNewAttr);
		}

		inline void XmlElement::AddStringAsAttr(const char *pszAttrName, const string& Value)
		{
			XmlAttribute *pNewAttr = new XmlAttribute;
			pNewAttr->Name = pszAttrName;
			pNewAttr->Value = Value;
			Attributes.push_back(pNewAttr);
		}				

		inline void XmlElement::AddStringAsText(const char *pszName, const string& Value)
		{
			XmlElement* pNewChild = new XmlElement();
			pNewChild->LocalName = pszName;
			XmlText* pNewText = new XmlText();
			pNewText->Text = Value;
			pNewChild->Children.push_back(pNewText);
			Children.push_back(pNewChild);
		}			

		inline void XmlElement::AddString(const string& Value)
		{
			XmlText* pNewText = new XmlText();
			pNewText->Text = Value;
			Children.push_back(pNewText);
		}		

		inline string XmlElement::ToString(XmlWriterOptions Options) 
		{
			string ret;
			Indent(Options, ret);
			ret += '<';
			ret += LocalName;
			if (!Options.IncludeContent) { ret += '>'; return ret; }
			
			for (size_t ii=0; ii < Attributes.size(); ii++)
			{
				XmlAttribute *pAttr = Attributes[ii];			
				ret += ' ';
				ret += pAttr->Name;
				ret += '='; ret += '\"';
				ret += Escape(Options, pAttr->Value);
				ret += '\"';
			}
						
			if (Children.size() == 0 && Options.AllowSingleTags) ret += '/';
			ret += '>';
			if (Children.size() == 0 && Options.AllowSingleTags) { ret += '\n'; return ret; }
			Options.Indentation ++;
			string ChildContent = XmlNode::ToString(Options);			
			Options.Indentation --;
			if (ChildContent.length() > 40 && ChildContent[0] != '\n') { 
				ret += '\n'; 
				if (ChildContent[0] != '\t') Indent(Options,ret); 
				ret += ChildContent;
				if (ChildContent[ChildContent.length() - 1] != '\n') ret += '\n'; 
				Indent(Options,ret);
			}
			else if (ChildContent[0] == '\t') {
				ret += '\n'; 
				ret += ChildContent;
				if (ChildContent[ChildContent.length() - 1] != '\n') ret += '\n'; 
				Indent(Options,ret);
			}
			else ret += ChildContent;
			ret += '<'; ret += '/';
			ret += LocalName;
			ret += '>';
			ret += '\n';
			return ret;
		}		

		inline string XmlElement::ToJsonValue(JsonWriterOptions Options)
		{
			// First, iterate through the children and collect the result so that we can plan what to do at the higher level.
			string childstr;
			string childtext;
			Options.Indentation++;			// Indent for creation of children representation
			bool NeedsComma = false;
			for (size_t ii=0; ii < Children.size(); ii++)
			{
				XmlNode* pChild = Children[ii];

				// Special handling for element-wrapped XmlText nodes (additional special handling for this happens after this loop):
				if (dynamic_cast<XmlText*>(pChild) != nullptr) { childtext += ((XmlText*)pChild)->Text; continue; }

				// Special case handling for JSON arrays:
				if (pChild->IsElement())
				{
					// First, check if this is an array that was previously handled (first element has already been iterated over):
					bool IsArrayHandled = false;					
					for (size_t jj = 0; jj < ii; jj++)
					{
						XmlElement* pOtherChild = dynamic_cast<XmlElement*>(Children[jj]);
						if (pOtherChild == nullptr) continue;
						if (IsEqual(((XmlElement*)pChild)->LocalName, pOtherChild->LocalName)) { IsArrayHandled = true; break; }
					}
					if (IsArrayHandled) continue;

					// We don't know yet if we have an array, but if we do, pChild is the head of it.  Next, find the length (and implicitly,
					// whether it is an array).
					Options.Indentation++;					// Indent during array (from the '['), although we may not have an array and might discard.
					vector<string> ArrayContents;					
					for (size_t jj = ii + 1; jj < Children.size(); jj++)
					{
						XmlElement* pOtherChild = dynamic_cast<XmlElement*>(Children[jj]);
						if (pOtherChild == nullptr) continue;
						if (IsEqual(((XmlElement*)pChild)->LocalName, pOtherChild->LocalName))
						{
							ArrayContents.push_back(pOtherChild->ToJsonValue(Options));
						}
						else if (!Options.MergeArrays) break;
					}
					// The pChild itself wasn't added to ArrayContents until now to save the computation.  If we're committed to writing it as an array,
					// it needs to be at the front.
					if (ArrayContents.size() || Options.AlwaysArrays)											
						ArrayContents.insert(ArrayContents.begin(), pChild->ToJsonValue(Options));
					Options.Indentation--;					// Wrapping up the array's [], so unindent back to ] level.

					if (ArrayContents.size() || Options.AlwaysArrays)
					{
						if (!Options.MergeArrays)
						{
							// We have an array and have already collected it.  But we need to verify that all entries in this array appear consecutively.  If they don't,
							// we have a problem - this is a case where XML does not translate readily into JSON.
							for (size_t jj = ii + ArrayContents.size(); jj < Children.size(); jj++)
							{
								XmlElement* pOtherChild = dynamic_cast<XmlElement*>(Children[jj]);
								if (pOtherChild == nullptr) continue;
								if (IsEqual(((XmlElement*)pChild)->LocalName, pOtherChild->LocalName))							
									throw NotSupportedException("Cannot convert this XML hierarchy to JSON because a JSON array can only be formed from repeated and consecutive elements of the same name.  In this case, we have non-consecutive occurrances of element <" + pOtherChild->LocalName + ">.");							
							}
						}

						if (NeedsComma) { childstr += ",\n"; NeedsComma = false; }
						Indent(Options, childstr);
						childstr += '\"';
						childstr += Escape(Options, ((XmlElement*)pChild)->LocalName);
						childstr += "\": [\n";
						Options.Indentation++;					// Indent during array (from the '[').
						for (size_t jj=0; jj < ArrayContents.size(); jj++)
						{
							if (NeedsComma) { childstr += ",\n"; NeedsComma = false; }
							Indent(Options, childstr);
							childstr += ArrayContents[jj];
							NeedsComma = true;
						}
						Options.Indentation--;
						if (NeedsComma) childstr += "\n";						
						Indent(Options, childstr);
						childstr += "]";
						NeedsComma = true;
						continue;
					}
				}

				if (NeedsComma) { childstr += ",\n"; Indent(Options, childstr); NeedsComma = false; }
				string substr = pChild->ToJson(Options);
				childstr += substr;
				NeedsComma = true;
			}
			Options.Indentation--;					// Remove the indention we had while writing out the children.
			
			string ret;

			// Additional special case handling for XmlText wrappers:
			if (childtext.length() > 0)
			{
				if (Attributes.size() > 0 || childstr.length() > 0) throw NotSupportedException("Cannot convert Xml that contains a mix of text and Xml elements.  For example, <MyXml>Where I have <b>bold</b> text.</MyXml> has no clear conversion to Json.");
				return "\"" + Escape(Options, childtext) + "\"";
			}

			// At this point, we've collected the text comprising the children, and we did this because we needed to know if we were a text-wrapper element up-front.  But we're not a text wrapper, and
			// the attributes should actually be listed ahead of the children, so now we have to actually form the object from the start and insert the child representation we're saving up.

			ret = "{\n";
			Options.Indentation++;					// Bring back the indention from the children so that we can also do the attributes.

			// Add the attributes:			
			NeedsComma = false;
			for (size_t ii=0; ii < Attributes.size(); ii++)
			{
				if (NeedsComma) { ret += ",\n"; NeedsComma = false; }
				Indent(Options, ret);
				XmlAttribute *pAttr = Attributes[ii];
				ret += '\"';
				ret += Escape(Options, pAttr->Name);
				ret += "\": \"";
				ret += Escape(Options, pAttr->Value);
				ret += "\"";
				NeedsComma = true;
			}			
			
			// Add the child representation and close it up:
			if (NeedsComma && childstr.length() > 0) { ret += ",\n"; NeedsComma = false; }
			ret += childstr;

			Options.Indentation--;					// Take away the attribute (and children) indentation again.

			if (childstr.length() > 0 || Attributes.size() > 0) { ret += "\n"; Indent(Options, ret); }			
			ret += "}";
			return ret;
		}

		inline string XmlElement::ToJson(JsonWriterOptions Options) 
		{
			string ret;
			Indent(Options, ret);
			ret += '\"';
			ret += Escape(Options, LocalName);
			ret += "\": ";
			ret += ToJsonValue(Options);
			return ret;
		}

		inline XmlNode* XmlElement::DeepCopy() 
		{ 
			XmlElement * pRet = new XmlElement();
			try
			{
				pRet->SourceLocation = this->SourceLocation;
				pRet->LocalName = this->LocalName;
				for (size_t ii=0; ii < Children.size(); ii++)
				{
					XmlNode* pCopy = Children[ii]->DeepCopy();
					pRet->Children.push_back(pCopy);
				}
				for (size_t ii=0; ii < Attributes.size(); ii++)
				{
					XmlAttribute* pCopy = new XmlAttribute(*Attributes[ii]);
					try
					{
						pRet->Attributes.push_back(pCopy);
					}
					catch (...) { delete pCopy; throw; }
				}
				return pRet;
			}
			catch (...) { delete pRet; throw; }
		}

		/** Text retrieval conveniences **/

		inline string XmlElement::GetTextAsString() const { 
			string ret;
			for (size_t ii=0; ii < Children.size(); ii++)
			{
				if (Children[ii]->GetType() != XmlNode::Type::Text) throw FormatException("Node contained non-textual content.");
				ret += ((XmlText*)Children[ii])->Text;
			}
			return ret;
		}

		inline int XmlElement::GetTextAsInt32() const {
			Int32 Value;
			if (Int32_TryParse(GetTextAsString().c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException("Xml node " + LocalName + " found but has invalid format.");
		}

		inline unsigned int XmlElement::GetTextAsUInt32() const {			
			UInt32 Value;
			if (UInt32_TryParse(GetTextAsString().c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException("Xml node " + LocalName + " found but has invalid format.");
		}

		inline Int64 XmlElement::GetTextAsInt64() const {
			Int64 Value;
			if (Int64_TryParse(GetTextAsString().c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException("Xml node " + LocalName + " found but has invalid format.");
		}

		inline UInt64 XmlElement::GetTextAsUInt64() const {
			UInt64 Value;
			if (UInt64_TryParse(GetTextAsString().c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException("Xml node " + LocalName + " found but has invalid format.");
		}

		inline float XmlElement::GetTextAsFloat() const {
			float Value;
			if (Float_TryParse(GetTextAsString().c_str(), NumberStyles::Float, Value)) return Value;
			throw FormatException("Xml node " + LocalName + " found but has invalid format.");
		}

		inline double XmlElement::GetTextAsDouble() const {
			double Value;
			if (Double_TryParse(GetTextAsString().c_str(), NumberStyles::Float, Value)) return Value;
			throw FormatException("Xml node " + LocalName + " found but has invalid format.");
		}

		inline bool XmlElement::GetTextAsBool() const {			
			bool Value;
			if (Bool_TryParse(GetTextAsString().c_str(), Value)) return Value;
			throw FormatException("Xml node " + LocalName + " found but has invalid format.");
		}

		/** Attribute retrieval conveniences **/

		inline string XmlElement::GetAttribute(const string& AttrName) const
		{
			XmlAttribute *pAttr = FindAttribute(AttrName);
			if (!pAttr) return "";
			return pAttr->Value;
		}

		inline string XmlElement::GetAttrAsString(const char *pszAttrName, const char *pszDefaultValue /*= _T("")*/) const { 
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return pszDefaultValue;
			return pAttr->Value;
		}

		inline int XmlElement::GetAttrAsInt32(const char *pszAttrName, int lDefaultValue /*= 0*/) const {
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return lDefaultValue;
			Int32 Value;
			if (Int32_TryParse(pAttr->Value.c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException(string("Xml attribute ") + string(pszAttrName) + " found but has invalid format.");
		}

		inline unsigned int XmlElement::GetAttrAsUInt32(const char *pszAttrName, unsigned int lDefaultValue /*= 0*/) const {
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return lDefaultValue;
			UInt32 Value;
			if (UInt32_TryParse(pAttr->Value.c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException(string("Xml attribute ") + string(pszAttrName) + " found but has invalid format.");
		}

		inline Int64 XmlElement::GetAttrAsInt64(const char *pszAttrName, Int64 DefaultValue /*= 0*/) const {
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return DefaultValue;
			Int64 Value;
			if (Int64_TryParse(pAttr->Value.c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException(string("Xml attribute ") + string(pszAttrName) + " found but has invalid format.");
		}

		inline UInt64 XmlElement::GetAttrAsUInt64(const char *pszAttrName, UInt64 DefaultValue /*= 0*/) const {
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return DefaultValue;
			UInt64 Value;
			if (UInt64_TryParse(pAttr->Value.c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException(string("Xml attribute ") + string(pszAttrName) + " found but has invalid format.");
		}

		inline float XmlElement::GetAttrAsFloat(const char *pszAttrName, float DefaultValue /*= 0.0*/) const {
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return DefaultValue;
			float Value;
			if (Float_TryParse(pAttr->Value.c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException(string("Xml attribute ") + string(pszAttrName) + " found but has invalid format.");
		}

		inline double XmlElement::GetAttrAsDouble(const char *pszAttrName, double DefaultValue /*= 0.0*/) const {
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return DefaultValue;
			double Value;
			if (Double_TryParse(pAttr->Value.c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException(string("Xml attribute ") + string(pszAttrName) + " found but has invalid format.");
		}

		inline bool XmlElement::GetAttrAsBool(const char *pszAttrName, bool DefaultValue /*= false*/) const {
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return DefaultValue;
			bool Value;
			if (Bool_TryParse(pAttr->Value.c_str(), Value)) return Value;
			throw FormatException(string("Xml attribute ") + string(pszAttrName) + " found but has invalid format.");
		}

		inline void XmlElement::AddInt32AsAttr(const char *pszAttrName, int Value) { AddStringAsAttr(pszAttrName, Convert::ToString((Int32)Value).c_str()); }
		inline void XmlElement::AddUInt32AsAttr(const char *pszAttrName, unsigned int Value) { AddStringAsAttr(pszAttrName, Convert::ToString((UInt32)Value).c_str()); }
		inline void XmlElement::AddInt64AsAttr(const char *pszAttrName, Int64 Value) { AddStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void XmlElement::AddUInt64AsAttr(const char *pszAttrName, UInt64 Value) { AddStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void XmlElement::AddFloatAsAttr(const char *pszAttrName, float Value) { AddStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void XmlElement::AddDoubleAsAttr(const char *pszAttrName, double Value) { AddStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void XmlElement::AddBoolAsAttr(const char *pszAttrName, bool Value) { AddStringAsAttr(pszAttrName, Convert::ToString(Value)); }

		inline bool XmlElement::IsAttrPresent(const char *pszAttrName) const { return FindAttribute(pszAttrName) != nullptr; }

		inline void	XmlElement::SetStringAsAttr(const char *pszAttrName, const char *pszValue)
		{	
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) { AddStringAsAttr(pszAttrName, pszValue); return; }
			pAttr->Value = pszValue;
		}

		inline void XmlElement::SetInt32AsAttr(const char *pszAttrName, int Value) { SetStringAsAttr(pszAttrName, Convert::ToString((Int32)Value).c_str()); }		
		inline void	XmlElement::SetUInt32AsAttr(const char *pszAttrName, unsigned int Value) { SetStringAsAttr(pszAttrName, Convert::ToString((UInt32)Value).c_str()); }
		inline void	XmlElement::SetInt64AsAttr(const char *pszAttrName, Int64 Value) { SetStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void	XmlElement::SetUInt64AsAttr(const char *pszAttrName, UInt64 Value) { SetStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void	XmlElement::SetFloatAsAttr(const char *pszAttrName, float Value) { SetStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void	XmlElement::SetDoubleAsAttr(const char *pszAttrName, double Value) { SetStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void XmlElement::SetBoolAsAttr(const char *pszAttrName, bool Value) { SetStringAsAttr(pszAttrName, Convert::ToString(Value)); }

		inline void XmlElement::AddInt32AsText(const char *pszName, int Value) { AddStringAsText(pszName, Convert::ToString((Int32)Value)); }
		inline void XmlElement::AddUInt32AsText(const char *pszName, unsigned int Value) { AddStringAsText(pszName, Convert::ToString((UInt32)Value)); }
		inline void XmlElement::AddInt64AsText(const char *pszName, Int64 Value) { AddStringAsText(pszName, Convert::ToString(Value)); }
		inline void XmlElement::AddUInt64AsText(const char *pszName, UInt64 Value) { AddStringAsText(pszName, Convert::ToString(Value)); }
		inline void XmlElement::AddFloatAsText(const char *pszName, float Value) { AddStringAsText(pszName, Convert::ToString(Value)); }
		inline void XmlElement::AddDoubleAsText(const char *pszName, double Value) { AddStringAsText(pszName, Convert::ToString(Value)); }
		inline void XmlElement::AddBoolAsText(const char *pszName, bool Value) { AddStringAsText(pszName, Convert::ToString(Value)); }

		inline void XmlElement::AddInt32(int Value) { AddString(Convert::ToString((Int32)Value)); }
		inline void XmlElement::AddUInt32(unsigned int Value) { AddString(Convert::ToString((UInt32)Value)); }
		inline void XmlElement::AddInt64(Int64 Value) { AddString(Convert::ToString(Value)); }
		inline void XmlElement::AddUInt64(UInt64 Value) { AddString(Convert::ToString(Value)); }
		inline void XmlElement::AddFloat(float Value) { AddString(Convert::ToString(Value)); }
		inline void XmlElement::AddDouble(double Value) { AddString(Convert::ToString(Value)); }
		inline void XmlElement::AddBool(bool Value) { AddString(Convert::ToString(Value)); }

		/** XmlText Implementation **/

		inline void String_ReplaceAll(string& input, char chFind, const char* pszReplace)
		{			
			size_t search_index = 0;
			size_t add_chars = strlen(pszReplace) - 1;
			for(;;)
			{
				size_t index = input.find(chFind, search_index);
				if (index == string::npos) return;
				input = input.substr(0, index) + pszReplace + input.substr(index + 1);
				search_index = index + 1 + add_chars;
			}			
		}
		
		inline string XmlText::ToString(XmlWriterOptions Options) 
		{
			if (!Options.IncludeContent) return "XmlText";

			string ret = Text;
			String_ReplaceAll(ret, '&', "&amp;");		// Note: This substitution must happen first.
			String_ReplaceAll(ret, '\"', "&quot;");
			String_ReplaceAll(ret, '\'', "&apos;");
			String_ReplaceAll(ret, '<', "&lt;");
			String_ReplaceAll(ret, '>', "&gt;");
			return ret;
		}

		inline string XmlText::ToJsonValue(JsonWriterOptions Options) 
		{
			throw NotSupportedException("XmlText must be intercepted by the XmlElement container before the ToJsonValue() call.  XmlText cannot be converted to Json directly, but must be converted as part of the larger Xml structure.");
		}

		inline string XmlText::ToJson(JsonWriterOptions Options) 
		{
			throw NotSupportedException("XmlText must be intercepted by the XmlElement container before the ToJson() call.  XmlText cannot be converted to Json directly, but must be converted as part of the larger Xml structure.");
		}

		inline XmlNode* XmlText::DeepCopy() 
		{ 
			XmlText * pRet = new XmlText();
			try
			{				
				assert (Children.size() == 0);			// XmlText nodes should not have children.			
				pRet->SourceLocation = this->SourceLocation;
				pRet->Text = this->Text;
				return pRet;
			}
			catch (...) { delete pRet; throw; }
		}
	}
}

#endif	// __WBXmlImpl_v4_h__

//	End of XmlImpl.h

