﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ILLink.Shared.DataFlow
{
	public struct ValueStack<TValue> : IEquatable<ValueStack<TValue>>
		where TValue : IEquatable<TValue>
	{
		private Stack<TValue>? _stack;

		private readonly int _capacity = 0;

		public ValueStack ()
		{
		}

		public ValueStack (ValueStack<TValue> stack)
		{
			_stack = stack._stack == null ? null : new Stack<TValue> (stack._stack);
		}

		public ValueStack (int capacity)
		{
			_capacity = capacity;
		}

		public int Count => _stack == null ? 0 : _stack.Count;

		public bool Equals (ValueStack<TValue> other)
		{
			if (Count != other.Count)
				return false;
			bool equals = true;
			IEnumerator<TValue> thisEnum = GetEnumerator ();
			IEnumerator<TValue> otherEnum = other.GetEnumerator ();
			while (thisEnum.MoveNext () && otherEnum.MoveNext ()) {
				equals &= thisEnum.Current.Equals (otherEnum.Current);
			}

			return equals;
		}

		internal IEnumerator<TValue> GetEnumerator ()
		{
			return _stack?.GetEnumerator () ?? Enumerable.Empty<TValue> ().GetEnumerator ();

		}

		internal void Push (TValue value)
		{
			_stack ??= new Stack<TValue> (_capacity);
			_stack.Push (value);
		}

		internal TValue Pop ()
		{
			if (_stack == null) throw new InvalidOperationException ("Stack is null");
			return _stack.Pop ();
		}

		internal TValue Pop (int count)
		{
			TValue topOfStack = Pop ();

			for (int i = 1; i < count; ++i) {
				Pop ();
			}
			return topOfStack;
		}
	}
}
