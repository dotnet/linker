using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	class PreservedInterfaceHasOtherImplementedInterfacesPreserved {
		public static void Main ()
		{
		}

		[Kept]
		[KeptInterface (typeof (IUsedByUsed1))]
		public interface IUnused1 : IUsedByUsed1 {
			[Kept]
			void Foo (object obj);
		}

		[Kept]
		[KeptInterface (typeof (IUsedByUsed2))]
		public interface IUnused2 : IUsedByUsed2 {
			[Kept]
			void Foo2 (object obj);
		}

		[Kept]
		public interface IUsedByUsed1 {
			[Kept]
			void Bar (object obj);
		}

		[Kept]
		public interface IUsedByUsed2 {
			[Kept]
			void Bar2 (object obj);
		}

		[Kept]
		[KeptInterface (typeof (IUnused1))]
		[KeptInterface (typeof (IUnused2))]
		[KeptInterface (typeof (IUsedByUsed1))]
		[KeptInterface (typeof (IUsedByUsed2))]
		public interface IUnusedGeneric<T> : IUnused1, IUnused2 {
			[Kept]
			void Foo (T obj);
		}

		class Boo : IUnusedGeneric<int>, IUnusedGeneric<string> {
			public void Foo (int obj)
			{
				throw new NotImplementedException ();
			}

			public void Foo (string obj)
			{
				throw new NotImplementedException ();
			}

			public void Bar (object obj)
			{
				throw new NotImplementedException ();
			}

			public void Foo (object obj)
			{
				throw new NotImplementedException ();
			}

			public void Bar2 (object obj)
			{
				throw new NotImplementedException ();
			}

			public void Foo2 (object obj)
			{
				throw new NotImplementedException ();
			}
		}
	}
}
