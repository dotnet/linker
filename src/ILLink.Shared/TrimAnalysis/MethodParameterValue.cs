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

		private readonly bool _overrideIsThis;
	}
}
