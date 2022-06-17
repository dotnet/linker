// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// Tracer.cs
//
// Copyright (C) 2017 Microsoft Corporation (http://www.microsoft.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using Mono.Cecil;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mono.Linker
{
	/// <summary>
	/// Class which implements IDependencyRecorder and writes the dependencies into an DGML file.
	/// </summary>
	public class DgmlDependencyRecorder : IDependencyRecorder, IDisposable
	{
		public const string DefaultDependenciesFileName = "linker-dependencies.dgml.gz";


		private readonly LinkContext context;
		private XmlWriter? writer;
		private Stream? stream;

		public DgmlDependencyRecorder (LinkContext context, string? fileName = null)
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

			if (Path.GetExtension (fileName) == ".dgml")
				stream = depsFile;
			else
				stream = new GZipStream (depsFile, CompressionMode.Compress);

			writer = XmlWriter.Create (stream, settings);
			writer.WriteStartDocument ();
			writer.WriteStartElement ("DirectedGraph", "http://schemas.microsoft.com/vs/2009/dgml");
			// writer.WriteStartAttribute ("xmlns");
			// writer.WriteString ("http://schemas.microsoft.com/vs/2009/dgml");
			// writer.WriteEndAttribute ();

		}

		public void WriteDgmlGraphToFile ()
		{
			if (writer == null)
				return;

			writer.WriteStartElement ("Nodes");
			{
				foreach (var pair in nodeList) {
					writer.WriteStartElement ("Node");
					writer.WriteAttributeString ("Id", pair.Value.ToString());
					writer.WriteAttributeString ("Label", pair.Key);
					writer.WriteEndElement ();
				}
			}
			writer.WriteEndElement ();

			writer.WriteStartElement ("Links");
			{
				foreach (var tup in linkList) {
					writer.WriteStartElement ("Link");
					writer.WriteAttributeString ("Source", nodeList[tup.dependent].ToString ());
					writer.WriteAttributeString ("Target", nodeList[tup.dependee].ToString ());
					writer.WriteAttributeString ("Reason", tup.reason);
					writer.WriteEndElement ();
				}
			}
			writer.WriteEndElement ();
		}

		public void Dispose ()
		{
			if (writer == null)
				return;

			// put all code to generate dgml here
			WriteDgmlGraphToFile ();

			writer.WriteStartElement ("Properties");
			{
				writer.WriteStartElement ("Property");
				writer.WriteAttributeString ("Id", "Label");
				writer.WriteAttributeString ("Label", "Label");
				writer.WriteAttributeString ("DataType", "String");
				writer.WriteEndElement ();

				writer.WriteStartElement ("Property");
				writer.WriteAttributeString ("Id", "Reason");
				writer.WriteAttributeString ("Label", "Reason");
				writer.WriteAttributeString ("DataType", "String");
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();

			writer.WriteEndElement ();
			writer.WriteEndDocument ();

			writer.Flush ();
			writer.Dispose ();
			stream?.Dispose ();
			writer = null;
			stream = null;
		}

		public Dictionary<string, int> nodeList = new ();
		public HashSet<(string dependent, string dependee, string reason)> linkList = new (); // first element is source, second is target (dependent --> dependee), third is reason
		private int _nodeNextId = 0;

		public void RecordDependency (object target, in DependencyInfo reason, bool marked)
		{
			if (writer == null)
				throw new InvalidOperationException ();

			if (reason.Kind == DependencyKind.Unspecified)
				return;

			// For now, just report a dependency from source to target without noting the DependencyKind.
			RecordDependency (reason.Source, target, reason);
		}

		public void RecordDependency (object? source, object target, DependencyInfo reason)
		{
			if (writer == null)
				throw new InvalidOperationException ();

			if (!ShouldRecord (source) && !ShouldRecord (target))
				return;

			// We use a few hacks to work around MarkStep outputting thousands of edges even
			// with the above ShouldRecord checks. Ideally we would format these into a meaningful format
			// however I don't think that is worth the effort at the moment.

			// Prevent useless logging of attributes like `e="Other:Mono.Cecil.CustomAttribute"`.
			if (source is CustomAttribute || target is CustomAttribute)
				return;

			// Prevent useless logging of interface implementations like `e="InterfaceImpl:Mono.Cecil.InterfaceImplementation"`.
			if (source is InterfaceImplementation || target is InterfaceImplementation)
				return;

			string _dependent = TokenString (source);
			string _dependee = TokenString (target);

			if (!(source == null | nodeList.ContainsKey (_dependent))) AddNode (_dependent);
			if (!(target == null | nodeList.ContainsKey (_dependee))) AddNode (_dependee);
			if (source != target && source != null && target != null) {
				AddLink (_dependent, _dependee, reason);
			}
		}

		private int _nodeIndex = 0;

		void AddNode (string node)
		{
			if (!nodeList.ContainsKey (node)) {
				nodeList.Add (node, _nodeIndex);
				_nodeIndex++;
			}
		}

		void AddLink (string source, string target, DependencyInfo reason)
		{
			linkList.Add ((source, target, TokenString (reason)));
		}

		static bool IsAssemblyBound (TypeDefinition td)
		{
			do {
				if (td.IsNestedPrivate || td.IsNestedAssembly || td.IsNestedFamilyAndAssembly)
					return true;

				td = td.DeclaringType;
			} while (td != null);

			return false;
		}

		string TokenString (object? o)
		{
			if (o == null)
				return "N:null";

			if (o is TypeReference t) {
				bool addAssembly = true;
				var td = context.TryResolve (t);

				if (td != null) {
					addAssembly = td.IsNotPublic || IsAssemblyBound (td);
					t = td;
				}

				var addition = addAssembly ? $":{t.Module}" : "";

				return $"{((IMetadataTokenProvider) o).MetadataToken.TokenType}:{o}{addition}";
			}

			if (o is IMetadataTokenProvider provider)
				return provider.MetadataToken.TokenType + ":" + o;

			return "Other:" + o;
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

		bool ShouldRecord (object? o)
		{
			if (!context.EnableReducedTracing)
				return true;

			if (o is TypeDefinition t)
				return WillAssemblyBeModified (t.Module.Assembly);

			if (o is IMemberDefinition m)
				return WillAssemblyBeModified (m.DeclaringType.Module.Assembly);

			if (o is TypeReference typeRef) {
				var resolved = context.TryResolve (typeRef);

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

		public void RecordDependency (object source, object target, bool marked)
		{
			throw new NotImplementedException ();
		}
	}
}
