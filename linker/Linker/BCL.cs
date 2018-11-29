using System;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker
{
	public static class BCL
	{
		public static bool IsType (TypeReference type)
		{
			return type.Namespace == "System" && type.Name == "Type";
		}

		public static class Activator {
			public static bool IsCreateInstanceWithType (MethodReference method)
			{
				return method.Parameters.Count >= 1 && IsType (method.Parameters [0].ParameterType);
			}

			public static bool IsCreateInstanceWithStringString (MethodReference method)
			{
				return method.Parameters.Count >= 2 && method.Parameters [0].ParameterType.MetadataType == MetadataType.String && method.Parameters [1].ParameterType.MetadataType == MetadataType.String;
			}

			public static bool IsCreateInstanceWithGeneric (MethodReference method)
			{
				return !method.HasParameters;
			}

			public static bool IsCreateInstance (MethodReference method)
			{
				return method.DeclaringType.Name == "Activator" && method.DeclaringType.Namespace == "System" && method.Name == "CreateInstance";
			}

			public static bool IsUnwrap (MethodReference method)
			{
				return method.Name == "Unwrap" && method.Parameters.Count == 0 && method.DeclaringType.Name == "ObjectHandle" && method.DeclaringType.Namespace == "System.Runtime.Remoting";
			}
			
			public static MethodDefinition [] CollectConstructorsToMarkForActivatorCreateInstanceUsage (TypeDefinition type, bool defaultCtorOnly)
			{
				if (defaultCtorOnly)
					return type.Methods.Where (MethodDefinitionExtensions.IsDefaultConstructor).ToArray ();

				return type.Methods.Where (m => m.IsConstructor).ToArray ();
			}

			public static TypeDefinition ResolveCastType (TypeReference activationCastType)
			{
				if (!activationCastType.IsGenericInstance && !activationCastType.IsGenericParameter)
					return activationCastType.Resolve ();

				if (activationCastType is GenericParameter genericParameter) {
					if (!genericParameter.HasConstraints)
						return null;

					return genericParameter.Constraints [0].Resolve ();
				}

				return null;
			}
		}

		public static class EventTracingForWindows
		{
			public static bool IsEventSourceImplementation (TypeDefinition type, LinkContext context = null)
			{
				if (!type.IsClass)
					return false;

				while (type.BaseType != null) {
					var bt = type.BaseType.Resolve ();

					if (bt == null) {
						if (context != null && !context.IgnoreUnresolved)
							throw new ResolutionException (type.BaseType);

						break;
					}

					if (IsEventSourceType (bt))
						return true;

					type = bt;
				}

				return false;
			}

			public static bool IsEventSourceType (TypeReference type)
			{
				return type.Namespace == "System.Diagnostics.Tracing" && type.Name == "EventSource";
			}

			public static bool IsNonEventAtribute (TypeReference type)
			{
				return type.Namespace == "System.Diagnostics.Tracing" && type.Name == "NonEventAttribute";
			}

			public static bool IsProviderName (string name)
			{
				return name == "Keywords" || name == "Tasks" || name == "Opcodes";
			}
		}

		public static bool IsIDisposableImplementation (MethodDefinition method)
		{
			if (method.Name != "Dispose" || method.ReturnType.MetadataType != MetadataType.Void)
				return false;

			if (method.HasParameters || method.HasGenericParameters || method.IsStatic)
				return false;

			if (!method.IsFinal)
				return false;

			return true;
		}

		public static TypeDefinition FindPredefinedType (string ns, string name, LinkContext context)
		{
			var cache = context.Resolver.AssemblyCache;

			AssemblyDefinition corlib;
			if (cache.TryGetValue ("mscorlib", out corlib))
				return corlib.MainModule.GetType (ns, name);
				
			if (cache.TryGetValue ("System.Private.CoreLib", out corlib))
				return corlib.MainModule.GetType (ns, name);

			return null;
		}
	}
}
