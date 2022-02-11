// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	internal interface IAttributeData
	{
		public INamedTypeSymbol AttributeClass { get; }
		public ImmutableArray<ITypedConstant> ConstructorArguments { get; }
		public IMethodSymbol AttributeConstructor { get; }
	}

	internal interface ITypedConstant
	{
		public ITypeSymbol? Type { get; }
		public object? Value { get; }
	}

	internal class XmlAttributeData : IAttributeData
	{
		internal XmlAttributeData(
			INamedTypeSymbol attributeClass, 
			IMethodSymbol constructor, 
			ImmutableArray<ITypedConstant> constructorArguments)
		{
			AttributeClass = attributeClass;
			AttributeConstructor = constructor;
			ConstructorArguments = constructorArguments;
		}
		internal XmlAttributeData (AttributeData attribute)
		{
			AttributeClass = attribute.AttributeClass!;
			AttributeConstructor = attribute.AttributeConstructor!;
			ConstructorArguments = attribute.ConstructorArguments
				.Select(ca => (ITypedConstant) new XmlTypedConstant(ca)).ToImmutableArray();
		}
		public INamedTypeSymbol AttributeClass { get; }

		public ImmutableArray<ITypedConstant> ConstructorArguments { get; }
		public IMethodSymbol AttributeConstructor { get; }
	}

	internal struct XmlTypedConstant : ITypedConstant
	{
		internal XmlTypedConstant (ITypeSymbol type, object value)
		{
			Type = type;
			Value = value;
		}
		internal XmlTypedConstant (TypedConstant typedConstant)
		{
			Type = typedConstant.Type!;
			Value = typedConstant.Value ?? typedConstant.Values;
		}
		public ITypeSymbol Type { get; }
		public object Value { get; }
	}
}
