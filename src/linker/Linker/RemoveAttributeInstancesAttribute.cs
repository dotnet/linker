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
			// Arguments were previously boxed in Object, but don't need to be
			Arguments = values.Select ((arg) => {
				Debug.Assert (arg.Value is CustomAttributeArgument);
				if (arg.Value is CustomAttributeArgument caa) return caa;
				return arg;
			}).ToArray ();
		}

		public CustomAttributeArgument[] Arguments { get; }

		// This might be also useful to add later
		// public bool ExactArgumentsOnly { get; set; }
	}
}
