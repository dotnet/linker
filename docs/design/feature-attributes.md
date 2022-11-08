# Feature attributes

This specification defines the semantics of "feature attributes", in terms of a hypothetical `RequiresFeature` attribute type. The rules described here are designed to be applicable to any attribute that describes feature or capability of the code or the platform.

## Motivation

Existing attributes like `RequiresUnreferencedCodeAttribute`, `RequiresDynamicCodeAttribute`, and `RequiresAssemblyFilesAttribute` have behavior close to what is described below. The behavior differs slightly between illink and NativeAot in the details, so this is an attempt to specify the semantics as clearly as possible, so that both tools can converge to match this.

The ILLink Roslyn analyzer also produces warnings for these attributes, but doesn't have insight into the compilation strategy used for compiler-generated code. These rules are designed so that the warnings produced by a Roslyn analyzer are matched by the IL analysis, but IL analysis may include additional warnings (specifically for reflection access to compiler-generated code).

There is also the possibility that we will create an attribute-based model which allows users to define their own such attributes; see this draft for example: https://github.com/dotnet/designs/pull/261. The semantics outlined here could be extended to those attributes if we determine that they are appropriate there.

## Goals

- Define the semantics of feature attributes
- Define the access patterns which are allowed and disallowed by these semantics

## Non-goals

- Specify the warning codes or wording of the specific warnings for disallowed access
- Define a model for defining new feature attributes
- Define an attribute-based model for feature switches
- Define the interactions between `RequiresUnreferencedCodeAttribute` and `DynamicallyAccessedMembersAttribute`

## RequiresFeatureAttribute

`RequiresFeature` may be used on methods, constructors, or classes only.

