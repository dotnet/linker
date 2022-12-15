// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	class ConstructedTypesDataFlow
	{
		public static void Main()
		{
			DeconstructedVariable.Test ();
		}

		class DeconstructedVariable
		{
			// https://github.com/dotnet/linker/issues/3158
			[ExpectedWarning ("IL2077", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
			static void DeconstructVariableNoAnnotation ((Type type, object instance) input)
			{
				var (type, instance) = input;
				type.RequiresPublicMethods ();
			}

			record TypeAndInstance ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type, object instance);

			// This is a tricky one - in a way this is a compiler bug
			// Even though the record's property is declared with the annotation, the generated Deconstruct doesn't
			// propagate the annotation into the out parameters.
			// So analyzer would see the annotation (since it doesn't see the Deconstruct call - I think)
			// But IL tooling will see a problem since it sees the Deconstruct call.
			[ExpectedWarning ("IL2067", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
			static void DeconstructRecordWithAnnotation (TypeAndInstance value)
			{
				var (type, instance) = value;
				type.RequiresPublicMethods ();
			}

			class TypeAndInstanceManual
			{
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				public Type type;
				public object instance;

				public TypeAndInstanceManual ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type, object instance)
					=> (this.type, this.instance) = (type, instance);

				public void Deconstruct ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] out Type type, out object instance)
					=> (type, instance) = (this.type, this.instance);
			}
		
			// This case actually works because the annotation is correctly propagated through the Deconstruct
			static void DeconstructClassWithAnnotation (TypeAndInstanceManual value)
			{
				var (type, instance) = value;
				type.RequiresPublicMethods ();
			}

			record TypeAndInstanceRecordManual ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type, object instance)
			{
				// The generated property getter doesn't have the same attributes???
				// The attributes are only propagated to the generated .ctor - so suppressing the warning the this.type doesn't have the matching annotations
				[UnconditionalSuppressMessage ("", "IL2072")]
				public void Deconstruct ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] out Type type, out object instance)
					=> (type, instance) = (this.type, this.instance);
			}

			static void DeconstructRecordManualWithAnnotation (TypeAndInstanceRecordManual value)
			{
				var (type, instance) = value;
				type.RequiresPublicMethods ();
			}

			public static void Test ()
			{
				DeconstructVariableNoAnnotation ((typeof (string), null));
				DeconstructRecordWithAnnotation (new (typeof (string), null));
				DeconstructClassWithAnnotation (new (typeof (string), null));
				DeconstructRecordManualWithAnnotation (new (typeof (string), null));
			}
		}
	}
}
