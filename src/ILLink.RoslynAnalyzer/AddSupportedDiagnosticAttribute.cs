// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace ILLink.RoslynAnalyzer
{
	/// <summary>
	/// Attribute consumed by the DiagnosticGenerator to populate the supported
	/// diagnostics for each of the types inheriting from DiagnosticAnalyzer.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class AddSupportedDiagnosticAttribute : Attribute
	{
		public AddSupportedDiagnosticAttribute (string code, string name)
		{
			Code = code;
			Name = name;
		}

		/// <summary>
		/// Diagnostic code.
		/// </summary>
		public string Code { get; }

		/// <summary>
		/// Diagnostic name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Category of the diagostic (e.g., Trimming, SingleFile, etc.). Defaults to Trimming.
		/// </summary>
		public string? Category { get; set; }

		/// <summary>
		/// Whether the diagnostic is enabled by default. Defaults to true.
		/// </summary>
		public bool IsEnabledByDefault { get; set; } = true;

		/// <summary>
		/// An optional hyperlink that provides more detailed information regarding the diagnostic.
		/// </summary>
		public string? HelpLinkURI { get; set; } = null;

		/// <summary>
		/// Optional identifier of the localizable format message string that should be used for formatting the diagnostic title.
		/// The string format must be declared in <see cref="Shared.SharedStrings"/>. Defaults to {this.Name}Title.
		/// </summary>
		public string? UseExistingMessageTitleResourceString { get; set; }

		/// <summary>
		/// Optional identifier of the localizable format message string that should be used for formatting the diagnostic.
		/// The string format must be declared in <see cref="Shared.SharedStrings"/>. Defaults to {this.Name}Message.
		/// </summary>
		public string? UseExistingMessageResourceString { get; set; }
	}
}