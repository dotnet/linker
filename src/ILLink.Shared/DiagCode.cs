
namespace ILLink.Shared
{
	internal enum DiagCode
	{
		WRN_UsingRequiresUnreferencedCode = 2026
	}

	internal static class DiagIdExtensions
	{
		public static string ToId(this DiagCode code) => "IL" + code;
	}
}