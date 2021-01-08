using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupLinkerSubstitutionFile ("BodiesWithSubstitutions.xml")]
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class BodiesWithSubstitutions
	{
		static class ClassWithField
		{
			[Kept]
			public static int SField;
		}

		static int field;

		public static void Main ()
		{
			TestProperty_int_1 ();
			TestField_int_1 ();
			NoInlining ();
			TestPropagation ();
			TestSubstitutionCollision ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestProperty_int_1 ()
		{
			if (Property != 3)
				NeverReached_1 ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestField_int_1 ()
		{
			if (ClassWithField.SField != 9)
				NeverReached_1 ();
		}

		[Kept]
		static int Property {
			[Kept]
			[ExpectBodyModified]
			get {
				return field;
			}
		}

		static void NeverReached_1 ()
		{
		}

		[Kept]
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		static int NoInliningInner ()
		{
			return 1;
		}

		// Methods with NoInlining won't be evaluated by the linker
		[Kept]
		static void NoInlining ()
		{
			if (NoInliningInner () != 1)
				Reached_1 ();
		}

		[Kept]
		static void Reached_1 ()
		{
		}

		[Kept]
		static int PropagateProperty {
			[Kept]
			get {
				return Property;
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestPropagation ()
		{
			// We don't propagate return values across method calls
			if (PropagateProperty != 3)
				Propagation_NeverReached ();
			else
				Propagation_Reached ();
		}

		static void Propagation_NeverReached ()
		{
		}

		[Kept]
		static void Propagation_Reached ()
		{
		}

		[Kept]
		static bool CollisionProperty {
			[Kept]
			[ExpectBodyModified]
			get {
				// Need to call something with constant value to force processing of this method
				_ = Property;
				return true; 
			} // Substitution will set this to false
		}

		// This tests that if there's a method (get_CollisionProperty) which itself is constant
		// and substitution changes its return value, the branch removal reacts to the substituted value
		// and not the value from the method's body.
		// This should ideally never happen, but still.
		// In the original code this test would be order dependent. Depending if TestSubstitutionsCollision
		// was processed before CollisionProperty, it would either propagate true or false.
		[Kept]
		[ExpectBodyModified]
		static void TestSubstitutionCollision ()
		{
			if (CollisionProperty)
				Collision_NeverReached ();
			else
				Collision_Reached ();
		}

		[Kept]
		static void Collision_Reached () { }
		static void Collision_NeverReached () { }
	}
}
