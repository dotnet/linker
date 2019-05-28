﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class ILCompiler {
		private readonly string _ilasmExecutable;

		public ILCompiler ()
		{
			_ilasmExecutable = LocateIlasm ().ToString ();
		}

		public ILCompiler (string ilasmExecutable)
		{
			_ilasmExecutable = ilasmExecutable;
		}

		public NPath Compile (CompilerOptions options)
		{
			var capturedOutput = new List<string> ();
			var process = new Process ();
			SetupProcess (process, options);
			process.StartInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += (sender, args) => capturedOutput.Add (args.Data);
			process.Start ();
			process.BeginOutputReadLine ();
			process.WaitForExit ();

			if (process.ExitCode != 0)
			{
				Assert.Fail($"Failed to compile IL assembly : {options.OutputPath}\n{capturedOutput.Aggregate ((buff, s) => buff + Environment.NewLine + s)}");
			}

			return options.OutputPath;
		}

		protected virtual void SetupProcess (Process process, CompilerOptions options)
		{
			process.StartInfo.FileName = _ilasmExecutable;
			process.StartInfo.Arguments = BuildArguments (options);
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		}

		private string BuildArguments (CompilerOptions options)
		{
			var args = new StringBuilder();
#if ILLINK
			args.Append(options.OutputPath.ExtensionWithDot == ".dll" ? "-dll" : "-exe");
			args.Append($" -out:{options.OutputPath.InQuotes ()}");
#else
			args.Append(options.OutputPath.ExtensionWithDot == ".dll" ? "/dll" : "/exe");
			args.Append($" /out:{options.OutputPath.InQuotes ()}");
#endif
			args.Append($" {options.SourceFiles.Aggregate (string.Empty, (buff, file) => $"{buff} {file.InQuotes ()}")}");
			return args.ToString ();
		}

		public static NPath LocateIlasm ()
		{
#if ILLINK
			var extension = RuntimeInformation.IsOSPlatform (OSPlatform.Windows) ? ".exe" : "";
#if ARCADE
			// working directory is artifacts/bin/Mono.Linker.Tests/<config>/<tfm>
			var toolsDir = Path.Combine (Directory.GetCurrentDirectory (), "..", "..", "..", "..", "tools");
#else
			// working directory is bin/Mono.Linker.Tests/<config>/<tfm>
			var toolsDir = Path.Combine (Directory.GetCurrentDirectory (), "..", "..", "obj", "tools");
#endif // ARCADE
			var ilasmPath = Path.GetFullPath (Path.Combine (toolsDir, "ilasm", $"ilasm{extension}")).ToNPath ();
			if (ilasmPath.FileExists ())
				return ilasmPath;

			throw new InvalidOperationException ("ilasm not found at " + ilasmPath);
#else
			return Environment.OSVersion.Platform == PlatformID.Win32NT ? LocateIlasmOnWindows () : "ilasm".ToNPath ();
#endif
		}

		public static NPath LocateIlasmOnWindows ()
		{
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
				throw new InvalidOperationException ("This method should only be called on windows");

			var possiblePath = RuntimeEnvironment.GetRuntimeDirectory ().ToNPath ().Combine ("ilasm.exe");
			if (possiblePath.FileExists ())
				return possiblePath;

			throw new InvalidOperationException ("Could not locate a ilasm.exe executable");
		}
	}
}
