// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace ILLink.Shared.DataFlow
{
	public struct StructStack<TValue> : IEquatable<StructStack<TValue>>
		where TValue : IEquatable<TValue>
	{
		private Stack<TValue>? Stack;

		private readonly int _count = 0;

		public StructStack ()
		{
		}

		public StructStack (StructStack<TValue> newStack)
		{
			Stack = newStack.Stack == null ? null : new Stack<TValue> (newStack.Stack);
		}

		public StructStack (int count)
		{
			_count = count;
		}

		public int Count => Stack == null ? 0 : Stack.Count;

		public bool Equals (StructStack<TValue> other)
		{
			if (this.Count != other.Count)
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
			Stack ??= new Stack<TValue> (_count);
			return Stack.GetEnumerator ();
		}

		internal void Push (TValue value)
		{
			Stack ??= new Stack<TValue> (_count);
			Stack.Push (value);
		}

		internal TValue Pop ()
		{
			if (Stack == null) throw new InvalidOperationException ("Stack is null");
			if (Stack.Count < 1) throw new InvalidOperationException ("Stack is empty");
			return Stack.Pop ();
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
