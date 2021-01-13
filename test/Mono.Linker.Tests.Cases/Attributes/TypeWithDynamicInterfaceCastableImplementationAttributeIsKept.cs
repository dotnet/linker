// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	public class TypeWithDynamicInterfaceCastableImplementationAttributeIsKept
	{
		public static void Main()
		{
			Foo foo = new Foo ();
			GetIBar (foo).Bar ();
			IBaz baz = GetIBaz (foo);
		}
		
		[Kept]
		private static IBar GetIBar (object obj)
		{
			return (IBar) obj;
		}

		[Kept]
		private static IBaz GetIBaz (object obj)
		{
			return (IBaz) obj;
		}
	}

	[Kept]
	[KeptMember (".ctor()")]
	class Foo : IDynamicInterfaceCastable
	{
		[Kept]
		public RuntimeTypeHandle GetInterfaceImplementation (RuntimeTypeHandle interfaceType)
		{
			throw new NotImplementedException ();
		}

		[Kept]
		public bool IsInterfaceImplemented (RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
		{
			throw new NotImplementedException ();
		}
	}

	[Kept]
	interface IBar
	{
		[Kept]
		void Bar ();
	}

	[Kept]
	[KeptAttributeAttribute (typeof(DynamicInterfaceCastableImplementationAttribute))]
	[KeptInterface(typeof(IBar))]
	[DynamicInterfaceCastableImplementation]
	interface IBarImpl : IBar
	{
		[Kept]
		void IBar.Bar () { }
	}

	[Kept]
	interface IBaz
	{
		void Baz ();
	}

	[Kept]
	[KeptAttributeAttribute (typeof (DynamicInterfaceCastableImplementationAttribute))]
	[KeptInterface (typeof (IBaz))]
	[DynamicInterfaceCastableImplementation]
	interface IBazImpl : IBaz
	{
		void IBaz.Baz () { }
	}

	interface IFrob
	{
		void Frob () { }
	}

	[DynamicInterfaceCastableImplementation]
	interface IFrobImpl : IFrob
	{
		void IFrob.Frob () { }
	}
}
