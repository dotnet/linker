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

			_context.Annotations.Mark (module, reason);
		}

		public void MarkForwardedScope (TypeReference typeReference)
		{
			ModuleDefinition module = typeReference.Module;
			foreach (var assemblyRef in module.AssemblyReferences)
				if (assemblyRef.MetadataToken == typeReference.Scope.MetadataToken) {
					AssemblyDefinition typeRefAssembly = module.AssemblyResolver.Resolve (assemblyRef);
					if (typeRefAssembly.MainModule.GetMatchingExportedType (typeReference.Resolve (), out var exportedType))
						MarkExportedType (exportedType, typeRefAssembly.MainModule, new DependencyInfo (DependencyKind.ExportedType, typeReference));

					break;
				}
		}
	}
}
