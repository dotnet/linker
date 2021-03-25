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

		public void MarkMatchingExportedType (TypeDefinition typeToMatch, AssemblyDefinition assembly, in DependencyInfo reason)
		{
			if (typeToMatch == null || assembly == null)
				return;

			ModuleDefinition module = assembly.MainModule;
			if (module.GetMatchingExportedType (typeToMatch, out var exportedType)) {
				MarkExportedType (exportedType, module, reason);
				if (_context.Annotations.GetAction (assembly) == AssemblyAction.Copy) {
					TypeReference typeRef = exportedType.AsTypeReference (module);
					MarkForwardedScope (typeRef);
				}
			}
		}

		public void MarkExportedType (ExportedType exportedType, ModuleDefinition module, in DependencyInfo reason)
		{
			if (!_context.Annotations.MarkProcessed (exportedType, reason))
				return;

			_context.Annotations.Mark (module, reason);
		}

		public void MarkForwardedScope (TypeReference typeReference)
		{
			if (typeReference == null)
				return;

			if (typeReference.Scope is AssemblyNameReference) {
				var assembly = _context.Resolve (typeReference.Scope);
				if (assembly != null && assembly.MainModule.GetMatchingExportedType (typeReference.Resolve (), out var exportedType))
					MarkExportedType (exportedType, assembly.MainModule, new DependencyInfo (DependencyKind.ExportedType, typeReference));
			}
		}
	}
}