The use of this attribute establishes a [_feature requirement_](#feature-requirement) for the attributed type or member, which restricts access to the attributed type or member (and in some cases to other related IL) in certain ways. It also establishes a [_feature available_](#feature-available-scope) scope (which includes the attributed member but may also include other related IL) wherein access to members with a _feature requirement_ is allowed.

Access to members with a _feature requirement_ is always allowed from a _feature available_ scope, and never produces feature warnings. The restrictions created by _feature requirement_ only limit access from scopes outside of _feature available_, where certain access patterns produce warnings.

## Feature available scopes

Methods and constructors (except static constructors) with a _feature requirement_ are in a _feature available_ scope.

Methods, constructors (except static constructors), fields declared in a class or struct with a _feature requirement_ are also in a _feature available_ scope.

Properties and events declared in a class or struct with a _feature requirement_ are also in a _feature available_ scope.

Lambdas and local functions inside of a method in a _feature available_ scope are also in a _feature available_ scope.

Note that nested types declared in a type that is in a _feature available_ scope are not necessarily in a _feature available_ scope.

## Feature requirement

### Methods

When `RequiresFeature` is used on a method or constructor (except static constructors), this declares a _feature requirement_ for the method.

Static constructors never have a _feature requirement_. `RequiresFeature` on a static constructor is not supported.

### Classes

When `RequiresFeature` is used on a class, this declares a _feature requirement_ for the class.

When a class has a _feature requirement_, this creates a _feature requirement_ for the following members of the class:
  - static methods (not including the static constructor)
  - static fields
  - instance constructors
  - static properties
  - static events

Note that this does not create a _feature requirement_ for nested types.
Note also that this may create a _feature requirement_ for fields, properties, and events, which cannot have `RequiresFeature` used on them directly.

### Structs

When a struct has a _feature requirement_, this creates a _feature requirement_ for the following members of the struct:
  - all methods (not including the static constructor)
  - instance constructors
  - all fields
  - all properties
  - all events

Note also that structs may have _feature requirement_ due to compiler-generated code, even though they can not have `RequiresFeature`.

### State machine types

When an iterator or async method is in a _feature available_ scope, the compiler-generated state machine class or type has a _feature requirement_.

### Nested functions

When a lambda or local function is declared in a method which is in a _feature available_ scope, then the following compiler-generated type or members have a _feature requirement_:

- The generated closure environment type, if it is unique to the lambda or local function, OR

- The generated method for the lambda or local function, if the compiler does not generate a type for the closure environment, OR

- The generated method and delegate cache field for the lambda or local function, if these are generated into a static closure environment type.

For analyzers which don't have visibility into the compiler-generated code for nested functions, nested functions declared in a method which is in a _feature available_ scope have a _feature requirement_.

Note that a lambda or local function inherits _feature requirement_ from the enclosing user method, not from an enclosing lambda or local function if one is present.

## Validation behavior

### RequiresFeatureAttribute

`RequiresFeatureAttribute` on a static constructor warns.

`RequiresFeatureAttribute` on a method that already has a _feature requirement_ due to another attribute is allowed.

`RequiresFeatureAttribute` on a method that is in a _feature available_ scope is allowed. This establishes a _feature requirement_ for the method even if there was not one previously. (Note: this could be made stricter by warning about redundant `RequiresFeatureAttribute` on methods that are already in a _feature available_ scope.)

### Virtual methods

- Overriding a _feature requirement_ method with a method outside of a _feature available_ scope warns.
- Overriding a method outside of a _feature available_ scope with a _feature requirement_ method warns.

### Member access

Access to a _feature requirement_ method, constructor, field, property, or event outside of a _feature available_ scope warns.

## Feature checks

Some feature attributes also come with corresponding feature checks that can be evaluated as constant at the time of trimming, with the guarded code removed when a feature is disabled. This effectivtely places the guarded code in a _feature available_ scope for the purposes of this analysis. However, the definition of such feature checks is left unspecified for now.

## Trimming

These semantics have been designed with trimming in mind. When a feature is disabled (by user configuration, or based on limitations of the target platform), trimming an app that will remove most or all of the feature-related code. Specifically, when a feature is disabled and an app has no trim warnings (including suppressed warnings):

- Methods, fields, properties, and events which have a _feature requirement_ are guaranteed to be removed.

- Methods which are in a _feature available_ scope but aren't entirely removed are guaranteed to have the method body replaced with a throwing instruction sequence.

Thie latter can happen for methods in a type with _feature requirement_ (but that do not themselves have _feature requirement_) that are referenced outside of a _feature available_ scope. The reference to such a method may remain even though the type is never constructed. The callsite would produce a `NullReferenceException` and the method body is unreachable.

## Alternatives

One simplification would be to unify the concepts of _feature requirement_ with _feature available_, and treat both as similar to preprocessor symbols, where _any_ reference to a guarded type or member from an unguarded context warns.

The advantage of the specified model is that it allows some references without warning, giving some extra flexibility and making it easier to migrate existing code. The downside is that it might lead to preserving more code, whereas a simplified model could guarantee that all code related to a disabled feature is removed.

Here is an example of a pattern which does not warn in the current model, but would warn with a simplified model. Assume that the code under `SomeFeatureIsSupported` is removed when the feature is unavailable.

```csharp
class FeatureConsumer {
    static void Run() {
        SomeFeatureProvider? some;
        if (Features.SomeFeatureIsSupported)
            some = new SomeFeatureProvider();
        OtherFeatureProvider other = new();
        Helper(some, other);
    }

    static void Helper(SomeFeatureProvider? some, OtherFeatureProvider other) {
        some?.Use(); // This callsite would warn with the simplified model.
        other.Use();
    }
}

[RequiresSomeFeature]
class SomeFeatureProvider {
    public void Use() {}
}

class OtherFeatureProvider {
    public void Use() {}
}
```

Note that the `SomeFeatureProvider` type and its `Use` method are kept, but the `Use` method will be rewritten to throw.

The simplified model would encourage the above to be rewritten as follows, resulting in the entire type `SomeFeatureProvider` being removed:

```csharp

class FeatureConsumer {
    static void Run() {
        if (Features.SomeFeatureIsSupported) {
            var some = new SomeFeatureProvider();
            some.Use();
        }
        OtherFeatureProvider other = new();
        other.Use();
    }
}
```

Perhaps we could introduce the simplified model as an optional strict mode for people who are interested in rewriting their code for maximal size savings.