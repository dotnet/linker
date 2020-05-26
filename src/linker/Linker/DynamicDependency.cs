using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

#nullable enable

namespace Mono.Linker
{
	/// Tracks dependencies created via DynamicDependencyAttribute in the linker.
	/// This is almost identical to DynamicDependencyAttribute, but it holds a
	/// TypeReference instead of a Type, and it has a reference to the original
	/// CustomAttribute for dependency tracing.
	internal class DynamicDependency
	{
		public CustomAttribute? OriginalAttribute { get; set; }
		public DynamicDependency (string memberSignature)
		{
			MemberSignature = memberSignature;
		}

		public DynamicDependency (string memberSignature, TypeReference type)
		{
			MemberSignature = memberSignature;
			Type = type;
		}

		public DynamicDependency (string memberSignature, string typeName, string assemblyName)
		{
			MemberSignature = memberSignature;
			TypeName = typeName;
			AssemblyName = assemblyName;
		}

		public DynamicDependency (DynamicallyAccessedMemberTypes memberTypes, TypeReference type)
		{
			MemberTypes = memberTypes;
			Type = type;
		}

		public DynamicDependency (DynamicallyAccessedMemberTypes memberTypes, string typeName, string assemblyName)
		{
			MemberTypes = memberTypes;
			TypeName = typeName;
			AssemblyName = assemblyName;
		}

		public string? MemberSignature { get; }

		public DynamicallyAccessedMemberTypes MemberTypes { get; }

		public TypeReference? Type { get; }

		public string? TypeName { get; }

		public string? AssemblyName { get; }

		public string? Condition { get; set; }
	}
}