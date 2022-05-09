// Copyright (c) .NET Foundation and contributors. All rights reserved.
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
	public class StaticAbstractInterfaceMethods
	{
		static void Main ()
		{
			InterfaceMethodsUsedThroughConstrainedType.Test ();
			InterfaceWithMethodsUsedEachWay.Test ();
			InterfaceMethodUsedOnConcreteType.Test ();
			InterfaceMethodsKeptThroughReflection.Test ();
			StaticInterfaceInheritance.Test ();
			GenericStaticInterface.Test ();
			RecursiveGenericInterface.Test ();
		}

		[Kept]
		class InterfaceMethodsUsedThroughConstrainedType
		{
			[Kept]
			internal interface IUsedThroughConstrainedType
			{
				[Kept]
				static abstract int UsedThroughConstrainedType ();
			}

			[Kept]
			[KeptInterface (typeof (IUsedThroughConstrainedType))]
			class UsesIUsedThroughConstrainedTypeMethods : IUsedThroughConstrainedType
			{
				[Kept]
				[KeptOverride (typeof (IUsedThroughConstrainedType))]
				public static int UsedThroughConstrainedType () => 0;
			}

			[Kept]
			[KeptInterface (typeof (IUsedThroughConstrainedType))]
			class UnusedIUsedThroughConstrainedTypeMethods : IUsedThroughConstrainedType
			{
				[Kept]
				[KeptOverride (typeof (IUsedThroughConstrainedType))]
				public static int UsedThroughConstrainedType () => 0;
			}

			[Kept]
			static void CallMethodOnConstrainedType<T> () where T : IUsedThroughConstrainedType
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
			public class ExplicitImplementation
			{
				[Kept]
				[KeptInterface (typeof (IUsedThroughConstrainedTypeExplicitImplementation))]
				class UsedIUsedThroughConstrainedTypeExplicitMethods : IUsedThroughConstrainedTypeExplicitImplementation
				{
					[Kept]
					[KeptOverride (typeof (IUsedThroughConstrainedTypeExplicitImplementation))]
					static int IUsedThroughConstrainedTypeExplicitImplementation.UsedThroughConstrainedType () => 0;
				}

				[Kept]
				[KeptInterface (typeof (IUsedThroughConstrainedTypeExplicitImplementation))]
				class UnusedIUsedThroughConstrainedTypeExplicitMethods : IUsedThroughConstrainedTypeExplicitImplementation
				{
					[Kept]
					[KeptOverride (typeof (IUsedThroughConstrainedTypeExplicitImplementation))]
					static int IUsedThroughConstrainedTypeExplicitImplementation.UsedThroughConstrainedType () => 0;
				}

				[Kept]
				internal interface IUsedThroughConstrainedTypeExplicitImplementation
				{
					[Kept]
					static abstract int UsedThroughConstrainedType ();
				}

				[Kept]
				static void CallTypeConstrainedMethod<T> () where T : IUsedThroughConstrainedTypeExplicitImplementation
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
		class InterfaceMethodUsedOnConcreteType
		{
			[Kept]
			class UsesIUsedOnConcreteTypeMethods : IUsedOnConcreteType
			{
				[Kept]
				[RemovedOverride (typeof (IUsedOnConcreteType))]
				public static int UsedOnConcreteType () => 0;
			}

			[Kept]
			class UnusedIUsedOnConcreteTypeMethods : IUsedOnConcreteType
			{
				public static int UsedOnConcreteType () => 0;
			}

			internal interface IUsedOnConcreteType
			{
				static abstract int UsedOnConcreteType ();
			}

			[Kept]
			public static void Test ()
			{
				UsesIUsedOnConcreteTypeMethods.UsedOnConcreteType ();

				Type t = typeof (UnusedIUsedOnConcreteTypeMethods);
			}
		}

		[Kept]
		class InterfaceWithMethodsUsedEachWay
		{

			[Kept]
			internal interface IUsedEveryWay
			{
				[Kept]
				static abstract int UsedThroughConstrainedType ();

				static abstract int UsedOnConcreteType ();

				[Kept]
				static abstract int UsedThroughConstrainedTypeExplicit ();
			}

			[Kept]
			[KeptInterface (typeof (IUsedEveryWay))]
			class UsedIUsedEveryWay : IUsedEveryWay
			{

				[Kept]
				[KeptOverride (typeof (IUsedEveryWay))]
				static int IUsedEveryWay.UsedThroughConstrainedTypeExplicit () => 0;

				[Kept]
				[RemovedOverride (typeof (IUsedEveryWay))]
				public static int UsedOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IUsedEveryWay))]
				public static int UsedThroughConstrainedType () => 0;
			}

			[Kept]
			[KeptInterface (typeof (IUsedEveryWay))]
			class UnusedIUsedEveryWay : IUsedEveryWay
			{
				[Kept]
				[KeptOverride (typeof (IUsedEveryWay))]
				static int IUsedEveryWay.UsedThroughConstrainedTypeExplicit () => 0;

				public static int UsedOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IUsedEveryWay))]
				public static int UsedThroughConstrainedType () => 0;
			}

			[Kept]
			static void CallTypeConstrainedMethods<T> () where T : IUsedEveryWay
			{
				T.UsedThroughConstrainedType ();
				T.UsedThroughConstrainedTypeExplicit ();
			}

			[Kept]
			public static void Test ()
			{
				UsedIUsedEveryWay.UsedOnConcreteType ();
				CallTypeConstrainedMethods<UsedIUsedEveryWay> ();

				Type t = typeof (UnusedIUsedEveryWay);
			}
		}

		[Kept]
		internal class InterfaceMethodsKeptThroughReflection
		{
			[Kept]
			internal interface IMethodsKeptThroughReflection
			{
				[Kept]
				public static abstract int UnusedMethod ();

				[Kept]
				public static abstract int UsedOnConcreteType ();

				[Kept]
				public static abstract int UsedOnConstrainedType ();
			}

			[Kept]
			[KeptInterface (typeof (IMethodsKeptThroughReflection))]
			class UsedMethodsKeptThroughtReflection : IMethodsKeptThroughReflection
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
			class UnusedMethodsKeptThroughtReflection : IMethodsKeptThroughReflection
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

		class InterfaceHasStaticAndInstanceMethods
		{
			[Kept]
			interface IStaticAndInstanceMethods
			{
				static abstract int StaticMethodCalledOnConcreteType ();

				[Kept]
				static abstract int StaticMethodExplicitImpl ();

				[Kept]
				int InstanceMethod ();
			}

			[Kept]
			[KeptInterface (typeof (IStaticAndInstanceMethods))]
			class UsesAllMethods : IStaticAndInstanceMethods
			{
				[Kept]
				[RemovedOverride (typeof (IStaticAndInstanceMethods))]
				public static int StaticMethodCalledOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IStaticAndInstanceMethods))]
				public int InstanceMethod () => 0;

				[Kept]
				[KeptOverride (typeof (IStaticAndInstanceMethods))]
				static int IStaticAndInstanceMethods.StaticMethodExplicitImpl () => 0;

				[Kept]
				public static void Test ()
				{
					UsesAllMethods.StaticMethodCalledOnConcreteType ();
					var x = new UsesAllMethods ();
					x.InstanceMethod ();
					CallExplicitImplMethod<UsesAllMethods> ();

					void CallExplicitImplMethod<T> () where T : IStaticAndInstanceMethods
					{
						T.StaticMethodExplicitImpl ();
					}
				}
			}

			[Kept]
			[KeptInterface (typeof (IStaticAndInstanceMethods))]
			class UnusedMethods : IStaticAndInstanceMethods
			{
				public static int StaticMethodCalledOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IStaticAndInstanceMethods))]
				static int IStaticAndInstanceMethods.StaticMethodExplicitImpl () => 0;

				public int InstanceMethod () => 0;

				[Kept]
				public static void Test () { }
			}
		}

		[Kept]
		class StaticInterfaceInheritance
		{
			[Kept]
			interface IBase1
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
			[KeptInterface (typeof (IBase1))]
			interface IInheritsFromBase : IBase1
			{
				static abstract int UsedOnConcreteType ();
				static abstract int UsedOnBaseOnlyConstrainedTypeImplicitImpl ();

				[Kept]
				static abstract int UsedOnConstrainedTypeExplicitImpl ();
				static abstract int UnusedImplicitImpl ();
				static abstract int UnusedExplicitImpl ();
			}

			[Kept]
			interface IBase2
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
			[KeptInterface (typeof (IBase1))]
			[KeptInterface (typeof (IBase2))]
			interface IInheritsFromMultipleBases : IBase1, IBase2, IUnusedInterface
			{
				static abstract int UsedOnConcreteType ();
				static abstract int UsedOnBaseOnlyConstrainedTypeImplicitImpl ();

				[Kept]
				static abstract int UsedOnConstrainedTypeExplicitImpl ();
				static abstract int UnusedImplicitImpl ();
				static abstract int UnusedExplicitImpl ();
			}

			interface IUnusedInterface
			{
				static abstract int UsedOnConcreteType ();

				static abstract int UnusedImplicitImpl ();

				static abstract int UnusedExplicitImpl ();
			}

			[Kept]
			[KeptInterface (typeof (IBase1))]
			[KeptInterface (typeof (IInheritsFromBase))]
			class ImplementsIInheritsFromBase : IInheritsFromBase
			{
				[Kept]
				[RemovedOverride (typeof (IInheritsFromBase))]
				[RemovedOverride (typeof (IBase1))]
				public static int UsedOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof(IBase1))]
				[RemovedOverride (typeof (IInheritsFromBase))]
				public static int UsedOnBaseOnlyConstrainedTypeImplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof(IInheritsFromBase))]
				static int IInheritsFromBase.UsedOnConstrainedTypeExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof(IBase1))]
				static int IBase1.UsedOnConstrainedTypeExplicitImpl () => 0;

				public static int UnusedImplicitImpl () =>0;

				static int IBase1.UnusedExplicitImpl () => 0;

				static int IInheritsFromBase.UnusedExplicitImpl () => 0;

				[Kept]
				public static void Test ()
				{
					ImplementsIInheritsFromBase.UsedOnConcreteType ();
					CallBase1TypeConstrainedMethod<ImplementsIInheritsFromBase> ();
					CallSingleInheritTypeConstrainedMethod<ImplementsIInheritsFromBase> ();
				}
			}

			[KeptInterface (typeof (IInheritsFromMultipleBases))]
			[KeptInterface (typeof (IBase1))]
			[KeptInterface (typeof (IBase2))]
			// [RemovedInterface (typeof (IUnusedInterface))]
			class ImplementsIInheritsFromTwoBases : IInheritsFromMultipleBases
			{
				[Kept]
				[RemovedOverride (typeof (IInheritsFromMultipleBases))]
				[RemovedOverride (typeof (IBase1))]
				[RemovedOverride (typeof (IBase2))]
				[RemovedOverride (typeof (IUnusedInterface))]
				public static int UsedOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof(IBase1))]
				[KeptOverride (typeof(IBase2))]
				[RemovedOverride (typeof (IInheritsFromMultipleBases))]
				public static int UsedOnBaseOnlyConstrainedTypeImplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof(IBase1))]
				static int IBase1.UsedOnConstrainedTypeExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof(IBase2))]
				static int IBase2.UsedOnConstrainedTypeExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof(IInheritsFromMultipleBases))]
				static int IInheritsFromMultipleBases.UsedOnConstrainedTypeExplicitImpl () => 0;

				public static int UnusedImplicitImpl () =>0;

				static int IBase1.UnusedExplicitImpl () => 0;

				static int IBase2.UnusedExplicitImpl () => 0;

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
			static void CallBase1TypeConstrainedMethod<T> () where T: IBase1
			{
				T.UsedOnBaseOnlyConstrainedTypeImplicitImpl ();
				T.UsedOnConstrainedTypeExplicitImpl ();
			}

			[Kept]
			static void CallBase2TypeConstrainedMethod<T> () where T: IBase2
			{
				T.UsedOnBaseOnlyConstrainedTypeImplicitImpl ();
				T.UsedOnConstrainedTypeExplicitImpl ();
			}

			[Kept]
			static void CallSingleInheritTypeConstrainedMethod<T> () where T: IInheritsFromBase
			{
				T.UsedOnConstrainedTypeExplicitImpl ();
			}

			[Kept]
			static void CallDoubleInheritTypeConstrainedMethod<T> () where T: IInheritsFromMultipleBases
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
		class GenericStaticInterface
		{
			[Kept]
			interface IGenericInterface<T>
			{
				static abstract T GetT ();
				[Kept]
				static abstract T GetTExplicit ();
			}

			[Kept]
			[KeptInterface (typeof (IGenericInterface<int>))]
			public class ImplementsGenericInterface : IGenericInterface<int>
			{
				[Kept]
				[RemovedOverride (typeof (IGenericInterface<int>))]
				public static int GetT () => 0;

				[Kept]
				[KeptOverride (typeof (IGenericInterface<int>))]
				static int IGenericInterface<int>.GetTExplicit () => 0;
			}

			[Kept]
			[KeptInterface (typeof (IGenericInterface<int>))]
			class ImplementsGenericInterfaceUnused : IGenericInterface<int>
			{
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
			static void CallExplicitMethod<T, U> () where T : IGenericInterface<U>
			{
				T.GetTExplicit ();
			}
		}

		[Kept]
		class RecursiveGenericInterface
		{
			[Kept]
			interface IGenericInterface<T> where T : IGenericInterface<T>
			{
				static abstract T GetT ();
				[Kept]
				static abstract T GetTExplicit ();
			}

			[Kept]
			[KeptInterface (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
			class ImplementsIGenericInterfaceOfSelf : IGenericInterface<ImplementsIGenericInterfaceOfSelf>
			{
				[Kept]
				[RemovedOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
				public static ImplementsIGenericInterfaceOfSelf GetT () => throw new NotImplementedException ();

				[Kept]
				[KeptOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
				static ImplementsIGenericInterfaceOfSelf IGenericInterface<ImplementsIGenericInterfaceOfSelf>.GetTExplicit ()
					=> throw new NotImplementedException ();
			}

			[Kept]
			[KeptInterface (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
			class ImplementsIGenericInterfaceOfOther : IGenericInterface<ImplementsIGenericInterfaceOfSelf>
			{
				[Kept]
				[RemovedOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
				public static ImplementsIGenericInterfaceOfSelf GetT () => throw new NotImplementedException ();

				[Kept]
				[KeptOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
				static ImplementsIGenericInterfaceOfSelf IGenericInterface<ImplementsIGenericInterfaceOfSelf>.GetTExplicit ()
					=> throw new NotImplementedException ();
			}

			[Kept]
			[KeptInterface (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelfUnused>))]
			class ImplementsIGenericInterfaceOfSelfUnused : IGenericInterface<ImplementsIGenericInterfaceOfSelfUnused>
			{
				public static ImplementsIGenericInterfaceOfSelfUnused GetT () => throw new NotImplementedException ();

				[Kept]
				[KeptOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelfUnused>))]
				static ImplementsIGenericInterfaceOfSelfUnused IGenericInterface<ImplementsIGenericInterfaceOfSelfUnused>.GetTExplicit ()
					=> throw new NotImplementedException ();
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

			[Kept]
			static void CallExplicitGetT<T> () where T : IGenericInterface<ImplementsIGenericInterfaceOfSelf>
			{
				T.GetTExplicit ();
			}
		}
	}
}
