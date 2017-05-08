using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	public sealed class KeptBaseTypeAttribute : KeptAttribute
	{
		public readonly Type BaseType;
		public readonly object [] GenericParameterNames;

		public KeptBaseTypeAttribute (Type baseType)
		{
			BaseType = baseType;
			GenericParameterNames = null;
		}

		public KeptBaseTypeAttribute (Type baseType, string arg1Name)
		{
			BaseType = baseType;
			GenericParameterNames = new [] { arg1Name };
		}

		public KeptBaseTypeAttribute (Type baseType, string arg1Name, Type arg2Name, Type arg3Name, Type arg4Name)
		{
			BaseType = baseType;
			GenericParameterNames = new object [] { arg1Name, arg2Name, arg3Name, arg4Name };
		}
	}
}