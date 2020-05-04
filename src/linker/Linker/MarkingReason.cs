// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Mono.Linker
{
	//
	// Try to keep the name short and self explamantory at the same time. The name
	// is stored in the output format as text and be better easily understood.
	//
	public enum MarkingReason
	{
		//
		// Entry points to the analysis
		//
		AssemblyAction = 1,
		RootAssembly,
		XmlDescriptor,
		EntryPoint,

		//
		// Membership and containment relationships
		//
		DeclaringType,
		EntireType,
		StructLayout,
		ElementType,
		ModuleType,

		//
		// Member type relationships
		//
		BaseType,
		FieldType,
		ParameterType,
		ReturnType,

		//
		// Override relationships
		//
		BaseMethod,
		Override,
		MethodImplOverride,
		VirtualDeclaringType,
		ExplicitOverride,
		SpecialMethod,

		//
		// Generic type relationships
		//
		GenericInstance,
		GenericArgument,
		ConstraintType,
		ConstraintCtor,
		GenericMethodSpec,
		ModifierType,

		//
		// Modules and assemblies
		//
		TypeScope,
		EntireAssembly,
		AssemblyExport,

		//
		// Grouping of property/event methods
		//
		EventAccessor,

		//
		// Interface implementations
		//
		InterfaceType,

		//
		// Interop methods
		//
		CustomMarshalInfo,
		PInvoke,
		RuntimeMethod,

		//
		// Security info
		//
		SecurityDeclaration,

		//
		// Method body relationships
		//
		CallInstr,
		LdtokenInstr,
		FieldAccessInstr,
		TypeCheckInstr,
		InterfaceTypeOnStack,
		VariableType,
		CatchType,

		//
		// Custom attributes on various providers
		//
		MemberAttribute,        // CA on type member
		ParameterAttribute,     // CA on method parameter
		ReturnTypeAttribute,    // CA on method return type
		GenericParameterAttribute,  // CA on generic parameter
		ConstraintAttribute,    // CA on generic parameter constraint
		BaseInterfaceAttribute, // CA on interface impl
		AssemblyAttribute,      // CA on assembly
		ModuleAttribute,        // CA on module

		//
		// Dependencies of custom attributes
		//
		AttributeConstructor,
		AttributeType,
		AttributeProperty,
		AttributeField,
		AttributeArgumentType,
		AttributeArgumentValue,

		//
		// Static constructors
		//
		StaticCtorFieldAccess,
		StaticCtorMethods,

		//
		// Instantiation dependencies
		//
		TypeIsInstantiated,

		//
		// Linker-specific behavior (preservation hints, patterns, user inputs, linker outputs, etc.)
		//
		DynamicDependency,
		ReflectionBlocked,


		// TODO: Remove and use original reason
		PreservedMethod,
		TypePreserve,

		//
		// Built-in framework features
		//
		Reflection,
		DebuggerDisplay,
		TypeConverter,
		EventTracing,
		Serialization,
		XmlSerialization,
		WebService,
		MulticastDelegate,

		//
		// Generated code dependencies
		//
		StubConversion,
		ThrowConversion,
		UnreachableBody,


		//
		// Special value for cases which need to be ignored
		//
		Hidden = int.MaxValue
	}
}