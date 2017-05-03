using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Core.Utils;
using NUnit.Framework;

namespace Mono.Linker.Tests.Core.Customizable {
	public class ResultChecker {
		private readonly ExpectationsProvider _expectations;

		public ResultChecker (ExpectationsProvider expectations)
		{
			_expectations = expectations;
		}

		public virtual void Check (LinkedTestCaseResult linkResult)
		{
			Assert.IsTrue (linkResult.OutputAssemblyPath.FileExists (), $"The linked output assembly was not found.  Expected at {linkResult.OutputAssemblyPath}");

			using (var original = ReadAssembly (linkResult.InputAssemblyPath)) {
				PerformOutputAssemblyChecks (original.Definition, linkResult.OutputAssemblyPath.Parent);

				using (var linked = ReadAssembly (linkResult.OutputAssemblyPath)) {
					var checker = new AssemblyChecker (original.Definition, linked.Definition);
					checker.Verify (); 
				}
			}
		}

		static AssemblyContainer ReadAssembly (NPath assemblyPath)
		{
			var readerParams = new ReaderParameters ();
			var resolver = new AssemblyResolver ();
			readerParams.AssemblyResolver = resolver;
			resolver.AddSearchDirectory (assemblyPath.Parent.ToString ());
			return new AssemblyContainer (AssemblyDefinition.ReadAssembly (assemblyPath.ToString (), readerParams), resolver);
		}

		void PerformOutputAssemblyChecks (AssemblyDefinition original, NPath outputDirectory)
		{
			var assembliesToCheck = original.MainModule.Types.SelectMany (t => t.CustomAttributes).Where (attr => _expectations.IsAssemblyAssertion(attr));

			foreach (var assemblyAttr in assembliesToCheck) {
				var name = (string) assemblyAttr.ConstructorArguments.First ().Value;
				var expectedPath = outputDirectory.Combine (name);
				Assert.IsTrue (expectedPath.FileExists (), $"Expected the assembly {name} to exist in {outputDirectory}, but it did not");
			}
		}

		struct AssemblyContainer : IDisposable
		{
			public readonly AssemblyResolver Resolver;
			public readonly AssemblyDefinition Definition;

			public AssemblyContainer (AssemblyDefinition definition, AssemblyResolver resolver)
			{
				Definition = definition;
				Resolver = resolver;
			}

			public void Dispose ()
			{
				Resolver?.Dispose ();
				Definition?.Dispose ();
			}
		}
	}
}