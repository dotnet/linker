// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	[LogContains("IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresWithLambdas.NoRequiresOnClass.<InvokeLocalRucAction>b__1_0(): Using member 'Mono.Linker.Tests.Cases.RequiresCapability.RequiresWithLambdas.NoRequiresOnClass.RucMethod()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. RucMethod.", ProducedBy = ProducedBy.Trimmer)]
	[LogContains("IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresWithLambdas.NoRequiresOnClass.<ReturnRucAction>b__2_0(): Using member 'Mono.Linker.Tests.Cases.RequiresCapability.RequiresWithLambdas.NoRequiresOnClass.RucMethod()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. RucMethod.", ProducedBy = ProducedBy.Trimmer)]
	public class RequiresWithLambdas
	{
		[ExpectedWarning("IL2026", nameof(RequiresOnClass), ProducedBy = ProducedBy.Trimmer)]
		[ExpectedWarning("IL2026", nameof(NoRequiresOnClass.RucMethod), ProducedBy = ProducedBy.Trimmer)]
		[ExpectedWarning("IL2026", nameof(NoRequiresOnClass.ReturnRucAction), ProducedBy = ProducedBy.Trimmer)]
		[ExpectedWarning("IL2026", nameof(NoRequiresOnClass.PassRucMethodToBeInvoked), ProducedBy = ProducedBy.Trimmer)]
		public static void Main ()
		{
			typeof (RequiresOnClass).RequiresAll ();
			typeof (NoRequiresOnClass).RequiresAll ();
		}

		[RequiresUnreferencedCode (nameof (RequiresOnClass))]
		public class RequiresOnClass
		{
			public void RucMethod () { }
			public void InvokeLocalRucAction ()
			{
				Action x = () => { RucMethod (); };
				x ();
			}

			public Action ReturnRucAction ()
			{
				return () => { RucMethod (); return; };
			}

			public void InvokeReturnedRucAction ()
			{
				var f = ReturnRucAction ();
				f ();
			}

			public void InvokeActionArg (Action f)
			{
				f ();
			}

			public void PassRucMethodToBeInvoked ()
			{
				InvokeActionArg (RucMethod);
			}
		}

		public class NoRequiresOnClass
		{
			[RequiresUnreferencedCode (nameof (RucMethod))]
			public void RucMethod () { }

			[ExpectedWarning ("IL2026", nameof (RucMethod), ProducedBy = ProducedBy.Analyzer)]
			public void InvokeLocalRucAction ()
			{
				Action x = () => { RucMethod (); };
				x ();
			}

			// Bug - Shouldn't warn since annotated with RUC
			[ExpectedWarning ("IL2026", ProducedBy = ProducedBy.Analyzer)]
			[RequiresUnreferencedCode (nameof (ReturnRucAction))]
			public Action ReturnRucAction ()
			{
				return () => { RucMethod (); return; };
			}

			// Bug - Should warn in Trimmer
			[ExpectedWarning ("IL2026", nameof (ReturnRucAction))]
			public void InvokeReturnedRucAction ()
			{
				var f = ReturnRucAction ();
				f ();
			}

			public void InvokeActionArg (Action f)
			{
				f ();
			}

			[RequiresUnreferencedCode (nameof (PassRucMethodToBeInvoked))]
			public void PassRucMethodToBeInvoked ()
			{
				InvokeActionArg (RucMethod);
				InvokeActionArg (ReturnRucAction ());
			}
		}

	}
}
