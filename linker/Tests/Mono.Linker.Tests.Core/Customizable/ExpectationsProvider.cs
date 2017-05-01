using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core.Customizable
{
	public class ExpectationsProvider
	{
		public bool HasExpectedLinkerBehaviorAttribute(ICustomAttributeProvider provider)
		{
			return provider.HasAttributeDerivedFrom(nameof(BaseExpectedLinkedBehaviorAttribute));
		}

		public bool IsExpectedLinkerBehaviorAttribute(CustomAttribute attr)
		{
			return attr.AttributeType.Resolve().DerivesFrom(nameof(BaseExpectedLinkedBehaviorAttribute));
		}

		public virtual bool ShouldBeRemoved(ICustomAttributeProvider provider)
		{
			// TODO by Mike : Is it time to ditch the extension method and do something that doesn't require this casting?
			var asMethodDef = provider as MethodDefinition;
			if (asMethodDef != null)
				return ShouldBeRemoved(asMethodDef);

			return provider.HasAttribute(nameof(RemovedAttribute));
		}

		public virtual bool ShouldBeKept(ICustomAttributeProvider provider)
		{
			// TODO by Mike : Is it time to ditch the extension method and do something that doesn't require this casting?
			var asMethodDef = provider as MethodDefinition;
			if (asMethodDef != null)
				return ShouldBeKept(asMethodDef);

			return provider.HasAttribute(nameof(KeptAttribute));
		}

		public virtual bool ShouldBeRemoved(MethodDefinition method)
		{
			if (method.HasAttribute(nameof(RemovedAttribute)))
				return true;

			// Getter & Setter methods may not have the expectation attributes on them.  They may be on the PropertyDefinition
			// so we need to go check there for expectations
			if (method.IsGetter || method.IsSetter)
			{
				if (ShouldBeRemoved(method.GetPropertyDefinition()))
					return true;
			}

			return false;
		}

		public virtual bool ShouldBeKept(MethodDefinition method)
		{
			if (method.HasAttribute(nameof(KeptAttribute)))
				return true;

			// Getter & Setter methods may not have the expectation attributes on them.  They may be on the PropertyDefinition
			// so we need to go check there for expectations
			if (method.IsGetter || method.IsSetter)
			{
				if (ShouldBeKept(method.GetPropertyDefinition()))
					return true;
			}

			return false;
		}

		public virtual bool HasSelfAssertions(ICustomAttributeProvider provider)
		{
			return ShouldBeKept(provider) || ShouldBeRemoved(provider);
		}

		public virtual bool IsSelfAssertion(CustomAttribute attr)
		{
			return attr.AttributeType.Name == nameof(KeptAttribute) || attr.AttributeType.Name == nameof(RemovedAttribute);
		}

		public virtual bool IsMemberAssertion(CustomAttribute attr)
		{
			return attr.AttributeType.Name == nameof(KeptMemberAttribute) || attr.AttributeType.Name == nameof(RemovedMemberAttribute);
		}

		public virtual bool IsAssemblyAssertion(CustomAttribute attr)
		{
			return attr.AttributeType.Name == nameof(KeptAssemblyAttribute) || attr.AttributeType.Name == nameof(RemovedAssemblyAttribute);
		}
	}
}
