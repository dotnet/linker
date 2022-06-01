﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	public static class StaticAbstractInterfaceMethodsLibrary
	{
		public static void Main ()
		{
			InterfaceMethodsUsedThroughConstrainedType.Test ();
			InterfaceWithMethodsUsedEachWay.Test ();
			InterfaceMethodUsedOnConcreteType.Test ();
			InterfaceMethodsKeptThroughReflection.Test ();
			StaticInterfaceInheritance.Test ();
			GenericStaticInterface.Test ();
			RecursiveGenericInterface.Test ();
			UnusedInterfaces.Test ();
		}

		[Kept]
		public static class InterfaceMethodsUsedThroughConstrainedType
		{
			[Kept]
			public interface IUsedThroughConstrainedType
			{
				[Kept]
				static abstract int UsedThroughConstrainedType ();
			}

			[Kept]
			internal interface IUsedThroughConstrainedTypeInternal
			{
				static abstract int UsedThroughConstrainedType ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IUsedThroughConstrainedType))]
			[KeptInterface (typeof (IUsedThroughConstrainedTypeInternal))]
			public class UsesIUsedThroughConstrainedTypeMethods : IUsedThroughConstrainedType, IUsedThroughConstrainedTypeInternal
			{
				[Kept]
				[KeptOverride (typeof (IUsedThroughConstrainedType))]
				[RemovedOverride (typeof (IUsedThroughConstrainedTypeInternal))]
				public static int UsedThroughConstrainedType () => 0;
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IUsedThroughConstrainedType))]
			[KeptInterface (typeof (IUsedThroughConstrainedTypeInternal))]
			public class UnusedIUsedThroughConstrainedTypeMethods : IUsedThroughConstrainedType, IUsedThroughConstrainedTypeInternal
			{
				[Kept]
				[KeptOverride (typeof (IUsedThroughConstrainedType))]
				[RemovedOverride (typeof (IUsedThroughConstrainedTypeInternal))]
				public static int UsedThroughConstrainedType () => 0;
			}

			private class UnusedIUsedThroughConstrainedTypeMethodsPrivate : IUsedThroughConstrainedType, IUsedThroughConstrainedTypeInternal
			{
				public static int UsedThroughConstrainedType () => 0;
			}

			[Kept]
			public static void CallMethodOnConstrainedType<T> () where T : IUsedThroughConstrainedType
			{
				T.UsedThroughConstrainedType ();
			}

			[Kept]
			public static void Test ()
			{
				CallMethodOnConstrainedType<UsesIUsedThroughConstrainedTypeMethods> ();
				Type t = typeof (UnusedIUsedThroughConstrainedTypeMethods);

				ExplicitImplementation.Test ();
			}

			[Kept]
			public static class ExplicitImplementation
			{
				[Kept]
				[KeptMember (".ctor()")]
				[KeptInterface (typeof (IUsedThroughConstrainedTypeExplicitImplementation))]
				public class UsedIUsedThroughConstrainedTypeExplicitMethods : IUsedThroughConstrainedTypeExplicitImplementation
				{
					[Kept]
					[KeptOverride (typeof (IUsedThroughConstrainedTypeExplicitImplementation))]
					static int IUsedThroughConstrainedTypeExplicitImplementation.UsedThroughConstrainedType () => 0;
				}

				[Kept]
				[KeptMember (".ctor()")]
				[KeptInterface (typeof (IUsedThroughConstrainedTypeExplicitImplementation))]
				public class UnusedIUsedThroughConstrainedTypeExplicitMethods
					: IUsedThroughConstrainedTypeExplicitImplementation
				{
					[Kept]
					[KeptOverride (typeof (IUsedThroughConstrainedTypeExplicitImplementation))]
					static int IUsedThroughConstrainedTypeExplicitImplementation.UsedThroughConstrainedType () => 0;
				}

				[Kept]
				public interface IUsedThroughConstrainedTypeExplicitImplementation
				{
					[Kept]
					static abstract int UsedThroughConstrainedType ();
				}

				[Kept]
				public static void CallTypeConstrainedMethod<T> () where T : IUsedThroughConstrainedTypeExplicitImplementation
				{
					T.UsedThroughConstrainedType ();
				}

				[Kept]
				public static void Test ()
				{
					CallTypeConstrainedMethod<UsedIUsedThroughConstrainedTypeExplicitMethods> ();

					Type t = typeof (UnusedIUsedThroughConstrainedTypeExplicitMethods);
				}
			}
		}

		[Kept]
		public static class InterfaceMethodUsedOnConcreteType
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IUsedOnConcreteType))]
			[KeptInterface (typeof (IUsedOnConcreteTypeInternal))]
			public class UsesIUsedOnConcreteTypeMethods : IUsedOnConcreteType, IUsedOnConcreteTypeInternal
			{
				[Kept]
				[KeptOverride (typeof (IUsedOnConcreteType))]
				[RemovedOverride (typeof (IUsedOnConcreteTypeInternal))]
				public static int UsedOnConcreteType () => 0;
			}

			[Kept]
			public interface IUsedOnConcreteType
			{
				[Kept]
				public static abstract int UsedOnConcreteType ();
			}

			[Kept]
			internal interface IUsedOnConcreteTypeInternal
			{
				static abstract int UsedOnConcreteType ();
			}

			[Kept]
			public static void Test ()
			{
				UsesIUsedOnConcreteTypeMethods.UsedOnConcreteType ();
			}
		}

		[Kept]
		public static class InterfaceWithMethodsUsedEachWay
		{

			[Kept]
			public interface IUsedEveryWay
			{
				[Kept]
				public static abstract int UsedThroughConstrainedType ();

				[Kept]
				public static abstract int UsedOnConcreteType ();

				[Kept]
				public static abstract int UsedThroughConstrainedTypeExplicit ();
			}

			[Kept]
			internal interface IUsedEveryWayInternal
			{
				[Kept]
				internal static abstract int UsedThroughConstrainedType ();

				internal static abstract int UsedOnConcreteType ();

				[Kept]
				internal static abstract int UsedThroughConstrainedTypeExplicit ();
			}

			[Kept]
			internal interface IUnusedEveryWayInternal
			{
				internal static abstract int UsedThroughConstrainedType ();

				internal static abstract int UsedOnConcreteType ();

				internal static abstract int UsedThroughConstrainedTypeExplicit ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IUsedEveryWay))]
			[KeptInterface (typeof (IUsedEveryWayInternal))]
			[KeptInterface (typeof (IUnusedEveryWayInternal))]
			public class UsedIUsedEveryWay : IUsedEveryWay, IUsedEveryWayInternal, IUnusedEveryWayInternal
			{

				[Kept]
				[KeptOverride (typeof (IUsedEveryWay))]
				static int IUsedEveryWay.UsedThroughConstrainedTypeExplicit () => 0;

				[Kept]
				[KeptOverride (typeof (IUsedEveryWayInternal))]
				static int IUsedEveryWayInternal.UsedThroughConstrainedTypeExplicit () => 0;

				static int IUnusedEveryWayInternal.UsedThroughConstrainedTypeExplicit () => 0;

				[Kept]
				[KeptOverride (typeof (IUsedEveryWay))]
				[RemovedOverride (typeof (IUsedEveryWayInternal))]
				[RemovedOverride (typeof (IUnusedEveryWayInternal))]
				public static int UsedOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IUsedEveryWay))]
				[KeptOverride (typeof (IUsedEveryWayInternal))]
				[RemovedOverride (typeof (IUnusedEveryWayInternal))]
				public static int UsedThroughConstrainedType () => 0;
			}

			[Kept]
			[KeptInterface (typeof (IUsedEveryWay))]
			[KeptInterface (typeof (IUsedEveryWayInternal))]
			[KeptInterface (typeof (IUnusedEveryWayInternal))]
			internal class UnusedIUsedEveryWayInternal : IUsedEveryWay, IUsedEveryWayInternal, IUnusedEveryWayInternal
			{
				[Kept]
				[KeptOverride (typeof (IUsedEveryWay))]
				static int IUsedEveryWay.UsedThroughConstrainedTypeExplicit () => 0;

				[Kept]
				[KeptOverride (typeof (IUsedEveryWayInternal))]
				static int IUsedEveryWayInternal.UsedThroughConstrainedTypeExplicit () => 0;

				static int IUnusedEveryWayInternal.UsedThroughConstrainedTypeExplicit () => 0;

				[Kept]
				public static int UsedOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IUsedEveryWay))]
				[KeptOverride (typeof (IUsedEveryWayInternal))]
				public static int UsedThroughConstrainedType () => 0;
			}

			[Kept]
			public static void CallTypeConstrainedMethods<T> () where T : IUsedEveryWay
			{
				T.UsedThroughConstrainedType ();
				T.UsedThroughConstrainedTypeExplicit ();
			}

			[Kept]
			internal static void CallTypeConstrainedMethodsInternal<T> () where T : IUsedEveryWayInternal
			{
				T.UsedThroughConstrainedType ();
				T.UsedThroughConstrainedTypeExplicit ();
			}

			[Kept]
			public static void Test ()
			{
				UsedIUsedEveryWay.UsedOnConcreteType ();
				CallTypeConstrainedMethods<UsedIUsedEveryWay> ();
				CallTypeConstrainedMethodsInternal<UsedIUsedEveryWay> ();

				Type t = typeof (UnusedIUsedEveryWayInternal);
			}
		}

		[Kept]
		public static class InterfaceMethodsKeptThroughReflection
		{
			[Kept]
			public interface IMethodsKeptThroughReflection
			{
				[Kept]
				public static abstract int UnusedMethod ();

				[Kept]
				public static abstract int UsedOnConcreteType ();

				[Kept]
				public static abstract int UsedOnConstrainedType ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IMethodsKeptThroughReflection))]
			public class UsedMethodsKeptThroughtReflection : IMethodsKeptThroughReflection
			{
				[Kept]
				[KeptOverride (typeof (IMethodsKeptThroughReflection))]
				public static int UnusedMethod () => 0;

				[Kept]
				[KeptOverride (typeof (IMethodsKeptThroughReflection))]
				public static int UsedOnConstrainedType () => 0;

				[Kept]
				[KeptOverride (typeof (IMethodsKeptThroughReflection))]
				public static int UsedOnConcreteType () => 0;
			}

			[Kept]
			[KeptInterface (typeof (IMethodsKeptThroughReflection))]
			internal class UnusedMethodsKeptThroughtReflection : IMethodsKeptThroughReflection
			{
				[Kept]
				[KeptOverride (typeof (IMethodsKeptThroughReflection))]
				public static int UnusedMethod () => 0;

				[Kept]
				[KeptOverride (typeof (IMethodsKeptThroughReflection))]
				public static int UsedOnConstrainedType () => 0;

				[Kept]
				[KeptOverride (typeof (IMethodsKeptThroughReflection))]
				public static int UsedOnConcreteType () => 0;
			}

			[Kept]
			public static void Test ()
			{
				typeof (IMethodsKeptThroughReflection).RequiresPublicMethods ();
				UsedMethodsKeptThroughtReflection.UsedOnConcreteType ();
				UseMethodThroughTypeConstraint<UsedMethodsKeptThroughtReflection> ();

				Type t = typeof (UnusedMethodsKeptThroughtReflection);

				[Kept]
				static void UseMethodThroughTypeConstraint<T> () where T : IMethodsKeptThroughReflection
				{
					T.UsedOnConstrainedType ();
				}
			}
		}

		[Kept]
		public static class InterfaceHasStaticAndInstanceMethods
		{
			[Kept]
			public interface IStaticAndInstanceMethods
			{
				[Kept]
				public static abstract int StaticMethodCalledOnConcreteType ();

				[Kept]
				public static abstract int StaticMethodExplicitImpl ();

				[Kept]
				public int InstanceMethod ();
			}

			[Kept]
			internal interface IStaticAndInstanceMethodsInternalUnused
			{
				static abstract int StaticMethodCalledOnConcreteType ();

				static abstract int StaticMethodExplicitImpl ();

				int InstanceMethod ();
			}

			[Kept]
			internal interface IStaticAndInstanceMethodsInternalUsed
			{
				static abstract int StaticMethodCalledOnConcreteType ();

				[Kept]
				static abstract int StaticMethodExplicitImpl ();

				[Kept]
				int InstanceMethod ();
			}

			[Kept]
			internal static void CallExplicitImplMethod<T> () where T : IStaticAndInstanceMethods, new()
			{
				T.StaticMethodExplicitImpl ();
				IStaticAndInstanceMethods x = new T ();
				x.InstanceMethod ();
			}

			[Kept]
			internal static void CallExplicitImplMethodInternalUsed<T> () where T : IStaticAndInstanceMethodsInternalUsed, new()
			{
				T.StaticMethodExplicitImpl ();
				IStaticAndInstanceMethodsInternalUsed x = new T ();
				x.InstanceMethod ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IStaticAndInstanceMethods))]
			[KeptInterface (typeof (IStaticAndInstanceMethodsInternalUsed))]
			[KeptInterface (typeof (IStaticAndInstanceMethodsInternalUnused))]
			public class UsesAllMethods : IStaticAndInstanceMethods, IStaticAndInstanceMethodsInternalUnused, IStaticAndInstanceMethodsInternalUsed
			{
				[Kept]
				[KeptOverride (typeof (IStaticAndInstanceMethods))]
				[RemovedOverride (typeof (IStaticAndInstanceMethodsInternalUsed))]
				[RemovedOverride (typeof (IStaticAndInstanceMethodsInternalUnused))]
				public static int StaticMethodCalledOnConcreteType () => 0;

				[Kept]
				// No .override / MethodImpl for implicit instance methods
				public int InstanceMethod () => 0;

				[Kept]
				[KeptOverride (typeof (IStaticAndInstanceMethods))]
				static int IStaticAndInstanceMethods.StaticMethodExplicitImpl () => 0;

				static int IStaticAndInstanceMethodsInternalUnused.StaticMethodExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IStaticAndInstanceMethodsInternalUsed))]
				static int IStaticAndInstanceMethodsInternalUsed.StaticMethodExplicitImpl () => 0;

				[Kept]
				public static void Test ()
				{
					UsesAllMethods.StaticMethodCalledOnConcreteType ();
					CallExplicitImplMethod<UsesAllMethods> ();
					CallExplicitImplMethodInternalUsed<UsesAllMethods> ();
				}
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IStaticAndInstanceMethods))]
			[KeptInterface (typeof (IStaticAndInstanceMethodsInternalUsed))]
			[KeptInterface (typeof (IStaticAndInstanceMethodsInternalUnused))]
			public class UnusedMethods : IStaticAndInstanceMethods, IStaticAndInstanceMethodsInternalUnused, IStaticAndInstanceMethodsInternalUsed
			{
				[Kept]
				[KeptOverride (typeof (IStaticAndInstanceMethods))]
				[RemovedOverride (typeof (IStaticAndInstanceMethodsInternalUsed))]
				[RemovedOverride (typeof (IStaticAndInstanceMethodsInternalUnused))]
				public static int StaticMethodCalledOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IStaticAndInstanceMethods))]
				static int IStaticAndInstanceMethods.StaticMethodExplicitImpl () => 0;

				static int IStaticAndInstanceMethodsInternalUnused.StaticMethodExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IStaticAndInstanceMethodsInternalUsed))]
				static int IStaticAndInstanceMethodsInternalUsed.StaticMethodExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IStaticAndInstanceMethods))]
				int IStaticAndInstanceMethods.InstanceMethod () => 0;

				[Kept]
				[KeptOverride (typeof (IStaticAndInstanceMethodsInternalUsed))]
				int IStaticAndInstanceMethodsInternalUsed.InstanceMethod () => 0;

				int IStaticAndInstanceMethodsInternalUnused.InstanceMethod () => 0;

				[Kept]
				public static void Test () { }
			}

			[Kept]
			public static void Test ()
			{
				UsesAllMethods.Test ();
				UnusedMethods.Test ();
			}
		}

		[Kept]
		public static class StaticInterfaceInheritance
		{
			[Kept]
			public interface IBase
			{
				[Kept]
				public static abstract int UsedOnConcreteType ();

				[Kept]
				public static abstract int UsedOnBaseOnlyConstrainedTypeImplicitImpl ();

				[Kept]
				public static abstract int UsedOnConstrainedTypeExplicitImpl ();

				[Kept]
				public static abstract int UnusedImplicitImpl ();

				[Kept]
				public static abstract int UnusedExplicitImpl ();
			}

			[Kept]
			[KeptInterface (typeof (IBase))]
			public interface IInheritsFromBase : IBase
			{
				[Kept]
				public static abstract int UsedOnConcreteType ();

				[Kept]
				public static abstract int UsedOnBaseOnlyConstrainedTypeImplicitImpl ();

				[Kept]
				public static abstract int UsedOnConstrainedTypeExplicitImpl ();

				[Kept]
				public static abstract int UnusedImplicitImpl ();

				[Kept]
				public static abstract int UnusedExplicitImpl ();
			}

			[Kept]
			internal interface IBaseInternal
			{
				static abstract int UsedOnConcreteType ();

				[Kept]
				static abstract int UsedOnBaseOnlyConstrainedTypeImplicitImpl ();

				[Kept]
				static abstract int UsedOnConstrainedTypeExplicitImpl ();

				static abstract int UnusedImplicitImpl ();

				static abstract int UnusedExplicitImpl ();
			}

			[Kept]
			[KeptInterface (typeof (IBase))]
			[KeptInterface (typeof (IBaseInternal))]
			[KeptInterface (typeof (IUnusedInterface))]
			internal interface IInheritsFromMultipleBases : IBase, IBaseInternal, IUnusedInterface
			{
				static abstract int UsedOnConcreteType ();
				static abstract int UsedOnBaseOnlyConstrainedTypeImplicitImpl ();

				[Kept]
				static abstract int UsedOnConstrainedTypeExplicitImpl ();
				static abstract int UnusedImplicitImpl ();
				static abstract int UnusedExplicitImpl ();
			}

			[Kept]
			internal interface IUnusedInterface
			{
				static abstract int UsedOnConcreteType ();

				static abstract int UnusedImplicitImpl ();

				static abstract int UnusedExplicitImpl ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IBase))]
			[KeptInterface (typeof (IInheritsFromBase))]
			public class ImplementsIInheritsFromBase : IInheritsFromBase
			{
				[Kept]
				[KeptOverride (typeof (IInheritsFromBase))]
				[KeptOverride (typeof (IBase))]
				public static int UsedOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IBase))]
				[KeptOverride (typeof (IInheritsFromBase))]
				public static int UsedOnBaseOnlyConstrainedTypeImplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IInheritsFromBase))]
				static int IInheritsFromBase.UsedOnConstrainedTypeExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IBase))]
				static int IBase.UsedOnConstrainedTypeExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IBase))]
				[KeptOverride (typeof (IInheritsFromBase))]
				public static int UnusedImplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IBase))]
				static int IBase.UnusedExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IInheritsFromBase))]
				static int IInheritsFromBase.UnusedExplicitImpl () => 0;

				[Kept]
				public static void Test ()
				{
					ImplementsIInheritsFromBase.UsedOnConcreteType ();
					CallBase1TypeConstrainedMethod<ImplementsIInheritsFromBase> ();
					CallSingleInheritTypeConstrainedMethod<ImplementsIInheritsFromBase> ();
				}
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IInheritsFromMultipleBases))]
			[KeptInterface (typeof (IBase))]
			[KeptInterface (typeof (IBaseInternal))]
			[KeptInterface (typeof (IUnusedInterface))]
			public class ImplementsIInheritsFromTwoBases : IInheritsFromMultipleBases
			{
				[Kept]
				[RemovedOverride (typeof (IInheritsFromMultipleBases))]
				[KeptOverride (typeof (IBase))]
				[RemovedOverride (typeof (IBaseInternal))]
				[RemovedOverride (typeof (IUnusedInterface))]
				public static int UsedOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IBase))]
				[KeptOverride (typeof (IBaseInternal))]
				[RemovedOverride (typeof (IInheritsFromMultipleBases))]
				public static int UsedOnBaseOnlyConstrainedTypeImplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IBase))]
				static int IBase.UsedOnConstrainedTypeExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IBaseInternal))]
				static int IBaseInternal.UsedOnConstrainedTypeExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IInheritsFromMultipleBases))]
				static int IInheritsFromMultipleBases.UsedOnConstrainedTypeExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IBase))]
				public static int UnusedImplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IBase))]
				static int IBase.UnusedExplicitImpl () => 0;

				static int IBaseInternal.UnusedExplicitImpl () => 0;

				static int IInheritsFromMultipleBases.UnusedExplicitImpl () => 0;

				static int IUnusedInterface.UnusedExplicitImpl () => 0;

				[Kept]
				public static void Test ()
				{
					ImplementsIInheritsFromTwoBases.UsedOnConcreteType ();
					CallBase1TypeConstrainedMethod<ImplementsIInheritsFromTwoBases> ();
					CallBase2TypeConstrainedMethod<ImplementsIInheritsFromTwoBases> ();
					CallDoubleInheritTypeConstrainedMethod<ImplementsIInheritsFromTwoBases> ();
				}
			}

			[Kept]
			public static void CallBase1TypeConstrainedMethod<T> () where T : IBase
			{
				T.UsedOnBaseOnlyConstrainedTypeImplicitImpl ();
				T.UsedOnConstrainedTypeExplicitImpl ();
			}

			[Kept]
			internal static void CallBase2TypeConstrainedMethod<T> () where T : IBaseInternal
			{
				T.UsedOnBaseOnlyConstrainedTypeImplicitImpl ();
				T.UsedOnConstrainedTypeExplicitImpl ();
			}

			[Kept]
			public static void CallSingleInheritTypeConstrainedMethod<T> () where T : IInheritsFromBase
			{
				T.UsedOnConstrainedTypeExplicitImpl ();
			}

			[Kept]
			internal static void CallDoubleInheritTypeConstrainedMethod<T> () where T : IInheritsFromMultipleBases
			{
				T.UsedOnConstrainedTypeExplicitImpl ();
			}

			[Kept]
			public static void Test ()
			{
				ImplementsIInheritsFromBase.Test ();
				ImplementsIInheritsFromTwoBases.Test ();
			}
		}

		[Kept]
		public static class GenericStaticInterface
		{
			[Kept]
			public interface IGenericInterface<T>
			{
				[Kept]
				public static abstract T GetT ();
				[Kept]
				public static abstract T GetTExplicit ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IGenericInterface<int>))]
			public class ImplementsGenericInterface : IGenericInterface<int>
			{
				[Kept]
				[KeptOverride (typeof (IGenericInterface<int>))]
				public static int GetT () => 0;

				[Kept]
				[KeptOverride (typeof (IGenericInterface<int>))]
				static int IGenericInterface<int>.GetTExplicit () => 0;
			}

			[Kept]
			[KeptInterface (typeof (IGenericInterface<int>))]
			internal class ImplementsGenericInterfaceUnused : IGenericInterface<int>
			{
				[Kept]
				[KeptOverride (typeof (IGenericInterface<int>))]
				public static int GetT () => 0;

				[Kept]
				[KeptOverride (typeof (IGenericInterface<int>))]
				static int IGenericInterface<int>.GetTExplicit () => 0;
			}

			[Kept]
			public static void Test ()
			{
				ImplementsGenericInterface.GetT ();
				CallExplicitMethod<ImplementsGenericInterface, int> ();
				Type t = typeof (ImplementsGenericInterfaceUnused);
			}

			[Kept]
			public static void CallExplicitMethod<T, U> () where T : IGenericInterface<U>
			{
				T.GetTExplicit ();
			}
		}

		[Kept]
		public static class RecursiveGenericInterface
		{
			[Kept]
			public interface IGenericInterface<T> where T : IGenericInterface<T>
			{
				[Kept]
				public static abstract T GetT ();
				[Kept]
				public static abstract T GetTExplicit ();
			}

			[Kept]
			internal interface IGenericInterfaceInternal<T> where T : IGenericInterfaceInternal<T>
			{
				static abstract T GetT ();

				static abstract T GetTExplicit ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
			[KeptInterface (typeof (IGenericInterfaceInternal<ImplementsIGenericInterfaceOfSelf>))]
			public class ImplementsIGenericInterfaceOfSelf : IGenericInterface<ImplementsIGenericInterfaceOfSelf>, IGenericInterfaceInternal<ImplementsIGenericInterfaceOfSelf>
			{
				[Kept]
				[KeptOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
				[RemovedOverride (typeof (IGenericInterfaceInternal<ImplementsIGenericInterfaceOfSelf>))]
				public static ImplementsIGenericInterfaceOfSelf GetT () => throw new NotImplementedException ();

				[Kept]
				[KeptOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
				static ImplementsIGenericInterfaceOfSelf IGenericInterface<ImplementsIGenericInterfaceOfSelf>.GetTExplicit ()
					=> throw new NotImplementedException ();

				static ImplementsIGenericInterfaceOfSelf IGenericInterfaceInternal<ImplementsIGenericInterfaceOfSelf>.GetTExplicit ()
					=> throw new NotImplementedException ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
			[KeptInterface (typeof (IGenericInterfaceInternal<ImplementsIGenericInterfaceOfSelf>))]
			public class ImplementsIGenericInterfaceOfOther : IGenericInterface<ImplementsIGenericInterfaceOfSelf>, IGenericInterfaceInternal<ImplementsIGenericInterfaceOfSelf>
			{
				[Kept]
				[KeptOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
				[RemovedOverride (typeof (IGenericInterfaceInternal<ImplementsIGenericInterfaceOfSelf>))]
				public static ImplementsIGenericInterfaceOfSelf GetT () => throw new NotImplementedException ();

				[Kept]
				[KeptOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
				static ImplementsIGenericInterfaceOfSelf IGenericInterface<ImplementsIGenericInterfaceOfSelf>.GetTExplicit ()
					=> throw new NotImplementedException ();

				static ImplementsIGenericInterfaceOfSelf IGenericInterfaceInternal<ImplementsIGenericInterfaceOfSelf>.GetTExplicit ()
					=> throw new NotImplementedException ();
			}

			[Kept]
			[KeptInterface (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelfUnused>))]
			[KeptInterface (typeof (IGenericInterfaceInternal<ImplementsIGenericInterfaceOfSelfUnused>))]
			internal class ImplementsIGenericInterfaceOfSelfUnused : IGenericInterface<ImplementsIGenericInterfaceOfSelfUnused>, IGenericInterfaceInternal<ImplementsIGenericInterfaceOfSelfUnused>
			{
				[Kept]
				[KeptOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelfUnused>))]
				public static ImplementsIGenericInterfaceOfSelfUnused GetT () => throw new NotImplementedException ();

				[Kept]
				[KeptOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelfUnused>))]
				static ImplementsIGenericInterfaceOfSelfUnused IGenericInterface<ImplementsIGenericInterfaceOfSelfUnused>.GetTExplicit ()
					=> throw new NotImplementedException ();

				static ImplementsIGenericInterfaceOfSelfUnused IGenericInterfaceInternal<ImplementsIGenericInterfaceOfSelfUnused>.GetTExplicit ()
					=> throw new NotImplementedException ();
			}

			[Kept]
			public static void CallExplicitGetT<T> () where T : IGenericInterface<ImplementsIGenericInterfaceOfSelf>
			{
				T.GetTExplicit ();
			}

			[Kept]
			public static void Test ()
			{
				ImplementsIGenericInterfaceOfSelf.GetT ();
				ImplementsIGenericInterfaceOfOther.GetT ();
				CallExplicitGetT<ImplementsIGenericInterfaceOfSelf> ();
				CallExplicitGetT<ImplementsIGenericInterfaceOfOther> ();

				Type t = typeof (ImplementsIGenericInterfaceOfSelfUnused);
			}
		}

		[Kept]
		public static class UnusedInterfaces
		{
			[Kept]
			internal interface IUnusedInterface
			{
				int UnusedMethodImplicit ();
				int UnusedMethodExplicit ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IUnusedInterface))]
			public class ImplementsUnusedInterface : IUnusedInterface
			{
				int IUnusedInterface.UnusedMethodExplicit () => 0;

				[Kept]
				// Bug: We should be able to remove this override
				//[RemovedOverride (typeof (IUnusedInterface))]
				public int UnusedMethodImplicit () => 0;
			}

			[Kept]
			public static void Test ()
			{
				Type t = typeof (ImplementsUnusedInterface);
			}
		}
	}
}

