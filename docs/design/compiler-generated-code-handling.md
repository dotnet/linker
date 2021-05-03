# Handling of compiler generated code

Modern compilers provide language features which require lot of fancy code generation by the compiler. Not just pure IL generation, but producing new types, methods and fields. An example is `async`/`await` in C# which turns the body of the method into a separate class which implements a state machine.

Lot of the trimming logic relies on attributes authored by the developer. These provide hints to the trimmer especially around ares which are otherwise problematic, like reflection. For example see [reflection-flow](reflection-flow.md) for an example of such attribute.

## Problem

User authored attributes are not propagated to the compiler generated code. For example:

```csharp
[RequiresUnreferencedCode ("--MethodRequiresUnreferencedCode--")]
static void MethodRequiresUnreferencedCode () { }

[UnconditionalSuppressMessage ("IL2026", "")]
static async void TestBeforeAwait ()
{
    MethodRequiresUnreferencedCode ();
    await AsyncMethod ();
}
```

This code should not produce any warning, because the `IL2026` is suppressed via an attribute. But currently this will produce the `IL2026` warning from a different method:

```console
ILLink: Trim analysis warning IL2026: SuppressWarningsInAsyncCode.<TestBeforeAwait>d__1.MoveNext(): Using method 'MethodRequiresUnreferencedCode()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code.
```

Note that the warning comes from a compiler generated method `MoveNext` on class `<TestBeforeAwait>d__1`. The `UnconditionalSuppressMessage` attribute is not propagated and so from a trimmer perspective this is completely unrelated code and thus the warning is not suppressed.

### Method body attributes

The trimmer currently recognizes two attributes which effectively apply to entire method body:

#### `RequiresUnreferencedCodeAttribute`

The `RequiresUnreferencedCodeAttribute` marks the method as incompatible with trimming and at the same time it suppressed trim analysis warnings from the entire method's body. So for example:

```csharp
[RequiresUnreferencedCode ("Incompatible with trimming")]
static IEnumerable<int> TestBeforeIterator ()
{
    MethodRequiresUnreferencedCode ();
    yield return 1;
}
```

Should not produce a warning.

#### `UnconditionalSuppressMessageAttribute`

The `UnconditionalSuppressMessageAttribute` can target lot of scopes, but the smallest one is a method. It can't target specific statements within a method. It is supposed to suppress a specific warning from the method's body, for example:

```csharp
[UnconditionalSuppressMessage("IL2026", "")]
static async void TestAfterAwait ()
{
    await AsyncMethod ();
    MethodRequiresUnreferencedCode ();
}
```

Should not produce a warning.

### Data flow analysis

The trimmer performs data flow analysis within a single method's body, mostly around track the flow of `System.Type` and related instances to be able to detect recursion usage.

#### `DynamicallyAccessedMembersAttribute`

The `DynamicallyAccessedMembersAttribute` annotates values (local variables, method parameters, ...) of type `System.Type` to hint the trimmer that the type will have its methods accessed dynamically (through reflection). Such annotation doesn't propagate currently. For example:

```csharp
static IEnumerable<int> TestParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
{
    type.GetMethod ("BeforeIteratorMethod");
    yield return 1;
    type.GetMethod ("AfterIteratorMethod");
}
```

Should not produce any warnings, because the `type` variable is properly annotated.

#### Intrinsic data flow

The trimmer also intrinsically recognizes certain patterns and perform data flow analysis around them. This allows full analysis of certain reflection usage even without annotations. For example, intrinsic handling of the `typeof` keyword:

```csharp
static IEnumerable<int> TestLocalVariable ()
{
    Type type = typeof (TestType);
    type.GetMethod ("BeforeIteratorMethod");
    yield return 1;
    type.GetMethod ("AfterIteratorMethod");
}
```

## Expected behavior

### A - Closure rewrite

In order to create a lambda method with captured variables, the compiler will generate a closure class which stores the captured values and the lambda method is then generated as a method on that class. Currently compiler doesn't propagate attributes to the generated methods.

#### A1 - `RequiresUnreferencedCode` with lambda

```csharp
[RequiresUnreferencedCode ("--TestLambdaWithCapture--")]
static void TestLambdaWithCapture (int p)
{
    Action a = () => MethodRequiresUnreferencedCode (p);
}
```

Trimmer should suppress trim analysis warnings due to `RequiresUnreferencedCode` even inside the lambda. In C# 10 it will be possible to add an attribute onto the lambda directly. The attribute should be propagated only if it's not already there.
**Open question Q1a**: Should method body attributes propagate to lambdas? Maybe we should rely on C# 10 and explicit attributes only.

#### A2 - `UnconditionalSuppressMessage` with lambda

```csharp
[UnconditionalSuppressMessage ("IL2026", "")]
static void TestLambdaWithCapture (int p)
{
    Action a = () => MethodRequiresUnreferencedCode (p);
}
```

Trimmer should suppress `IL2026` due to the suppression attribute. In C# 10 it will be possible to add an attribute onto the lambda directly. The attribute should be propagated only if it's not already there.
**Open question Q1a**: Should method body attributes propagate to lambdas? Maybe we should rely on C# 10 and explicit attributes only.

#### A3 - Data flow annotations with lambda

```csharp
static void TestParameterInLambda ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
{
    Action a = () => {
        type.GetMethod ("InLambdaMethod");
    };
}
```

Trimmer should be able to flow the annotation from the parameter into the closure for the lambda and thus avoid warning in this case.

