// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Linker.Dataflow;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.TestCasesRunner;
using NUnit.Framework;

namespace Mono.Linker.Tests
{
	[NonParallelizable]
	[TestFixture]
	public class ControlFlowGraphTests
	{
		[TestCaseSource (nameof (GetMemberAssertions), new object[] { typeof (ControlFlowGraphTests) })]
		public void TestControlFlowGraph (IMemberDefinition member, CustomAttribute customAttribute)
		{
			// The only intention with these tests is to check that the language elements that could
			// show up in a warning are printed in a way that is friendly to the user.
			if (customAttribute.AttributeType.Name != nameof (ControlFlowGraphAttribute))
				throw new NotImplementedException ();

			var expectedDisplayName = (string) customAttribute.ConstructorArguments[0].Value;

			if (member.MetadataToken.TokenType == TokenType.Method) {
				Assert.AreEqual (expectedDisplayName, ControlFlowGraph.Create ((member as MethodReference).Resolve ().Body).ToString ());
			}
		}

		public static IEnumerable<TestCaseData> GetMemberAssertions (Type type) => MemberAssertionsCollector.GetMemberAssertionsData (type);

		[ControlFlowGraph ("Id: 0, Range: Empty, Predecessors: [] | " +
			"Id: 1, Range: [IL_0000, IL_0001], Predecessors: [0] | " +
			"Id: 2, Range: Empty, Predecessors: [1]")]
		public static void EmptyMethod ()
		{
		}

		[ControlFlowGraph ("Id: 0, Range: Empty, Predecessors: [] | " +
			"Id: 1, Range: [IL_0000, IL_0006], Predecessors: [0] | " +
			"Id: 2, Range: [IL_0008, IL_0014], Predecessors: [1] | " +
			"Id: 3, Range: [IL_0015, IL_0017], Predecessors: [1,2] | " +
			"Id: 4, Range: [IL_0019, IL_001A], Predecessors: [3] | " +
			"Id: 5, Range: Empty, Predecessors: [4]")]
		public static bool BranchIf ()
		{
			var foo = true;

			if (foo == true) {
				Console.WriteLine ("foo");
			}

			return foo;
		}

		[ControlFlowGraph ("Id: 0, Range: Empty, Predecessors: [] | " +
			"Id: 1, Range: [IL_0000, IL_0006], Predecessors: [0] | " +
			"Id: 2, Range: [IL_0008, IL_0015], Predecessors: [1] | " +
			"Id: 3, Range: [IL_0017, IL_0023], Predecessors: [1] | " +
			"Id: 4, Range: [IL_0024, IL_0026], Predecessors: [2,3] | " +
			"Id: 5, Range: [IL_0028, IL_0029], Predecessors: [4] | " +
			"Id: 6, Range: Empty, Predecessors: [5]")]
		public static bool BranchIfElse ()
		{
			var foo = true;

			if (foo == true) {
				Console.WriteLine ("foo");
			} else {
				Console.WriteLine ("bar");
			}

			return foo;
		}

		[ControlFlowGraph ("Id: 0, Range: Empty, Predecessors: [] | " +
			"Id: 1, Range: [IL_0000, IL_0003], Predecessors: [0] | " +
			"Id: 2, Range: [IL_0005, IL_0011], Predecessors: [3] | " +
			"Id: 3, Range: [IL_0012, IL_0018], Predecessors: [1,2] | " +
			"Id: 4, Range: [IL_001A, IL_001A], Predecessors: [3] | " +
			"Id: 5, Range: Empty, Predecessors: [4]")]
		public static void ForLoop ()
		{
			for (var i = 0; i < 5; i++) {
				Console.WriteLine (i);
			}
		}

		[ControlFlowGraph ("Id: 0, Range: Empty, Predecessors: [] | " +
			"Id: 1, Range: [IL_0000, IL_0009], Predecessors: [0] | " +
			"Id: 2, Range: [IL_000B, IL_000B], Predecessors: [1] | " +
			"Id: 3, Range: [IL_000D, IL_000F], Predecessors: [2] | " +
			"Id: 4, Range: [IL_0011, IL_0011], Predecessors: [3] | " +
			"Id: 5, Range: [IL_0013, IL_001A], Predecessors: [1] | " +
			"Id: 6, Range: [IL_001C, IL_0025], Predecessors: [3] | " +
			"Id: 7, Range: [IL_0027, IL_0027], Predecessors: [4] | " +
			"Id: 8, Range: [IL_0029, IL_0029], Predecessors: [5,6,7] | " +
			"Id: 9, Range: Empty, Predecessors: [8]")]
		public static void BranchSwitch ()
		{
			var a = 1;

			switch (a) {
			case 1:
				Console.WriteLine (a);
				break;
			case 2:
				Console.WriteLine (a + 1);
				break;
			default:
				break;
			}
		}

		[ControlFlowGraph ("Id: 0, Range: Empty, Predecessors: [] | " +
			"Id: 1, Range: [IL_0000, IL_0006], Predecessors: [0] | " +
			"Id: 2, Range: [IL_0008, IL_000D], Predecessors: [1] | " +
			"Id: 3, Range: [IL_000E, IL_000E], Predecessors: [1,2] | " +
			"Id: 4, Range: Empty, Predecessors: [3]")]
		public static void NullCoalesce ()
		{
			_ = Console.ReadLine () ?? Console.ReadLine ();
		}

		[ControlFlowGraph ("Id: 0, Range: Empty, Predecessors: [] | " +
			"Id: 1, Range: [IL_0000, IL_0001], Predecessors: [0] | " +
			"Id: 2, Range: [IL_0003, IL_000B], Predecessors: [3] | " +
			"Id: 3, Range: [IL_000D, IL_0014], Predecessors: [1] | " +
			"Id: 4, Range: [IL_0016, IL_0016], Predecessors: [2] | " +
			"Id: 5, Range: Empty, Predecessors: [4]")]
		public static void TestBackwardsEdgeGoto ()
		{
			string str;
			goto ForwardTarget;
		BackwardTarget:
			GetString (str);
			return;

		ForwardTarget:
			str = GetTestString ();
			goto BackwardTarget;
		}

		public static string GetTestString () => "test";

		public static void GetString (string s)
		{

		}

	}
}