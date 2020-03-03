using System.Runtime.CompilerServices;

using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
	static class TypeHelper
	{
		public static bool TypesAreEquivalent (TypeDefinition t1, TypeDefinition t2)
		{
			return RuntimeHelpers.Equals (t1, t2);
		}

		public static int GetHashCode (TypeDefinition t) => t.GetHashCode ();

		public static bool FieldsAreEquivalent (FieldDefinition f1, FieldDefinition f2)
		{
			return RuntimeHelpers.Equals (f1, f2);
		}

		public static int GetHashCode (FieldDefinition f) => f.GetHashCode ();
	}
}
