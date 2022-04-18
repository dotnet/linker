# Removal Behavior for interfaces

## `unusedinterfaces` optimization

The `unusedinterfaces` optimization controls whether or not classes have an annotation that denotes whether a class implements an interface. When the optimization is off, the linker will not remove the annotations regardless of the visibility of the interface (even private interface implementations will be kept). When the optimization is on and the linker can provably determine that an interface will not be used on a type, the annotation will be removed. In order to know whether it is safe to remove an interface implementation, it is necessary to have a "global" view of the code. In other words, if an assembly is passed without selecting `link` for the `action` option for the linker, all classes that implement interfaces from that assembly will keep those interface implementation annotations. For example, if you implement `System.IFormattable` from the `System.Runtime` assembly, but pass the assembly with `--action copy System.Runtime`, the interface implementation will be kept even if your code doesn't use it.

## Static abstract interface methods

The linker's behavior for methods declared on interfaces as `static abstract` like below are defined in the following cases:

```C#
interface IFoo
{
    static abstract int GetNum();
}

class C : IFoo
{
    static int GetNum() => 1;
}

### Method is accessed on concrete types only

```C#
public class Program 
{
    public static void Main()
    {
        C.GetNum();
    }
}
```

If the interface methods are called only on concrete types, then the implementation method is kept, but the method on the interface type is removed.

### Method is accessed through a constrained type parameter

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

If the interface method is accessed through a constrained type parameter, then the interface method is kept, as well as every implementation on every class. In the example above, `GetNum` will be kept on IFoo, C, and C2.