using System;
using System.Runtime.Serialization;

namespace Mono.Linker
{
	[DataContract]
	public class PInvokeInfo : IComparable
	{
		[DataMember (Name = "assembly")]
		internal string AssemblyName { get; set; }

		[DataMember (Name = "entryPoint")]
		internal string EntryPoint { get; set; }

		[DataMember (Name = "fullName")]
		internal string FullName { get; set; }

		[DataMember (Name = "moduleName")]
		internal string ModuleName { get; set; }

		public int CompareTo (object obj)
		{
			if (obj == null) return 1;

			PInvokeInfo compareTo = obj as PInvokeInfo;
			int compareField = string.Compare (this.AssemblyName, compareTo.AssemblyName);
			if (compareField != 0) return compareField;

			compareField = string.Compare (this.ModuleName, compareTo.ModuleName);
			if (compareField != 0) return compareField;

			compareField = string.Compare (this.FullName, compareTo.FullName);
			if (compareField != 0) return compareField;

			return string.Compare (this.EntryPoint, compareTo.EntryPoint);
		}
	}
}