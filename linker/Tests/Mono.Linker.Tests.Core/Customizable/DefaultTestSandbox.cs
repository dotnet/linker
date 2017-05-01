using System;
using System.Collections.Generic;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core.Customizable
{
	public class DefaultTestSandbox
	{
		protected readonly TestCase _testCase;
		protected readonly NPath _directory;

		public DefaultTestSandbox(TestCase testCase)
			: this(testCase, NPath.SystemTemp)
		{
		}

		public DefaultTestSandbox(TestCase testCase, NPath rootTemporaryDirectory)
			 : this(testCase, rootTemporaryDirectory, string.Empty)
		{
		}

		public DefaultTestSandbox(TestCase testCase, string rootTemporaryDirectory, string namePrefix)
			: this(testCase, rootTemporaryDirectory.ToNPath(), namePrefix)
		{
		}

		public DefaultTestSandbox(TestCase testCase, NPath rootTemporaryDirectory, string namePrefix)
		{
			_testCase = testCase;
			var name = string.IsNullOrEmpty(namePrefix) ? "linker_tests" : $"{namePrefix}_linker_tests";
			_directory = rootTemporaryDirectory.Combine(name);

			_directory.DeleteContents();

			InputDirectory = _directory.Combine("input").EnsureDirectoryExists();
			OutputDirectory = _directory.Combine("output").EnsureDirectoryExists();
		}

		public NPath InputDirectory { get; }

		public NPath OutputDirectory { get; }

		public  IEnumerable<NPath> SourceFiles
		{
			get { return _directory.Files("*.cs"); }
		}

		public IEnumerable<NPath> References
		{
			get { return InputDirectory.Files("*.dll"); }
		}

		public IEnumerable<NPath> LinkXmlFiles
		{
			get { return InputDirectory.Files("*.xml"); }
		}

		public virtual void Populate(DefaultTestCaseMetadaProvider metadataProvider)
		{
			_testCase.SourceFile.Copy(_directory);

			if (_testCase.HasLinkXmlFile)
				_testCase.LinkXmlFile.Copy(InputDirectory);

			GetExpectationsAssemblyPath().Copy(InputDirectory);

			foreach (var dep in metadataProvider.AdditionalFilesToSandbox())
			{
				dep.FileMustExist().Copy(_directory);
			}
		}

		private static NPath GetExpectationsAssemblyPath()
		{
			return new Uri(typeof(RemovedAttribute).Assembly.CodeBase).LocalPath.ToNPath();
		}
	}
}
