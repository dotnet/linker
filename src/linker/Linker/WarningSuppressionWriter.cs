using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Mono.Linker
{
	public class WarningSuppressionWriter
	{
		private readonly LinkContext _context;
		private readonly Dictionary<AssemblyNameDefinition, HashSet<(int, IMemberDefinition)>> _warnings;

		public WarningSuppressionWriter (LinkContext context)
		{
			_context = context;
			_warnings = new Dictionary<AssemblyNameDefinition, HashSet<(int, IMemberDefinition)>> ();
		}

		public void AddWarning ((int, IMemberDefinition) warning)
		{
			var assemblyName = _context.Suppressions.GetModuleFromProvider (warning.Item2).Assembly.Name;
			if (!_warnings.TryGetValue (assemblyName, out var warnings)) {
				warnings = new HashSet<(int, IMemberDefinition)> ();
				_warnings.Add (assemblyName, warnings);
			}

			warnings.Add (warning);
		}

		public void OutputSuppressions ()
		{
			foreach (var assemblyName in _warnings.Keys) {
				using (var sw = new StreamWriter (Path.Combine (_context.OutputDirectory, $"{assemblyName.Name}.WarningSuppressions.cs"))) {
					StringBuilder sb = new StringBuilder ("using System.Diagnostics.CodeAnalysis;").AppendLine ().AppendLine ();
					List<(int, IMemberDefinition)> listOfWarnings = _warnings[assemblyName].ToList ();
					listOfWarnings.Sort ((a, b) => {
						if (a.Item1 == b.Item1)
							return a.Item2.Name.CompareTo (b.Item2.Name);

						return a.Item1.CompareTo (b.Item1);
					});

					foreach (var warning in listOfWarnings) {
						int warningCode = warning.Item1;
						IMemberDefinition warningOrigin = warning.Item2;
						sb.Append ("[assembly: UnconditionalSuppressMessage (\"\", \"IL");
						sb.Append (warningCode).Append ("\", Scope = \"");
						switch (warningOrigin.MetadataToken.TokenType) {
						case TokenType.TypeDef:
							sb.Append ("type\", Target = \"");
							break;
						case TokenType.Method:
						case TokenType.Property:
						case TokenType.Field:
						case TokenType.Event:
							sb.Append ("member\", Target = \"");
							break;
						default:
							break;
						}

						DocumentationSignatureGenerator.Instance.VisitMember (warningOrigin, sb);
						sb.AppendLine ("\")]");
					}

					sw.Write (sb.ToString ());
				}
			}
		}
	}
}
