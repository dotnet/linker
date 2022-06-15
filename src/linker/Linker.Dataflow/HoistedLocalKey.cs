// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
    public readonly struct HoistedLocalKey : IEquatable<HoistedLocalKey>
    {
        readonly FieldDefinition Field;

        public HoistedLocalKey (FieldDefinition field) {
            Debug.Assert (CompilerGeneratedState.IsHoistedLocal (field));
            Field = field;
        }

        public bool Equals (HoistedLocalKey other) => Field.Equals (other.Field);

        public override bool Equals (object? obj) => obj is HoistedLocalKey other && Equals (other);

        public override int GetHashCode () => Field.GetHashCode ();
    }
}