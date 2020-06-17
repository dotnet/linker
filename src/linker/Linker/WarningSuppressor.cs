using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.Cecil;

namespace Mono.Linker
{
	public class WarningSuppressor
	{
		private readonly LinkContext _context;
		private readonly string _warningSuppressionFile;
		private readonly List<(int, IMemberDefinition)> _warnings;

		public WarningSuppressor (LinkContext context, string warningSuppressionFile)
		{
			_context = context;
			_warningSuppressionFile = warningSuppressionFile;
			_warnings = new List<(int, IMemberDefinition)> ();
		}

		public void AddWarning ((int, IMemberDefinition) warning)
		{
			_warnings.Add (warning);
		}

		public void OutputSuppressions ()
		{
			if (_warnings.Count == 0) {
				_context.LogMessage ("");
			}

			using (var sw = new StreamWriter (Path.Combine (_context.OutputDirectory, _warningSuppressionFile))) {
				StringBuilder sb = new StringBuilder ("using System.Diagnostics.CodeAnalysis;").AppendLine ().AppendLine ();
				foreach (var warning in _warnings) {
					int warningCode = warning.Item1;
					IMemberDefinition warningOrigin = warning.Item2;
					sb.Append ("[module: UnconditionalSuppressMessage(\"\", \"IL");
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
					sb.AppendLine("\")]");
				}

				sw.Write (sb.ToString ());
			}
		}
	}
}
