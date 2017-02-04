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

		inline /*static*/ void XmlNode::Indent(const XmlWriterOptions& Options, string& OnString)
		{
			for (int ii=0; ii < Options.Indentation; ii++) OnString += '\t';
		}

		inline string XmlNode::ToString(XmlWriterOptions Options) override
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

		inline XmlNode *XmlNode::AppendChild(XmlNode *pNewChild)
		{	
			Children.push_back(pNewChild);
			return pNewChild;
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
		
		inline string XmlDocument::ToString(XmlWriterOptions Options) override
		{		
			if (!Options.IncludeContent) return "XmlDocument";
			return XmlNode::ToString(Options);
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
			for (size_t ii=0; ii < Children.size(); ii++)
			{
				XmlNode* pCopy = Children[ii]->DeepCopy();
				pRet->Children.push_back(pCopy);
			}
			return pRet;
		}		

		/** XmlElement Implementation **/

		inline XmlElement::XmlElement()
		{			
		}

		inline XmlElement::~XmlElement()
		{
			// The XmlElement is responsible for deleting all its attributes.
			for (size_t ii=0; ii < Attributes.size(); ii++) delete Attributes[ii];
			Attributes.clear();
		}

		inline XmlAttribute *XmlElement::FindAttribute(const char *pszAttrName) const
		{
			for (size_t ii=0; ii < Attributes.size(); ii++)
			{
				if (IsEqual(Attributes[ii]->Name, pszAttrName)) return Attributes[ii];
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

		inline string XmlElement::ToString(XmlWriterOptions Options) override
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

		inline XmlNode* XmlElement::DeepCopy() 
		{ 
			XmlElement * pRet = new XmlElement();
			for (size_t ii=0; ii < Children.size(); ii++)
			{
				XmlNode* pCopy = Children[ii]->DeepCopy();
				pRet->Children.push_back(pCopy);
			}
			for (size_t ii=0; ii < Attributes.size(); ii++)
			{
				XmlAttribute* pCopy = new XmlAttribute(*Attributes[ii]);
				pRet->Attributes.push_back(pCopy);
			}
			return pRet;
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

		inline string XmlElement::GetAttribute(const char *pszAttrName) const
		{
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
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
			throw FormatException(to_string("Xml attribute ") + to_string(pszAttrName) + " found but has invalid format.");
		}

		inline unsigned int XmlElement::GetAttrAsUInt32(const char *pszAttrName, unsigned int lDefaultValue /*= 0*/) const {
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return lDefaultValue;
			UInt32 Value;
			if (UInt32_TryParse(pAttr->Value.c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException(to_string("Xml attribute ") + to_string(pszAttrName) + " found but has invalid format.");
		}

		inline Int64 XmlElement::GetAttrAsInt64(const char *pszAttrName, Int64 DefaultValue /*= 0*/) const {
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return DefaultValue;
			Int64 Value;
			if (Int64_TryParse(pAttr->Value.c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException(to_string("Xml attribute ") + to_string(pszAttrName) + " found but has invalid format.");
		}

		inline UInt64 XmlElement::GetAttrAsUInt64(const char *pszAttrName, UInt64 DefaultValue /*= 0*/) const {
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return DefaultValue;
			UInt64 Value;
			if (UInt64_TryParse(pAttr->Value.c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException(to_string("Xml attribute ") + to_string(pszAttrName) + " found but has invalid format.");
		}

		inline float XmlElement::GetAttrAsFloat(const char *pszAttrName, float DefaultValue /*= 0.0*/) const {
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return DefaultValue;
			float Value;
			if (Float_TryParse(pAttr->Value.c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException(to_string("Xml attribute ") + to_string(pszAttrName) + " found but has invalid format.");
		}

		inline double XmlElement::GetAttrAsDouble(const char *pszAttrName, double DefaultValue /*= 0.0*/) const {
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return DefaultValue;
			double Value;
			if (Double_TryParse(pAttr->Value.c_str(), NumberStyles::Integer, Value)) return Value;
			throw FormatException(to_string("Xml attribute ") + to_string(pszAttrName) + " found but has invalid format.");
		}

		inline bool XmlElement::GetAttrAsBool(const char *pszAttrName, bool DefaultValue /*= false*/) const {
			XmlAttribute *pAttr = FindAttribute(pszAttrName);
			if (!pAttr) return DefaultValue;
			bool Value;
			if (Bool_TryParse(pAttr->Value.c_str(), Value)) return Value;
			throw FormatException(to_string("Xml attribute ") + to_string(pszAttrName) + " found but has invalid format.");
		}

		inline void XmlElement::AddInt32AsAttr(const char *pszAttrName, int Value) { AddStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void XmlElement::AddUInt32AsAttr(const char *pszAttrName, unsigned int Value) { AddStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
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

		inline void XmlElement::SetInt32AsAttr(const char *pszAttrName, int Value) { SetStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }		
		inline void	XmlElement::SetUInt32AsAttr(const char *pszAttrName, unsigned int Value) { SetStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void	XmlElement::SetInt64AsAttr(const char *pszAttrName, Int64 Value) { SetStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void	XmlElement::SetUInt64AsAttr(const char *pszAttrName, UInt64 Value) { SetStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void	XmlElement::SetFloatAsAttr(const char *pszAttrName, float Value) { SetStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void	XmlElement::SetDoubleAsAttr(const char *pszAttrName, double Value) { SetStringAsAttr(pszAttrName, Convert::ToString(Value).c_str()); }
		inline void XmlElement::SetBoolAsAttr(const char *pszAttrName, bool Value) { SetStringAsAttr(pszAttrName, Convert::ToString(Value)); }

		inline void XmlElement::AddInt32AsText(const char *pszName, int Value) { AddStringAsText(pszName, Convert::ToString(Value)); }
		inline void XmlElement::AddUInt32AsText(const char *pszName, unsigned int Value) { AddStringAsText(pszName, Convert::ToString(Value)); }
		inline void XmlElement::AddInt64AsText(const char *pszName, Int64 Value) { AddStringAsText(pszName, Convert::ToString(Value)); }
		inline void XmlElement::AddUInt64AsText(const char *pszName, UInt64 Value) { AddStringAsText(pszName, Convert::ToString(Value)); }
		inline void XmlElement::AddFloatAsText(const char *pszName, float Value) { AddStringAsText(pszName, Convert::ToString(Value)); }
		inline void XmlElement::AddDoubleAsText(const char *pszName, double Value) { AddStringAsText(pszName, Convert::ToString(Value)); }
		inline void XmlElement::AddBoolAsText(const char *pszName, bool Value) { AddStringAsText(pszName, Convert::ToString(Value)); }

		inline void XmlElement::AddInt32(int Value) { AddString(Convert::ToString(Value)); }
		inline void XmlElement::AddUInt32(unsigned int Value) { AddString(Convert::ToString(Value)); }
		inline void XmlElement::AddInt64(Int64 Value) { AddString(Convert::ToString(Value)); }
		inline void XmlElement::AddUInt64(UInt64 Value) { AddString(Convert::ToString(Value)); }
		inline void XmlElement::AddFloat(float Value) { AddString(Convert::ToString(Value)); }
		inline void XmlElement::AddDouble(double Value) { AddString(Convert::ToString(Value)); }
		inline void XmlElement::AddBool(bool Value) { AddString(Convert::ToString(Value)); }

		/** XmlText Implementation **/

		inline void String_ReplaceAll(string& input, char chFind, const char* pszReplace)
		{			
			size_t index = input.find(chFind);
			while (index != string::npos) input = input.substr(0, index) + pszReplace + input.substr(index + 1);
		}
		
		inline string XmlText::ToString(XmlWriterOptions Options) override
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

		inline XmlNode* XmlText::DeepCopy() 
		{ 
			XmlText * pRet = new XmlText();
			assert (Children.size() == 0);			// XmlText nodes should not have children.			
			pRet->Text = Text;
			return pRet;
		}
	}
}

#endif	// __WBXmlImpl_v4_h__

//	End of XmlImpl.h

