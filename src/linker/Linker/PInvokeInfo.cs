using System.Runtime.Serialization;

namespace Mono.Linker
{
	[DataContract]
	public class PInvokeInfo
	{
		[DataMember (Name = "entryPoint")]
		internal string EntryPoint { get; set; }
		
		[DataMember (Name = "moduleName")]
		internal string ModuleName { get; set; }
	}
}