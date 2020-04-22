using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("FeatureSubstitutionsInvalid.xml")]
	[SetupLinkerArgument ("--set-feature", "NoValueFeature", "true")]
	[LogContains ("Feature NoValueFeature does not specify a \"featurevalue\" attribute")]
	[SetupLinkerArgument ("--set-feature", "NonBooleanFeature", "nonboolean")]
	[LogContains ("Unsupported non-boolean feature definition NonBooleanFeature")]
	[SetupLinkerArgument ("--set-feature", "BooleanFeature", "nonboolean")]
	[LogContains ("Boolean feature BooleanFeature was set to a non-boolean value")]
	public class FeatureSubstitutionsInvalid
	{
		public static void Main ()
		{
			NoValueFeatureMethod ();
			NonBooleanFeatureMethod ();
			BooleanFeatureMethod ();
		}

		[Kept]
		static void NoValueFeatureMethod ()
		{
		}

		[Kept]
		static void NonBooleanFeatureMethod ()
		{
		}

		[Kept]
		static void BooleanFeatureMethod ()
		{
		}
	}
}
