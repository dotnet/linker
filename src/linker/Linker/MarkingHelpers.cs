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

			if (reason.Kind == DependencyKind.ModuleOfExportedType) {
				var facadeAssembly = (reason.Source as ModuleDefinition).Assembly;
				TypeDefinition resolvedType = exportedType.Resolve ();
				while (facadeAssembly.FullName != resolvedType.Module.Assembly.FullName) {
					_context.Annotations.Mark (module, new DependencyInfo (DependencyKind.ModuleOfExportedType, exportedType));
					foreach (var assemblyReference in module.AssemblyReferences) {
						if (assemblyReference.MetadataToken == exportedType.Scope.MetadataToken) {
							facadeAssembly = module.AssemblyResolver.Resolve (assemblyReference);
							module = facadeAssembly.MainModule;
							break;
						}
					}
				}

				return;
			}

			if (_context.KeepTypeForwarderOnlyAssemblies)
				_context.Annotations.Mark (module, new DependencyInfo (DependencyKind.ModuleOfExportedType, exportedType));
		}
	}
}
