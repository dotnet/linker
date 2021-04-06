using System;
using System.Xml.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Serialization
{
	[Reference ("System.Xml.XmlSerializer.dll")]
	[Reference ("System.Private.Xml.dll")]
	[SetupLinkerArgument ("--keep-serialization", "true")]
	public class XmlSerialization
	{
		public static void Main ()
		{
			// We don't discover roots passed to the ctor
			new XmlSerializer (typeof (RootType));

			// We don't model type arrays, so ExtraType1 will not be discovered for serialization.
			new XmlSerializer (typeof (RootType), new Type[] { typeof (ExtraType) });

			// We don't track generic instance typerefs in dataflow, so generic parameters in root types will not be discovered.
			new XmlSerializer (typeof (GenericRootType<GenericRootParameter>));

			// There are no annotations for serialized types, so we can only discover types statically referenced by the direct caller of the XmlSerializer ctor.
			XmlSerializerHelper (typeof (RootType));
			GenericXmlSerializerHelper<RootType> ();
		}

		[Kept]
		public static XmlSerializer XmlSerializerHelper (Type t)
		{
			return new XmlSerializer (t);
		}

		[Kept]
		public static XmlSerializer GenericXmlSerializerHelper<T> ()
		{
			return new XmlSerializer (typeof (T));
		}
	}

	[Kept]
	class RootType
	{
		// removed
		int f1;
	}
	[Kept]
	class ExtraType
	{
		// removed
		int f1;
	}

	[Kept]
	class GenericRootParameter
	{
		// removed
		int f2;
	}

	[Kept]
	class GenericRootType<T>
	{
		// removed
		T f1;
		// removed
		int f2;
	}

	[Kept]
	[KeptMember (".ctor()")]
	[KeptAttributeAttribute (typeof (XmlRootAttribute))]
	[XmlRoot]
	class AttributedType
	{
		[Kept]
		int f1;
	}

	// removed
	class XmlIgnoreMember
	{
		// removed
		[XmlIgnore]
		int f1;
	}

	[Kept]
	[KeptMember (".ctor()")]
	[KeptAttributeAttribute (typeof (XmlRootAttribute))]
	[XmlRoot]
	class AttributedTypeWithIgnoreField
	{
		// Kept due to outer attribute
		[Kept]
		[KeptAttributeAttribute (typeof (XmlIgnoreAttribute))]
		[XmlIgnore]
		int f1;

		[Kept]
		int f2;
	}

	[Kept]
	[KeptMember (".ctor()")]
	class AttributedFieldType
	{
		[Kept]
		[KeptAttributeAttribute (typeof (XmlElementAttribute))]
		[XmlElement]
		int f1;

		[Kept]
		int f2;
	}

	[Kept]
	[KeptMember (".ctor()")]
	class AttributedPropertyType
	{
		[Kept]
		[KeptBackingField]
		[KeptAttributeAttribute (typeof (XmlElementAttribute))]
		[XmlElement]
		static int P { [Kept] get; }

		[Kept]
		int f1;
	}
}
