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
			return variableDefinition.ToString () == other.variableDefinition.ToString ();
		}
	}

	public struct BlockState<TValue> : IEquatable<BlockState<TValue>>
		where TValue : IEquatable<TValue>
	{
		public DefaultValueDictionary<LocalKey, TValue> Dictionary;

		public StructStack<TValue> Stack;

		public bool Equals (BlockState<TValue> other)
		{
			return Dictionary.Equals (other.Dictionary);
		}

		public BlockState (TValue defaultValue)
			: this (new DefaultValueDictionary<LocalKey, TValue> (defaultValue), new StructStack<TValue> ())
		{
		}

		public BlockState (DefaultValueDictionary<LocalKey, TValue> dictionary, StructStack<TValue> stack)
		{
			Dictionary = dictionary;
			Stack = stack;
		}

		public BlockState (DefaultValueDictionary<LocalKey, TValue> dictionary)
			: this (dictionary, new StructStack<TValue> ())
		{
		}

		public TValue Get (LocalKey key) => Dictionary.Get (key);

		public void Set (LocalKey key, TValue value) => Dictionary.Set (key, value);

		public void Push (TValue value) => Stack.Push (value);

		public TValue Pop () => Stack.Pop ();

		public TValue Pop (int count) => Stack.Pop (count);
	}

	public readonly struct BlockStateLattice<TValue, TValueLattice> : ILattice<BlockState<TValue>>
		where TValue : IEquatable<TValue>
		where TValueLattice : ILatticeWithUnknownValue<TValue>
	{
		public readonly DictionaryLattice<LocalKey, TValue, TValueLattice> Lattice;
		public readonly StackLattice<TValue, TValueLattice> StackLattice;
		public BlockState<TValue> Top { get; }

		public BlockStateLattice (TValueLattice valueLattice)
		{
			Lattice = new DictionaryLattice<LocalKey, TValue, TValueLattice> (valueLattice);
			StackLattice = new StackLattice<TValue, TValueLattice> (valueLattice);
			Top = new (Lattice.Top);
		}

		public BlockState<TValue> Meet (BlockState<TValue> left, BlockState<TValue> right)
		{
			var dictionary = Lattice.Meet (left.Dictionary, right.Dictionary);
			var stack = StackLattice.Meet (left.Stack, right.Stack);
			return new BlockState<TValue> (dictionary, stack);
		}
	}
}
