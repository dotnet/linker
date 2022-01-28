// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	class DiagnosticSolver
	{
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods
		internal void SolveDiagnosticsForDynamicallyAccessedMembers (in DiagnosticContext diagnosticContext, ITypeSymbol typeSymbol, DynamicallyAccessedMemberTypes requiredMemberTypes, bool declaredOnly = false)
		{
			foreach (var member in typeSymbol.GetDynamicallyAccessedMembers (requiredMemberTypes, declaredOnly)) {
				switch (member) {
				case IMethodSymbol method:
					SolveDiagnosticsForMethod (diagnosticContext, method);
					break;
				case IFieldSymbol field:
					SolveDiagnosticsForField (diagnosticContext, field);
					break;
				case IPropertySymbol property:
					SolveDiagnosticsForProperty (diagnosticContext, property);
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
					SolveDiagnosticsForEvent (diagnosticContext, @event);
					break;
				}
			}
		}

		static void ReportRequiresUnreferencedCodeDiagnostic (in DiagnosticContext diagnosticContext, AttributeData requiresAttributeData, ISymbol member)
		{
			var message = RequiresUnreferencedCodeUtils.GetMessageFromAttribute (requiresAttributeData);
			var url = RequiresAnalyzerBase.GetUrlFromAttribute (requiresAttributeData);
			diagnosticContext.ReportDiagnostic (DiagnosticId.RequiresUnreferencedCode, member.GetDisplayName (), message, url);
		}

		static void SolveDiagnosticsForMethod (in DiagnosticContext diagnosticContext, IMethodSymbol methodSymbol)
		{
			if (methodSymbol.TryGetRequiresUnreferencedCodeAttribute(out var requiresAttributeData))
				ReportRequiresUnreferencedCodeDiagnostic (diagnosticContext, requiresAttributeData, methodSymbol);

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

		static void SolveDiagnosticsForProperty (in DiagnosticContext diagnosticContext, IPropertySymbol propertySymbol)
		{
			if (propertySymbol.SetMethod is not null)
				SolveDiagnosticsForMethod (diagnosticContext, propertySymbol.SetMethod);
			if (propertySymbol.GetMethod is not null)
				SolveDiagnosticsForMethod (diagnosticContext, propertySymbol.GetMethod);
		}

		static void SolveDiagnosticsForEvent (in DiagnosticContext diagnosticContext, IEventSymbol eventSymbol)
		{
			if (eventSymbol.AddMethod is not null)
				SolveDiagnosticsForMethod (diagnosticContext, eventSymbol.AddMethod);
			if (eventSymbol.RemoveMethod is not null)
				SolveDiagnosticsForMethod (diagnosticContext, eventSymbol.RemoveMethod);
			if (eventSymbol.RaiseMethod is not null)
				SolveDiagnosticsForMethod (diagnosticContext, eventSymbol.RaiseMethod);
		}

		static void SolveDiagnosticsForField (in DiagnosticContext diagnosticContext, IFieldSymbol fieldSymbol)
		{
			if (fieldSymbol.TryGetRequiresUnreferencedCodeAttribute (out var requiresAttributeData))
				ReportRequiresUnreferencedCodeDiagnostic (diagnosticContext, requiresAttributeData, fieldSymbol);

			if (fieldSymbol.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None)
				diagnosticContext.ReportDiagnostic (DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection, fieldSymbol.GetDisplayName ());
		}
	}
}
