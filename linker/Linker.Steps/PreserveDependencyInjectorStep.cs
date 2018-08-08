//
// PreserveDependencyInjectorStep.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Xml.XPath;

using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class PreserveDependencyInjectorStep : BaseStep
	{
		static string[] types = new[] {
			"Mono.ISystemDependencyProvider",
		};

		bool need_injector;

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (need_injector)
				return;

			/*
			 * If any class in these libraries references the dependency injector
			 * (via `Mono.ISystemDependencyProvider`), then we must keep the `System.dll`
			 * side of it alive by preserving `Mono.SystemDependencyProvider`.
			 */

			switch (assembly.Name.Name) {
			case "mscorlib":
			case "System":
			case "System.Core":
			case "System.Security":
			case "Mono.Btls.Interface":
				break;
			default:
				return;
			}

			need_injector = HasNeededReference (assembly.MainModule);
		}

		static bool HasNeededReference (ModuleDefinition module)
		{
			foreach (var type in types)
				if (module.HasTypeReference (type))
					return true;

			return false;
		}

		protected override void EndProcess ()
		{
			if (!need_injector)
				return;

			var system = Context.Resolve ("System");
			if (system == null)
				return;

			if (Annotations.GetAction (system) != AssemblyAction.Link)
				return;

			var preserveStep = CreatePreserveStep ();
			Context.Pipeline.AddStepAfter (typeof (PreserveDependencyInjectorStep), preserveStep);
		}

		static IStep CreatePreserveStep ()
		{
			return new ResolveFromXmlStep (
				new XPathDocument (
					new StringReader (descriptor)));
		}

		const string descriptor = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<linker>
	<assembly fullname=""System"">
		<type fullname=""Mono.SystemDependencyProvider"" />
	</assembly>
</linker>
";
	}
}
