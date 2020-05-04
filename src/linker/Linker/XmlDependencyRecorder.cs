// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;
using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Mono.Linker
{
	/// <summary>
	/// Class which implements IDependencyRecorder and writes the dependencies into an XML file.
	/// </summary>
	public class XmlDependencyRecorder : IDependencyRecorder, IDisposable
	{
		public const string DefaultDependenciesFileName = "linker-dependencies.xml.gz";

		private readonly LinkContext context;
		private XmlWriter writer;
		private Stream stream;

		readonly static System.Reflection.FieldInfo etModuleFieldInfo =
			typeof (ExportedType).GetField ("module", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		public XmlDependencyRecorder (LinkContext context, string fileName = null)
		{
			this.context = context;

			XmlWriterSettings settings = new XmlWriterSettings {
				Indent = true,
				IndentChars = "\t"
			};

			if (fileName == null)
				fileName = DefaultDependenciesFileName;

			if (string.IsNullOrEmpty (Path.GetDirectoryName (fileName)) && !string.IsNullOrEmpty (context.OutputDirectory)) {
				fileName = Path.Combine (context.OutputDirectory, fileName);
				Directory.CreateDirectory (context.OutputDirectory);
			}

			var depsFile = File.OpenWrite (fileName);

			if (Path.GetExtension (fileName) == ".xml")
				stream = depsFile;
			else
				stream = new GZipStream (depsFile, CompressionMode.Compress);

			writer = XmlWriter.Create (stream, settings);
			writer.WriteStartDocument ();
			writer.WriteStartElement ("dependencies");
			writer.WriteStartAttribute ("version");
			writer.WriteString ("1.3");
			writer.WriteEndAttribute ();
		}

		public void Dispose ()
		{
			if (writer == null)
				return;

			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Flush ();
			writer.Dispose ();
			stream.Dispose ();
			writer = null;
			stream = null;
		}

		public void RecordDependency (object target, in MarkingInfo markingInfo)
		{
			if (target == null)
				throw new ArgumentNullException (nameof (target));

			object source = markingInfo.Source;
			if (source == null)
				throw new ArgumentNullException (nameof (source));

			if (!ShouldRecord (source) && !ShouldRecord (target))
				return;

			if (source != target) {
				writer.WriteStartElement ("edge");
				writer.WriteAttributeString ("b", TokenString (source, out string bAssembly));
				if (bAssembly != null)
					writer.WriteAttributeString ("ba", bAssembly);

				writer.WriteAttributeString ("e", TokenString (target, out string eAssembly));
				if (eAssembly != null && bAssembly != eAssembly)
					writer.WriteAttributeString ("ea", eAssembly);

				writer.WriteAttributeString ("c", markingInfo.Reason.ToString ());
				writer.WriteEndElement ();
			}
		}

		string TokenString (object o, out string assembly)
		{
			switch (o) {
			case MemberReference mr:
				assembly = mr.Module.Assembly.Name.Name;
				return FormatTypeReference (mr.MetadataToken.TokenType, mr);

			case CustomAttribute ca:
				assembly = ca.AttributeType.Module.Assembly.Name.Name;
				return FormatTypeReference (TokenType.CustomAttribute, ca.AttributeType);

			case ExportedType et:
				ModuleDefinition md = (ModuleDefinition) etModuleFieldInfo.GetValue (et);
				assembly = md.Assembly.Name.Name;
				return TokenTypeToCodeID (TokenType.ExportedType) + ":" + et.FullName;

			case IMetadataTokenProvider itp:
				assembly = null;
				return TokenTypeToCodeID (itp.MetadataToken.TokenType) + ":" + o.ToString ();

			case (string str, AssemblyDefinition a):
				assembly = a.Name.Name;
				return "OT:" + str.ToString ();

			default:
				assembly = null;
				return "OT:" + o.ToString ();
			}
		}

		static string FormatTypeReference (TokenType tokenType, MemberReference tr)
		{
			string fullName = tr.FullName;
			if (tr is MemberReference || tr is FieldReference || tr is PropertyReference || tr is EventReference) {
				// Remove return-type
				int rt_idx = fullName.IndexOf (' ');
				fullName = fullName.Substring (rt_idx + 1);
			}

			return TokenTypeToCodeID (tokenType) + ":" + fullName;
		}

		static string TokenTypeToCodeID (TokenType tokenType)
		{
			return tokenType switch
			{
				TokenType.Assembly => "AS",
				TokenType.Module => "MO",
				TokenType.ModuleRef => "MF",
				TokenType.TypeDef => "TD",
				TokenType.TypeSpec => "TS",
				TokenType.Method => "ME",
				TokenType.MethodSpec => "MS",
				TokenType.Field => "FI",
				TokenType.Property => "PR",
				TokenType.CustomAttribute => "CA",
				TokenType.InterfaceImpl => "II",
				TokenType.MemberRef => "MR",
				TokenType.Event => "EV",
				TokenType.ExportedType => "ET",
				_ => throw new NotImplementedException (tokenType.ToString ()),
			};
		}

		bool WillAssemblyBeModified (AssemblyDefinition assembly)
		{
			switch (context.Annotations.GetAction (assembly)) {
			case AssemblyAction.Link:
			case AssemblyAction.AddBypassNGen:
			case AssemblyAction.AddBypassNGenUsed:
				return true;
			default:
				return false;
			}
		}

		bool ShouldRecord (object o)
		{
			if (!context.EnableReducedTracing)
				return true;

			if (o is TypeDefinition t)
				return WillAssemblyBeModified (t.Module.Assembly);

			if (o is IMemberDefinition m)
				return WillAssemblyBeModified (m.DeclaringType.Module.Assembly);

			if (o is TypeReference typeRef) {
				var resolved = typeRef.Resolve ();

				// Err on the side of caution if we can't resolve
				if (resolved == null)
					return true;

				return WillAssemblyBeModified (resolved.Module.Assembly);
			}

			if (o is MemberReference mRef) {
				var resolved = mRef.Resolve ();

				// Err on the side of caution if we can't resolve
				if (resolved == null)
					return true;

				return WillAssemblyBeModified (resolved.DeclaringType.Module.Assembly);
			}

			if (o is ModuleDefinition module)
				return WillAssemblyBeModified (module.Assembly);

			if (o is AssemblyDefinition assembly)
				return WillAssemblyBeModified (assembly);

			if (o is ParameterDefinition parameter) {
				if (parameter.Method is MethodDefinition parameterMethodDefinition)
					return WillAssemblyBeModified (parameterMethodDefinition.DeclaringType.Module.Assembly);
			}

			return true;
		}
	}
}
