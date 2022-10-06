// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable


using System.Collections.Generic;

namespace ILLink.Shared.TrimAnalysis
{
	sealed partial record MethodParameterValue : ValueWithDynamicallyAccessedMembers
	{
		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> Parameter.GetDiagnosticArgumentsForAnnotationMismatch ();

		// _overrideIsThis is needed for backwards compatibility with MakeGenericType/Method https://github.com/dotnet/linker/issues/2428
		private readonly bool _overrideIsThis;
	}
}
