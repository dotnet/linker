// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	partial record MethodParameterValue
	{
		public MethodParameterValue (IParameterSymbol parameterSymbol)
			: this ((IMethodSymbol) parameterSymbol.ContainingSymbol, parameterSymbol.ILIndex (), FlowAnnotations.GetMethodParameterAnnotation ((IMethodSymbol) parameterSymbol.ContainingSymbol, parameterSymbol.ILIndex ()))
		{ }

		public MethodParameterValue (IMethodSymbol methodSymbol, ILParameterIndex parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> (MethodSymbol, ILIndex, DynamicallyAccessedMemberTypes) = (methodSymbol, parameterIndex, dynamicallyAccessedMemberTypes);

		public readonly IMethodSymbol MethodSymbol;

		public readonly ILParameterIndex ILIndex;

		private int _sourceIndex => MethodSymbol.IsStatic ? (int) ILIndex : (int) ILIndex - 1;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> IsThisParameter () ?
				new string[] { MethodSymbol.GetDisplayName () }
				: new string[] { MethodSymbol.Parameters[_sourceIndex].GetDisplayName (), MethodSymbol.GetDisplayName () };

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString ()
			=> this.ValueToString (MethodSymbol, DynamicallyAccessedMemberTypes);

		public bool IsThisParameter () => MethodSymbol.IsThisParameterIndex(ILIndex);
	}
}
