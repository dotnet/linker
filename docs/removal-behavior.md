# Removal Behavior for interfaces

## `unusedinterfaces` optimization

The `unusedinterfaces` optimization controls whether or not classes have an annotation that denotes whether a class implements an interface. When the optimization is off, the linker will not remove the annotations regardless of the visibility of the interface (even private interface implementations will be kept). When the optimization is on and the linker can provably determine that an interface will not be used on a type, the annotation will be removed. In order to know whether it is safe to remove an interface implementation, it is necessary to have a "global" view of the code. In other words, if an assembly is passed without selecting `link` for the `action` option for the linker, all classes that implement interfaces from that assembly will keep those interface implementation annotations. For example, if you implement `System.IFormattable` from the `System.Runtime` assembly, but pass the assembly with `--action copy System.Runtime`, the interface implementation will be kept even if your code doesn't use it.

## Static abstract interface methods

The linker's behavior for methods declared on interfaces as `static abstract` like below are defined in the following cases using the example interface and class below:

```C#
interface IFoo
{
    static abstract int GetNum();
}

class C : IFoo
{
    static int GetNum() => 1;
}
```

### Method call on concrete type

On a direct call to a static method which implements a static interface method, only the body is rooted, not its associated `MethodImpl`. Similarly, the interface method which it implements is not rooted.

Example:

In the following program, `C.GetNum()` would be kept, but `IFoo.GetNum()` would be removed.

```C#
public class Program
{
    public static void Main()
    {
        C.GetNum();
    }
}
```

### Method call on a constrained type parameter

On a call to a static abstract interface method that is accessed through a constrained type parameter, the interface method is rooted, as well as every implementation method on every type.

Example:

In the following program, `C.GetNum()`, `IFoo.GetNum()`, and `C2.GetNum()` are all kept.

```C#
public class C2 : IFoo
{
    static int GetNum() => 2;
}
public class Program
{
    public static void Main()
    {
        M<C>();
    }
    public static void M<T>() where T : IFoo
    {
        T.GetNum();
    }
}
```
