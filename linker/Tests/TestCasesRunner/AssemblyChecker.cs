﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner {
	class AssemblyChecker {
		readonly AssemblyDefinition originalAssembly, linkedAssembly;

		HashSet<string> linkedMembers;

		public AssemblyChecker (AssemblyDefinition original, AssemblyDefinition linked)
		{
			this.originalAssembly = original;
			this.linkedAssembly = linked;
		}

		public void Verify ()
		{
			// TODO: Implement fully, probably via custom Kept attribute
			Assert.IsFalse (linkedAssembly.MainModule.HasExportedTypes);

			VerifyCustomAttributes (linkedAssembly, originalAssembly);

			linkedMembers = new HashSet<string> (linkedAssembly.MainModule.AllMembers ().Select (s => {
				return s.FullName;
			}), StringComparer.Ordinal);

			var membersToAssert = originalAssembly.MainModule.Types;
			foreach (var originalMember in membersToAssert) {
				var td = originalMember as TypeDefinition;
				if (td != null) {
					if (td.Name == "<Module>") {
						linkedMembers.Remove (td.Name);
						continue;
					}

					TypeDefinition linkedType = linkedAssembly.MainModule.GetType (originalMember.FullName);
					VerifyTypeDefinition (td, linkedType);
					linkedMembers.Remove (td.FullName);

					continue;
				}

				throw new NotImplementedException ($"Don't know how to check member of type {originalMember.GetType ()}");
			}

			Assert.IsEmpty (linkedMembers, "Linked output includes unexpected member");
		}

		protected virtual void VerifyTypeDefinition (TypeDefinition original, TypeDefinition linked)
		{
			ModuleDefinition linkedModule = linked?.Module;

			//
			// Little bit complex check to allow easier test writting to match
			// - It has [Kept] attribute or any variation of it
			// - It contains Main method
			// - It contains at least one member which has [Kept] attribute (not recursive)
			//
			bool expectedKept =
				original.HasAttributeDerivedFrom (nameof (KeptAttribute)) ||
				(linked != null && linkedModule.Assembly.EntryPoint.DeclaringType == linked) ||
				original.AllMembers ().Any (l => l.HasAttribute (nameof (KeptAttribute)));

			if (!expectedKept) {
				if (linked != null)
					Assert.Fail ($"Type `{original}' should have been removed");

				return;
			}

			if (linked == null)
				Assert.Fail ($"Type `{original}' should have been kept");

			if (!original.IsInterface) {
				var expectedBase = GetCustomAttributeStringCtorValue (original, nameof (KeptBaseTypeAttribute)) ?? "System.Object";
				Assert.AreEqual (expectedBase, linked.BaseType?.FullName);
			}

			var expectedInterfaces = new HashSet<string> (GetCustomAttributeStringCtorValues (original, nameof (KeptInterfaceAttribute)));
			if (expectedInterfaces.Count == 0) {
				Assert.IsFalse (linked.HasInterfaces, $"Type `{original}' has unexpected interfaces");
			} else {
				foreach (var iface in linked.Interfaces) {
					Assert.IsTrue (expectedInterfaces.Remove (iface.InterfaceType.FullName), $"Type `{original}' interface `{iface.InterfaceType.FullName}' should have been removed");
				}

				Assert.IsEmpty (expectedInterfaces);
			}

			VerifyGenericParameters (original, linked);
			VerifyCustomAttributes (original, linked);

			foreach (var td in original.NestedTypes) {
				VerifyTypeDefinition (td, linked?.NestedTypes.FirstOrDefault (l => td.FullName == l.FullName));
				linkedMembers.Remove (td.FullName);
			}

			foreach (var f in original.Fields) {
				VerifyField (f, linked?.Fields.FirstOrDefault (l => f.Name == l.Name));
				linkedMembers.Remove (f.FullName);
			}

			foreach (var m in original.Methods) {
				VerifyMethod (m, linked?.Methods.FirstOrDefault (l => m.GetSignature () == l.GetSignature ()));
				linkedMembers.Remove (m.FullName);
			}

			foreach (var p in original.Properties) {
				VerifyProperty (p, linked?.Properties.FirstOrDefault (l => p.Name == l.Name));
				linkedMembers.Remove (p.FullName);
			}

			foreach (var e in original.Events) {
				VerifyEvent (e, linked?.Events.FirstOrDefault (l => e.Name == l.Name));
				linkedMembers.Remove (e.FullName);
			}
		}

		void VerifyField (FieldDefinition src, FieldDefinition linked)
		{
			bool expectedKept = ShouldBeKept (src);

			if (!expectedKept) {
				if (linked != null)
					Assert.Fail ($"Field `{src}' should have been removed");

				return;
			}

			if (linked == null)
				Assert.Fail ($"Field `{src}' should have been kept");

			Assert.AreEqual (src?.Attributes, linked?.Attributes, $"Field `{src}' attributes");
			Assert.AreEqual (src?.Constant, linked?.Constant, $"Field `{src}' value");

			VerifyCustomAttributes (src, linked);
		}

		void VerifyProperty (PropertyDefinition src, PropertyDefinition linked)
		{
			bool expectedKept = ShouldBeKept (src);

			if (!expectedKept) {
				if (linked != null)
					Assert.Fail ($"Property `{src}' should have been removed");

				return;
			}

			if (linked == null)
				Assert.Fail ($"Property `{src}' should have been kept");

			Assert.AreEqual (src?.Attributes, linked?.Attributes, $"Property `{src}' attributes");
			Assert.AreEqual (src?.Constant, linked?.Constant, $"Property `{src}' value");

			VerifyCustomAttributes (src, linked);
		}

		void VerifyEvent (EventDefinition src, EventDefinition linked)
		{
			bool expectedKept = ShouldBeKept (src);

			if (!expectedKept) {
				if (linked != null)
					Assert.Fail ($"Event `{src}' should have been removed");

				return;
			}

			if (linked == null)
				Assert.Fail ($"Event `{src}' should have been kept");

			Assert.AreEqual (src?.Attributes, linked?.Attributes, $"Event `{src}' attributes");

			VerifyCustomAttributes (src, linked);
		}

		void VerifyMethod (MethodDefinition src, MethodDefinition linked)
		{
			var srcSignature = src.GetSignature ();
			bool expectedKept = ShouldBeKept (src, srcSignature) || (linked != null && linked.DeclaringType.Module.EntryPoint == linked);

			if (!expectedKept) {
				if (linked != null)
					Assert.Fail ($"Method `{src.FullName}' should have been removed");

				return;
			}

			if (linked == null)
				Assert.Fail ($"Method `{src.FullName}' should have been kept");

			Assert.AreEqual (src?.Attributes, linked?.Attributes, $"Method `{src}' attributes");

			VerifyGenericParameters (src, linked);
			VerifyCustomAttributes (src, linked);
		}

		static void VerifyCustomAttributes (ICustomAttributeProvider src, ICustomAttributeProvider linked)
		{
			var expectedAttrs = new List<string> (GetCustomAttributeStringCtorValues (src, nameof (KeptAttributeAttribute)));
			var linkedAttrs = new List<string> (FilterLinkedAttributes (linked));

			// FIXME: Linker unused attributes removal is not working
			// Assert.That (linkedAttrs, Is.EquivalentTo (expectedAttrs), $"Custom attributes on `{src}' are not matching");
		}

		static IEnumerable<string> FilterLinkedAttributes (ICustomAttributeProvider linked)
		{
			foreach (var attr in linked.CustomAttributes) {
				switch (attr.AttributeType.FullName) {
				case "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute":
					continue;
				}

				yield return attr.AttributeType.FullName;
			}
		}

		static void VerifyGenericParameters (IGenericParameterProvider src, IGenericParameterProvider linked)
		{
			Assert.AreEqual (src.HasGenericParameters, linked.HasGenericParameters);
			if (src.HasGenericParameters) {
				for (int i = 0; i < src.GenericParameters.Count; ++i) {
					// TODO: Verify constraints
					VerifyCustomAttributes (src.GenericParameters [i], linked.GenericParameters [i]);
				}
			}
		}

		static bool ShouldBeKept<T> (T member, string signature = null) where T : MemberReference, ICustomAttributeProvider
		{
			if (member.HasAttribute (nameof (KeptAttribute)))
				return true;

			ICustomAttributeProvider cap = (ICustomAttributeProvider)member.DeclaringType;
			if (cap == null)
				return false;

			return GetCustomAttributeStringCtorValues (cap, nameof (KeptMemberAttribute)).Any (a => a == (signature ?? member.Name));
		}

		static string GetCustomAttributeStringCtorValue (ICustomAttributeProvider provider, string attributeName)
		{
			return GetCustomAttributeStringCtorValues (provider, attributeName).FirstOrDefault ();
		}

		static IEnumerable<string> GetCustomAttributeStringCtorValues (ICustomAttributeProvider provider, string attributeName)
		{
			return provider.CustomAttributes.
						   Where (w => w.AttributeType.Name == attributeName && w.Constructor.Parameters.Count == 1).
						   Select (l => l.ConstructorArguments [0].Value as string);
		}
	}
}
