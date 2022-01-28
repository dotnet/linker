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
	public class RequiresWithLambdas
	{
		[ExpectedWarning("IL2026", nameof(RequiresOnClass))]
		[ExpectedWarning("IL2026", nameof(NoRequiresOnClass))]
		public static void Main ()
		{
			typeof (RequiresOnClass).RequiresAll ();
			typeof (NoRequiresOnClass).RequiresAll ();
		}

		[RequiresUnreferencedCode (nameof (RequiresOnClass))]
		public class RequiresOnClass
		{
			public delegate void action ();
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

			public void InvokeActionArg (action f)
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
			public delegate void action ();

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
			[ExpectedWarning ("IL2026", nameof (ReturnRucAction), ProducedBy = ProducedBy.Analyzer)]
			public void InvokeReturnedRucAction ()
			{
				var f = ReturnRucAction ();
				f ();
			}

			public void InvokeActionArg (action f)
			{
				f ();
			}

			[RequiresUnreferencedCode (nameof (PassRucMethodToBeInvoked))]
			public void PassRucMethodToBeInvoked ()
			{
				InvokeActionArg (RucMethod);
			}
		}

	}
}
