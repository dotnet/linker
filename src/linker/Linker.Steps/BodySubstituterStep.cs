using System.Xml.XPath;

namespace Mono.Linker.Steps
{
	public class BodySubstituterStep : BaseStep
	{
		readonly BodySubstitutionParser _parser;

		public BodySubstituterStep (XPathDocument document, string xmlDocumentLocation)
		{
			_parser = new BodySubstitutionParser (document, xmlDocumentLocation);
		}

		protected override void Process ()
		{
			_parser.Parse (Context, Context.Annotations.MemberActions.GlobalSubstitutionInfo);
		}
	}
}
