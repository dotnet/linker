// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	public sealed record PropertyValue (IPropertyReferenceOperation? PropertyReference);

	public struct PropertyLattice : ILattice<PropertyValue>
	{
		public PropertyValue Top => new PropertyValue (null);

		public PropertyValue Meet (PropertyValue left, PropertyValue right)
		{
			if (left == right)
				return left;
			if (left.PropertyReference == null)
				return right;
			if (right.PropertyReference == null)
				return left;
			// Both non-null and different shouldn't happen.
			// We assume that a flow capture can capture only a single property.
			throw new InvalidOperationException ();
		}
	}
}