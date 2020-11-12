# ILLink Assembly Loading

We would like to support loading and processing assemblies later in the pipeline, so that dynamically referenced assemblies can be correctly processed. A secondary goal is to avoid unnecessary processing of unused assemblies.

## Assembly processing

Assemblies loaded into the context usually need to have the following processing done before or during marking:
- build the type hierarchy info
- read descriptor XML and mark its new dependencies
- read attribute XML and process it for additional/removed attributes
- read substitutions XML and process it for constant methods
- process the assembly IL for constant methods
- remove unused branches after propagating constants (possibly from another assembly)

## .NET5 behavior

### Assembly references loaded up-front

The linker processes a set of roots from command-line inputs (specific assemblies by name, or XML files that specify members to root). `LoadReferencesStep`, an early step in the linker pipeline, recursively loads all assemblies statically referenced by the root assemblies. The set of loaded assemblies is maintained in the `LinkContext`, and passed through each pipeline step in turn.

Other early steps which load additional assemblies (for example, steps which process embedded XML, or scan for `DynamicDependencyAttribute`s referencing assemblies by name) are implemented as subclasses of `LoadReferencesStep`, calling into the same logic to recursively load referenced assemblies. However, these steps do not do recursive _processing_ of the referenced assemblies - so for example, an assembly referenced only by embedded XML will not have its own embedded XML processed.

XML descriptors from the static reference closure are processed whether or not the embedding assembly is marked.

### Dynamic assembly references not discovered

Later steps do not load additional assemblies, with rare exceptions. Dynamic references (from reflection, or from special attributes) are only looked up in already-loaded assemblies, so the linker is unable to find assemblies only referenced dynamically, producing incorrect outputs which are missing the code required by such references. This problem is partially mitigated for `DynamicDependencyAttribute` by a step that pre-scans for assemblies referenced from instances of this attribute, but it does not recursively handle `DynamicDependencyAttribute` instances in these referenced assemblies.


## .NET6 behavior

### Assembly references loaded lazily

Assemblies will be resolved and loaded lazily, whether by name or from a metadata reference. Unused references will typically not be loaded at all, removing the need to process potentially large amounts of unused code. The result of calling `Resolve` will stay predictable (it will either find a cached result, or look for and possibly load a new assembly). Just resolving an assembly will not trigger any additional processing or recursive assembly resolution.

### Additional processing done lazily

Additional processing will be done lazily (not triggered by `Resolve`), running on a single assembly at a time.

- Type info base/override methods will be built per assembly as requested

  The overrides tracked for a method may be incomplete, and will be updated as new assemblies are loaded. This is compatible with the current design of `MarkStep` which re-processes virtual methods, but this could be optimized in the future.

- XML from an assembly will be processed only if the assembly is marked

  Embedded XML from statically referenced assemblies may no longer be processed if the embedding assembly is not marked.

- Constant propagation and branch elimination will will run per marked assembly

  Unmarked assemblies will not be processed for constants and will not have unused branches removed. This will not inline constants across assemblies, and leaves room to optimize constant propagation to run at a more granular level inline with the marking logic.

### Restrictions

We will restrict the embedded XML so that it may only modify the containing assembly. This is to prevent cases where a (possibly lazily loaded) assembly modifies code in another assembly that we have already processed, breaking assumptions and creating logical inconsistencies.

Embedded XML from assemblies which are not marked may not be processed, to avoid the need to load all referenced assemblies in case they have embedded XML.

To support on-demand processing one assembly at a time, we will temporarily remove support for constant propagation across assembly boundaries. Note that to remove unused branches based on constant callees, we need to know whether callees - possibly in other assemblies - are constant. This would require loading and processing direct assembly references for constants before doing branch elimination. In general case, this could require recursive processing of assembly references for new constants introduced by branch elimination. There are plans to move constant propagation to `MarkStep`, which will add back cross-assembly constant propagation. 

### Exceptions to lazy loading

When the linker default action is `copy`, we will preserve the .NET5 behavior that keeps all statically referenced assemblies (that don't override the default action). For consistency with this behavior, static references of dynamically loaded assemblies will be kept as well. This will be done by pre-loading the reference closure of assemblies if the linker's default action is `copy`.

However, embedded XML, constant propagation, and branch elimination still obey the rules above, and may not be processed even when the default action is `copy`.

## Approaches considered

### Load references up-front

Loading static assembly references up-front would ensure that resolving a metadata reference does not introduce new assembly load failures - they would all be surfaced at the point where the root assemblies or the dynamically referenced assemblies are loaded.

This would also ensure that static dependencies have been loaded before processing an assembly, allowing us to preserve the existing behavior of constant propagation across assembly boundaries. This approach leaves room to optimize constant propagation by running bottom-up, one assembly at a time, on the referenced assemblies.

The main downside of this approach is that it requires a lot of unnecessary processing to preserve the existing behavior of constant propagation and XML processing. We would like to avoid doing this unnecessary work.

### Trigger additional processing on Resolve

Doing the additional processing on `Resolve` (which would still be called lazily) would not require the mark logic to call extra processing explicitly. Simply calling `Resolve` would ensure that prerequisites of marking have been met, making the results of constant propagation and branch elimination available. This has the same challenges with constant propagation across assemblies as the suggested approach, so we would impose similar restrictions.

This approach also has the challenge that steps calling `Resolve` don't directly control which processing is done for the loaded assembly. Just resolving a reference could lead to a lot of undesired processing, so this would require the behavior of `Resolve` to depend on where it is called from - for example, by allowing different steps to register temporary `Resolve` handles, or similar.

Recursion is another potential issue with this approach. For example, processing XML descriptors on `Resolve` could lead to the processing of more descriptors for newly resolved assemblies. The suggested approach mitigates this by deferring processing of XML would end up looking similar to the suggested approach.

