using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using ILLink.Shared;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	// TODO: this may store unannotated symbols too!
	// TODO: find a better name for it!
	public class AnnotatedSymbol : DynamicallyAccessedMembersValue
	{
		public ISymbol Source;
		public bool IsMethodReturn;

		public AnnotatedSymbol (IMethodSymbol method, bool isMethodReturn)
		{
			Source = method;
			IsMethodReturn = isMethodReturn;
		}

		public AnnotatedSymbol (IParameterSymbol parameter)
		{
			Source = parameter;
			IsMethodReturn = false;
		}

		public AnnotatedSymbol (IFieldSymbol field)
		{
			Source = field;
			IsMethodReturn = false;
		}

		public AnnotatedSymbol (INamedTypeSymbol type)
		{
			Source = type;
			IsMethodReturn = false;
		}

		public AnnotatedSymbol (ITypeParameterSymbol typeParameter)
		{
			Source = typeParameter;
			IsMethodReturn = false;
		}

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes =>
			IsMethodReturn
				? ((IMethodSymbol) Source).GetDynamicallyAccessedMemberTypesOnReturnType ()
				: Source.GetDynamicallyAccessedMemberTypes ();

		protected override Type EqualityContract => typeof (AnnotatedSymbol);

		public virtual bool Equals (AnnotatedSymbol other)
		{
			return this == other || (other != null && EqualityContract == other.EqualityContract &&
				EqualityComparer<ISymbol>.Default.Equals (Source, other.Source) &&
				EqualityComparer<bool>.Default.Equals (IsMethodReturn, other.IsMethodReturn));
		}

		public override int GetHashCode ()
		{
			// TODO: use EqualityComparer<Foo>.Default.GetHashCode vs .GetHashCode()?
			return EqualityContract.GetHashCode () * -1521134295
				+ SymbolEqualityComparer.Default.GetHashCode (Source) * -1521134295
				+ IsMethodReturn.GetHashCode ();
		}

		public override string ToString ()
		{
			// TODO: move to AnnotatedLocation?
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
	}
}