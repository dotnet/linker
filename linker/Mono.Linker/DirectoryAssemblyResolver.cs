using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using Mono.Collections.Generic;
using Mono.Cecil;

#if NET_CORE
namespace Mono.Linker {

	public sealed class AssemblyResolutionException : FileNotFoundException {

		readonly AssemblyNameReference reference;

		public AssemblyNameReference AssemblyReference {
			get { return reference; }
		}

		public AssemblyResolutionException (AssemblyNameReference reference)
			: base (string.Format ("Failed to resolve assembly: '{0}'", reference))
		{
			this.reference = reference;
		}
	}

	public abstract class DirectoryAssemblyResolver : IAssemblyResolver {

		readonly Collection<string> directories;

		public void AddSearchDirectory(string directory)
		{
			directories.Add(directory);
		}

		public void RemoveSearchDirectory(string directory)
		{
			directories.Remove(directory);
		}

		public string[] GetSearchDirectories()
		{
			return this.directories.ToArray();
		}

		protected DirectoryAssemblyResolver()
		{
			directories = new Collection<string>(2) { "." };
		}

		AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
		{
			if (parameters.AssemblyResolver == null)
				parameters.AssemblyResolver = this;

			return ModuleDefinition.ReadModule(file, parameters).Assembly;
		}

		public virtual AssemblyDefinition Resolve (AssemblyNameReference name)
		{
			return Resolve (name, new ReaderParameters ());
		}

		public virtual AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			if (parameters == null)
				parameters = new ReaderParameters();

			var assembly = SearchDirectory(name, directories, parameters);
			if (assembly != null)
				return assembly;

			// TODO: understand whether we need special logic for
			// retargetable references, like BaseAssemblyResolver in
			// Mono.Cecil

			var framework_dir = Path.GetDirectoryName(typeof(object).GetTypeInfo().Module.FullyQualifiedName);

			// TODO: understand whether we need special logic for
			// checking whether the version is zero, like
			// BaseAssemblyResolver in Mono.Cecil

			assembly = SearchDirectory(name, new[] { framework_dir }, parameters);
			if (assembly != null)
				return assembly;

			// TODO: possibly add assembly resolve failure event
			// handler and call it here

			throw new AssemblyResolutionException(name);
		}

		AssemblyDefinition SearchDirectory(AssemblyNameReference name, IEnumerable<string> directories, ReaderParameters parameters)
		{
			var extensions = new [] { ".dll" };
			foreach (var directory in directories)
			{
				foreach (var extension in extensions)
				{
					string file = Path.Combine(directory, name.Name + extension);
					if (!File.Exists(file))
						continue;
					try
					{
						return GetAssembly(file, parameters);
					}
					catch (System.BadImageFormatException)
					{
						continue;
					}
				}
			}

			return null;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
		}
	}
}
#endif
