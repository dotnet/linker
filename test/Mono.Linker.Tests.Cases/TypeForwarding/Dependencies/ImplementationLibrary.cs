﻿using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

//[assembly: AssemblyVersion ("2.0")]

namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies
{
	public class ImplementationLibrary
	{
		public class ImplementationLibraryNestedType
		{
		}

		public static int someField = 42;

		public string GetSomeValue ()
		{
			return "Hello";
		}
	}

	public struct ImplementationStruct
	{
		public int Field;
	}
}
