using System.Linq;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class RemoveSecurityStep : BasePerAssemblyStep
	{
		public RemoveSecurityStep (AssemblyDefinition assembly, LinkContext context) : base (assembly, context)
		{
		}

		public override void Process ()
		{
			if (Annotations.GetAction (_assembly) == AssemblyAction.Link) {
				ClearSecurityDeclarations (_assembly);
				RemoveCustomAttributesThatAreForSecurity (_assembly);

				RemoveCustomAttributesThatAreForSecurity (_assembly.MainModule);

				foreach (var type in _assembly.MainModule.Types)
					ProcessType (type);
			}
		}

		static void ProcessType (TypeDefinition type)
		{
			ClearSecurityDeclarations (type);
			RemoveCustomAttributesThatAreForSecurity (type);
			type.HasSecurity = false;

			foreach (var field in type.Fields)
				RemoveCustomAttributesThatAreForSecurity (field);

			foreach (var method in type.Methods) {
				ClearSecurityDeclarations (method);
				RemoveCustomAttributesThatAreForSecurity (method);
				method.HasSecurity = false;
			}

			foreach (var nested in type.NestedTypes)
				ProcessType (nested);
		}

		static void ClearSecurityDeclarations (ISecurityDeclarationProvider provider)
		{
			if (provider.HasSecurityDeclarations)
				provider.SecurityDeclarations.Clear ();
		}

		/// <summary>
		/// We have to remove some security attributes, otherwise pe verify will complain that a type has HasSecurity = false
		/// </summary>
		/// <param name="provider"></param>
		static void RemoveCustomAttributesThatAreForSecurity (ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
				return;

			var attrsToRemove = provider.CustomAttributes.Where (IsCustomAttributeForSecurity).ToArray ();
			foreach (var remove in attrsToRemove)
				provider.CustomAttributes.Remove (remove);
		}

		static bool IsCustomAttributeForSecurity (CustomAttribute attr)
		{
			var attr_type = attr.AttributeType;
			if (attr_type.Namespace == "System.Security") {
				switch (attr_type.Name) {
				case "SecurityCriticalAttribute":
				case "SecuritySafeCriticalAttribute":
				case "SuppressUnmanagedCodeSecurityAttribute":
				case "DynamicSecurityMethodAttribute":
				case "UnverifiableCodeAttribute":
				case "AllowPartiallyTrustedCallersAttribute":
				case "SecurityTransparentAttribute":
				case "SecurityRulesAttribute":
					return true;
				}
			}

			return false;
		}
	}
}