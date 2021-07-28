# RequiresUnreferencedCode on Types
The RequiresUnreferencedCode being allowed on types its a feature that allows developers to quickly mark entire classes as dangerous for trimming. instead of having to mark every method in the class. This document will go over whats the behavior of certain scenarios when the RequiresUnreferencedCode attribute is placed on a type

## RequiresUnreferencedCode on a type warns on instance constructor or static method access
Having RequiresUnreferencedCode on the type implies that any instance constructor or static method is considered dangerous for trimming. Following are examples of this behavior:

```C#
[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
class ClassWithRequiresUnreferencedCode
{
    public static void StaticMethod () { }

    public void NonStaticMethod () { }
}

static void TestRequiresUnreferencedCodeInClass ()
{
    // IL2026: TestRequiresUnreferencedCodeInClass(): Using method 'ClassWithRequiresUnreferencedCode.StaticMethod()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --ClassWithRequiresUnreferencedCode--.
    ClassWithRequiresUnreferencedCode.StaticMethod ();
    // IL2026: TestRequiresUnreferencedCodeInClass(): Using method 'ClassWithRequiresUnreferencedCode.ClassWithRequiresUnreferencedCode()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --ClassWithRequiresUnreferencedCode--.
    var classWithRequiresUnreferencedCode = new ClassWithRequiresUnreferencedCode ();

    ClassWithRequiresUnreferencedCode.NonStaticMethod();
}
```
With the previous example we can see that calling the static method or generating a new instance of the type will result in generating a warning. Also, in the case of the instance method 'NonStaticMethod()' we dont generate a warning since we consider that in order to call this method an instance of the type needs to be generated first causing the constructor to warn with IL2026.

## RequiresUnreferencedCode on a type suppresing other warnings
The behavior of RequiresUnreferencedCode suppressions on a method is that it will suppress the warnings inside the method body and only generate a single IL2026 warning instead.
```C#
[RequiresUnreferencedCode ("This method calls dangeours code")]
public static void MethodWithDangerousCode () 
{
    // Dangerous code that generetares a trimming warning

static void TestSuppressionOnMethod ()
{
    // IL2026: TestSuppressionOnMethod(): Using method 'MethodWithDangerousCode()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. This method calls dangeours code.
    MethodWithDangerousCode()
}
```

Similarly RequiresUnreferencedCode on a type suppresses other warnings, with the difference that it will silence warnings from all methods in the type and attributes (except maybe static constructors, pending discussion)

```C#
[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
class ClassWithRequiresUnreferencedCode
{
    public static void MethodWithDangerousCode ()
    { 
        // Dangerous code that generetares a trimming warning
    }
}

static void TestSuppressionOnType ()
{
    // IL2026: TestSuppressionOnMethod(): Using method 'ClassWithRequiresUnreferencedCode.MethodWithDangerousCode()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --ClassWithRequiresUnreferencedCode--.
    ClassWithRequiresUnreferencedCode.MethodWithDangerousCode()
}
```

## The behavior of Nested Classes
We decided to exclude the auto propagation of the attribute to nested classes, this means that a nested class is not considered to have RequiresUnreferencedCode if inside a type marked with RequiresUnreferencedCode

```C#
[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
class ClassWithRequiresUnreferencedCode
{
    public static void StaticMethod () { }
    class NestedClass {
        public static void StaticMethod () { }
    }
}

static void TestRequiresUnreferencedCodeInClass ()
{
    // IL2026: TestRequiresUnreferencedCodeInClass(): Using method 'ClassWithRequiresUnreferencedCode.StaticMethod()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --ClassWithRequiresUnreferencedCode--.
    ClassWithRequiresUnreferencedCode.StaticMethod ();

    ClassWithRequiresUnreferencedCode.NestedClass.StaticMethod ();
}

```
In the previous example calling ClassWithRequiresUnreferencedCode.StaticMethod () is considered dangerous since the declaring type of the static method is annotated with RequiresUnreferencedCode. For ClassWithRequiresUnreferencedCode.NestedClass.StaticMethod () the declared type NestedClass is not considered dangerous since it doesn't contain the RequiresUnreferencedCode on it nor the method contains the attribute.

If we want ClassWithRequiresUnreferencedCode.NestedClass.StaticMethod () to generate the warning we would need to annotate either the NestedClass type or the ClassWithRequiresUnreferencedCode.NestedClass.StaticMethod static method.

Also, RequiresUnreferencedCode on type does not suppress the warnings inside a nested type in any way.

```C#
[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
class ClassWithRequiresUnreferencedCode
{
    class NestedClass {
        // ILXXXX: The method will generate a warning that will not be suppressed
        public static void MethodWithDangerousCode ()
        { 
            // Dangerous code that generetares a trimming warning
        }
    }
}

static void TestSuppressionOnType ()
{
    ClassWithRequiresUnreferencedCode.NestedClass.MethodWithDangerousCode()
}
```

