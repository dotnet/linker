using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.Linker
{
	public readonly struct MSBuildMessageContainer
	{
		/// <summary>
		/// Can either be the tool name (illinker, monolinker) or a filename with
		/// parenthesized information about the line and column that triggered the
		/// linker to output an error (or warning) message.
		/// </summary>
		public MessageOrigin Origin { get; }
		
		public MessageCategory Category { get; }

		/// <summary>
		/// Further categorize the message.
		/// </summary>
		public string Subcategory { get; }

		/// <summary>
		/// Code identifier for errors and warnings reported by the IL linker.
		/// </summary>
		public int Code { get; }
		
		/// <summary>
		/// Optional user friendly text describing the error or warning.
		/// </summary>
		public string Text { get; }

		public MSBuildMessageContainer (
			int code,
			MessageCategory category,
			MessageOrigin origin,
			string subcategory = MessageSubcategory.None,
			string text = "")
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
			Subcategory = subcategory;
			Text = text;
		}

		public override string ToString()
		{
			string message = string.Format ("{0}: {1}{2} {3}{4}",
				Origin.ToString(),
				Subcategory != MessageSubcategory.None ? Subcategory + " " : "",
				Category.ToString().ToLowerInvariant(),
				"IL" + Code.ToString("D4"),
				!String.IsNullOrEmpty(Text) ? ": " + Text : Text);
			return message;
		}
	}
}
