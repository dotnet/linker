# Linker attributes

As the linker scans the referenced code, it warns about non-understood reflection patterns or other code patterns whose behavior may not be preserved by linking. The following attributes influence the pattern-detection and warning behavior.

## DynamicallyAccessedMembers

Gives extra information about dynamic access patterns which would not otherwise be understood. This can tell the linker to keep additional members that are reflected over, making simple reflection patterns linker-friendly.

TODO: provide examples

The attribute specifies which members of a type are dynamically accessed (e.g. by reflection or interop code). It can be applied to IL locations (method parameters, method return parameters, fields, and properties) that hold a representation of a type, and it indicates via DynamicallyAccessedMemberTypes which members of that type are used dynamically. The linker uses information from these attributes to keep additional members of types, which can make some reflection access patterns linker-safe, eliminating the corresponding warnings.

See [Cross-method annotations](design/reflection-flow.md#Cross-method-annotations) for more information.

## RequiresUnreferencedCode

Specifies that a method requires code which is not statically referenced, preventing analysis of the method, and warning for calls to it.

TODO: provide examples

When annotating linker-unfriendly code, this attribute can be used to prevent multiple warnings originating from reflection patterns in the same method body, instead turning them into a single warning with a message that explains why the method is conceptually linker-unsafe. Calls to methods with this attribute will result in a single warning for the callsite.

## DynamicDependency

Indicates that a member has a dependency on other members. This results in the referenced members being kept whenever the member with the attribute is kept. It does not otherwise influence the analysis behavior.

```csharp
using System;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

class Program
{
    [DynamicDependency("Helper")]
    public static void Main()
    {
        var helper = Assembly.GetExecutingAssembly().GetType("Program").GetMethod("Helper");
        helper.Invoke(null, null);
    }

    public static void Helper()
    {
        Console.WriteLine("Dynamic call to helper");
    }
}
```

Without `DynamicDependency`, linking this assembly would remove `Helper`, causing a failure at runtime. The attribute ensures that `Helper` is kept.

The attribute specifies the members to keep via a `string` or via `DynamicallyAccessedMemberTypes`. The type and assembly are either implicit in the attribute context, or explicitly specified in the attribute (by `Type`, or by `string`s for the type and assembly name).

The type and member strings use the format described at https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format, without the member prefix. The member string should not include the name of the declaring type, and may omit parameters to keep all members of the specified name. Some examples of the format follow:

```csharp
[DynamicDependency("Method()")]
[DynamicDependency("Method(System,Boolean,System.String)")]
[DynamicDependency("MethodOnDifferentType()", typeof(ContainingType))]
[DynamicDependency("MemberName")]
[DynamicDependency("MemberOnUnreferencedAssembly", "ContainingType", "UnreferencedAssembly")]
[DynamicDependency("MemberName", "Namespace.ContainingType.NestedType", "Assembly")]

// generics
[DynamicDependency("GenericMethodName`1")]
[DynamicDependency("GenericMethod``2(``0,``1)")]
[DynamicDependency("MethodWithGenericParameterTypes(System.Collections.Generic.List{System.String})")]
[DynamicDependency("MethodOnGenericType(`0)", "GenericType`1", "UnreferencedAssembly")]
[DynamicDependency("MethodOnGenericType(`0)", typeof(GenericType<>))]
```

This attribute will most often be used in cases where a method contains reflection patterns that can not be analyzed even with the help of DynamicallyAccessedMembers. In these cases, the linker will warn about non-understood patterns in the method body. Applying DynamicDependencyAttribute to the method allows the dynamically referenced members to be kept. The warnings will still show up unless silenced by another mechanism (see UnconditianalSuppressMessage).

## UnconditionalSuppressMessage

Silences linker warnings originating from a method.

TODO: provide examples

In some cases, the linker may warn about unrecognized reflection access patterns which the developer knows to be safe. For example, the accessed members may be referenced elsewhere (such as by DynamicDependency), or they may have safe fallback behavior when the reflection targets are trimmed by the linker. This attribute can be used to suppress messages about such warnings. It can also suppress other linker warnings that are unrelated to the analysis.