
namespace ILLink.Shared
{
	internal static class MessageFormat
	{
		public static string GetRucMessageArg (string? message)
		{
			string arg1 = "";
			if (!string.IsNullOrEmpty (message))
				arg1 = $" {message}{(message!.TrimEnd ().EndsWith (".") ? "" : ".")}";
			return arg1;
		}

		public static string GetRucUrlArg (string? url)
		{
			string arg2 = "";
			if (!string.IsNullOrEmpty (url))
				arg2 = " " + url;
			return arg2;
		}
	}
}