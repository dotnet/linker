// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.XPath;

namespace Mono.Linker.Steps
{
	public class LinkAttributesStep : BaseStep
	{
		readonly LinkAttributesParser _parser;

		public LinkAttributesStep (XPathDocument document, string xmlDocumentLocation)
		{
			_parser = new LinkAttributesParser (document, xmlDocumentLocation);
		}

		protected override void Process ()
		{
			_parser.Parse (Context, Context.CustomAttributes.PrimaryAttributeInfo);
		}
	}
}