# ILLink Assembly Loading

Current versions of the linker do not support loading and processing assemblies that are only referenced dynamically (via reflection, strings in special attributes, or embedded XML), which leads to incorrect outputs that are missing code required by these dependencies. We need a way to load and process assemblies during later steps of the pipeline, to solve these correctness issues.

The linker also does a lot of unnecessary processing because it loads static references of root assemblies up-front. Pipeline steps process all assemblies in the static reference closure, reading embedded XML, propagating constants, and doing branch elimination, whether or not they contain any code that is used at runtime. A secondary goal is to avoid unnecessary processing of such unused assemblies to improve the linker's performance.

## Assembly processing

Assemblies loaded into the context usually need to have the following processing done before or during marking:
- build the type hierarchy info
- read descriptor XML and mark its new dependencies
- read attribute XML and process it for additional/removed attributes
- read substitutions XML and process it for constant methods
- process the assembly IL for constant methods
- remove unused branches after propagating constants (possibly from another assembly)

## Current behavior

### Assembly references loaded up-front

The linker processes a set of roots from command-line inputs (specific assemblies by name, or XML files that specify members to root). `LoadReferencesStep`, an early step in the linker pipeline, recursively loads all assemblies statically referenced by the root assemblies. The set of loaded assemblies is maintained in the `LinkContext`, and passed through each pipeline step in turn.

Other early steps which load additional assemblies (for example, steps which process embedded XML, or scan for `DynamicDependencyAttribute`s referencing assemblies by name) are implemented as subclasses of `LoadReferencesStep`, calling into the same logic to recursively load referenced assemblies. However, these steps do not do recursive _processing_ of the referenced assemblies - so for example, an assembly referenced only by embedded XML will not have its own embedded XML processed.

XML descriptors from the static reference closure are processed whether or not the embedding assembly is marked.

### Dynamic assembly references not discovered

Later steps do not load additional assemblies, with rare exceptions. Dynamic references (from reflection, or from special attributes) are only looked up in already-loaded assemblies, so the linker is unable to find assemblies only referenced dynamically, producing incorrect outputs which are missing the code required by such references. This problem is partially mitigated for `DynamicDependencyAttribute` by a step that pre-scans for assemblies referenced from instances of this attribute, but it does not recursively handle `DynamicDependencyAttribute` instances in these referenced assemblies.

## .NET6 behavior

### Assembly references loaded lazily

Assemblies will be resolved and loaded lazily, whether by name or from a metadata reference. We will avoid loading referenced assemblies up-front, instead relying on various APIs that all ultimately call `IAssemblyResolver.Resolve` to load assemblies as needed. Trying to `Resolve` an assembly name (using `IAssemblyResolver` or a `LinkContext` helper) or a Cecil member reference (`MethodReference`, `ExportedType`, etc...) will continue to have predictable assembly loading behavior - it will either find a cached result, or look for and possibly load a new assembly. Unused references will typically not be loaded at all, removing the need to process potentially large amounts of unused code. Just resolving an assembly will not trigger any additional processing or recursive assembly resolution.

### Additional processing done lazily

Additional processing will be done lazily (not triggered by `Resolve`), running on a single assembly at a time.

- Type info base/override methods will be built per assembly as requested

  The overrides tracked for a method may be incomplete, and will be updated as new assemblies are loaded. This is compatible with the current design of `MarkStep` which re-processes virtual methods, but this could be optimized in the future.

- XML from an assembly will be processed only if the assembly is marked

  Embedded XML descriptors will logically be considered dependencies of the assembly, not global dependencies.

- Constant propagation and branch elimination will run per marked assembly

  This leaves room to optimize constant propagation to run at a more granular level inline with the marking logic.

### Breaking changes

We will restrict the embedded XML so that it may only modify the containing assembly. This is to prevent cases where a (possibly lazily loaded) assembly modifies code in another assembly that we have already processed, breaking assumptions and creating logical inconsistencies. XML passed on the command-line will continue to work as they do today. Descriptor XML (which may reference other assemblies but is purely additive) will also continue to work as today, whether embedded or passed on the command-line, but this imposes a restriction that the descriptor XML must stay additive. That is, it can cause existing IL in other assemblies to be marked, but may not *modify* other assembly IL.

Embedded XML from assemblies which are not marked may not be processed, to avoid the need to load all referenced assemblies in case they have embedded XML.

Unmarked assemblies will not be processed for constants and will not have unused branches removed.

To support on-demand processing one assembly at a time, we will temporarily remove support for constant propagation across assembly boundaries. Note that to remove unused branches based on constant callees, we need to know whether callees - possibly in other assemblies - are constant. This would require loading and processing direct assembly references for constants before doing branch elimination. In general case, this could require recursive processing of assembly references for new constants introduced by branch elimination. There are plans to move constant propagation to `MarkStep`, which will add back cross-assembly constant propagation. 

### Exceptions to lazy loading

The assembly reference closure will still be loaded in a few cases:

- `copy`/`save` action

  When the linker input has assemblies with the `copy` or `save` actions (either specific assembly actions via `-p`, or default actions via `-c` or `-u`), we will preserve the .NET5 behavior that keeps such assemblies and their dependencies. For consistency with this behavior, static references of dynamically loaded assemblies will be kept as well. This requires loading the assembly reference closure in cases where some of the references are `copy` or `save`.

  However, embedded XML, constant propagation, and branch elimination still obey the rules above, and may not be processed even when the default action is `copy` or `save`.

- `--keep-facades`

  This option will preserve facades from the entire reference closure, even when the facades are only referenced by unused assemblies. We preserve the existing behavior by loading all assembly references.

- Looking up unqualified type names

  These types are currently looked up in the entire reference closure, so in the worst case we may end up loading all references. In some cases, we might be able to match the `GetType` behavior that only searches the calling assembly and corelib.

## Approaches considered

### Load references up-front

Loading static assembly references up-front would ensure that resolving a metadata reference does not introduce new assembly load failures - they would all be surfaced at the point where the root assemblies or the dynamically referenced assemblies are loaded.

This would also ensure that static dependencies have been loaded before processing an assembly, allowing us to preserve the existing behavior of constant propagation across assembly boundaries. This approach leaves room to optimize constant propagation by running bottom-up, one assembly at a time, on the referenced assemblies.

The main downside of this approach is that it requires a lot of unnecessary processing to preserve the existing behavior of constant propagation and XML processing. We would like to avoid doing this unnecessary work.

### Trigger additional processing on Resolve

Doing the additional processing on `Resolve` (which would still be called lazily) would not require the mark logic to call extra processing explicitly. Simply calling `Resolve` would ensure that prerequisites of marking have been met, making the results of constant propagation and branch elimination available. This has the same challenges with constant propagation across assemblies as the suggested approach, so we would impose similar restrictions.

This approach also has the challenge that steps calling `Resolve` don't directly control which processing is done for the loaded assembly. Just resolving a reference could lead to a lot of undesired processing, so this would require the behavior of `Resolve` to depend on where it is called from - for example, by allowing different steps to register temporary `Resolve` handlers, or similar.

Recursion is another potential issue with this approach. For example, processing XML descriptors on `Resolve` could lead to the processing of more descriptors for newly resolved assemblies. Mitigating this by deferring processing of XML would end up looking similar to the suggested approach.

