using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;

namespace Mono.Linker
{
	public class WarningSuppressionWriter
	{
		private readonly LinkContext _context;
		private readonly Dictionary<AssemblyNameDefinition, HashSet<(int, IMemberDefinition)>> _warnings;
		private readonly FileOutputKind _fileOutputKind;

		public WarningSuppressionWriter (LinkContext context,
			FileOutputKind fileOutputKind = FileOutputKind.CSharp)
		{
			_context = context;
			_warnings = new Dictionary<AssemblyNameDefinition, HashSet<(int, IMemberDefinition)>> ();
			_fileOutputKind = fileOutputKind;
		}

		public void AddWarning (int code, IMemberDefinition memberDefinition)
		{
			var assemblyName = _context.Suppressions.GetModuleFromProvider (memberDefinition).Assembly.Name;
			if (!_warnings.TryGetValue (assemblyName, out var warnings)) {
				warnings = new HashSet<(int, IMemberDefinition)> ();
				_warnings.Add (assemblyName, warnings);
			}

			warnings.Add ((code, memberDefinition));
		}

		public void OutputSuppressions ()
		{
			foreach (var assemblyName in _warnings.Keys) {
				if (_fileOutputKind == FileOutputKind.Xml)
					OutputSuppressionsXmlFormat (assemblyName);

				OutputSuppressionsCsFormat (assemblyName);
			}
		}

		void OutputSuppressionsXmlFormat (AssemblyNameDefinition assemblyName)
		{
			var xmlTree =
				new XElement ("linker",
					new XElement ("assembly", new XAttribute ("fullname", assemblyName.FullName)));

			StringBuilder sb = new StringBuilder ();
			foreach (var warning in GetListOfWarnings (assemblyName)) {
				DocumentationSignatureGenerator.Instance.VisitMember (warning.Member, sb);
				xmlTree.Element ("assembly").Add (
					new XElement ("attribute",
						new XAttribute ("fullname", "System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute"),
						new XElement ("argument", Constants.ILLink),
						new XElement ("argument", $"IL{warning.Code}"),
						new XElement ("argument", GetWarningSuppressionScopeString (warning.Member)),
						new XElement ("argument", sb.ToString ())));

				sb.Clear ();
			}

			XDocument xdoc = new XDocument (xmlTree);
			using (var xw = XmlWriter.Create (Path.Combine (_context.OutputDirectory, $"{assemblyName.Name}.WarningSuppressions.xml"),
				new XmlWriterSettings { Indent = true })) {
				xdoc.Save (xw);
			}
		}

		void OutputSuppressionsCsFormat (AssemblyNameDefinition assemblyName)
		{
			using (var sw = new StreamWriter (Path.Combine (_context.OutputDirectory, $"{assemblyName.Name}.WarningSuppressions.cs"))) {
				StringBuilder sb = new StringBuilder ("using System.Diagnostics.CodeAnalysis;").AppendLine ().AppendLine ();
				foreach (var warning in GetListOfWarnings (assemblyName)) {
					sb.Append ("[assembly: UnconditionalSuppressMessage (\"")
						.Append (Constants.ILLink)
						.Append ("\", \"IL").Append (warning.Code)
						.Append ("\", Scope = \"").Append (GetWarningSuppressionScopeString (warning.Member))
						.Append ("\", Target = \"");

					DocumentationSignatureGenerator.Instance.VisitMember (warning.Member, sb);
					sb.AppendLine ("\")]");
				}

				sw.Write (sb.ToString ());
			}
		}

		List<(int Code, IMemberDefinition Member)> GetListOfWarnings (AssemblyNameDefinition assemblyName)
		{
			List<(int Code, IMemberDefinition Member)> listOfWarnings = _warnings[assemblyName].ToList ();
			listOfWarnings.Sort ((a, b) => {
				string lhs = a.Member is MethodReference lhsMethod ? lhsMethod.GetDisplayName () : a.Member.FullName;
				string rhs = b.Member is MethodReference rhsMethod ? rhsMethod.GetDisplayName () : b.Member.FullName;
				if (lhs == rhs)
					return a.Code.CompareTo (b.Code);

				return string.CompareOrdinal (lhs, rhs);
			});

			return listOfWarnings;
		}

		static string GetWarningSuppressionScopeString (IMemberDefinition member)
		{
			switch (member.MetadataToken.TokenType) {
			case TokenType.TypeDef:
				return "type";
			case TokenType.Method:
			case TokenType.Property:
			case TokenType.Field:
			case TokenType.Event:
				return "member";
			}

			return string.Empty;
		}

		public enum FileOutputKind
		{
			CSharp,
			Xml
		};
	}
}
