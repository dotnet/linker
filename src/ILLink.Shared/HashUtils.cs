using System;

namespace ILLink.Shared
{
	static class HashUtils
	{
#if NETSTANDARD2_0
		// This constant is taken from code that Roslyn generates for GetHashCode of records.
		const int Multiplier = -1521134295;
#endif
		public static int Combine<T1, T2> (T1 value1, T2 value2)
			where T1 : notnull
			where T2 : notnull
		{
#if NETSTANDARD2_0
			return value1.GetHashCode () * Multiplier + value2.GetHashCode ();
#else
			return HashCode.Combine (value1, value2);
#endif
		}

		public static int Combine<T1, T2, T3> (T1 value1, T2 value2, T3 value3)
			where T1 : notnull
			where T2 : notnull
			where T3 : notnull
		{
#if NETSTANDARD2_0
			return (value1.GetHashCode () * Multiplier + value2.GetHashCode ()) * Multiplier + value3.GetHashCode ();
#else
			return HashCode.Combine (value1, value2, value3);
#endif
		}
	}
}