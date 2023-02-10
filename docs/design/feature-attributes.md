# Feature Attributes

ILLink allows assemblies to define feature switches which can be used to conditionally remove features, depending on swtches passed to the linker at publish time. These feature switches are defined in an unfriendly XML format that is hard to use, so this is a proposal to enable equivalent functionality using attributes instead.

## Background

### Feature switches

.NET libraries often ship with many features, only some of which may be used by a given app. The linker already has the ability to conditionally remove code based on "feature switches", and the core libraries have already [defined](https://github.com/dotnet/runtime/blob/master/docs/workflow/trimming/feature-switches.md) several feature switches that are used to remove unused feature and reduce the size of blazor applications. See [Feature switch](https://github.com/dotnet/designs/blob/main/accepted/2020/feature-switch.md) for more motivation and background.

These feature switches are defined in an XML file embedded into the feature library. This is not a great long-term solution:
- There is extra ceremony involved in adding feature definitions as embedded resources
- Setting up one of these definitions requires learning a new XML format that is hard to understand
- Feature definition files aren't validated until link time, so syntax errors could make it into published applications
- They carry logic which really belongs with the code, not with an external file

This is a proposal to enable equivalent functionality using attributes in code.

### Conditional compilation

Roslyn and the SDK support conditional compilation via preprocessor symbols, `ConditionalAttribute`, and conditions in the project file. In cases where the same sources are compiled with different feature sets, this may be preferable to using feature attributes. For example, these approaches are suitable when shipping nuget packages with platform-specific implementations:

```xml
<Compile Include="src/unix/x64Implementation.cs" Condition="'$(TargetArchitecture) == 'x64'" />
```

```xml
<PackageReference Include="CoolPackage" Condition="'$(TargetFramework)' == 'netcoreapp3.0'" />
```

```csharp
public static void CoolFeature()
{
#if NETCOREAPP
    // implementation for netcoreapp
#else
    // implementation for full framework
#endif
}
```

```csharp
public static void CoolFeature()
{
    NetcoreappImplementation();
    NetfxImplementation();
}

[Condition("NETCOREAPP")]
public static void NetcoreappImplementation()
{

}

[Condition("NETFX")]
public static void NetfxImplementation()
{
}
```

However, since we typically distribute packages containing dlls that include all optional functionality, we additionally need a way to remove features from already-compiled assemblies when publishing an application - this is especially important for self-contained applications, which would otherwise include all optional functionality of the runtime and framework libraries.

## Proposal

The feature switch XML maps feature settings (feature name string, bool pairs) to a constant return of a particular property. We enable equivalent functionality by attributing the feature property with `FeatureDefinitionAttribute("FeatureName")`. For example, the following are equivalent:

```xml
<linker>
  <type fullname="Foo" feature="EnableFoo" featurevalue="true">
    <method signature="System.Boolean get_IsSupported()" body="stub" value="true">
  </type>
</linker>
```

```csharp
class Foo
{
    [FeatureDefinition("EnableFoo")]
    static bool IsSupported { get; } = InitializeIsSupported();

    private static bool InitializeIsSupported() => // ...;
}
```

If the linker is invoked with a feature setting for `EnableFoo` (via `--feature EnableFoo true` or `--feature EnableFoo false`), it will substitute the value of `IsSupported` with the supplied boolean.

Note that unlike `ConditionAttribute`, calls to `IsSupported` are preserved regardless of the feature setting. The feature definition only affects the returned value and removes unused branches in callers - it does not eliminate the callsite.

## Open questions

The XML is more flexible than the simple 1-to-1 mapping above. We would need to consider whether and how to represent the following:
- Cases where the feature name has the opposite polarity of the property value

  I wasn't able to find any cases where this is actually used today. We could simplify this and make the value pased to `--feature` the same as the default value.

- Default values (applied when linking without any `--feature` settings)

  This is currently only used in descriptor XML. We would like to move from descriptor XML to `DynamicDependency`, and module constructors could provide a natural place to put assembly-level `DynamicDependency` attributes. Then the feature attribute could be used to make this conditional:

  ```csharp
  class Foo
  {
      [FeatureDefinition("EnableFoo")]
      static bool IsSupported { get; } = InitializeIsSupported();

      [ModuleInitializer]
      internal static void Init() {
          if (Foo.IsSupported)
              RequireDynamicDependencies();
      }

      // These dynamic dependencies are kept by default,
      // but removed when feature 'EnableFoo' is 'false'.
      [DynamicDependency("OtherAssembly", "Bar.SomeMethod")]
      static void RequireDynamicDependencies() {

      }
  }
  ```

- Multiple method substitutions controlled by the same feature switch
- Non-boolean return values (not currently supported by `--feature`, but supported by the XML substitutions in general
- Removing embedded resources based on feature settings

## Future extensions

### API removal

Currently, feature removal doesn't necessarily remove feature APIs from the linked assembly - it only removes unused branches from the feature implementation. This may result in failures that are not detected until runtime. For example:

```csharp
class AwesomeFeature
{
    [FeatureDefinition("AwesomeFeature")]
    static bool IsSupported { get; } = true;

    public static bool DoSomethingCool() {
        if (AwesomeFeature.IsSupported) {
            Console.WriteLine("Cool things are happening!");
        } else {
            throw new NotSupportedException("Sorry... :(");
        }
    }
}
```

```csharp
class AwesomeApplication
{
    public static void Main() {
        AwesomeFeature.DoSomethingCool();
    }
}
```

The application might be tested with "dotnet run", then deployed with the linker. Ideally, feature switches will be written so that they are backed by runtimeconfig settings, so that if the project file has `<RuntimeHostConfigurationOption Include="AwesomeFeature" Value="false" />`, the exception will show up in "dotnet run". However, we don't currently enforce that the feature switches are backed by runtimeconfig settings. It would be useful to have a way to turn this runtime exception into a link-time error, and there might also be size advantages to removing the feature API entirely from the linked app.

We could solve this by adding an attribute that removes API surface when linking with a removed feature. For example:

```csharp
[RequiresFeature("AwesomeFeature")]
public static bool DoSomethingCool {
    Console.WriteLine("Cool things are happening!");
}
```

This way, if linking with `--feature AwesomeFeature false`, the linker would produce an error when it encounters the above call to `DoSomethingCool` in `AwesomeApplication`. The linker's constant branch eliminiation would still allow calling this API under a feature check (and there would be no problem with calling it under a method that itself has the same `RequiresFeatureAttribute`):

```csharp
class AwesomeApplication
{
    public static void Main() {
        if (AwesomeFeature.IsSupported) {
            AwesomeFeature.DoSomethingCool();
            DoItAgain();
        } else {
            Console.WriteLine("Falling back to our less cool implementation.");
        }
    }

    [RequiresFeature("AwesomeFeature")]
    public static void DoItAgain()
    {
        AwesomeFeature.DoSomethingCool();
    }
}
```

A Roslyn analyzer could also be used to check that calls to a method with `RequiresFeature` are only made within a context that already has `RequiresFeature`, or guarded by a call to the corresponding `FeatureDefinition` property.

## Alternatives

If we are willing to revisit the design of feature switches, we might consider other ways to represent the features in attributes. Similar proposals have been made in the past:
- [Add Link-time framework feature removal spec](https://github.com/dotnet/designs/pull/42)

  This proposes a similar attribute which stubs out method bodies to return the `default` value of the return type.
- [First draft of an analyzer for capability APIs](https://github.com/dotnet/designs/pull/111)

  This proposes a way to represent capabalities using attributes.

### Typed feature definitions

Instead of using strings to represent features, we could use attributes (like the [capability API](https://github.com/dotnet/designs/pull/111) draft). There could be an abstract base `FeatureDefinition` attribute, whose subclasses represent specific features. For example, the above could be rewritten as:

```csharp
class AwesomeFeatureAttribute : FeatureDefinitionAttribute { }

class AwesomeFeature
{
    [AwesomeFeature]
    static bool IsSupported { get; } = true;

    [RequiresFeature(typeof(AwesomeFeatureAttribute))]
    public static bool DoSomethingCool() {
        Console.WriteLine("Cool things are happening!");
    }
}
```

```csharp
class AwesomeApplication
{
    public static void Main() {
        if (AwesomeFeature.IsSupported) {
            AwesomeFeature.DoSomethingCool();
        } else {
            Console.WriteLine("Falling back to our less cool implementation.");
        }
    }
}
```

In this case, the name of the feature passed to the linker would need to be determined. We could make it the fully-qualified attribute name (minus "Attribute"), for example `--feature AwesomeFeature false`. Alternatively, it could be supplied as an attribute argument.

A variation of this approach suggested by the the [capability API](https://github.com/dotnet/designs/pull/111) draft is to tie the feature property to the feature definition attribute with another attribute.

### Feature definitions without a configurable name

Another alternative is to enforce that the feature name must match the property name. The above would become:

```csharp
class AwesomeFeature
{
    [FeatureDefinition]
    static bool IsSupported { get; } = true;

    [RequiresFeature("AwesomeFeature.IsSupported")]
    public static bool DoSomethingCool() {
        Console.WriteLine("Cool things are happening!");
    }
}
```

### Feature conditions for attribute removal

The linker has another XML file which can be used to remove attribute instances of a particular attribute type. We could implement this using attributes instead. For example, we could conditionally eliminate `NullableAttribute` instances when the feature "RuntimeNullableAttributes" is "false". We could reuse `RequiresFeature` for this, but it is probably better to use a new attribute since this has different semantics (the linker would silently remove attribute instances, without emitting an error like it does for callsites to `RequiresFeature` methods).

```csharp
[AttributeInstancesRequireFeature("RuntimeNullableAttributes")]
class NullableAttribute : Attribute
{
    // ...
}
```

```csharp
class AwesomeClass
{
    [return:Nullable(2)] // emitted by the compiler
    public string? NullableReturnMethod() {}
}
```

If linking with `--feature RuntimeNullableAttributes false`, this would remove the `Nullable` attribute instance from `NullableReturnMethod`.

## Other considerations

Sometimes we compile the same classes into multiple framework assemblies. If such classes contain feature definitions, the `IsSupported` method (whose fullyqualified name is typically the same as the feature name) is defined in multiple assemblies.

These features are currently treated as the same feature - if a feature setting with that name is passed to the linker, any assemblies which contain a matching feature definition will be modified. With a type-based approach, the assembly name could becomes part of the feature's identity, unless we have some other mapping from a feature name string.
