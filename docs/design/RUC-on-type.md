# RequiresUnreferencedCode on Types
`RequiresUnreferencedCode` is a feature for providing and suppressing diagnostics related to trimming. In general, when accessing a member attributed with `RequiresUnreferencedCode`, a warning should be produced at the access location that the member is not compatible with trimming. In addition, when a member is annotated with `RequiresUnreferencedCode`, trimming compatibility warnings will be suppressed in the body of that member.

In the case in which `RequiresUnreferencedCode` is present on a class declaration, the following members behave as if they were annotated directly with the attribute:
- Static methods
- Constructors
- Static properties
- Static events

Members which are not affected are:
- Nested types
- Instance members

The following sections will help to illustrate the behavior of having `RequiresUnreferencedCode` on a type for different scenarios
## RequiresUnreferencedCode on a type warns on instance constructor or static method access
Having `RequiresUnreferencedCode` on the type implies that any instance constructor or static method is considered dangerous for trimming. Following are examples of this behavior:

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

    classWithRequiresUnreferencedCode.NonStaticMethod();
}
```
With the previous example we can see that calling the static method or generating a new instance of the type will result in generating a warning. Also, in the case of the instance method `NonStaticMethod()` we don't generate a warning since we consider that in order to call this method an instance of the type needs to be generated first causing the constructor to warn with IL2026.

## RequiresUnreferencedCode on a type suppressing other warnings
The behavior of `RequiresUnreferencedCode` suppressions on a method is that it will suppress the warnings inside the method body and only generate a single IL2026 warning instead.
```C#
[RequiresUnreferencedCode ("This method calls dangerous code")]
public static void MethodWithDangerousCode () 
{
    // Dangerous code that generates a trimming warning

static void TestSuppressionOnMethod ()
{
    // IL2026: TestSuppressionOnMethod(): Using method 'MethodWithDangerousCode()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. This method calls dangerous code.
    MethodWithDangerousCode()
}
```

Similarly, `RequiresUnreferencedCode` on a type suppresses other warnings, with the difference that it will silence warnings from all methods in the type and attributes.

```C#
[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
class ClassWithRequiresUnreferencedCode
{
    public static void MethodWithDangerousCode ()
    { 
        // Dangerous code that generates a trimming warning
    }
}

static void TestSuppressionOnType ()
{
    // IL2026: TestSuppressionOnMethod(): Using method 'ClassWithRequiresUnreferencedCode.MethodWithDangerousCode()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --ClassWithRequiresUnreferencedCode--.
    ClassWithRequiresUnreferencedCode.MethodWithDangerousCode()
}
```

## The behavior of Nested Classes
We decided to exclude the auto propagation of the attribute to nested classes, this means that a nested class is not considered to have `RequiresUnreferencedCode` if inside a type marked with `RequiresUnreferencedCode`

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
In the previous example calling `ClassWithRequiresUnreferencedCode.StaticMethod` is considered dangerous since the declaring type of the static method is annotated with `RequiresUnreferencedCode`. For `ClassWithRequiresUnreferencedCode.NestedClass.StaticMethod` the declared type `NestedClass` is not considered dangerous since it doesn't contain the `RequiresUnreferencedCode` on it nor the method contains the attribute.

If we want `ClassWithRequiresUnreferencedCode.NestedClass.StaticMethod` to generate the warning we would need to annotate either the `NestedClass` type or the `ClassWithRequiresUnreferencedCode.NestedClass.StaticMethod` static method.

Also, `RequiresUnreferencedCode` on type does not suppress the warnings inside a nested type in any way.

```C#
[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
class ClassWithRequiresUnreferencedCode
{
    class NestedClass {
        // ILXXXX: The method will generate a warning that will not be suppressed
        public static void MethodWithDangerousCode ()
        { 
            // Dangerous code that generates a trimming warning
        }
    }
}

static void TestSuppressionOnType ()
{
    ClassWithRequiresUnreferencedCode.NestedClass.MethodWithDangerousCode()
}
```

## Derived types
Although we don't auto propagate the `RequiresUnreferencedCode` attribute to the derived type a warning is generated to warn the user that calling methods in the derived type might end up calling something dangerous in the base class. Therefore, is recommended to also mark the derived type with `RequiresUnreferencedCode`. The warning will come from the base instance constructor since is being called when we try to derive from it.

```C#
[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
class ClassWithRequiresUnreferencedCode
{
    public static void StaticMethod () { }
}

class DerivedWithoutRequires : ClassWithRequiresUnreferencedCode
{
    public static void StaticMethodInInheritedClass () { }
}

// IL2026: TestRequiresOnBaseButNotOnDerived(): Using method 'ClassWithRequiresUnreferencedCode.StaticMethod()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --ClassWithRequiresUnreferencedCode--.
static void TestRequiresOnBaseButNotOnDerived ()
{
    DerivedWithoutRequires.StaticMethodInInheritedClass ();
    DerivedWithoutRequires.StaticMethod ();
}
```
As the previous example illustrates the DerivedType without `RequiresUnreferencedCode` will generate a warning only for deriving from `ClassWithRequiresUnreferencedCode`. Additionally, by calling some of the methods we can see the difference on behavior on methods declared on the derived type vs methods on the base type. Calling a method in the derived type will not generate a warning since the derived type is not annotated, while calling a method for which the base type will be called will generate IL2026.

## Behavior using virtual methods
Although we threat virtual methods as just another method, meaning that the same rules applying for other methods will also apply here (constructors and static methods will warn). There are some special behaviors of having `RequiresUnreferencedCode` on a type that later on gets override, and that is the behavior of mismatching annotations. 
In the scenario when there is a mismatch in annotations between the base and derived members there is a warning generated asking for consistency in the attribute in the base and the derived member. See the following example:
```C#
public class Base
{
  [RequiresUnreferencedCode("Message")]
  public virtual void TestMethod() {}
}

public class Derived : Base
{
  // IL2046: Base member 'Base.TestMethod' with 'RequiresUnreferencedCodeAttribute' has a derived member 'Derived.TestMethod()' without 'RequiresUnreferencedCodeAttribute'. For all interfaces and overrides the implementation attribute must match the definition attribute.
  public override void TestMethod() {}
}
```
In this case if the derived type gets annotated with `RequiresUnreferencedCode` the annotation mismatch warning will dissapear since the `TestMethod` would now be considered as annotated with `RequiresUnreferencedCode`

```C#
public class Base
{
  [RequiresUnreferencedCode("Message")]
  public virtual void TestMethod() {}
}

[RequiresUnreferencedCode("Message")]
public class Derived : Base
{
  public override void TestMethod() {}
}
```

This same behavior applies for implementing an interface
```C#
interface IRUC
{
  [RequiresUnreferencedCode("Message")]
  void TestMethod();
}

[RequiresUnreferencedCode("Message")]
class Implementation : IRUC
{
  public void TestMethod () { }
}
```
Notice that you cannot do the following
```C#
[RequiresUnreferencedCode("Message")]
interface IRUC
{
  void TestMethod();
}


class Implementation : IRUC
{
  [RequiresUnreferencedCode("Message")]
  public void TestMethod () { }
}
```
Since `RequiresUnreferencedCode` cannot be placed in interfaces
## Static interface methods

```C#
// IL2026: Invoker<Foo>.Invoke(): Using method 'Foo.StaticMethod()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --ClassWithRequiresUnreferencedCode--.
Invoker<Foo>.Invoke();

interface IStatic
{
    static abstract void StaticMethod();
}

[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
class Foo : IStatic
{
    static void IStatic.StaticMethod() { /* Do dangerous things */ }
}

class Invoker<T> where T: IStatic
{
    public static void Invoke() => T.StaticMethod();
}
```

## Behavior of RequiresUnreferencedCode on type while used in generic instantiation
The behavior of the `RequiresUnreferencedCode` on a type when used in generics is defined on the new() constraint which lets the generic type call the constructor of the type passed.
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
For scenarios in GenericClass1 and GenericClass2 the new constraint is present which means that the constructor of the `ClassWithRequiresUnreferencedCode` will be called and a warning with IL2026 will be produced. In the GenericClass3 the new constraint is not present therefore the warning will not be produced.

### MakeGenericType and MakeGenericMethod
MakeGenericType and MakeGenericMethod are both functions that help to substitute the elements of an array of types for the type parameters of the current generic type definition or generic method definition. The end result is a Type/Method object representing the resulting constructed type/method. See the following examples
```C#
private class GenericClass<T> where T : new ()
{ }

[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
public class ClassWithRequiresUnreferencedCode {}

public static void Main()
{      
    Type generic = typeof(GenericClass<>);

    Type typeArg = typeof(ClassWithRequiresUnreferencedCode);

    // IL2026: Main(): Using method 'ClassWithRequiresUnreferencedCode.ctor()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --ClassWithRequiresUnreferencedCode--.
    Type constructed = generic.MakeGenericType(typeArg);
}
```
In the previous example we have an open type generic class `GenericClass<T>`, meaning that `T` is an unspecified type argument. This open type gets store in the variable `generic`. Then we store the type `ClassWithRequiresUnreferencedCode` in `typeArg`. Then we execute `generic.MakeGenericType(typeArg)` which will generate a constructed type. A constructed type is an open type if one or more of its type arguments is an open type, in this case `ClassWithRequiresUnreferencedCode` is considered a closed type therefore the value constructed stores type formed by substituting the elements of typeArg on the current generic type.
Printing the value of the `generic` variable would generate `GenericClass<T>` whereas printing the value of the `constructed` variable would generate `GenericClass<ClassWithRequiresUnreferencedCode>`.

For the purposes of `RequiresUnreferencedCode` on a type, the warning will be generated using the same rules as with generic instantiation. If the generic ends up calling the constructor method on a type with `RequiresUnreferencedCode` then a warning will be generated. In previous example since GenericClass<T> has the new constraint it means that will call the constructor and warn

```C#
public class ClassWithOpenGenericMethod
{
    public static void Generic<T>(T toDisplay){}
}

[RequiresUnreferencedCode ("Message for --ClassWithRequiresUnreferencedCode--")]
public class ClassWithRequiresUnreferencedCode {}

public static void Main()
{
    Type example = typeof(ClassWithOpenGenericMethod);
    MethodInfo mi = example.GetMethod("Generic");
    
    // IL2026: Main(): Using method 'ClassWithRequiresUnreferencedCode.ctor()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --ClassWithRequiresUnreferencedCode--.
    MethodInfo miConstructed = mi.MakeGenericMethod(typeof(ClassWithRequiresUnreferencedCode));
    miConstructed.Invoke(null, null);
}
```
This is a very similar example as with MakeGenericType, we have an open type generic method `Generic<T>` then we have a call to MakeGenericMethod in which we pass a closed type to generate a constructed type called `miConstructed` that has the substitution of the elements. Printing `mi` variable would generate `Generic[T](T)` whereas printing the value of the `miConstructed` variable would generate `Generic[ClassWithRequiresUnreferencedCode](ClassWithRequiresUnreferencedCode)`.

Similar to the previous example, if the trimmer detects that the behavior is dangerous for trimming a warning will be issued.
## Structs and interfaces
We have decided not to allow `RequiresUnreferencedCode` on structs and interfaces because the semantics are hard to guard - both structs and interfaces can be "instantiated" without running an explicit constructor and at that point all instance methods on them are callable, even indirectly through virtual dispatch.

## Static constructors and static fields
`RequiresUnreferencedCode` is an annotation that allows you to tell callers which piece of code is safe/unsafe, static constructors are not callable method from the user perspective, the runtime will issue calls to the static constructor as a type initialization method, but from the user perspective the method is not callable and therefore we don't allow to annotate with `RequiresUnreferencedCode` the static constructor method. In case you annotate the static constructor directly a warning will be generated and the annotation will have no effect on the method (will not generate IL2026 nor suppress other warnings).
```C#
public class MyClass {
    // IL2116: 'RequiresUnreferencedCodeAttribute' cannot be placed directly on static constructor 'MyClass..cctor()', consider placing 'RequiresUnreferencedCodeAttribute' on the type declaration instead.
    [RequiresUnreferencedCode ("Static constructor with RequiresUnreferencedCode")]
    public static MyClass {
        // Does something dangerous
    }
}
```
In case of being in this scenario and want to present warnings to callers the solution would be to add `RequiresUnreferencedCode` on the type, which will threat the static constructor method as dangerous. This way we also guarantee that all implicit calls to the static constructor are seen by the trimming tool.
```C#
[RequiresUnreferencedCode ("Message for --MyClass--")]
public class MyClass {
    public static MyClass {
        // Does something dangerous
    }
}
```
Static constructors are considered special since most of the calls of these methods will be from the runtime environment, understanding the semantics of when and what triggers execution of such type initialization methods is something that most of the developers are not aware. Most common uses of the static constructor will come from the usage of the static field initializers, see the following example
```C#
class C
{
    static readonly int A = CallRUCAnnotatedMethod();
    static readonly int B = 42;
    [RequiresUnreferencedCode ("Dangerous Method")]
    static int CallRUCAnnotatedMethod() => B;

    static void Main()
    {
        Console.WriteLine(A); // 0
    }
}
```
The key part in this example is that since the static fields are being assigned a value, the runtime will generate an implicit static constructor method to execute the initialization. The code is not visible to the user but gets produced in what is called Intermediate Language (IL), the static constructor IL code generated for the previous example is the following
```IL
.method private hidebysig specialname rtspecialname static 
    void .cctor () cil managed 
{
    // Method begins at RVA 0x206e
    // Code size 18 (0x12)
    .maxstack 8

    IL_0000: call int32 C::CallRUCAnnotatedMethod()
    IL_0005: stsfld int32 C::A
    IL_000a: ldc.i4.s 42
    IL_000c: stsfld int32 C::B
    IL_0011: ret
} // end of method C::.cctor
```
The static constructor gets executed in a particular way by first trying to initilize the value of A which then calls to `CallRUCAnnotatedMethod`, we get the unitialized value of B (the default value for an int is 0) and assign it to A. Then we proceed to initialize B with the value 42. Meaning that in the `Console.WriteLine(A);` we will print the value of 0. Also, since `CallRUCAnnotatedMethod` has `RequiresUnreferecedCode` attribute but the method is called from inside the static constructor method the origin of the warning will be the static constructor. For more information about static constructor semantics and beforefieldinit initialization read the CLI specification (ECMA 335), partition I section 8.9.5

In this case the recommended action for this is to analyze if the CallRUCAnnotatedMethod() represents an actual threat for trimming the application. If it's safe to call the method you can generate a explicit static constructor method and suppress the warning in the explicit static constructor or if its actually trimming dangerous annotate the class with `RequiresUnreferencedCode`
```C#
[RequiresUnreferencedCode ("Message for --C--")]
class C
{
    static readonly int A = CallRUCAnnotatedMethod();
    static readonly int B = 42;
    static int CallRUCAnnotatedMethod() => B;

    static void Main()
    {
        Console.WriteLine(A); // 0
    }
}
```
Having `RequiresUnreferencedCode` on the type will make that accessing the type fields from another part of the code will also generate a warning when a static constructor is called.
```C#
[RequiresUnreferencedCode ("Message for --C--")]
class C
{
    public static readonly int A = CallRUCAnnotatedMethod();
    static int CallRUCAnnotatedMethod() => // Does something dangerous;
}
class Program
{
    static void Main()
    {
        // IL2026: Main(): Using method 'C.cctor()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --C--.
        var result = C.A;
    }
}
```
Or
```C#
[RequiresUnreferencedCode ("Message for --C--")]
class C
{
    public static readonly int A;
}
class Program
{
    static void Main()
    {
        // IL2026: Main(): Using method 'C.cctor()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message for --C--.
        var result = C.A;
    }
}
```