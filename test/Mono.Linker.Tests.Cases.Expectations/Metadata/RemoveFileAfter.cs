using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	/// <summary>
	/// Allows removing files from sandbox input directory
	/// </summary>
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class RemoveFileAfterAttribute : BaseMetadataAttribute
	{
		public RemoveFileAfterAttribute (string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException (nameof (fileName));

			if (string.IsNullOrEmpty (fileName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (fileName));
		}
	}
}
