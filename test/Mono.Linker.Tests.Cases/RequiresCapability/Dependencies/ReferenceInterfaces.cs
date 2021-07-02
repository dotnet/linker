// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.RequiresCapability.Dependencies
{
	public class ReferenceInterfaces
	{
		public interface IBaseWithRequiresInReference
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles (Message = "Message")]
			public void Method ();

			public string PropertyAnnotationInAccesor {
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles (Message = "Message")]
				get;
				set;
			}

			[RequiresAssemblyFiles (Message = "Message")]
			public string PropertyAnnotationInProperty { get; set; }
		}

		public interface IBaseWithoutRequiresInReference
		{
			public void Method ();

			public string PropertyAnnotationInAccesor {
				get;
				set;
			}

			public string PropertyAnnotationInProperty { get; set; }
		}
	}
}
