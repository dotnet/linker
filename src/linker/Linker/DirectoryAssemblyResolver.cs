// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Collections.Generic;
using Mono.Cecil;
using System.IO.MemoryMappedFiles;

#if FEATURE_ILLINK
namespace Mono.Linker {

	public abstract class DirectoryAssemblyResolver : IAssemblyResolver {

		readonly Collection<string> directories;

		public readonly Dictionary<AssemblyDefinition, string> AssemblyToPath = new Dictionary<AssemblyDefinition, string> ();

		readonly List<MemoryMappedViewStream> viewStreams = new List<MemoryMappedViewStream> ();

		public void AddSearchDirectory (string directory)
		{
			directories.Add (directory);
		}

		public void RemoveSearchDirectory (string directory)
		{
			directories.Remove (directory);
		}

		public string [] GetSearchDirectories ()
		{
			return this.directories.ToArray ();
		}

		protected DirectoryAssemblyResolver ()
		{
			directories = new Collection<string> (2) { "." };
		}

		protected AssemblyDefinition GetAssembly (string file, ReaderParameters parameters)
		{
			if (parameters.AssemblyResolver == null)
				parameters.AssemblyResolver = this;

			FileStream fileStream = null;
			MemoryMappedFile mappedFile = null;
			MemoryMappedViewStream viewStream = null;
			try {
				// Create stream because CreateFromFile(string, ...) uses FileShare.None which is too strict
				fileStream = new FileStream (file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false);
				mappedFile = MemoryMappedFile.CreateFromFile (
					fileStream, null, fileStream.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
				viewStream = mappedFile.CreateViewStream (0, 0, MemoryMappedFileAccess.Read);

				AssemblyDefinition result = ModuleDefinition.ReadModule (viewStream, parameters).Assembly;

				AssemblyToPath.Add (result, file);

				viewStreams.Add (viewStream);

				// We transferred the ownership of the viewStream to the collection.
				viewStream = null;

				return result;
			} finally {
				if (fileStream != null)
					fileStream.Dispose ();
				if (mappedFile != null)
					mappedFile.Dispose ();
				if (viewStream != null)
					viewStream.Dispose ();
			}
		}

		public virtual AssemblyDefinition Resolve (AssemblyNameReference name)
		{
			return Resolve (name, new ReaderParameters ());
		}

		public virtual AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			if (parameters == null)
				throw new ArgumentNullException ("parameters");

			var assembly = SearchDirectory (name, directories, parameters);
			if (assembly != null)
				return assembly;

			throw new AssemblyResolutionException (name);
		}

		AssemblyDefinition SearchDirectory (AssemblyNameReference name, IEnumerable<string> directories, ReaderParameters parameters)
		{
			var extensions = new [] { ".dll", ".exe" };
			foreach (var directory in directories) {
				foreach (var extension in extensions) {
					string file = Path.Combine (directory, name.Name + extension);
					if (!File.Exists (file))
						continue;
					try {
						return GetAssembly (file, parameters);
					} catch (System.BadImageFormatException) {
						continue;
					}
				}
			}

			return null;
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing) {
				foreach (var viewStream in viewStreams) {
					viewStream.Dispose ();
				}

				viewStreams.Clear ();
			}
		}
	}
}
#endif
