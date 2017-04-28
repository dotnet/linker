using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Linker.Tests.Cases.Expectations;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Core.Base;
using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core
{
	public class DefaultTestSandbox : BaseTestSandbox
	{
		private readonly NPath _directory;

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
			: base(testCase)
		{
			var name = string.IsNullOrEmpty(namePrefix) ? "linker_tests" : $"{namePrefix}_linker_tests";
			_directory = rootTemporaryDirectory.Combine(name);

			_directory.DeleteContents();

			InputDirectory = _directory.Combine("input").EnsureDirectoryExists();
			OutputDirectory = _directory.Combine("output").EnsureDirectoryExists();
		}

		public override NPath InputDirectory { get; }

		public override NPath OutputDirectory { get; }

		public override IEnumerable<NPath> SourceFiles
		{
			get { return _directory.Files("*.cs"); }
		}

		public override IEnumerable<NPath> References
		{
			get { return InputDirectory.Files("*.dll"); }
		}

		public override IEnumerable<NPath> LinkXmlFiles
		{
			get { return InputDirectory.Files("*.xml"); }
		}

		public override void Populate(BaseTestCaseMetadaProvider metadataProvider)
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
