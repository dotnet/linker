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

			_context.Annotations.Mark (module, new DependencyInfo (DependencyKind.ModuleOfExportedType, exportedType));
		}
	}
}
