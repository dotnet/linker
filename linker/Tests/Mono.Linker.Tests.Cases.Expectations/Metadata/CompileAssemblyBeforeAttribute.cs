﻿using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	/// <summary>
	/// Use to compile an assembly before compiling the main test case executabe
	/// </summary>
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class CompileAssemblyBeforeAttribute : BaseMetadataAttribute {
		public CompileAssemblyBeforeAttribute (string outputName, string[] sourceFiles, string[] references = null, string[] defines = null, bool addAsReference = true)
		{
			if (sourceFiles == null)
				throw new ArgumentNullException (nameof (sourceFiles));

			if (string.IsNullOrEmpty (outputName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (outputName));
		}
	}
}
