// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using ILLink.Shared;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	// Don't look too closely at this yet...
	// the AnnotatedLocation/SingleValue/DynamicallyAccessedTypeValue need some cleanup
	// and unification (with each other and with ValueNode).

	public class AnnotatedLocation : IEquatable<AnnotatedLocation>
	{
		public ISymbol Symbol;
		public bool MethodReturn;
		public bool IsThisParameter => Symbol is IMethodSymbol && !MethodReturn;
		public AnnotatedLocation (IMethodSymbol method, bool methodReturn)
		{
			Symbol = method;
			MethodReturn = methodReturn;
		}
		public AnnotatedLocation (IParameterSymbol parameter)
		{
			Symbol = parameter;
			MethodReturn = false;
		}

		public AnnotatedLocation (IFieldSymbol field)
		{
			Symbol = field;
			MethodReturn = false;
		}

		public AnnotatedLocation (INamedTypeSymbol type)
		{
			Symbol = type;
			MethodReturn = false;
		}

		public AnnotatedLocation (ITypeParameterSymbol typeParameter)
		{
			Symbol = typeParameter;
			MethodReturn = false;
		}

		public DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypes () =>
			MethodReturn
				? ((IMethodSymbol) Symbol).GetDynamicallyAccessedMemberTypesOnReturnType ()
				: Symbol.GetDynamicallyAccessedMemberTypes ();

		// TODO: does this need EqualityContract? can we just make this sealed instead?
		protected virtual Type EqualityContract => typeof (AnnotatedLocation);

		public virtual bool Equals (AnnotatedLocation other)
		{
			return this == other || (other != null && EqualityContract == other.EqualityContract &&
				EqualityComparer<ISymbol>.Default.Equals (Symbol, other.Symbol) &&
				EqualityComparer<bool>.Default.Equals (MethodReturn, other.MethodReturn));
		}

		public override int GetHashCode ()
		{
			// TODO: use EqualityComparer<Foo>.Default.GetHashCode vs .GetHashCode()?
			return EqualityContract.GetHashCode () * -1521134295
				+ SymbolEqualityComparer.Default.GetHashCode (Symbol) * -1521134295
				+ MethodReturn.GetHashCode ();
		}
	}

	public class SingleValue : IEquatable<SingleValue>
	{
		// Rename?
		public AnnotatedLocation Symbol { get; }

		public SingleValue (ISymbol symbol, bool methodReturn = false)
		{
			// TODO: just merge these classes, instead of storing the symbol location separately.
			// should just need one class that has symbol, whether it's a method return, and DAMT.
			Symbol = symbol switch {
				IMethodSymbol method => new AnnotatedLocation (method, methodReturn),
				IParameterSymbol parameter => new AnnotatedLocation (parameter),
				IFieldSymbol field => new AnnotatedLocation (field),
				INamedTypeSymbol type => new AnnotatedLocation (type),
				_ => throw new NotImplementedException ()
				// Type parameter???
			};
		}
		protected virtual Type EqualityContract => typeof (SingleValue);

		// public static bool operator ==(SingleValue left, SingleValue right)
		// {
		// 	return (object)left == right || ((object)left != null && left.Equals (right));
		// }

		public virtual bool Equals (SingleValue other)
		{
			return this == other || (other != null && EqualityContract == other.EqualityContract &&
				EqualityComparer<AnnotatedLocation>.Default.Equals (Symbol, other.Symbol));
		}

		public override int GetHashCode ()
		{
			// return EqualityComparer<Type>.Default.GetHashCode(EqualityContract) * -1521134295 + EqualityComparer<C>.Default.GetHashCode(<C>k__BackingField);;
			return EqualityContract.GetHashCode () * -1521134295 + Symbol.GetHashCode ();
			// return HashCode.Combine (EqualityContract, Symbol);
		}

		// TODO: Object.Equals, GetHashCode

		public override string ToString ()
		{
			// TODO: move to AnnotatedLocation?
			StringBuilder sb = new ();
			var annotatedLocation = Symbol;
			switch (annotatedLocation.Symbol) {
			case IMethodSymbol method:
				if (annotatedLocation.MethodReturn)
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
				throw new NotImplementedException (annotatedLocation.Symbol.GetType ().ToString ());
			}
			var damt = annotatedLocation.GetDynamicallyAccessedMemberTypes ();
			var damtStr = Annotations.GetMemberTypesString (damt);
			var memberTypesStr = damtStr.Split ('.')[1].TrimEnd ('\'');
			sb.Append ("[").Append (memberTypesStr).Append ("]");
			return sb.ToString ();
		}
	}

	// a "known" unknown value. covers the cases of MethodParameter, MethodReturnType, etc.
	// can be None to represent an unannotated or annotated-None value.
	public class DynamicallyAccessedTypeValue : SingleValue, IEquatable<DynamicallyAccessedTypeValue>
	{
		public DynamicallyAccessedMemberTypes MemberTypes { get; }

		public DynamicallyAccessedTypeValue (DynamicallyAccessedMemberTypes memberTypes, ISymbol symbol, bool methodReturn = false) : base (symbol, methodReturn)
		{
			MemberTypes = memberTypes;
		}

		protected override Type EqualityContract => typeof (DynamicallyAccessedTypeValue);

		public virtual bool Equals (DynamicallyAccessedTypeValue other)
		{
			return this == other || (base.Equals (other) &&
				EqualityComparer<DynamicallyAccessedMemberTypes>.Default.Equals (MemberTypes, other.MemberTypes));
		}

		public override int GetHashCode ()
		{
			// return HashCode.Combine (base.GetHashCode (), MemberTypes);
			// return base.GetHashCode() * -1521134295 + EqualityComparer<C>.Default.GetHashCode(<D>k__BackingField);
			return base.GetHashCode () * -1521134295 + MemberTypes.GetHashCode ();
		}
	}
}

