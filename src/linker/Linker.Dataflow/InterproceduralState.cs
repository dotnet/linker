// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;
using HoistedLocalState = ILLink.Shared.DataFlow.DefaultValueDictionary<
	Mono.Linker.Dataflow.HoistedLocalKey,
	ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>>;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	// Tracks the set of methods which get analyzer together during interprocedural analysis,
	// and the possible states of hoisted locals in state machine methods and lambdas/local functions.
	struct InterproceduralState : IEquatable<InterproceduralState>
	{
		public ValueSet<MethodProxy> Methods;
		public HoistedLocalState HoistedLocals;
		readonly InterproceduralStateLattice lattice;

		public InterproceduralState (ValueSet<MethodProxy> methods, HoistedLocalState hoistedLocals, InterproceduralStateLattice lattice)
			=> (Methods, HoistedLocals, this.lattice) = (methods, hoistedLocals, lattice);

		public bool Equals (InterproceduralState other)
			=> Methods.Equals (other.Methods) && HoistedLocals.Equals (other.HoistedLocals);

		public InterproceduralState Clone ()
			=> new (Methods.Clone (), HoistedLocals.Clone (), lattice);

		public void TrackMethod (MethodProxy method)
		{
			// Work around the fact that ValueSet is readonly
			var methodsList = new List<MethodProxy> (Methods);
			methodsList.Add (method);
			Methods = new ValueSet<MethodProxy> (methodsList);
		}

		public void SetHoistedLocal (HoistedLocalKey key, MultiValue value)
		{
			// For hoisted locals, we track the entire set of assigned values seen
			// in the closure of a method, so setting a hoisted local value meets
			// it with any existing value.
			HoistedLocals.Set (key,
				lattice.HoistedLocalsLattice.ValueLattice.Meet (
					HoistedLocals.Get (key), value));
		}

		public MultiValue GetHoistedLocal (HoistedLocalKey key)
		{
			var value = HoistedLocals.Get (key);
			return value;
		}
	}

	struct InterproceduralStateLattice : ILattice<InterproceduralState>
	{
		public readonly ValueSetLattice<MethodProxy> MethodLattice;
		public readonly DictionaryLattice<HoistedLocalKey, MultiValue, ValueSetLattice<SingleValue>> HoistedLocalsLattice;

		public InterproceduralStateLattice (
			ValueSetLattice<MethodProxy> methodLattice,
			DictionaryLattice<HoistedLocalKey, MultiValue, ValueSetLattice<SingleValue>> hoistedLocalsLattice)
			=> (MethodLattice, HoistedLocalsLattice) = (methodLattice, hoistedLocalsLattice);

		public InterproceduralState Top => new InterproceduralState (MethodLattice.Top, HoistedLocalsLattice.Top, this);

		public InterproceduralState Meet (InterproceduralState left, InterproceduralState right)
			=> new (
				MethodLattice.Meet (left.Methods, right.Methods),
				HoistedLocalsLattice.Meet (left.HoistedLocals, right.HoistedLocals),
				this);
	}
}