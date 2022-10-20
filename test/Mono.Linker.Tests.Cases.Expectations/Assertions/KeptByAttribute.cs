// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.All, Inherited = false)]
	public class KeptByAttribute : KeptAttribute
	{
		private KeptByAttribute () { }

		/// <summary>
		/// Place on an type member to indicate that the linker should log that the member is kept as a depenendency of <paramref name="dependencyProvider"/> with reason <paramref name="reason"/>.
		/// </summary>
		public KeptByAttribute (string dependencyProvider, DependencyKind reason) { }

		/// <summary>
		/// Place on an type member to indicate that the linker should log that the member is kept as a depenendency of <paramref name="dependencyProviderType"/> with reason <paramref name="reason"/>.
		/// </summary>
		public KeptByAttribute (Type dependencyProviderType, DependencyKind reason) { }

		/// <summary>
		/// Place on an type member to indicate that the linker should log that the member is kept as a depenendency of <paramref name="dependencyProviderType"/>.<paramref name="memberName"/> with reason <paramref name="reason"/>.
		/// </summary>
		public KeptByAttribute (Type dependencyProviderType, string memberName, DependencyKind reason) { }
	}
}