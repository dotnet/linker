// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using ILLink.Shared;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public class DynamicallyAccessedMembersSymbol : DynamicallyAccessedMembersValue
	{
		public readonly ISymbol Source;
		public readonly bool IsMethodReturn;

		public DynamicallyAccessedMembersSymbol (IMethodSymbol method, bool isMethodReturn) => (Source, IsMethodReturn) = (method, isMethodReturn);

		public DynamicallyAccessedMembersSymbol (IParameterSymbol parameter) => Source = parameter;

		public DynamicallyAccessedMembersSymbol (IFieldSymbol field) => Source = field;

		public DynamicallyAccessedMembersSymbol (INamedTypeSymbol type) => Source = type;

		// This ctor isn't used for dataflow - it's really just a wrapper
		// for annotations on type arguments/parameters which are type-checked
		// by the analyzer (outside of the dataflow analysis).
		public DynamicallyAccessedMembersSymbol (ITypeSymbol typeArgument) => Source = typeArgument;

		public DynamicallyAccessedMembersSymbol (ITypeParameterSymbol typeParameter) => Source = typeParameter;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes =>
			IsMethodReturn
				? ((IMethodSymbol) Source).GetDynamicallyAccessedMemberTypesOnReturnType ()
				: Source.GetDynamicallyAccessedMemberTypes ();

		protected override Type EqualityContract => typeof (DynamicallyAccessedMembersSymbol);

		public virtual bool Equals (DynamicallyAccessedMembersSymbol other)
		{
			return this == other || (other != null && EqualityContract == other.EqualityContract &&
				EqualityComparer<ISymbol>.Default.Equals (Source, other.Source) &&
				EqualityComparer<bool>.Default.Equals (IsMethodReturn, other.IsMethodReturn));
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (EqualityContract, SymbolEqualityComparer.Default.GetHashCode (Source), IsMethodReturn);
		}

#if DEBUG
		public override string ToString ()
		{
			StringBuilder sb = new ();
			switch (Source) {
			case IMethodSymbol method:
				if (IsMethodReturn)
					sb.Append (method.Name);
				else
					sb.Append ("'this'");
				break;
			case IParameterSymbol param:
				sb.Append (param.Name);
				break;
			case IFieldSymbol field:
				sb.Append (field.Name);
				break;
			case INamedTypeSymbol:
				sb.Append ("type 'this'");
				break;
			default:
				throw new NotImplementedException (Source.GetType ().ToString ());
			}
			var damtStr = Annotations.GetMemberTypesString (DynamicallyAccessedMemberTypes);
			var memberTypesStr = damtStr.Split ('.')[1].TrimEnd ('\'');
			sb.Append ("[").Append (memberTypesStr).Append ("]");
			return sb.ToString ();
		}
#endif
	}
}