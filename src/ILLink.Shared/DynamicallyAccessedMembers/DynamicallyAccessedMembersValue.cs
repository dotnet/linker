using System.Diagnostics.CodeAnalysis;

namespace ILLink.Shared
{
	public abstract class DynamicallyAccessedMembersValue : SingleValue
	{
		public abstract DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		// TODO: equality should check DAMT? Might not strictly be necessary.
	}
}