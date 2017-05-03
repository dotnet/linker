using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core.Customizable {
	public class ExpectationsProvider {

		public virtual bool IsAssemblyAssertion (CustomAttribute attr)
		{
			return attr.AttributeType.Name == nameof (KeptAssemblyAttribute) || attr.AttributeType.Name == nameof (RemovedAssemblyAttribute);
		}

	}
}
