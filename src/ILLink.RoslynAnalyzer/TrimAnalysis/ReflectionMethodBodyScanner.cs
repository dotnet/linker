// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	class ReflectionMethodBodyScanner
	{
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods
		internal void MarkTypeForDynamicallyAccessedMembers (in DiagnosticContext diagnosticContext, ITypeSymbol typeSymbol, DynamicallyAccessedMemberTypes requiredMemberTypes, bool declaredOnly = false)
		{
			foreach (var member in typeSymbol.GetDynamicallyAccessedMembers (requiredMemberTypes, declaredOnly)) {
				switch (member) {
				case IMethodSymbol method:
					MarkMethod (diagnosticContext, method);
					break;
				case IFieldSymbol field:
					MarkField (diagnosticContext, field);
					break;
				case IPropertySymbol property:
					MarkProperty (diagnosticContext, property);
					break;
				/* Skip Type and InterfaceImplementation marking since doesnt seem relevant for diagnostic generation
				case ITypeSymbol nestedType:
					MarkType (diagnosticContext, nestedType);
					break;
				case InterfaceImplementation interfaceImplementation:
					MarkInterfaceImplementation (analysisContext, interfaceImplementation, dependencyKind);
					break;
				*/
				case IEventSymbol @event:
					MarkEvent (diagnosticContext, @event);
					break;

				}
			}
		}

		static void ReportRequiresUnreferencedCodeDiagnostic (DiagnosticContext diagnosticContext, AttributeData requiresAttributeData, ISymbol member)
		{
			var message = RequiresUnreferencedCodeUtils.GetMessageFromAttribute (requiresAttributeData);
			var url = RequiresAnalyzerBase.GetUrlFromAttribute (requiresAttributeData);
			diagnosticContext.ReportDiagnostic (DiagnosticId.RequiresUnreferencedCode, member.GetDisplayName (), message, url);
		}

		static void MarkMethod (DiagnosticContext diagnosticContext, IMethodSymbol methodSymbol)
		{
			if (methodSymbol.TargetHasRequiresUnreferencedCodeAttribute (out var requiresAttributeData)
						&& RequiresUnreferencedCodeUtils.VerifyRequiresUnreferencedCodeAttributeArguments (requiresAttributeData)) {
				ReportRequiresUnreferencedCodeDiagnostic (diagnosticContext, requiresAttributeData, methodSymbol);
			}

			if (methodSymbol.IsVirtual && methodSymbol.GetDynamicallyAccessedMemberTypesOnReturnType () != DynamicallyAccessedMemberTypes.None)
				diagnosticContext.ReportDiagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, methodSymbol.GetDisplayName ());
			else if (methodSymbol.IsVirtual && methodSymbol.MethodKind is MethodKind.PropertyGet &&
				(methodSymbol.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None ||
				methodSymbol.GetDynamicallyAccessedMemberTypesOnAssociatedSymbol () != DynamicallyAccessedMemberTypes.None)) {
				diagnosticContext.ReportDiagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, methodSymbol.GetDisplayName ());
			} else if (methodSymbol.MethodKind is MethodKind.PropertySet &&
				  (methodSymbol.GetDynamicallyAccessedMemberTypesOnReturnType () != DynamicallyAccessedMemberTypes.None ||
				  methodSymbol.GetDynamicallyAccessedMemberTypesOnAssociatedSymbol () != DynamicallyAccessedMemberTypes.None)) {
				diagnosticContext.ReportDiagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, methodSymbol.GetDisplayName ());
			} else {
				foreach (var parameter in methodSymbol.Parameters) {
					if (parameter.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None) {
						diagnosticContext.ReportDiagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, methodSymbol.GetDisplayName ());
						return;
					}
				}
			}
		}

		static void MarkProperty (DiagnosticContext diagnosticContext, IPropertySymbol propertySymbol)
		{
			if (propertySymbol.TargetHasRequiresUnreferencedCodeAttribute (out var requiresAttributeData)
						&& RequiresUnreferencedCodeUtils.VerifyRequiresUnreferencedCodeAttributeArguments (requiresAttributeData)) {
				if (propertySymbol.GetMethod != null)
					ReportRequiresUnreferencedCodeDiagnostic (diagnosticContext, requiresAttributeData, propertySymbol.GetMethod);
				if (propertySymbol.SetMethod != null)
					ReportRequiresUnreferencedCodeDiagnostic (diagnosticContext, requiresAttributeData, propertySymbol.SetMethod);
			}

			if (propertySymbol.SetMethod is not null && propertySymbol.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None)
				diagnosticContext.ReportDiagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, propertySymbol.SetMethod.GetDisplayName ());

			if (propertySymbol.IsVirtual && propertySymbol.GetMethod is not null && propertySymbol.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None)
				diagnosticContext.ReportDiagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, propertySymbol.GetMethod.GetDisplayName ());
		}

		static void MarkEvent (DiagnosticContext diagnosticContext, IEventSymbol eventSymbol)
		{
			if (eventSymbol.TargetHasRequiresUnreferencedCodeAttribute (out var requiresAttributeData)
						&& RequiresUnreferencedCodeUtils.VerifyRequiresUnreferencedCodeAttributeArguments (requiresAttributeData)) {
				if (eventSymbol.AddMethod != null)
					ReportRequiresUnreferencedCodeDiagnostic (diagnosticContext, requiresAttributeData, eventSymbol.AddMethod);
				if (eventSymbol.RemoveMethod != null)
					ReportRequiresUnreferencedCodeDiagnostic (diagnosticContext, requiresAttributeData, eventSymbol.RemoveMethod);
				if (eventSymbol.RaiseMethod != null)
					ReportRequiresUnreferencedCodeDiagnostic (diagnosticContext, requiresAttributeData, eventSymbol.RaiseMethod);
			}
		}

		static void MarkField (DiagnosticContext diagnosticContext, IFieldSymbol fieldSymbol)
		{
			if (fieldSymbol.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None)
				diagnosticContext.ReportDiagnostic (DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection, fieldSymbol.GetDisplayName ());
		}
	}
}
