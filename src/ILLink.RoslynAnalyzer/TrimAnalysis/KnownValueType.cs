// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public record KnownValueType : ValueWithDynamicallyAccessedMembers
	{
		public readonly ITypeSymbol Source;

		public KnownValueType (INamedTypeSymbol type) => Source = type;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes {
			get => Source.GetDynamicallyAccessedMemberTypes ();
		}

		public override string ToString ()
		{
			StringBuilder sb = new ();
			sb.Append ("type 'this'");
			var damtStr = Annotations.GetMemberTypesString (DynamicallyAccessedMemberTypes);
			var memberTypesStr = damtStr.Split ('.')[1].TrimEnd ('\'');
			sb.Append ("[").Append (memberTypesStr).Append ("]");
			return sb.ToString ();
		}
	}
}