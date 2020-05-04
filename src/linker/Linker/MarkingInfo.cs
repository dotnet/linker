// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Mono.Linker
{
	readonly public struct MarkingInfo : IEquatable<MarkingInfo>
	{
		public MarkingInfo (MarkingReason reason, object source)
		{
			Reason = reason;
			Source = source ?? throw new ArgumentNullException (nameof (source));
		}

		MarkingInfo (MarkingReason kind)
		{
			Reason = kind;
			Source = null;
		}

		public static readonly MarkingInfo None = new MarkingInfo (MarkingReason.Hidden);

		public MarkingReason Reason { get; }
		public object Source { get; }

		public bool Equals (MarkingInfo other) => (Reason, Source) == (other.Reason, other.Source);
		public override bool Equals (object obj) => obj is MarkingInfo info && this.Equals (info);
		public override int GetHashCode () => (Reason, Source).GetHashCode ();
		public static bool operator == (MarkingInfo lhs, MarkingInfo rhs) => lhs.Equals (rhs);
		public static bool operator != (MarkingInfo lhs, MarkingInfo rhs) => !lhs.Equals (rhs);
	}
}
