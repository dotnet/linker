// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker
{
	/// <summary>
	/// This attribute name will be the name hardcoded in linker which will remove all 
	/// attribute usages but not the attribute definition
	/// </summary>
	[AttributeUsage (
		AttributeTargets.Class, Inherited = false)]
	public sealed class RemoveAttributeInstancesAttribute : Attribute
	{
		public RemoveAttributeInstancesAttribute (System.Collections.Generic.ICollection<CustomAttributeArgument> values)
		{
			// Arguments must be boxed in an Object CustomArgumentAttribute where the .Value is a CustomArgumentAttribute
			Debug.Assert(values.All (a => a.Value is CustomAttributeArgument));
			Arguments = values
				.Where (arg => arg.Value is CustomAttributeArgument) // So we don't crash on conversion if the arg isn't boxed
				.Select (arg => (CustomAttributeArgument) arg.Value)
				.ToArray ();
		}

		public CustomAttributeArgument[] Arguments { get; }

		// This might be also useful to add later
		// public bool ExactArgumentsOnly { get; set; }
	}
}
