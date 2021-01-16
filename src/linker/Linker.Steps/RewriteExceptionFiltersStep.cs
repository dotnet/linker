using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps
{
	//
	// This step removes filter clauses by doing the following transformation:
	// catch (Exception ex) when (<cond>) {
	//   ..
	// }
	// ->
	// catch (Exception ex) {
	//   if (!<cond>)
	//	 throw ex;
	//	 ..
	//
	// }
	public class RewriteExceptionFiltersStep : BaseStep
	{
		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			RewriteBodies (assembly.MainModule.Types);
		}

		void RewriteBodies (Collection<TypeDefinition> types)
		{
			foreach (var type in types) {
				if (type.HasMethods) {
					foreach (var method in type.Methods) {
						if (method.HasBody && method.Body.HasExceptionHandlers)
							RewriteBody (method);
					}
				}

				if (type.HasNestedTypes)
					RewriteBodies (type.NestedTypes);
			}
		}

		void RewriteBody (MethodDefinition method)
		{
			if (!(method.HasBody && method.Body.ExceptionHandlers.Any (clause => clause.HandlerType == ExceptionHandlerType.Filter)))
				return;
			var body = method.Body;
			var processor = method.Body.GetILProcessor ();
			foreach (var clause in method.Body.ExceptionHandlers.Where (clause => clause.HandlerType == ExceptionHandlerType.Filter)) {
				var oldStart = clause.FilterStart;
				// Add a dup at the beginning since the catch clause expects the exception object on the stack
				var newStart = Instruction.Create (OpCodes.Dup);
				processor.InsertBefore (clause.FilterStart, newStart);

				// endfilter -> brtrue <catch> + throw
				var endfilter = clause.HandlerStart.Previous;
				if (endfilter.OpCode.Code != Code.Endfilter)
					throw new NotImplementedException (endfilter.ToString ());
				endfilter.OpCode = OpCodes.Brtrue;
				endfilter.Operand = clause.HandlerStart;
				// The exception is on the stack
				var rethrow = Instruction.Create (OpCodes.Throw);
				processor.InsertAfter (endfilter, rethrow);

				clause.HandlerType = ExceptionHandlerType.Catch;
				clause.TryEnd = newStart;
				clause.HandlerStart = newStart;
				clause.CatchType = method.Module.ImportReference (BCL.FindPredefinedType ("System", "Exception", Context));
			}
		}
	}
}
