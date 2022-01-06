// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILLink.Shared.DataFlow
{
	public sealed class Box<T> where T : struct
	{
		public Box (T value) => Value = value;
		public T Value { get; set; }

	}

	public class DataFlowState<TValue>
		where TValue : struct, IEquatable<TValue>
	{
		public TValue Current;
		public Box<TValue>? Exception;

		public DataFlowState (TValue current, Box<TValue>? exception) => (Current, Exception) = (current, exception);
		public DataFlowState (TValue current) : this (current, default) { }
	}
}