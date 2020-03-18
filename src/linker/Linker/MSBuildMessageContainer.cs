using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.Linker
{
	public struct MSBuildMessageContainer
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
		public MessageCode Code { get; }
		
		/// <summary>
		/// Optional user friendly text describing the error or warning.
		/// </summary>
		public string Text { get; }

		public MSBuildMessageContainer (
			MessageOrigin origin,
			MessageCategory category,
			MessageCode code,
			string subcategory = MessageSubcategory.None,
			string text = "")
		{
			Origin = origin;
			Category = category;
			Code = code;
			Subcategory = subcategory;
			Text = text;
		}

		public override string ToString()
		{
			string message = string.Format ("{0}: {1}{2} {3}{4}",
				Origin.ToString(),
				Subcategory != MessageSubcategory.None ? Subcategory + " " : "",
				Category.ToString().ToLower(),
				Code.ToString(),
				!String.IsNullOrEmpty(Text) ? ": " + Text : Text);
			return message;
		}
	}
}
