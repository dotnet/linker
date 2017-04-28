using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Core.Utils
{
	public static class ExpectationUtils
	{
		public static bool HasExpectedLinkerBehaviorAttribute(this ICustomAttributeProvider provider)
		{
			return provider.HasAttributeDerivedFrom(nameof(BaseExpectedLinkedBehaviorAttribute));
		}

		public static bool IsExpectedLinkerBehaviorAttribute(this CustomAttribute attr)
		{
			return attr.AttributeType.Resolve().DerivesFrom(nameof(BaseExpectedLinkedBehaviorAttribute));
		}

		public static bool ShouldBeRemoved(this ICustomAttributeProvider provider)
		{
			// TODO by Mike : Is it time to ditch the extension method and do something that doesn't require this casting?
			var asMethodDef = provider as MethodDefinition;
			if (asMethodDef != null)
				return asMethodDef.ShouldBeRemoved();

			return provider.HasAttribute(nameof(RemovedAttribute));
		}

		public static bool ShouldBeKept(this ICustomAttributeProvider provider)
		{
			// TODO by Mike : Is it time to ditch the extension method and do something that doesn't require this casting?
			var asMethodDef = provider as MethodDefinition;
			if (asMethodDef != null)
				return asMethodDef.ShouldBeKept();

			return provider.HasAttribute(nameof(KeptAttribute));
		}

		public static bool ShouldBeRemoved(this MethodDefinition method)
		{
			if (method.HasAttribute(nameof(RemovedAttribute)))
				return true;

			// Getter & Setter methods may not have the expectation attributes on them.  They may be on the PropertyDefinition
			// so we need to go check there for expectations
			if (method.IsGetter || method.IsSetter)
			{
				if (method.GetPropertyDefinition().ShouldBeRemoved())
					return true;
			}

			return false;
		}

		public static bool ShouldBeKept(this MethodDefinition method)
		{
			if (method.HasAttribute(nameof(KeptAttribute)))
				return true;

			// Getter & Setter methods may not have the expectation attributes on them.  They may be on the PropertyDefinition
			// so we need to go check there for expectations
			if (method.IsGetter || method.IsSetter)
			{
				if (method.GetPropertyDefinition().ShouldBeKept())
					return true;
			}

			return false;
		}

		// TODO by Mike : Going to have to refactor this, no way to get the Unity StubAttribute included here
		public static bool HasSelfAssertions(this ICustomAttributeProvider provider)
		{
			return provider.ShouldBeKept() || provider.ShouldBeRemoved();
		}

		// TODO by Mike : Going to have to refactor this, no way to get the Unity StubAttribute included here
		public static bool IsSelfAssertion(this CustomAttribute attr)
		{
			return attr.AttributeType.Name == nameof(KeptAttribute) || attr.AttributeType.Name == nameof(RemovedAttribute);
		}
	}
}
