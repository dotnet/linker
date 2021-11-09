// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace ILLink.Shared
{
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