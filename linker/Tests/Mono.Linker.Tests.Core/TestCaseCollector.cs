using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core
{
	public class TestCaseCollector
	{
		private readonly NPath _rootDirectory;
		private readonly NPath _testCaseAssemblyPath;

		public TestCaseCollector(string rootDirectory, string testCaseAssemblyPath)
			: this(rootDirectory.ToNPath(), testCaseAssemblyPath.ToNPath())
		{
		}

		public TestCaseCollector(NPath rootDirectory, NPath testCaseAssemblyPath)
		{
			_rootDirectory = rootDirectory;
			_testCaseAssemblyPath = testCaseAssemblyPath;
		}

		public IEnumerable<TestCase> Collect()
		{
			_rootDirectory.DirectoryMustExist();
			_testCaseAssemblyPath.FileMustExist();

			using (var caseAssemblyDefinition = AssemblyDefinition.ReadAssembly(_testCaseAssemblyPath.ToString()))
			{
				foreach (var file in _rootDirectory.Files("*.cs"))
				{
					TestCase testCase;
					if (ProcessSourceFile(caseAssemblyDefinition, file, out testCase))
						yield return testCase;
				}

				foreach (var subDir in _rootDirectory.Directories())
				{
					if (subDir.FileName == "bin" || subDir.FileName == "obj" || subDir.FileName == "Properties")
						continue;

					foreach (var file in subDir.Files("*.cs", true))
					{
						TestCase testCase;
						if (ProcessSourceFile(caseAssemblyDefinition, file, out testCase))
							yield return testCase;
					}
				}
			}
		}

		private bool ProcessSourceFile(AssemblyDefinition caseAssemblyDefinition, NPath sourceFile, out TestCase testCase)
		{
			var potentialCase = new TestCase(sourceFile, _rootDirectory, _testCaseAssemblyPath);

			var typeDefinition = FindTypeDefinition(caseAssemblyDefinition, potentialCase);

			if (typeDefinition == null)
				throw new InvalidOperationException($"Could not find the matching type for test case {sourceFile}.  Ensure the file name and class name match");

			if (typeDefinition.HasAttribute(nameof(NotATestCaseAttribute)))
			{
				testCase = null;
				return false;
			}

			// Verify the class as a static main method
			var mainMethod = typeDefinition.Methods.FirstOrDefault(m => m.Name == "Main");

			if (mainMethod == null)
				throw new InvalidOperationException($"{typeDefinition} in {sourceFile} is missing a Main() method");

			if (!mainMethod.IsStatic)
				throw new InvalidOperationException($"The Main() method for {typeDefinition} in {sourceFile} should be static");

			testCase = potentialCase;
			return true;
		}

		private static TypeDefinition FindTypeDefinition(AssemblyDefinition caseAssemblyDefinition, TestCase testCase)
		{
			var typeDefinition = caseAssemblyDefinition.MainModule.GetType(testCase.FullTypeName);

			if (typeDefinition != null)
				return typeDefinition;

			// TODO by Mike : Is this to hacky?  It's for unity tests to pair up MonoBehaviour.cs which the MonoBehavioir type which has UnityEngine as a namespace
			foreach (var type in caseAssemblyDefinition.MainModule.Types)
			{
				//  Let's assume we should never have to search for a test case that has no namespace.  If we don't find the type from GetType, then o well, that's not a test case.
				if (string.IsNullOrEmpty(type.Namespace))
					continue;

				if (type.Name == testCase.Name)
				{
					// TODO by Mike : This is really hacky.  Maybe there is a way to improve it by getting the debug information and if that knows about the source
					// file location we could check to see if that source file matches the testCase.SourceFile.
					if (!testCase.SourceFile.ReadAllText().Contains($"namespace {type.Namespace}"))
						continue;

					return type;
				}
			}

			return null;
		}
	}
}
