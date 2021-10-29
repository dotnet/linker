using Microsoft.CodeAnalysis;
using ILLink.Shared;
namespace ILLink.RoslynAnalyzer
{
	public class KnownTypeValue<TType> : SingleValue
	{
		public readonly TType TypeValue;
		public KnownTypeValue (TType typeValue) => TypeValue = typeValue;
	}
}