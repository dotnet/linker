using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.Linker
{
	public class Message
	{
		public MessageOrigin Origin { get; }
		
		public string Subcategory { get; }
		
		public MessageCategory Category { get; }
		
		public MessageCode Code { get; }
		
		public string Text { get; }

		public Message (
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
