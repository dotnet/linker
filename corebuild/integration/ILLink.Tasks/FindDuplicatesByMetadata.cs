using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
	public class FindDuplicatesByMetadata : Task
	{
		/// <summary>
		///   Items to scan.
		/// </summary>
		[Required]
		public ITaskItem [] Items { get; set; }

		/// <summary>
		///   Name of metadata to scan for.
		/// </summary>
		[Required]
		public String MetadataName { get; set; }

		/// <summary>
		///   Duplicate items: the input items for which the
		///   specified metadata was shared by multiple input
		///   items.
		/// </summary>
		[Output]
		public ITaskItem [] DuplicateItems { get; set; }

		public override bool Execute ()
		{
			DuplicateItems = Items.GroupBy (i => i.GetMetadata(MetadataName))
				.Where (g => g.Count () > 1)
				.SelectMany (g => g)
				.ToArray ();
			return true;
		}
	}
}
