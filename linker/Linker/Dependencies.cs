using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Mono.Cecil;

namespace Mono.Linker {
	public class Dependencies {
		protected readonly LinkContext _context;
		Stack<object> dependency_stack;
		System.Xml.XmlWriter writer;
		GZipStream zipStream;

		public Dependencies (LinkContext context)
		{
			_context = context;
		}

		public void PrepareDependenciesDump ()
		{
			PrepareDependenciesDump ("linker-dependencies.xml.gz");
		}

		public void PrepareDependenciesDump (string filename)
		{
			dependency_stack = new Stack<object> ();
			System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings ();
			settings.Indent = true;
			settings.IndentChars = "\t";
			var depsFile = File.OpenWrite (filename);
			zipStream = new GZipStream (depsFile, CompressionMode.Compress);

			writer = System.Xml.XmlWriter.Create (zipStream, settings);
			writer.WriteStartDocument ();
			writer.WriteStartElement ("dependencies");
			writer.WriteStartAttribute ("version");
			writer.WriteString ("1.0");
			writer.WriteEndAttribute ();
		}

		public void AddDependency(object o)
		{
			if (writer == null)
				return;

			KeyValuePair<object, object> pair = new KeyValuePair<object, object> (dependency_stack.Count > 0 ? dependency_stack.Peek () : null, o);

			if (pair.Key != pair.Value) {
				WriteEdge (pair.Key, pair.Value);
			}
		}

		public void Push (object o)
		{
			if (writer == null)
				return;

			if (dependency_stack.Count > 0)
				AddDependency (o);
			dependency_stack.Push (o);
		}

		public void Pop ()
		{
			if (writer == null)
				return;

			dependency_stack.Pop ();
		}

		public void SaveDependencies ()
		{
			if (writer == null)
				return;

			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Flush ();
			writer.Dispose ();
			zipStream.Dispose ();
			writer = null;
			zipStream = null;
			dependency_stack = null;
		}

		void WriteEdge (object b, object e)
		{
			writer.WriteStartElement ("edge");
			writer.WriteAttributeString ("b", TokenString (b));
			writer.WriteAttributeString ("e", TokenString (e));
			writer.WriteEndElement ();
		}

		string TokenString (object o)
		{
			if (o == null)
				return "N:null";

			if (o is IMetadataTokenProvider)
				return (o as IMetadataTokenProvider).MetadataToken.TokenType + ":" + o;

			return "Other:" + o;
		}
	}
}
