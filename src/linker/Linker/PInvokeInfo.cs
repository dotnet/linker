using System.Runtime.Serialization;

namespace Mono.Linker
{
	[DataContract]
	public class PInvokeInfo
	{
		[DataMember (Name = "assembly")]
		internal string AssemblyName { get; set; }

		[DataMember (Name = "entryPoint")]
		internal string EntryPoint { get; set; }

		[DataMember (Name = "fullName")]
		internal string FullName { get; set; }

		[DataMember (Name = "moduleName")]
		internal string ModuleName { get; set; }
	}
}