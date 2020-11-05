﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings.Individual
{
#if !NETCOREAPP
	[IgnoreTestCase ("Can be enabled once MonoBuild produces a dll from which we can grab the types in the Mono.Linker namespace.")]
#else
	[SetupCompileBefore ("CustomStep.dll", new[] { "../Dependencies/CustomStep.cs" }, new[] { "illink.dll" })]
#endif
	[SetupLinkerArgument ("--custom-step", "CustomStep.CustomStep,CustomStep.dll")]
	[SetupLinkerArgument ("--custom-step", "CustomStep.CustomStepWithInvalidWarningCode,CustomStep.dll")]
	[LogContains ("CustomStep.cs(1,1): warning IL6001: Warning")]
	[LogContains ("The provided code '2500' does not fall into the permitted range for external warnings. To avoid possible collisions " +
					"with existing and future ILLink warnings, external messages should use codes starting from 6001.")]
	public class CustomStepWithWarnings
	{
		static void Main ()
		{
		}
	}
}
