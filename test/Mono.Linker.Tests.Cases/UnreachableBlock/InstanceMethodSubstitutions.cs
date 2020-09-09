﻿using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupLinkerSubstitutionFile ("InstanceMethodSubstitutions.xml")]
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class InstanceMethodSubstitutions
	{
		[Kept]
		private InstanceMethodSubstitutions ()
		{
		}

		public static void Main ()
		{
			var instance = new InstanceMethodSubstitutions ();
			instance.TestSimpleCallsite ();
			instance.TestCallOnInstance ();
			instance.TestInstanceMethodWithoutSubstitution ();
			instance.TestPropagation ();
			instance.TestStaticPropagation ();
			instance.TestVirtualMethod ();
		}

		bool _isEnabledField;

		[Kept]
		[ExpectBodyModified]
		bool IsEnabled ()
		{
			return _isEnabledField;
		}

		[Kept]
		InstanceMethodSubstitutions GetInstance ()
		{
			return null;
		}

		[Kept]
		[ExpectBodyModified]
		void TestCallOnInstance ()
		{

			if (GetInstance ().IsEnabled ())
				CallOnInstance_NeverReached ();
			else
				CallOnInstance_Reached ();
		}

		void CallOnInstance_NeverReached ()
		{
		}

		[Kept]
		void CallOnInstance_Reached ()
		{
		}

		[Kept]
		[ExpectBodyModified]
		void TestSimpleCallsite ()
		{
			if (IsEnabled ())
				SimpleCallsite_NeverReached ();
			else
				SimpleCallsite_Reached ();
		}

		void SimpleCallsite_NeverReached ()
		{
		}

		[Kept]
		void SimpleCallsite_Reached ()
		{
		}

		[Kept]
		void TestInstanceMethodWithoutSubstitution ()
		{
			if (InstanceMethodWithoutSubstitution ())
				InstanceMethodWithoutSubstitution_Reached1 ();
			else
				InstanceMethodWithoutSubstitution_Reached2 ();
		}

		[Kept]
		bool InstanceMethodWithoutSubstitution ()
		{
			return true;
		}

		[Kept]
		void InstanceMethodWithoutSubstitution_Reached1 ()
		{
		}

		[Kept]
		void InstanceMethodWithoutSubstitution_Reached2 ()
		{
		}

		[Kept]
		void TestPropagation ()
		{
			// Propagation of return value across instance method is not supported
			// (propagation of return value from a method which has call in the body is not supported)
			if (PropagateIsEnabled ())
				Propagation_Reached1 ();
			else
				Propagation_Reached2 ();
		}

		[Kept]
		bool PropagateIsEnabled ()
		{
			return IsEnabled ();
		}

		[Kept]
		void Propagation_Reached1 ()
		{
		}

		[Kept]
		void Propagation_Reached2 ()
		{
		}

		[Kept]
		void TestStaticPropagation ()
		{
			// Propagation of return value across static method is not supported
			// (propagation of return value from a method which has call in the body is not supported)
			if (PropagateStaticIsEnabled ())
				StaticPropagation_Reached1 ();
			else
				StaticPropagation_Reached2 ();
		}

		[Kept]
		private static InstanceMethodSubstitutions _staticInstance;

		[Kept]
		static bool PropagateStaticIsEnabled ()
		{
			return _staticInstance.IsEnabled ();
		}

		[Kept]
		void StaticPropagation_Reached1 ()
		{
		}

		[Kept]
		void StaticPropagation_Reached2 ()
		{
		}

		[Kept]
		void TestVirtualMethod ()
		{
			TestVirtualMethodBase instance = new TestVirtualMethodType ();
			// Virtual method return value inlining not supported
			if (instance.IsEnabled ())
				VirtualMethod_Reached1 ();
			else
				VirtualMethod_Reached2 ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class TestVirtualMethodBase
		{
			[Kept]
			[ExpectBodyModified]
			public virtual bool IsEnabled () { return false; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (TestVirtualMethodBase))]
		class TestVirtualMethodType : TestVirtualMethodBase
		{
			[Kept]
			[ExpectBodyModified]
			public override bool IsEnabled () { return false; }
		}

		[Kept]
		void VirtualMethod_Reached1 ()
		{
		}

		[Kept]
		void VirtualMethod_Reached2 ()
		{
		}
	}
}
