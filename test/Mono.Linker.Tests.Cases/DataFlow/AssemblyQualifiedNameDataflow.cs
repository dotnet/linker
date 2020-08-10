﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
	[SkipKeptItemsValidation]
	class AssemblyQualifiedNameDataflow
	{
		static void Main ()
		{
			TestPublicParameterlessConstructor ();
			TestPublicConstructors ();
			TestConstructors ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (AssemblyQualifiedNameDataflow), nameof (RequirePublicConstructors), new Type[] { typeof (string) }, messageCode: "IL3000")]
		[UnrecognizedReflectionAccessPattern (typeof (AssemblyQualifiedNameDataflow), nameof (RequireNonPublicConstructors), new Type[] { typeof (string) }, messageCode: "IL3000")]
		static void TestPublicParameterlessConstructor ()
		{
			string type = GetTypeWithPublicParameterlessConstructor ().AssemblyQualifiedName;
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (AssemblyQualifiedNameDataflow), nameof (RequireNonPublicConstructors), new Type[] { typeof (string) }, messageCode: "IL3000")]
		static void TestPublicConstructors ()
		{
			string type = GetTypeWithPublicConstructors ().AssemblyQualifiedName;
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (AssemblyQualifiedNameDataflow), nameof (RequirePublicParameterlessConstructor), new Type[] { typeof (string) }, messageCode: "IL3000")]
		[UnrecognizedReflectionAccessPattern (typeof (AssemblyQualifiedNameDataflow), nameof (RequirePublicConstructors), new Type[] { typeof (string) }, messageCode: "IL3000")]
		static void TestConstructors ()
		{
			string type = GetTypeWithNonPublicConstructors ().AssemblyQualifiedName;
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		private static void RequirePublicParameterlessConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			string type)
		{
		}

		private static void RequirePublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			string type)
		{
		}

		private static void RequireNonPublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			string type)
		{
		}

		private static void RequireNothing (string type)
		{
		}


		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		private static Type GetTypeWithPublicParameterlessConstructor ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		private static Type GetTypeWithPublicConstructors ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		private static Type GetTypeWithNonPublicConstructors ()
		{
			return null;
		}

	}
}
