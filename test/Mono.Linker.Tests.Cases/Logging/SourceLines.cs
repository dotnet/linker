using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Logging
{
	[SkipKeptItemsValidation]
	[SetupCompileBefore ("FakeSystemAssembly.dll", new[] { "../PreserveDependencies/Dependencies/PreserveDependencyAttribute.cs" })]
	[SetupCompileArgument ("/debug:full")]
	[LogContains ("(38,4): Unrecognized reflection pattern warning IL2006: The return value of method 'System.Type Mono.Linker.Tests.Cases.Logging.SourceLines::GetUnknownType()' " +
		"with dynamically accessed member kinds 'None' is passed into the field 'System.Type Mono.Linker.Tests.Cases.Logging.SourceLines::type' which requires dynamically " +
		"accessed member kinds `Constructors`. To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Constructors'.")]
	[LogContains ("(39,4): Unrecognized reflection pattern warning IL2006: The return value of method 'System.Type Mono.Linker.Tests.Cases.Logging.SourceLines::GetUnknownType()' " +
		"with dynamically accessed member kinds 'None' is passed into the field 'System.Type Mono.Linker.Tests.Cases.Logging.SourceLines::type' which requires dynamically " +
		"accessed member kinds `Constructors`. To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Constructors'.")]
	[LogContains ("(45,3): warning IL2004: Could not resolve 'Mono.Linker.Tests.Cases.Logging.MissingType' type dependency")]
	[LogContains ("(45,3): warning IL2005: Could not resolve dependency member 'MissingMethod' declared in type 'Mono.Linker.Tests.Cases.Logging.SourceLines'")]
	public class SourceLines
	{
		public static void Main ()
		{
			UnrecognizedReflectionPattern ();
			UnresolvedUserDependencies ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Constructors)]
		private static Type type;

		private static Type GetUnknownType ()
		{
			return typeof (SourceLines);
		}

		private static void UnrecognizedReflectionPattern ()
		{
			type = GetUnknownType ();
			type = GetUnknownType ();
		}

		[PreserveDependency ("MissingType", "Mono.Linker.Tests.Cases.Logging.MissingType")]
		[PreserveDependency ("MissingMethod", "Mono.Linker.Tests.Cases.Logging.SourceLines")]
		private static void UnresolvedUserDependencies ()
		{
		}
	}
}
