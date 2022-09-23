// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil.Cil;

namespace Mono.Linker.Dataflow
{
	public readonly struct LocalKey : IEquatable<LocalKey>
	{
		readonly VariableDefinition variableDefinition;

		public LocalKey (VariableDefinition variableDefinition) => this.variableDefinition = variableDefinition;

		public bool Equals (LocalKey other)
		{
			return variableDefinition == other.variableDefinition;
		}
	}

	public struct BlockState<TValue> : IEquatable<BlockState<TValue>>
		where TValue : IEquatable<TValue>
	{
		public DefaultValueDictionary<LocalKey, TValue> Dictionary;

		public ValueStack<TValue> Stack;

		public bool Equals (BlockState<TValue> other)
		{
			return Dictionary.Equals (other.Dictionary) && Stack.Equals (other.Stack);
		}

		public BlockState (TValue defaultValue)
			: this (new DefaultValueDictionary<LocalKey, TValue> (defaultValue), new ValueStack<TValue> ())
		{
		}

		public BlockState (DefaultValueDictionary<LocalKey, TValue> dictionary, ValueStack<TValue> stack)
		{
			Dictionary = dictionary;
			Stack = stack;
		}

		public BlockState (DefaultValueDictionary<LocalKey, TValue> dictionary)
			: this (dictionary, new ValueStack<TValue> ())
		{
		}

		public TValue GetLocal (LocalKey key) => Dictionary.Get (key);

		public void SetLocal (LocalKey key, TValue value) => Dictionary.Set (key, value);

		public void Push (TValue value) => Stack.Push (value);

		public TValue Pop () => Stack.Pop ();

		public TValue Pop (int count) => Stack.Pop (count);
	}

	public readonly struct BlockStateLattice<TValue, TValueLattice> : ILattice<BlockState<TValue>>
		where TValue : IEquatable<TValue>
		where TValueLattice : ILatticeWithUnknownValue<TValue>
	{
		public readonly DictionaryLattice<LocalKey, TValue, TValueLattice> LocalsLattice;
		public readonly StackLattice<TValue, TValueLattice> StackLattice;
		public BlockState<TValue> Top { get; }

		public BlockStateLattice (TValueLattice valueLattice)
		{
			LocalsLattice = new DictionaryLattice<LocalKey, TValue, TValueLattice> (valueLattice);
			StackLattice = new StackLattice<TValue, TValueLattice> (valueLattice);
			Top = new (LocalsLattice.Top);
		}

		public BlockState<TValue> Meet (BlockState<TValue> left, BlockState<TValue> right)
		{
			var dictionary = LocalsLattice.Meet (left.Dictionary, right.Dictionary);
			var stack = StackLattice.Meet (left.Stack, right.Stack);
			return new BlockState<TValue> (dictionary, stack);
		}
	}
}
