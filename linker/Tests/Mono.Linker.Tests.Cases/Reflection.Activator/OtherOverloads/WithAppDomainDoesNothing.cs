using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.OtherOverloads {
	public class WithAppDomainDoesNothing {
		public static void Main ()
		{
			var tmp = System.Activator.CreateInstance (GetDomain (), string.Empty, string.Empty);
			HereToUseCreatedInstance (tmp);
		}

		[Kept]
		static AppDomain GetDomain ()
		{
			return null;
		}
		
		[Kept]
		static void HereToUseCreatedInstance (object arg)
		{
		}
	}
}