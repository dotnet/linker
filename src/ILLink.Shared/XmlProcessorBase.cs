using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ILLink.Shared
{
	public abstract class XmlProcessorBase
	{
		protected const string FullNameAttributeName = "fullname";
		protected const string LinkerElementName = "linker";
		protected const string TypeElementName = "type";
		protected const string SignatureAttributeName = "signature";
		protected const string NameAttributeName = "name";
		protected const string FieldElementName = "field";
		protected const string MethodElementName = "method";
		protected const string EventElementName = "event";
		protected const string PropertyElementName = "property";
		protected const string AttributeElementName = "attribute";
		protected const string AssemblyElementName = "assembly";
		protected const string ArgumentElementName = "argument";
		protected const string ParameterElementName = "parameter";
		protected const string ReturnElementName = "return";
		protected const string AllAssembliesFullName = "*";

		protected const string XmlNamespace = "";

		protected readonly string _xmlDocumentLocation;
		protected readonly XPathNavigator _document;

		protected XmlProcessorBase (string xmlDocumentLocation, Stream documentStream)
		{
			_xmlDocumentLocation = xmlDocumentLocation;
			using (documentStream) {
				_document = XDocument.Load (documentStream, LoadOptions.SetLineInfo).CreateNavigator ();
			}
		}

		protected static string GetFullName (XPathNavigator nav)
		{
			return GetAttribute (nav, FullNameAttributeName);
		}

		protected static string GetName (XPathNavigator nav)
		{
			var name = GetAttribute (nav, NameAttributeName);
			return name;
			//if (name != null && name != ""){
			//	return name;
			//}
			//var fullname = GetFullName (nav);
			//if (fullname != null) {
			//	var lastDot = fullname.LastIndexOf ('.');
			//	return fullname.Substring (lastDot + 1);
			//}
			//return "";
		}

		protected static string GetSignature (XPathNavigator nav)
		{
			return GetAttribute (nav, SignatureAttributeName);
		}

		protected static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, XmlNamespace);
		}
	}
}
