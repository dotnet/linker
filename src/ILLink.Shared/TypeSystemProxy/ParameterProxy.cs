// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace ILLink.Shared.TypeSystemProxy
{
	internal partial struct ParameterProxy
	{
		// C# doesn't have partial constructors or partial properties, but the following should be implemented in every project

		// public ParameterProxy (MethodProxy method, ParameterIndex index)

		// public partial MethodProxy Method { get; }

		// public partial ParameterIndex Index { get; }

		// public partial ReferenceKind RefKind { get; }

		public partial string GetDisplayName ();

		public bool IsImplicitThis => Method.HasImplicitThis () && Index == ParameterIndex.This;


		public partial bool IsTypeOf (string typeName);

		public IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> IsImplicitThis ?
				new string[] { GetDisplayName () }
				: new string[] { GetDisplayName (), Method.GetDisplayName () };
	}
}
