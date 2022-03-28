using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Libraries
{
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	[SetupLinkerArgument ("--skip-unresolved")]
	[VerifyMetadataNames]
	public class RootLibrary
	{
		private int field;

		[Kept]
		public RootLibrary ()
		{
		}

		[Kept]
		public static void Main ()
		{
			var t = typeof (SerializationTestPrivate);
			t = typeof (SerializationTestNested.SerializationTestPrivate);
		}

		[Kept]
		public void UnusedPublicMethod ()
		{
		}

		[Kept]
		protected void UnusedProtectedMethod ()
		{
		}

		[Kept]
		protected internal void UnusedProtectedInternalMethod ()
		{
		}

		protected private void UnusedProtectedPrivateMethod ()
		{
		}

		internal void UnusedInternalMethod ()
		{
		}

		private void UnusedPrivateMethod ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicDependencyAttribute))]
		[DynamicDependency (nameof (MethodWithDynamicDependencyTarget))]
		public void MethodWithDynamicDependency ()
		{
		}

		[Kept]
		private void MethodWithDynamicDependencyTarget ()
		{
		}

		[Kept]
		public class SerializationTest
		{
			[Kept]
			private SerializationTest (SerializationInfo info, StreamingContext context)
			{
			}
		}

		[Kept]
		private class SerializationTestPrivate
		{
			[Kept]
			private SerializationTestPrivate (SerializationInfo info, StreamingContext context)
			{
			}

			public void NotUsed ()
			{
			}

			[Kept]
			[OnSerializing]
			[KeptAttributeAttribute (typeof (OnSerializingAttribute))]
			private void OnSerializingMethod (StreamingContext context)
			{
			}

			[Kept]
			[OnSerialized]
			[KeptAttributeAttribute (typeof (OnSerializedAttribute))]
			private void OnSerializedMethod (StreamingContext context)
			{
			}

			[Kept]
			[OnDeserializing]
			[KeptAttributeAttribute (typeof (OnDeserializingAttribute))]
			private void OnDeserializingMethod (StreamingContext context)
			{
			}

			[Kept]
			[OnDeserialized]
			[KeptAttributeAttribute (typeof (OnDeserializedAttribute))]
			private void OnDeserializedMethod (StreamingContext context)
			{
			}
		}

		[Kept]
		private class SerializationTestNested
		{
			internal class SerializationTestPrivate
			{
				[Kept]
				private SerializationTestPrivate (SerializationInfo info, StreamingContext context)
				{
				}

				public void NotUsed ()
				{
				}
			}

			public void NotUsed ()
			{
			}
		}

		[Kept]
		public class SubstitutionsTest
		{
			[Kept]
			private static bool FalseProp { [Kept] get { return false; } }

			[Kept]
			[ExpectBodyModified]
			public SubstitutionsTest ()
			{
				if (FalseProp)
					LocalMethod ();
			}

			private void LocalMethod ()
			{
			}
		}

		[Kept]
		[KeptInterface (typeof (I))]
		public class IfaceClass : I
		{
			[Kept]
			public IfaceClass ()
			{
			}

			[Kept]
			public override string ToString ()
			{
				return "test";
			}
		}

		[Kept]
		public interface I
		{
		}

		[Kept]
		[KeptInterface (typeof (IEnumerator))]
		public class UninstantiatedPublicClassWithInterface : IEnumerator
		{
			internal UninstantiatedPublicClassWithInterface () { }

			[Kept]
			bool IEnumerator.MoveNext () { throw new PlatformNotSupportedException (); }

			[Kept]
			object IEnumerator.Current { [Kept] get { throw new PlatformNotSupportedException (); } }

			[Kept]
			void IEnumerator.Reset () { }
		}

		[Kept]
		[KeptInterface (typeof (ICollection<CollectedType>))]
		[KeptInterface (typeof (IEnumerable<CollectedType>))]
		[KeptInterface (typeof (IEnumerable))]
		public class UninstantiatedPublicClassWithImplicitlyImplementedInterface : ICollection<CollectedType>
		{
			[Kept]
			[KeptBackingField]
			public int Count { [Kept] get; }

			[Kept]
			[KeptBackingField]
			public bool IsReadOnly { [Kept] get; }

			internal UninstantiatedPublicClassWithImplicitlyImplementedInterface () { }

			[Kept]
			public void CopyTo (Array array, int index)
			{
				throw new NotImplementedException ();
			}

			[Kept]
			public void Add (CollectedType item)
			{
				throw new NotImplementedException ();
			}

			[Kept]
			public void Clear ()
			{
				throw new NotImplementedException ();
			}

			[Kept]
			public bool Contains (CollectedType item)
			{
				throw new NotImplementedException ();
			}

			[Kept]
			public void CopyTo (CollectedType[] array, int arrayIndex)
			{
				throw new NotImplementedException ();
			}

			[Kept]
			public bool Remove (CollectedType item)
			{
				throw new NotImplementedException ();
			}

			[Kept]
			public IEnumerator GetEnumerator ()
			{
				throw new NotImplementedException ();
			}

			[Kept]
			IEnumerator<CollectedType> IEnumerable<CollectedType>.GetEnumerator ()
			{
				throw new NotImplementedException ();
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class CollectedType { }

		[Kept]
		[KeptInterface (typeof (IPublicInterface))]
		[KeptInterface (typeof (IInternalInterface))]
		public class InstantiatedClassWithInterfaces : IPublicInterface, IInternalInterface
		{
			[Kept]
			public InstantiatedClassWithInterfaces () { }

			[Kept]
			void IPublicInterface.PublicInterfaceMethod () { }

			void IInternalInterface.InternalInterfaceMethod () { }
		}

		[Kept]
		public interface IPublicInterface
		{
			[Kept]
			void PublicInterfaceMethod ();
		}

		[Kept]
		internal interface IInternalInterface
		{
			void InternalInterfaceMethod ();
		}
	}

	internal class RootLibrary_Internal
	{
		protected RootLibrary_Internal (SerializationInfo info, StreamingContext context)
		{
		}

		internal void Unused ()
		{
		}
	}
}
