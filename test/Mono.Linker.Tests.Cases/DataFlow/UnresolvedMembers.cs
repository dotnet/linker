using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SetupCompileBefore ("UnresolvedLibrary.dll", new[] { "Dependencies/UnresolvedLibrary.cs" })]
	[SetupCompileAfter ("UnresolvedLibrary.dll", new[] { "Dependencies/UnresolvedLibrary.cs" }, defines: new[] { "EXCLUDE_STUFF" })]
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	public class UnresolvedMembers
	{
		static void Main()
		{
			UnresolvedGenericArgument ();
			UnresolvedAttributeArgument ();
			UnresolvedAttributePropertyValue ();
			UnresolvedAttributeFieldValue ();
			UnresolvedObjectGetType ();
			UnresolvedMethodParameter ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class TypeWithUnresolvedGenericArgument<
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T>
		{
		}

		[Kept]
		static void MethodWithUnresolvedGenericArgument<
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{ }

		[Kept]
		[ExpectedWarning ("IL2066", "TypeWithUnresolvedGenericArgument")]
		[ExpectedWarning ("IL2066", nameof (MethodWithUnresolvedGenericArgument))]
		static void UnresolvedGenericArgument ()
		{
			var a = new TypeWithUnresolvedGenericArgument<Dependencies.UnresolvedType> ();
			MethodWithUnresolvedGenericArgument<Dependencies.UnresolvedType> ();
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class AttributeWithRequirements : Attribute
		{
			[Kept]
			[ExpectedWarning ("IL2065", nameof (PropertyWithRequirements))]
			[ExpectedWarning ("IL2064", nameof (FieldWithRequirements))]
			public AttributeWithRequirements (
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{ }

			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public Type PropertyWithRequirements { get; [Kept] set; }

			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public Type FieldWithRequirements;
		}

		[Kept]
		[KeptAttributeAttribute (typeof (AttributeWithRequirements))]
		[AttributeWithRequirements (typeof (Dependencies.UnresolvedType))]
		[ExpectedWarning ("IL2065", nameof (AttributeWithRequirements))]
		static void UnresolvedAttributeArgument ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (AttributeWithRequirements))]
		[AttributeWithRequirements (typeof (EmptyType), PropertyWithRequirements = typeof (Dependencies.UnresolvedType))]
		static void UnresolvedAttributePropertyValue ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (AttributeWithRequirements))]
		[AttributeWithRequirements (typeof (EmptyType), FieldWithRequirements = typeof (Dependencies.UnresolvedType))]
		static void UnresolvedAttributeFieldValue ()
		{
		}

		[Kept]
		static Dependencies.UnresolvedType _unresolvedField;

		[Kept]
		[ExpectedWarning ("IL2072", nameof (Object.GetType))]
		static void UnresolvedObjectGetType ()
		{
			RequirePublicMethods (_unresolvedField.GetType ());
		}

		[Kept]
		[ExpectedWarning ("IL2072", nameof (Object.GetType))]
		static void UnresolvedMethodParameter ()
		{
			RequirePublicMethods (typeof (Dependencies.UnresolvedType));
		}

		[Kept]
		class EmptyType
		{
		}

		[Kept]
		static void RequirePublicMethods (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			Type t)
		{
		}
	}
}
