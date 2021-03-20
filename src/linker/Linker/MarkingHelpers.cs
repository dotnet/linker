using Mono.Cecil;

namespace Mono.Linker
{
	public class MarkingHelpers
	{
		protected readonly LinkContext _context;

		public MarkingHelpers (LinkContext context)
		{
			_context = context;
		}

		public void MarkExportedType (ExportedType exportedType, ModuleDefinition module, in DependencyInfo reason)
		{
			if (!_context.Annotations.MarkProcessed (exportedType, reason))
				return;

			_context.Annotations.Mark (exportedType, reason);
			_context.Annotations.Mark (module, reason);

			AssemblyDefinition facadeAssembly = module.Assembly;
			TypeDefinition resolvedType = exportedType.Resolve ();
			ExportedType forwarder = exportedType.IsForwarder ? exportedType : null;

			if (facadeAssembly != null && forwarder != null) {
				AssemblyAction facadeAction = _context.Annotations.GetAction (facadeAssembly);
				while ((facadeAssembly != resolvedType.Module.Assembly) &&
					facadeAction == AssemblyAction.Copy || _context.KeepTypeForwarderOnlyAssemblies) {
					_context.Annotations.Mark (forwarder, new DependencyInfo (DependencyKind.ExportedType, exportedType));
					_context.Annotations.Mark (module, new DependencyInfo (DependencyKind.ExportedType, exportedType));
					foreach (var assemblyReference in module.AssemblyReferences) {
						if (assemblyReference.MetadataToken == exportedType.Scope.MetadataToken) {
							facadeAssembly = module.AssemblyResolver.Resolve (assemblyReference);
							facadeAction = _context.Annotations.GetAction (facadeAssembly);
							break;
						}
					}

					if (module == facadeAssembly.MainModule)
						break;

					module = facadeAssembly.MainModule;
					// Get the type the current forwarder `forwarder` points to. Note that we cannot simply call resolve on
					// `forwarder` since that would give us the final type T in the (possibly non-empty) chain of forwarders,
					// thus failing to mark any forwarders in copy assemblies between `forwarder` and T. This logic is needed
					// for the following scenario:
					//
					// [copy]          [copy]  [non-copy]
					// `forwarder` --> S ----> U --------> ... ---> T
					if (!module.GetMatchingExportedType (resolvedType, out forwarder) || !forwarder.IsForwarder)
						break;
				}

				// Mark the last type that a forwarder in a copy assembly pointed to (U in the above example.)
				if (forwarder != null)
					_context.Annotations.Mark (forwarder, new DependencyInfo (DependencyKind.ExportedType, resolvedType));
				
				_context.Annotations.Mark (module, new DependencyInfo (DependencyKind.ExportedType, resolvedType));
			}
		}
	}
}
