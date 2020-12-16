namespace System.Diagnostics.CodeAnalysis
{
#nullable enable
	[AttributeUsage (AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
	public sealed class SingleFileUnsupportedAttribute : Attribute
	{
		public SingleFileUnsupportedAttribute (string message)
		{
			Message = message;
		}

		public string Message { get; }

		public string? Url { get; set; }
	}
}