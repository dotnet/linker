// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

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
				case int _code when (_code > 0 && _code <= 2000):
					if (category != MessageCategory.Error)
						throw new ArgumentException ($"MSBuild Message Container with code ${code} was expected to be of 'Error' category.");
					break;

				// Warnings
				case int _code when (_code > 2000 && _code <= 6000):
					if (category != MessageCategory.Warning)
						throw new ArgumentException ($"MSBuild Message Container with code ${code} was expected to be of 'Warning' category.");
					break;

				// Info.
				case int _code when (_code > 6000 && _code <= 8000):
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
			const string originApp = "illinker";
			string origin = Origin?.ToString () ?? originApp;

			StringBuilder sb = new StringBuilder ();
			sb.Append (origin).Append (":");

			if (!string.IsNullOrEmpty (SubCategory))
				sb.Append (" ").Append (SubCategory);

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

			if (!string.IsNullOrEmpty (cat))
				sb.Append (" ").Append (cat);

			sb.Append (" IL").Append (Code.ToString ("D4"));
			if (!string.IsNullOrEmpty (Text))
				sb.Append (": ").Append (Text);

			// Expected output $"{Origin}: {SubCategory}{Category} IL{Code}: {Text}");
			return sb.ToString ();
		}
	}
}
