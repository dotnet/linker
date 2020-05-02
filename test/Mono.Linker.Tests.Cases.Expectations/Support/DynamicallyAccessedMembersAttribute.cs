﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>
	/// Specifies the types of members that are dynamically accessed.
	///
	/// This enumeration has a <see cref="FlagsAttribute"/> attribute that allows a
	/// bitwise combination of its member values.
	/// </summary>
	/// <remarks>
	/// This is a copy of the enum definition in the framework at https://github.com/dotnet/runtime/blob/master/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/DynamicallyAccessedMemberTypes.cs
	/// </remarks>
	[Flags]
	public enum DynamicallyAccessedMemberTypes
	{
		/// <summary>
		/// Specifies no members.
		/// </summary>
		None = 0,

		/// <summary>
		/// Specifies the default, parameterless public constructor.
		/// </summary>
		DefaultConstructor = 0x0001,

		/// <summary>
		/// Specifies all public constructors.
		/// </summary>
		PublicConstructors = 0x0002 | DefaultConstructor,

		/// <summary>
		/// Specifies all non-public constructors.
		/// </summary>
		NonPublicConstructors = 0x0004,

		/// <summary>
		/// Specifies all public methods.
		/// </summary>
		PublicMethods = 0x0008,

		/// <summary>
		/// Specifies all non-public methods.
		/// </summary>
		NonPublicMethods = 0x0010,

		/// <summary>
		/// Specifies all public fields.
		/// </summary>
		PublicFields = 0x0020,

		/// <summary>
		/// Specifies all non-public fields.
		/// </summary>
		NonPublicFields = 0x0040,

		/// <summary>
		/// Specifies all public nested types.
		/// </summary>
		PublicNestedTypes = 0x0080,

		/// <summary>
		/// Specifies all non-public nested types.
		/// </summary>
		NonPublicNestedTypes = 0x0100,

		/// <summary>
		/// Specifies all public properties.
		/// </summary>
		PublicProperties = 0x0200,

		/// <summary>
		/// Specifies all non-public properties.
		/// </summary>
		NonPublicProperties = 0x0400,

		/// <summary>
		/// Specifies all public events.
		/// </summary>
		PublicEvents = 0x0800,

		/// <summary>
		/// Specifies all non-public events.
		/// </summary>
		NonPublicEvents = 0x1000,

		/// <summary>
		/// Specifies all members.
		/// </summary>
		All = ~None
	}

	/// <summary>
	/// Indicates that certain members on a specified <see cref="Type"/> are accessed dynamically,
	/// for example through <see cref="System.Reflection"/>.
	/// </summary>
	/// <remarks>
	/// This allows tools to understand which members are being accessed during the execution
	/// of a program.
	///
	/// This attribute is valid on members whose type is <see cref="Type"/> or <see cref="string"/>.
	///
	/// When this attribute is applied to a location of type <see cref="string"/>, the assumption is
	/// that the string represents a fully qualified type name.
	/// 
	/// This is a copy of the code from framework at https://github.com/dotnet/runtime/blob/master/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/DynamicallyAccessedMembersAttribute.cs
	/// </remarks>
	[AttributeUsage (
		AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter |
		AttributeTargets.Parameter | AttributeTargets.Property |
		AttributeTargets.Method, // This is used only to mark the "this" parameter - it should not appear in public API for this attribute (and we don't support it on anything but System.Type)
		Inherited = false)]
	public sealed class DynamicallyAccessedMembersAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DynamicallyAccessedMembersAttribute"/> class
		/// with the specified member types.
		/// </summary>
		/// <param name="memberTypes">The types of members dynamically accessed.</param>
		public DynamicallyAccessedMembersAttribute (DynamicallyAccessedMemberTypes memberTypes)
		{
			MemberTypes = memberTypes;
		}

		/// <summary>
		/// Gets the <see cref="DynamicallyAccessedMemberTypes"/> which specifies the type
		/// of members dynamically accessed.
		/// </summary>
		public DynamicallyAccessedMemberTypes MemberTypes { get; }
	}
}
