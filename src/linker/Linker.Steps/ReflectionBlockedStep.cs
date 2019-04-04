using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

using System.Runtime.CompilerServices;

namespace Mono.Linker.Steps {
	public class ReflectionBlockedStep : BaseStep
	{
		AssemblyDefinition assembly;
		MethodReference noOptAttr;

		static MethodDefinition _reflectionMethod;

		public static MethodDefinition GetReflectionBlockedAttr (LinkContext context) {
			if (_reflectionMethod != null)
				return _reflectionMethod;

			TypeDefinition methodImpl = BCL.FindPredefinedType("System.Runtime.CompilerServices", "ReflectionBlockedAttribute", context);
			if (methodImpl == null)
				throw new Exception("Could not find System.Runtime.CompilerServices.ReflectionBlockedAttribute in BCL.");

			foreach (var ref_method in methodImpl.Methods)
			{
				if (!ref_method.IsConstructor)
					continue;
				if (ref_method.Parameters.Count != 0)
					continue;
				_reflectionMethod = ref_method;
			}

			return _reflectionMethod;
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			this.assembly = assembly;
			this.noOptAttr = assembly.MainModule.ImportReference (GetReflectionBlockedAttr (Context));
			if (noOptAttr == null)
				throw new Exception("Could not import System.Runtime.CompilerServices.ReflectionBlockedAttribute in BCL.");

			foreach (var type in assembly.MainModule.Types)
				ProcessType (type);
		}

		void ProcessType (TypeDefinition type)
		{
			bool canAnnotateAll = true;
			List<MethodDefinition> canAnnotate = new List<MethodDefinition> ();

			foreach (var method in type.Methods) {
				// Public methods have non-visible call sites by default, this attribute doesn't
				// help us at all.
				//
				// See mono_aot_can_specialize in aot-compiler.c in mono
				if (!method.IsPrivate)
					continue;

				if (!method.HasBody)
					continue;

				if (Annotations.HasUnseenCallers (method)) {
					canAnnotateAll = false;
					continue;
				}

				canAnnotate.Add (method);
			}

			if (canAnnotateAll) {
				AnnotateType (type);
			} else {
				foreach (var method in canAnnotate)
					AnnotateMethod (method);
			}

			foreach (var nested in type.NestedTypes)
				ProcessType (nested);
		}

		void AnnotateType (TypeDefinition type)
		{
			var cattr = new CustomAttribute (noOptAttr);
			type.CustomAttributes.Add (cattr);
			Annotations.Mark (cattr);
		}

		void AnnotateMethod (MethodDefinition method)
		{
			var cattr = new CustomAttribute (noOptAttr);
			method.CustomAttributes.Add (cattr);
			Annotations.Mark (cattr);
		}
	}
}
