using System;
using System.Collections.Generic;

namespace ILLink.Shared
{
	// TODO: fix and share this
	public static class HashUtils
	{
		public static int CalcHashCodeEnumerable<T> (IEnumerable<T> list)
		{
			HashCode hashCode = new ();
			foreach (var item in list)
				hashCode.Add (item);
			return hashCode.ToHashCode ();
		}
	}
}