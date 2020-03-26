// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Mono.Linker
{
	public readonly struct MessageContainer
	{
		/// <summary>
		/// Optional data with a filename, line and column that triggered the
		/// linker to output an error (or warning) message.
		/// </summary>
		public MessageOrigin? Origin { get; }

		public MessageCategory Category { get; }

		/// <summary>
		/// Further categorize the message.
		/// </summary>
		public string SubCategory { get; }

		/// <summary>
		/// Code identifier for errors and warnings reported by the IL linker.
		/// </summary>
		public int Code { get; }

		/// <summary>
		/// Optional user friendly text describing the error or warning.
		/// </summary>
		public string Text { get; }

		public MessageContainer (MessageCategory category, string text, int code, string subcategory = MessageSubCategory.None, MessageOrigin? origin = null)
		{
			switch (code) {
				// Errors
				case int _code when (_code >= 0 && _code <= 2000):
					if (category != MessageCategory.Error)
						throw new ArgumentException ($"MSBuild Message Container with code ${code} was expected to be of 'Error' category.");
					break;

				// Warnings
				case int _code when (_code >= 2001 && _code <= 6000):
					if (category != MessageCategory.Warning)
						throw new ArgumentException ($"MSBuild Message Container with code ${code} was expected to be of 'Warning' category.");
					break;

				// Info.
				case int _code when (_code >= 6001 && _code <= 8000):
					if (category != MessageCategory.Info)
						throw new ArgumentException ($"MSBuild Message Container with code ${code} was expected to be of 'Info' category.");
					break;

				// Custom step
				default:
					break;
			}

			Code = code;
			Category = category;
			Origin = origin;
			SubCategory = subcategory;
			Text = text;
		}

		public override string ToString () => ToMSBuildString ();

		public string ToMSBuildString ()
		{
			const string originApp =
#if NETCOREAPP
			"illinker";
#else
			"monolinker";
#endif

			string origin = Origin?.ToString () ?? originApp;
			string subCat = SubCategory != MessageSubCategory.None ? SubCategory + " " : "";
			string cat;
			switch (Category) {
			case MessageCategory.Error:
				cat = "error";
				break;
			case MessageCategory.Warning:
				cat = "warning";
				break;
			default:
				cat = "";
				break;
			}

			string code = Code.ToString ("D4");
			string text = !string.IsNullOrEmpty (Text) ? ": " + Text : Text;

			return string.Format ($"{origin}: {subCat}{cat} IL{code}{text}");
		}
	}
}
