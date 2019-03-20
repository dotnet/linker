using System;
using System.Runtime.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink {
	/// <summary>
	/// Delegate and is created from 
	/// </summary>
	[SetupLinkerCoreAction ("link")]
#if NETCOREAPP
	[KeptBaseOnTypeInAssembly ("System.Private.CoreLib.dll", typeof (MulticastDelegate), "System.Private.CoreLib.dll", typeof (Delegate))]

	// Check a couple override methods to verify they were not removed
	[KeptMemberInAssembly ("System.Private.CoreLib.dll", typeof (MulticastDelegate), "GetHashCode()")]
	[KeptMemberInAssembly ("System.Private.CoreLib.dll", typeof (MulticastDelegate), "Equals(System.Object)")]

	[KeptMemberInAssembly ("System.Private.CoreLib.dll", typeof (Delegate), "GetHashCode()")]
	[KeptMemberInAssembly ("System.Private.CoreLib.dll", typeof (Delegate), "Equals(System.Object)")]
	[KeptInterfaceOnTypeInAssembly("System.Private.CoreLib.dll", typeof (Delegate), "System.Private.CoreLib.dll", typeof (ICloneable))]
	[KeptInterfaceOnTypeInAssembly("System.Private.CoreLib.dll", typeof (Delegate), "System.Runtime.dll", typeof (ISerializable))]
#else
	[KeptBaseOnTypeInAssembly ("mscorlib.dll", typeof (MulticastDelegate), "mscorlib.dll", typeof (Delegate))]

	// Check a couple override methods to verify they were not removed
	[KeptMemberInAssembly ("mscorlib.dll", typeof (MulticastDelegate), "GetHashCode()")]
	[KeptMemberInAssembly ("mscorlib.dll", typeof (MulticastDelegate), "Equals(System.Object)")]

	[KeptMemberInAssembly ("mscorlib.dll", typeof (Delegate), "GetHashCode()")]
	[KeptMemberInAssembly ("mscorlib.dll", typeof (Delegate), "Equals(System.Object)")]
	[KeptInterfaceOnTypeInAssembly("mscorlib.dll", typeof (Delegate), "mscorlib.dll", typeof (ICloneable))]
	[KeptInterfaceOnTypeInAssembly("mscorlib.dll", typeof (Delegate), "mscorlib.dll", typeof (ISerializable))]
#endif

	// Fails due to Runtime critical type System.Reflection.CustomAttributeData not found.
	[SkipPeVerify(SkipPeVerifyForToolchian.Pedump)]
	public class DelegateAndMulticastDelegateKeepInstantiatedReqs {
		public static void Main ()
		{
			typeof (MulticastDelegate).ToString ();

			// Cause the interfaces to be marked in order to eliminate the possibility of them being removed
			// due to no code path marking the interface type
			typeof (ISerializable).ToString ();
			typeof (ICloneable).ToString();
		}
	}
}