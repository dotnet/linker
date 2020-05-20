﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using System.Text;

namespace Mono.Linker
{
	public readonly struct MessageOrigin
	{
#nullable enable
		public string? FileName { get; }
		public IMetadataTokenProvider? MdTokenProvider { get; }
#nullable disable
		public int SourceLine { get; }
		public int SourceColumn { get; }

		public MessageOrigin (string fileName, int sourceLine = 0, int sourceColumn = 0)
		{
			FileName = fileName;
			SourceLine = sourceLine;
			SourceColumn = sourceColumn;
			MdTokenProvider = null;
		}

		public MessageOrigin (IMetadataTokenProvider mdTokenProvider, int sourceLine = 0, int sourceColumn = 0)
		{
			FileName = null;
			MdTokenProvider = mdTokenProvider;
			SourceLine = sourceLine;
			SourceColumn = sourceColumn;
		}

		private MessageOrigin (string fileName, IMetadataTokenProvider mdTokenProvider, int sourceLine = 0, int sourceColumn = 0)
		{
			FileName = fileName;
			MdTokenProvider = mdTokenProvider;
			SourceLine = sourceLine;
			SourceColumn = sourceColumn;
		}

		public static MessageOrigin TryGetOrigin (IMetadataTokenProvider sourceMethod, int ilOffset)
		{
			if (sourceMethod is MethodDefinition methodDef) {
				if (!methodDef.DebugInformation.HasSequencePoints)
					return new MessageOrigin (methodDef);

				SequencePoint correspondingSequencePoint = methodDef.DebugInformation.SequencePoints
					.Where (s => s.Offset <= ilOffset)?.Last ();
				if (correspondingSequencePoint == null)
					return new MessageOrigin (correspondingSequencePoint.Document.Url, methodDef);

				return new MessageOrigin (correspondingSequencePoint.Document.Url, methodDef, correspondingSequencePoint.StartLine, correspondingSequencePoint.StartColumn);
			}

			return new MessageOrigin (sourceMethod);
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
	}
}
