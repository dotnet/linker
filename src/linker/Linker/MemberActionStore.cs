using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{
	public class MemberActionStore
	{
		readonly Dictionary<MethodDefinition, MethodAction> _methodActions;
		readonly Dictionary<MethodDefinition, object> _methodStubValues;

		readonly Dictionary<FieldDefinition, object> _fieldValues;
		readonly HashSet<FieldDefinition> _fieldInit;
		readonly HashSet<AssemblyDefinition> _processedSubstitutionXml;
		readonly LinkContext _context;

		public MemberActionStore (LinkContext context)
		{
			_methodActions = new Dictionary<MethodDefinition, MethodAction> ();
			_methodStubValues = new Dictionary<MethodDefinition, object> ();
			_fieldValues = new Dictionary<FieldDefinition, object> ();
			_fieldInit = new HashSet<FieldDefinition> ();
			_processedSubstitutionXml = new HashSet<AssemblyDefinition> ();
			_context = context;
		}

		void EnsureProcessedSubstitutionXml (AssemblyDefinition assembly)
		{
			if (_processedSubstitutionXml.Add (assembly))
				EmbeddedXmlInfo.ProcessSubstitutions (assembly, _context);
		}

		public void SetAction (MethodDefinition method, MethodAction action)
		{
			_methodActions[method] = action;
		}

		public MethodAction GetAction (MethodDefinition method)
		{
			EnsureProcessedSubstitutionXml (method.Module.Assembly);

			if (_methodActions.TryGetValue (method, out MethodAction action))
				return action;

			return MethodAction.Nothing;
		}

		public void SetMethodStubValue (MethodDefinition method, object value)
		{
			_methodStubValues[method] = value;
		}

		public bool TryGetMethodStubValue (MethodDefinition method, out object value)
		{
			EnsureProcessedSubstitutionXml (method.Module.Assembly);

			return _methodStubValues.TryGetValue (method, out value);
		}

		public void SetFieldValue (FieldDefinition field, object value)
		{
			_fieldValues[field] = value;
		}

		public bool TryGetFieldUserValue (FieldDefinition field, out object value)
		{
			EnsureProcessedSubstitutionXml (field.Module.Assembly);

			return _fieldValues.TryGetValue (field, out value);
		}

		public void SetSubstitutedInit (FieldDefinition field)
		{
			_fieldInit.Add (field);
		}

		public bool HasSubstitutedInit (FieldDefinition field)
		{
			EnsureProcessedSubstitutionXml (field.Module.Assembly);

			return _fieldInit.Contains (field);
		}
	}
}