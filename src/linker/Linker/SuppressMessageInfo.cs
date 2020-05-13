using Mono.Cecil;

namespace Mono.Linker
{
	public struct SuppressMessageInfo
	{
		public string Id;
		public string Scope;
		public string Target;
		public string MessageId;
	}
}
