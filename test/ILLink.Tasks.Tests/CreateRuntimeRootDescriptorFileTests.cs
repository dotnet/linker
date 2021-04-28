﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xunit;

namespace ILLink.Tasks.Tests
{
	public class CreateRuntimeRootDescriptorFileTests
	{
		[Fact]
		public void TestCoreLibClassGen ()
		{
			File.WriteAllLines ("corelib.h", new string[] {
				"#ifndef TESTDEF",
				"#define TESTDEF",
				"#endif",
				"DEFINE_CLASS(TESTCLASS, TestNS, TestClass)",
				"DEFINE_METHOD(TESTCLASS, TESTMETHOD, TestMethod, 0)",
				"#ifdef FEATURE_ON",
				"DEFINE_METHOD(TESTCLASS, TESTMETHODIFON, TestMethodIfOn, 1)",
				"#endif",
				"#ifdef FEATURE_OFF",
				"DEFINE_METHOD(TESTCLASS, TESTMETHODIFOFF, TestMethodIfOff, 2)",
				"#endif",
				"#ifndef FEATURE_BOTH // Comment",
				"DEFINE_METHOD(TESTCLASS, TESTMETHODIFBOTH, TestMethodIfBoth, 3)",
				"#if FOR_ILLINK",
				"DEFINE_METHOD(TESTCLASS, TESTMETHODIFBOTH, TestMethodIfBothForILLink, 3)",
				"#endif",
				"#else",
				"DEFINE_METHOD(TESTCLASS, TESTMETHODIFNOTBOTH, TestMethodIfNotBoth, 4)",
				"#if FOR_ILLINK",
				"DEFINE_METHOD(TESTCLASS, TESTMETHODIFNOTBOTH, TestMethodIfNotBothForILLink, 4)",
				"#endif",
				"#endif // FEATURE_BOTH",
				"#if FOR_ILLINK",
				"DEFINE_METHOD(TESTCLASS, TESTMETHODFORILLINK, TestMethodForILLink, 5)",
				"#endif"
				});

			File.WriteAllText ("namespace.h",
				"#define g_TestNS \"TestNS\"" + Environment.NewLine);

			File.WriteAllText ("cortypeinfo.h", "");

			File.WriteAllText ("rexcep.h", "");

			XElement existingAssembly = new XElement ("assembly", new XAttribute ("fullname", "testassembly"),
					new XComment ("Existing content"));
			XElement existingContent = new XElement ("linker", existingAssembly);
			(new XDocument (existingContent)).Save ("Test.ILLink.Descriptors.Combined.xml");

			var task = new CreateRuntimeRootILLinkDescriptorFile () {
				NamespaceFilePath = new TaskItem ("namespace.h"),
				MscorlibFilePath = new TaskItem ("corelib.h"),
				CortypeFilePath = new TaskItem ("cortypeinfo.h"),
				RexcepFilePath = new TaskItem ("rexcep.h"),
				ILLinkTrimXmlFilePath = new TaskItem ("Test.ILLink.Descriptors.Combined.xml"),
				DefineConstants = new TaskItem[] {
					new TaskItem("FOR_ILLINK"),
					new TaskItem("_TEST"),
					new TaskItem("FEATURE_ON"),
					new TaskItem("FEATURE_BOTH")
				},
				RuntimeRootDescriptorFilePath = new TaskItem ("Test.ILLink.Descriptors.xml")
			};

			Assert.True (task.Execute ());

			XDocument output = XDocument.Load ("Test.ILLink.Descriptors.xml");
			string expectedXml = new XElement ("linker",
				new XElement ("assembly",
					existingAssembly.Attributes (),
					existingAssembly.Nodes (),
					new XElement ("type", new XAttribute ("fullname", "TestNS.TestClass"),
						new XElement ("method", new XAttribute ("name", "TestMethod")),
						new XElement ("method", new XAttribute ("name", "TestMethodIfOn")),
						new XElement ("method", new XAttribute ("name", "TestMethodIfNotBoth")),
						new XElement ("method", new XAttribute ("name", "TestMethodIfNotBothForILLink")),
						new XElement ("method", new XAttribute ("name", "TestMethodForILLink")))
					)).ToString ();
			Assert.Equal (expectedXml, output.Root.ToString ());
		}
	}
}
