﻿using System;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[Reference ("System.Reflection.Emit.dll")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class ComplexConditionsOptimized
	{
		public static void Main ()
		{
			TestSwitch.Test ();
		}

		[Kept]
		class TestSwitch
		{
			static int KnownInteger {
				get => 2;
			}

			[Kept]
			public static void Test ()
			{
				Helper ();
				switch (KnownInteger) {
				case 0:
					Unreached ();
					break;
				case 1:
					Unreached ();
					break;
				case 2:
					Reached ();
					break;
				default: throw new ApplicationException ();
				}
			}

			static void Unreached () { }

			[Kept]
			static void Reached () { }

			[Kept]
			static void Helper () { }
		}
	}
}