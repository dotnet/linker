using System;

namespace Mono.Linker
{
	public enum DependencyKind {

	// Entry points to the analysis
		AssemblyAction, // assembly action -> entry assembly
		RootAssembly, // assembly -> entry type
		XmlDescriptor, // xml document -> entry member
		// Attributes on assemblies are marked whether or not we keep
		// the assembly, so mark these as entry points.
		AssemblyOrModuleAttribute, // assembly/module -> entry attribute

	// Membership and containment relationships
		NestedType, // parent type -> nested type
		MemberOfType, // type -> member
		DeclaringType, // member -> type
		FieldOnGenericInstance, // fieldref on instantiated generic -> field on generic typedef
		MethodOnGenericInstance, // methodref on instantiated generic -> method on generic typedef

	// Type relationships
		BaseType, // type -> its base type
		FieldType, // field -> its type
		ParameterType, // method -> types of its parameters
		ReturnType, // method -> type it returns
		VariableType, // method -> types of its variables
		CatchType, // method -> types of its exception handlers

	// Override relationships
		BaseMethod, // override -> base method
		Override, // base method -> override
		MethodImplOverride, // method -> .override on the method
		VirtualNeededDueToPreservedScope, // type -> virtuals kept because scope requires it
		MethodForInstantiatedType, // type -> methods kept because type is instantiated or scope requires it
		BaseDefaultCtorForStubbedMethod, // stubbed method -> default ctor of base type

	// Generic type relationships
		GenericArgumentType, // generic instance -> argument type
		GenericParameterConstraintType, // generic typedef/methoddef -> parameter constraint type
		DefaultCtorForNewConstrainedGenericArgument, // generic instance -> argument ctor
		ElementType, // generic type instantiation -> generic typedef
		ElementMethod, // generic method instantiation -> generic methoddef
		ModifierType, // modified type -> type modifier

	// Modules and assemblies
		ScopeOfType, // type -> module/assembly
		TypeInAssembly, // assembly -> type
		ModuleOfExportedType, // exported type -> module
		ExportedType, // type -> exported type

	// Grouping of property/event methods
		PropertyOfPropertyMethod, // property method -> its property
		EventOfEventMethod, // event method -> its event
		EventMethod, // event -> its event methods
		// PropertyMethod doesn't exist because property methods aren't always marked for a property

	// Interface implementations
		InterfaceImplementationInterfaceType, // interfaceimpl -> interface type
		InterfaceImplementationOnType, // type -> interfaceimpl on it

	// Interop methods
		ReturnTypeMarshalSpec, // interop method -> marshal spec of its return type
		ParameterMarshalSpec, // interop method -> marshal spec of its parameters
		FieldMarshalSpec, // field -> its marshal spec
		InteropMethodDependency, // interop method -> required members of its parameters, return type, declaring type

	// Dependencies created by instructions
		DirectCall, // method -> method
		VirtualCall, // method -> method
		Ldvirtftn, // method -> method
		Ldftn, // method -> method
		Newobj, // method -> method
		Ldtoken, // method -> member referenced
		FieldAccess, // method -> field (for instructions that load/store fields)
		InstructionTypeRef, // other instructions that have an inline type token (method -> type)

	// Custom attributes on various providers
		CustomAttribute, // attribute provider (type/field/method/etc...) -> attribute on it
		ParameterAttribute, // method parameter -> attribute on it
		ReturnTypeAttribute, // method return type -> attribute on it
		GenericParameterCustomAttribute, // generic parameter -> attribute on it
		GenericParameterConstraintCustomAttribute, // generic parameter constraint -> attribute on it

	// Dependencies of custom attributes
		AttributeConstructor, // attribute -> its ctor
		// used for security attributes, where we mark the type/properties directly
		AttributeType, // attribute -> attribute type
		AttributeProperty, // attribute -> attribute property
		CustomAttributeArgumentType, // attribute -> type of an argument to the attribute ctor
		CustomAttributeArgumentValue, // attribute -> type passed as an argument to the attribute ctor
		CustomAttributeField, // attribute -> field on the attribute

	// Tracking cctors
		TriggersCctorThroughFieldAccess, // field-accessing method -> cctor of field's declaring type
		TriggersCctorForCalledMethod, // caller method -> callee method type cctor
		DeclaringTypeOfCalledMethod, // called method -> its declaring type (used to track when a cctor is triggered by a method call)
		CctorForType, // type -> cctor of type
		CctorForField, // field -> cctor of field's declaring type

	// Tracking instantiations
		InstantiatedByCtor, // ctor -> its declaring type (indicating that it was marked instantiated due to the ctor)
		OverrideOnInstantiatedType, // instantiated type -> override method on the type

	// Linker-specific behavior (preservation hints, patterns, user inputs, linker outputs, etc.)
		PreservedDependency, // PreserveDependency attribute -> member
		AccessedViaReflection, // method -> detected member accessed via reflection from that method
		PreservedMethod, // type/method -> preserved method (explicitly preserved in Annotations by XML or other steps)
		TypePreserve, // type -> field/method preserved for the type (explicitly set in Annotations by XML or other steps)
		DisablePrivateReflection, // type/method -> DisablePrivateReflection attribute added by linkerf

	// Built-in knowledge of special runtime/diagnostic subsystems
		// XmlSchemaProvider, DebuggerDisplay, DebuggerTypeProxy, SoapHeader, TypeDescriptionProvider
		ReferencedBySpecialAttribute, // attribute -> referenced members
		KeptForSpecialAttribute, // attribute -> kept members (used when the members are not explicitly referenced)
		SerializationMethodForType, // type -> method required for serialization
		EventSourceProviderField, // EventSource derived type -> fields on nested Keywords/Tasks/Opcodes provider classes
		MethodForSpecialType, // type -> methods kept (currently used for MulticastDelegate)

	// Linker internals, requirements for certain optimizations
		UnreachableBodyRequirement, // method -> well-known type required for unreachable bodies optimization
		DisablePrivateReflectionRequirement, // null -> DisablePrivateReflectionAttribute type/methods (note that no specific source is reported)
		AlreadyMarked, // null -> member that has already been marked for a particular reason (used to propagate reasons internally, not reported)

	// For use when we don't care about tracking a particular dependency
		Unspecified, // currently unused
	}

	readonly public struct DependencyInfo : IEquatable<DependencyInfo> {
		public DependencyKind Kind { get; }
		public object Source { get; }
		public DependencyInfo (DependencyKind kind, object source) => (Kind, Source) = (kind, source);
		public DependencyInfo (DependencyKind kind) => (Kind, Source) = (kind, default);

		public bool Equals (DependencyInfo other) => (Kind, Source) == (other.Kind, other.Source);
		public override bool Equals (Object obj) => obj is DependencyInfo info && this.Equals (info);
		public override int GetHashCode() => (Kind, Source).GetHashCode ();
		public static bool operator == (DependencyInfo lhs, DependencyInfo rhs) => lhs.Equals (rhs);
		public static bool operator != (DependencyInfo lhs, DependencyInfo rhs) => !lhs.Equals (rhs);
	}
}
