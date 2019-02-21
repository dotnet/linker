using System;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Steps {
	public class CodeRewriterStep : BaseStep
	{
		AssemblyDefinition assembly;

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			this.assembly = assembly;

			foreach (var type in assembly.MainModule.Types)
				ProcessType (type);
		}

		void ProcessType (TypeDefinition type)
		{
			ProcessMethods (type.Methods);

			foreach (var nested in type.NestedTypes)
				ProcessType (nested);
		}

		void ProcessMethods (IList<MethodDefinition> methods)
		{
			for (int i = 0; i < methods.Count; i++) {
				if (Annotations.GetAction (methods[i]) == MethodAction.Delete)
					methods.RemoveAt (i--);
				else if (methods[i].HasBody)
					ProcessMethod (methods[i]);
			}
		}

		void ProcessMethod (MethodDefinition method)
		{
			switch (Annotations.GetAction (method)) {
			case MethodAction.ConvertToStub:
				RewriteBodyToStub (method);
				break;
			case MethodAction.ConvertToThrow:
				RewriteBodyToLinkedAway (method);
				break;
			case MethodAction.ConvertToThrowNull:
				// We only use this for internal framework code.
				RewriteBodyToThrowNull (method);
				break;
			case MethodAction.ConvertToReturn:
				RewriteBodyToReturn (method);
				break;
			}
		}

		void RewriteBodyToLinkedAway (MethodDefinition method)
		{
			method.ImplAttributes &= ~(MethodImplAttributes.AggressiveInlining | MethodImplAttributes.Synchronized);
			method.ImplAttributes |= MethodImplAttributes.NoInlining;

			method.Body = CreateThrowLinkedAwayBody (method);
			ClearDebugInformation (method);
		}

		void RewriteBodyToThrowNull (MethodDefinition method)
		{
			method.ImplAttributes &= ~(MethodImplAttributes.AggressiveInlining | MethodImplAttributes.Synchronized);
			method.ImplAttributes |= MethodImplAttributes.NoInlining;

			method.Body = CreateThrowNullBody (method);
			ClearDebugInformation (method);
		}

		void RewriteBodyToStub (MethodDefinition method)
		{
			if (!method.IsIL)
				throw new NotImplementedException ();

			method.Body = CreateStubBody (method);

			ClearDebugInformation (method);
		}

		void RewriteBodyToReturn (MethodDefinition method)
		{
			if (!method.IsIL)
				throw new NotImplementedException ();

			method.Body = CreateReturnBody (method);

			ClearDebugInformation (method);
		}

		MethodBody CreateThrowLinkedAwayBody (MethodDefinition method)
		{
			var body = new MethodBody (method);
			var il = body.GetILProcessor ();

			// import the method into the current assembly
			var ctor = Context.MarkedKnownMembers.NotSupportedExceptionCtorString;
			ctor = assembly.MainModule.ImportReference (ctor);

			il.Emit (OpCodes.Ldstr, "Linked away");
			il.Emit (OpCodes.Newobj, ctor);
			il.Emit (OpCodes.Throw);
			return body;
		}

		MethodBody CreateThrowNullBody (MethodDefinition method)
		{
			var body = new MethodBody (method);
			var il = body.GetILProcessor ();

			il.Emit (OpCodes.Ldnull);
			il.Emit (OpCodes.Throw);
			return body;
		}

		MethodBody CreateStubBody (MethodDefinition method)
		{
			var body = new MethodBody (method);

			if (method.HasParameters && method.Parameters.Any (l => l.IsOut))
				throw new NotImplementedException ();

			var il = body.GetILProcessor ();
			if (method.IsInstanceConstructor ()) {
				var base_ctor = GetDefaultInstanceConstructor (method.DeclaringType.BaseType);
				base_ctor = assembly.MainModule.ImportReference (base_ctor);

				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Call, base_ctor);
			}

			switch (method.ReturnType.MetadataType) {
			case MetadataType.Void:
				break;
			case MetadataType.Boolean:
				il.Emit (OpCodes.Ldc_I4_0);
				break;
			default:
				throw new NotImplementedException (method.ReturnType.FullName);
			}

			il.Emit (OpCodes.Ret);
			return body;
		}

		MethodBody CreateReturnBody (MethodDefinition method)
		{
			var body = new MethodBody (method);
			var il = body.GetILProcessor ();
			switch (method.ReturnType.MetadataType) {
			case MetadataType.Boolean:
				il.Emit (OpCodes.Ldc_I4_0);
				break;
			case MetadataType.Class:
			case MetadataType.Object:
				il.Emit (OpCodes.Ldnull);
				break;
			case MetadataType.Void:
				break;
			default:
				throw new NotImplementedException ();
			}
			il.Emit (OpCodes.Ret);
			return body;
		}

		static MethodReference GetDefaultInstanceConstructor (TypeReference type)
		{
			foreach (var m in type.GetMethods ()) {
				if (m.HasParameters)
					continue;

				var definition = m.Resolve ();
				if (!definition.IsDefaultConstructor ())
					continue;

				return m;
			}

			throw new NotImplementedException ();
		}

		static void ClearDebugInformation (MethodDefinition method)
		{
			// TODO: This always allocates, update when Cecil catches up
			var di = method.DebugInformation;
			di.SequencePoints.Clear ();
			if (di.Scope != null) {
				di.Scope.Variables.Clear ();
				di.Scope.Constants.Clear ();
				di.Scope = null;
			}
		}
	}
}
