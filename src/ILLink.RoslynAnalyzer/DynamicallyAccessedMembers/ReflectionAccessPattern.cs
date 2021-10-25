using System;
using Microsoft.CodeAnalysis;

using MultiValue = ILLink.Shared.HashSetWrapper<ILLink.RoslynAnalyzer.SingleValue>;

namespace ILLink.Shared
{
	// TODO: arg/ret types? For now takes and returns an AnalysisState
	// using State = LocalState<MyLocalValue>; // TODO: use these usings!

	// Why is this necessary?
	// Because IOperation doesn't implement IEquatable<IOperation>.
	public struct OperationWrapper : IEquatable<OperationWrapper>
	{
		public IOperation Operation;

		public OperationWrapper (IOperation operation) => Operation = operation;

		public bool Equals (OperationWrapper other)
		{
			return Operation == other.Operation;
		}
	}

	// TODO: share this
	public struct ReflectionAccessPattern : IEquatable<ReflectionAccessPattern>
	{
		public readonly MultiValue Source;
		public readonly MultiValue Target;

		// TODO: create abstraction for operation/location suitable for use by
		// roslyn or linker
		public readonly IOperation Operation;

		public ReflectionAccessPattern (MultiValue source, MultiValue target, IOperation operation)
		{
			Source = source;
			Target = target;
			Operation = operation;
		}

		public bool Equals (ReflectionAccessPattern other)
		{
			return Source.Equals (other.Source) &&
				Target.Equals (other.Target) &&
				Operation == other.Operation;
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Source, Target, Operation);
		}
	}
}