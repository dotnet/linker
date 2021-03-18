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

			AssemblyDefinition facadeAssembly = module.Assembly;
			TypeDefinition resolvedType = exportedType.Resolve ();
			AssemblyAction facadeAction = _context.Annotations.GetAction (facadeAssembly);
			if (facadeAssembly != null && resolvedType != null) {
				while ((facadeAssembly != resolvedType.Module.Assembly) &&
						facadeAction == AssemblyAction.Copy || facadeAction == AssemblyAction.CopyUsed) {
					_context.Annotations.Mark (module, new DependencyInfo (DependencyKind.ModuleOfExportedType, exportedType));
					foreach (var assemblyReference in module.AssemblyReferences) {
						if (assemblyReference.MetadataToken == exportedType.Scope.MetadataToken) {
							facadeAssembly = module.AssemblyResolver.Resolve (assemblyReference);
							facadeAction = _context.Annotations.GetAction(facadeAssembly);
							break;
						}
					}

					if (module == facadeAssembly.MainModule)
						break;

					module = facadeAssembly.MainModule;
				}
			}

			_context.Annotations.Mark (module, new DependencyInfo (DependencyKind.ModuleOfExportedType, exportedType));
		}
	}
}
