// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class NullableAnnotations
	{
		struct TestStruct
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
		}

		class TestClass
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }
		}

		public static void Main ()
		{
			TestStruct? a = new TestStruct ();
			PrintProperties (a);
			TestStruct b = new TestStruct ();
			PrintProperties (b);
			TestClass? c = new TestClass ();
			PrintProperties (c);
			TestClass d = new TestClass ();
			PrintProperties (d);
		}

		static void PrintProperties<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> (T instance) 
		{
			Type type = Nullable.GetUnderlyingType (typeof (T)) ?? typeof (T);
			foreach (var p in type.GetProperties ()) Console.WriteLine (p.Name);
		}
	}
}
