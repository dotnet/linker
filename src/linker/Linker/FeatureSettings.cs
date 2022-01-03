// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.XPath;
using ILLink.Shared;

namespace Mono.Linker
{
	public static class FeatureSettings
	{
		public static bool ShouldProcessElement (XPathNavigator nav, LinkContext context, string documentLocation)
		{
			var feature = GetAttribute (nav, "feature");
			if (string.IsNullOrEmpty (feature))
				return true;

			var value = GetAttribute (nav, "featurevalue");
			if (string.IsNullOrEmpty (value)) {
				context.LogError (DiagnosticId.XmlFeatureDoesNotSpecifyFeatureValue, args: new string[] { documentLocation, feature });
				return false;
			}

			if (!bool.TryParse (value, out bool bValue)) {
				context.LogError (DiagnosticId.XmlUnsupportedNonBooleanValueForFeature, args: new string[] { documentLocation, feature });
				return false;
			}

			var isDefault = GetAttribute (nav, "featuredefault");
			bool bIsDefault = false;
			if (!string.IsNullOrEmpty (isDefault) && (!bool.TryParse (isDefault, out bIsDefault) || !bIsDefault)) {
				context.LogError (DiagnosticId.XmlDocumentLocationHasInvalidFeatureDefault, args: documentLocation);
				return false;
			}

			if (!context.FeatureSettings.TryGetValue (feature, out bool featureSetting))
				return bIsDefault;

			return bValue == featureSetting;
		}

		public static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, String.Empty);
		}
	}
}