## Derived types
There is a special handling for types deriving from a base type which is annotated with RequiresUnreferencedCode. Although we dont auto propagate the RequiresUnreferencedCode attribute to the derived type a warning is generated in the derived type declaration to warn the user that calling methods in the derived type might end up calling something dangerous at it's base. Therefore, is recomendable to also mark the derived type with RequiresUnreferencedCode.

```C#
[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
class ClassWithRequiresUnreferencedCode
{
    public static void StaticMethod () { }
}

// IL2109: Type 'DerivedWithoutRequires' derives from 'ClassWithRequiresUnreferencedCode' which has 'RequiresUnreferencedCodeAttribute'.  Message for --ClassWithRequiresUnreferencedCode--.
class DerivedWithoutRequires : ClassWithRequiresUnreferencedCode
{
    public static void StaticMethodInInheritedClass () { }
}

// TestRequiresOnBaseButNotOnDerived(): Using method 'ClassWithRequiresUnreferencedCode.StaticMethod()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --ClassWithRequiresUnreferencedCode--.
static void TestRequiresOnBaseButNotOnDerived ()
{
    DerivedWithoutRequires.StaticMethodInInheritedClass ();
    DerivedWithoutRequires.StaticMethod ();
}
```
As the previous example ilustrates the DerivedType without RequiresUnreferencedCode will generate a warning only for deriving from ClassWithRequiresUnreferencedCode. Additionally, by calling some of the methods we can see the difference on behavior on methods declared on the derived type vs methods on the base type. Calling a method in the derived type will not generate a warning since the derived type is not annotated, while calling a method for which the base type will be called will generate IL2026.

Arguably we could say that the warning in the type declaration is not necessary and that we could rely on the warning being generated in the constructor instead, but this could lead to some behaviors in which we can call a method without ever using the constructor.

```C#
var t = Type.GetType("Derived").GetMethod("SomeMethod").Invoke(null, null);

[RequiresUnreferencedCode("This base type is dangerous")]
class Base {}

class Derived : Base 
{
    static void SomeMethod() {}
}
```
Therefore, we decided that generating a warning in type declaration even if there is no apparent use it's the correct behavior.

## Behavior of RequiresUnreferencedCode on type while used in generic instantiation
The behavior of the RequiresUnreferencedCode on a type when used in generics is defined on the new() constrain. Which will call the constructor of the type passed.
```C#
[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
class ClassWithRequiresUnreferencedCode { }

private class GenericClass1<T> where T : ClassWithRequiresUnreferencedCode, new() { }
private class GenericClass2<T> where T : new() { }
private class GenericClass3<T> { }

static void TestRequiresUsedAsGeneric
{
    var classGeneric1 = new GenericClass1<ClassWithRequiresUnreferencedCode> ();
    var classGeneric2 = new GenericClass2<ClassWithRequiresUnreferencedCode> ();
    var classGeneric3 = new GenericClass3<ClassWithRequiresUnreferencedCode> ();
}
```
For scenarios in GenericClass1 and GenericClass2 the new constraint is present which means that the constructor of the ClassWithRequiresUnreferencedCode will be called and a warning with IL2026 will be produced. In the GenericClass3 the new constraint is not present therefore the warning will not be produced.

## Structs and interfaces
We have decided to exclude to do any analysis if structs or interfaces are inside of a class marked with RequiresUnreferencedCode, this is to follow the targets of the RequiresUnreferencedCode attribute which at this moment they dont support the attribute on a struct or an interface.

## Static constructors and static fields
So far we have two approaches to deal with static constructors and static fields

### Approach 1
RUC is not allowed on static constructors. 
- The attribute is too dificult to detect when the static constructor is being called. This has the advantage of not having to understand the rules for .cctor invocation nor to detect static field accesses
- Placing RUC on a .cctor would generate a warning saying that is not valid to put RUC on a .cctor
- RUC on types would not silence warnings from static constructor methodbody
- The only options available for a developer would be to silence the warnings (seems not the best approach since we know there is a warning inside the methodbody) or refactor the code in such a way the .cctor no longer has to deal with the warning. But then we must rewrite code when cctor has RUC code
  - how? what are some common patterns and how do we rewrite them?
  - rewriting field to lazy-init property is a breaking change
  - rewriting field to Lazy<T> will still warn from the cctor, won't solve the problem

### Approach 2
Allow RUC on static constructors. Need to detect anything that can trigger cctor and warn if cctor is RUC
- Linker doesn't see explicit calls to static constructor
- Linker must understand all the rules for when cctor is triggered (field access, etc.)
- Is it ok to be conservative (assume every member access triggers cctor), or is that too noisy?
- Similar to Obsolete behavior