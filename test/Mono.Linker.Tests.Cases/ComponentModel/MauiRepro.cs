// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.ComponentModel
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class MauiRepro
	{
		public static void Main ()
		{
			VisualTypeConverter.CreateVisual(typeof(System.String));
			var x = new Visual ();
		}
	}
	[Kept]
	[KeptAttributeAttribute (typeof (TypeConverterAttribute))]
	[TypeConverter (typeof (VisualTypeConverter))]
	public interface IVisual
	{ }

	[Kept]
	[KeptInterface (typeof (IVisual))]
	public class Visual : IVisual
	{
		[Kept]
		public Visual () { }
	}

	[Kept]
	[KeptBaseType (typeof (TypeConverter))]
	public class VisualTypeConverter : TypeConverter
	{
		[Kept]
		public static IVisual CreateVisual (
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type visualType)
		{
			try {
				return (IVisual) Activator.CreateInstance (visualType);
			} catch {
			}

			return null;
		}

		[Kept]
		public VisualTypeConverter () { }
	}
}
