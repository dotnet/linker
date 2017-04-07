using System;
using System.Collections;
using System.Collections.Generic;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Steps {

	public class MarkStepWithReflectionHeuristics : MarkStep {

		protected ICollection<string> _reflectionHeuristics;

		public MarkStepWithReflectionHeuristics (ICollection<string> reflectionHeuristics)
		{
			_reflectionHeuristics = reflectionHeuristics;
		}

		protected override void MarkInstruction (Instruction instruction)
		{
			base.MarkInstruction (instruction);

			if (instruction.OpCode == OpCodes.Ldtoken) {
				object token = instruction.Operand;
				if (token is TypeReference) {
					TypeDefinition type = ResolveTypeDefinition (GetOriginalType (((TypeReference) token)));
					if (type != null) {
						if (_reflectionHeuristics.Contains ("LdtokenTypeMethods")) {
							MarkMethods (type);
						}
						if (_reflectionHeuristics.Contains  ("LdtokenTypeFields")) {
							MarkFields (type, includeStatic: true);
						}
					}
				}
			}
		}

		protected override void DoAdditionalProcessing()
		{
			if (_reflectionHeuristics.Contains ("InstanceConstructors")) {
				ProcessConstructors ();
			}
		}

		void ProcessConstructors()
		{
			foreach (AssemblyDefinition assembly in _context.GetAssemblies ()) {
				foreach (TypeDefinition type in assembly.MainModule.Types) {
					ProcessConstructors (type);
				}
			}
		}

		void ProcessConstructors(TypeDefinition type)
		{
			if (Annotations.IsMarked (type)) {

				bool hasMarkedConstructors = false;
				bool hasMarkedInstanceMember = false;
				foreach (var method in type.Methods) {
					if (Annotations.IsMarked (method)) {
						if (!method.IsStatic) {
							hasMarkedInstanceMember = true;
						}

						if (IsConstructor (method)) {
							hasMarkedConstructors = true;
						}

						if (hasMarkedInstanceMember && hasMarkedConstructors) {
							break;
						}
					}
				}

				if (!hasMarkedConstructors) {
					if (!hasMarkedInstanceMember) {
						foreach (var field in type.Fields) {
							if (!field.IsStatic && Annotations.IsMarked (field)) {
								hasMarkedInstanceMember = true;
								break;
							}
						}
					}

					if (hasMarkedInstanceMember) {
						MarkMethodsIf (type.Methods, IsConstructorPredicate);
					}
				}

				foreach (var nestedType in type.NestedTypes) {
					ProcessConstructors (nestedType);
				}
			}
		}
	}
}
