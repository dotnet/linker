// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using System.Text;

namespace Mono.Linker
{
	public struct MessageOrigin
	{
#nullable enable
		public string? FileName { get; private set; }
		public IMemberDefinition? MemberDefinition { get; private set; }
#nullable disable
		public int SourceLine { get; private set; }
		public int SourceColumn { get; private set; }
		public readonly int? ILOffset { get; }

		public MessageOrigin (string fileName, int sourceLine = 0, int sourceColumn = 0)
		{
			FileName = fileName;
			SourceLine = sourceLine;
			SourceColumn = sourceColumn;
			MemberDefinition = null;
			ILOffset = null;
		}

		public MessageOrigin (IMemberDefinition memberDefinition, int? ilOffset = null)
		{
			FileName = null;
			MemberDefinition = memberDefinition;
			SourceLine = 0;
			SourceColumn = 0;
			ILOffset = ilOffset;
		}

		internal void TryGetSourceInfo ()
		{
			if (MemberDefinition == null || !(MemberDefinition is MethodDefinition))
				return;

			var sourceMethod = MemberDefinition as MethodDefinition;
			var offeset = ILOffset ?? 0;
			if (sourceMethod.DebugInformation.HasSequencePoints) {
				SequencePoint correspondingSequencePoint = sourceMethod.DebugInformation.SequencePoints
					.Where (s => s.Offset <= offeset)?.Last ();
				if (correspondingSequencePoint == null)
					return;

				FileName = correspondingSequencePoint.Document.Url;
				SourceLine = correspondingSequencePoint.StartLine;
				SourceColumn = correspondingSequencePoint.StartColumn;
				MemberDefinition = sourceMethod;
			}
		}

		public override string ToString ()
		{
			if (FileName == null)
				return null;

			StringBuilder sb = new StringBuilder (FileName);
			if (SourceLine != 0) {
				sb.Append ("(").Append (SourceLine);
				if (SourceColumn != 0)
					sb.Append (",").Append (SourceColumn);

				sb.Append (")");
			}

			return sb.ToString ();
		}

		public bool Equals (MessageOrigin other) =>
			(FileName, MemberDefinition, SourceLine, SourceColumn) == (other.FileName, other.MemberDefinition, other.SourceLine, other.SourceColumn);

		public override bool Equals (object obj) => obj is MessageOrigin messageOrigin && Equals (messageOrigin);
		public override int GetHashCode () => (FileName, MemberDefinition, SourceLine, SourceColumn).GetHashCode ();
		public static bool operator == (MessageOrigin lhs, MessageOrigin rhs) => lhs.Equals (rhs);
		public static bool operator != (MessageOrigin lhs, MessageOrigin rhs) => !lhs.Equals (rhs);
	}
}
