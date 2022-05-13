// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.Linker.Steps;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	public class FlowInsensitiveReflectionScanner
	{
		readonly LinkContext _context;
		readonly MarkStep _markStep;
		readonly MessageOrigin _origin;

		public FlowInsensitiveReflectionScanner (LinkContext context, MarkStep markStep, in MessageOrigin origin)
		{
			_context = context;
			_markStep = markStep;
			_origin = origin;
		}

		public void ProcessAttributeDataflow (MethodDefinition method, IList<CustomAttributeArgument> arguments)
		{
			for (int i = 0; i < method.Parameters.Count; i++) {
				var parameterValue = _context.Annotations.FlowAnnotations.GetMethodParameterValue (method, i);
				if (parameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None) {
					MultiValue value = GetValueNodeForCustomAttributeArgument (arguments[i]);
					var diagnosticContext = new DiagnosticContext (_origin, diagnosticsEnabled: true, _context);
					RequireDynamicallyAccessedMembers (diagnosticContext, value, parameterValue);
				}
			}
		}

		public void ProcessAttributeDataflow (FieldDefinition field, CustomAttributeArgument value)
		{
			MultiValue valueNode = GetValueNodeForCustomAttributeArgument (value);
			var fieldValueCandidate = _context.Annotations.FlowAnnotations.GetFieldValue (field);
			if (fieldValueCandidate is not ValueWithDynamicallyAccessedMembers fieldValue)
				return;

			var diagnosticContext = new DiagnosticContext (_origin, diagnosticsEnabled: true, _context);
			RequireDynamicallyAccessedMembers (diagnosticContext, valueNode, fieldValue);
		}

		MultiValue GetValueNodeForCustomAttributeArgument (CustomAttributeArgument argument)
		{
			SingleValue value;
			if (argument.Type.Name == "Type") {
				TypeDefinition? referencedType = ((TypeReference) argument.Value).ResolveToTypeDefinition (_context);
				if (referencedType == null)
					value = UnknownValue.Instance;
				else
					value = new SystemTypeValue (referencedType);
			} else if (argument.Type.MetadataType == MetadataType.String) {
				value = new KnownStringValue ((string) argument.Value);
			} else {
				// We shouldn't have gotten a non-null annotation for this from GetParameterAnnotation
				throw new InvalidOperationException ();
			}

			Debug.Assert (value != null);
			return value;
		}

		public void ProcessGenericArgumentDataFlow (GenericParameter genericParameter, TypeReference genericArgument)
		{
			var genericParameterValue = _context.Annotations.FlowAnnotations.GetGenericParameterValue (genericParameter);
			Debug.Assert (genericParameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None);

			MultiValue genericArgumentValue = _context.Annotations.FlowAnnotations.GetTypeValueNodeFromGenericArgument (genericArgument);

			var diagnosticContext = new DiagnosticContext (_origin, !_context.Annotations.ShouldSuppressAnalysisWarningsForRequiresUnreferencedCode (_origin.Provider), _context);
			RequireDynamicallyAccessedMembers (diagnosticContext, genericArgumentValue, genericParameterValue);
		}

		void RequireDynamicallyAccessedMembers (in DiagnosticContext diagnosticContext, in MultiValue value, ValueWithDynamicallyAccessedMembers targetValue)
		{
			var reflectionMarker = new ReflectionMarker (_context, _markStep, enabled: true);
			var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction (reflectionMarker, diagnosticContext);
			requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
		}
	}
}