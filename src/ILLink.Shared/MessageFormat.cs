
#nullable enable

using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace ILLink.Shared
{
	internal static class MessageFormat
	{
		static readonly ResourceManager sharedStrings = new ResourceManager (Path.GetFileNameWithoutExtension (Assembly.GetExecutingAssembly ().GetManifestResourceNames ().FirstOrDefault (name => name.Contains ("SharedStrings")))!, Assembly.GetExecutingAssembly ());

		public static string FormatRequiresAttributeMessageArg (string? message)
		{
			string arg1 = "";
			if (!string.IsNullOrEmpty (message))
				arg1 = $" {message}{(message!.TrimEnd ().EndsWith (".") ? "" : ".")}";
			return arg1;
		}

		public static string FormatRequiresAttributeUrlArg (string? url)
		{
			string arg2 = "";
			if (!string.IsNullOrEmpty (url))
				arg2 = " " + url;
			return arg2;
		}

		public static string FormatRequiresAttributeMismatch (bool memberHasAttribute, bool condition, string var0, string var1, string var2)
		{
			if (!memberHasAttribute && condition)
				return string.Format (sharedStrings.GetString ("BaseRequiresMismatchMessage")!, var0, var1, var2);
			else if (memberHasAttribute && condition)
				return string.Format (sharedStrings.GetString ("DerivedRequiresMismatchMessage")!, var0, var1, var2);
			else if (!memberHasAttribute && !condition)
				return string.Format (sharedStrings.GetString ("InterfaceRequiresMismatchMessage")!, var0, var1, var2);
			else if (memberHasAttribute && !condition)
				return string.Format (sharedStrings.GetString ("ImplementationRequiresMismatchMessage")!, var0, var1, var2);
			else
				return string.Empty;
		}
	}
}