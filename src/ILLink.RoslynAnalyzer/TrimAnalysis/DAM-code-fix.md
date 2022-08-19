# DynamicallyAccessedMembers (DAM) Code Fix
The DAM warning pattern can be annotated in a way that makes the reflection usage statically analyzable and trim-safe. Adding attributes where certain DAM warnings are displayed to users addresses these warnings and makes user code trim-safe. Previously, users were required to figure out both where and which attribute needed to be added to their code to resolve their DAM warnings, but with the introduction of this Code Fixer users can simply use the quick fixes in Visual Studio and VSCode to resolve the warning.

## How information travels
The DAM Analyzer generates DAM trim warnings, and has the location of which node requires an attribute and which attribute is missing. This information is then able to be passed through in to the diagnostic, either in `DiagnosticContext.cs` or within the DAM Analyzer (if the methods that conflict are a base and an override). We use `diagnositc.Create()`'s `additionalLocations` and `properties` dictionary to pass along the necessary information for the Code Fixer.

## How the Code Fix changes the file
We use `SyntaxGenerator` to create the attribute to add from the DAM argument passed through the `properties` dictionary. We also get the Syntax Node from the `additionalLocations` of the diagnostic. `SyntaxEditor` uses the location and the attribute to add the attribute to the correct spot and update the original document.

## Next Steps
1. **Multiple Arguments:** The Code Fix does not support the case where there are multiple arguments present on a node (ie `DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields)`).
2. **Merging Arguments:** When there are two differing DAM attributes on nodes that should have the same attribute, we do not provide a Code Fix. However, we could read which attributes are present, merge them, and replace the attributes in both locations.
3. **Replace Checks in `DAMCodeFixProvider.AddAttributeAsync()`:** Changes to `AddAttribute()` and `AddReturnAttribute()` were made that should be updated in the `DAMCodeFixProvider` once the new Roslyn package is published and the repo uses the new package. We can remove the `addGenericParameterAttribute` check from `DAMCodeFixProvider.AddReturnAttribute()` entirely as the API will support adding a generic parameter using `AddAttribute()`. Additionally, we can replace the lambda function in the return attribute check with `AddReturnAttribute()`.