#### A4 - Intrinsic data flow with lambda

```csharp
static void TestLocalVariableInLambda ()
{
    Type type = typeof (TestType);
    Action a = () => {
        type.GetMethod ("InLambdaMethod");
    };
}
```

Internal data flow tracking should propagate into lambdas.

#### A5 - `RequiresUnreferencedCode` with local function

```csharp
[RequiresUnreferencedCode ("--TestLocalFunctionWithNoCapture--")]
static void TestLocalFunctionWithNoCapture ()
{
    LocalFunction ();

    void LocalFunction()
    {
        MethodRequiresUnreferencedCode ();
    }
}
```

The trimmer could propagate the `RequiresUnreferencedCode` to the local function. Unless the function already has that attribute present.
**Question Q1b**: Should method body attributes propagate to local functions? It's possible to add the attribute manually to the local function, so maybe we should simply rely on that.

#### A6 - `UnconditionalSuppressMessage` with local function

```csharp
[UnconditionalSuppressMessage ("IL2026", "")]
static void TestLocalFunctionWithNoCapture ()
{
    LocalFunction ();

    void LocalFunction ()
    {
        MethodRequiresUnreferencedCode ();
    }
}
```

Similarly to the A5 case, the trimmer could propagate the warning suppression to the local function. Unless the function already has suppressions.
**Question Q1b**: Should method body attributes propagate to local functions? It's possible to add the attribute manually to the local function, so maybe we should simply rely on that.

#### A7 - Data flow annotations with local function

```csharp
static void TestParameterInLocalFunction ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
{
    LocalFunction ();

    void LocalFunction ()
    {
        type.GetMethod ("InLocalMethod");
    }
}
```

Identical to A3, annotations should propagate into local functions.

#### A8 - Intrinsic data flow with local function

```csharp
static void TestLocalVariableInLocalFunction ()
{
    Type type = typeof (TestType);
    LocalFunction ();

    void LocalFunction ()
    {
        type.GetMethod ("InLocalMethod");
    }
}
```

Identical to A4 - Internal data flow tracking should propagate into local functions.

### B Iterator rewrites

Specifically the C# compiler will rewrite entire method bodies. Iterators which return enumeration and use `yield return` will rewrite entire method body and move it into a separate class. This has similar problems as closures since it effectively behaves a lot like closure, but has additional challenges due to different syntax.

#### B1 - `RequiresUnreferencedCode` with iterator body

```csharp
[RequiresUnreferencedCode ("--TestAfterIterator--")]
static IEnumerable<int> TestAfterIterator ()
{
    yield return 1;
    MethodRequiresUnreferencedCode ();
}
```

The attribute should apply to the entire method body and thus suppress trim analysis warnings. Even if the body is spread by the compiler into different methods.

#### B2 - `UnconditionalSuppressMessage` with iterator body

```csharp
[UnconditionalSuppressMessage ("IL2026", "")]
static IEnumerable<int> TestBeforeIterator ()
{
    MethodRequiresUnreferencedCode ();
    yield return 1;
}
```

The attribute should apply to the entire method body and thus suppress trim analysis warnings. Even if the body is spread by the compiler into different methods.

#### B3 = Data flow annotations in iterator body

```csharp
static IEnumerable<int> TestParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
{
    type.GetMethod ("BeforeIteratorMethod");
    yield return 1;
    type.GetMethod ("AfterIteratorMethod");
}
```

The data flow annotation from method parameter should flow through the entire body.

#### B4 - Intrinsic data flow in iterator body

```csharp
static IEnumerable<int> TestLocalVariable ()
{
    Type type = typeof (TestType);
    type.GetMethod ("BeforeIteratorMethod");
    yield return 1;
    type.GetMethod ("AfterIteratorMethod");
}
```

The data flow annotation from method parameter should flow through the entire body.

### C async rewrites

Similarly to iterators, C# compiler also rewrites method bodies which use `async`/`await`. This has similar problems as closures since it effectively behaves a lot like closure, but has additional challenges due to different syntax.

#### C1 - `RequiresUnreferencedCode` with async body

```csharp
[RequiresUnreferencedCode ("--TestAfterAwait--")]
static async void TestAfterAwait ()
{
    await AsyncMethod ();
    MethodRequiresUnreferencedCode ();
}
```

The attribute should apply to the entire method body and thus suppress trim analysis warnings. Even if the body is spread by the compiler into different methods. Very similar to B1.

#### C2 - `UnconditionalSuppressMessage` with iterator body

```csharp
[UnconditionalSuppressMessage("IL2026", "")]
static async void TestBeforeAwait()
{
    MethodRequiresUnreferencedCode ();
    await AsyncMethod ();
}
```

The attribute should apply to the entire method body and thus suppress trim analysis warnings. Even if the body is spread by the compiler into different methods. Very similar to B2.

#### C3 = Data flow annotations in async body

```csharp
static async void TestParameter ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
{
    type.GetMethod ("BeforeAsyncMethod");
    await AsyncMethod ();
    type.GetMethod ("AfterAsyncMethod");
}
```

The data flow annotation from method parameter should flow through the entire body. Very similar to B3.

#### C4 - Intrinsic data flow in async body

```csharp
static async void TestLocalVariable ()
{
    Type type = typeof  (TestClass);
    type.GetMethod ("BeforeAsyncMethod");
    await AsyncMethod ();
    type.GetMethod ("AfterAsyncMethod");
}
```

The data flow annotation from method parameter should flow through the entire body. Very similar to B4.
