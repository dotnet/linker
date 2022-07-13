# Redundant Warning Suppression Detection

Dynamic reflection patterns pose a serious challenge to the linker trimming capabilities. The tool is is able to infer simple reflection patterns; but there still cases for which the tool will not be able to reason. When the linker fails to recognize a certain pattern, a warning appears informing the user that the trimming process may break the functionality of the solution.

There are cases where we are confident about safety of a given pattern, but the linker is unable to reason about it and still produces a warning. We may use warning suppression to silence the warning. An example of such pattern may be listing all properties of an object using reflection mechanism.
```csharp
    [UnconditionalSuppressMessage("trim", "IL2072", Justification = "It's OK to print only the properties which were actually used.")]

    void PrintProperties(object instance)
    {
        foreach (var p in instance.GetType().GetProperties())
        {
            PrintPropertyValue(p, instance);
        }
    }

```

## Redundant warnings
The warning suppression may present a threat to the software development lifecycle. Let us again consider the above example of listing all properties on an object instance. 

```csharp
    [UnconditionalSuppressMessage("trim", "IL2072", Justification = "It's OK to print only the properties which were actually used.")]

    void PrintProperties(object instance)
    {
        foreach (var p in instance.GetType().GetProperties())
        {
            PrintPropertyValue(p, instance);
        }
    }

```

We can rewrite the code in such a way, that the linker is able to statically reason about it. Then, the warning is no longer issued and the suppression becomes redundant. We should remove it.

```csharp
    [UnconditionalSuppressMessage("trim", "IL2072", Justification = "It's OK to print only the properties which were actually used.")]

    void PrintProperties<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T instance)
    {
        foreach (var p in typeof(T).GetProperties())
        {
            PrintPropertyValue(p, instance);
        }
    }

```

If we keep the warning suppression on this trimmer-safe code, we will end up with a potentially dangerous case. Should we later add some trimmer-unsafe code within the scope of the suppression, we will not be informed about it during the trimming process. That is, the warning issued by the linker will be silenced by the suppression we left over and it will not be displayed. This may result in a scenario, in which the trimming completes with no warnings, yet errors occur in a runtime. 


## Detecting redundant warning suppressions

In order to avoid the above scenario, we would like to have a option of detecting and reporting the warning suppressions which are not tied to any trimmer-unsafe patterns.

This may be achieved by extending the linker tool functionality to check which suppression do in fact suppress warnings and reporting those which do not.

The said functionality can be implemented as an optional feature, triggered by passing a `
--check-suppressions` command line argument to the linker. Running the tool with this option will report all of the warning suppressions not tied to any trimmer-unsafe code as warnings.

### Example:
Let us again consider the example of the trimmer-safe code with a redundant warning suppression. 

```csharp
    [UnconditionalSuppressMessage("trim", "IL2072", Justification = "It's OK to print only the properties which were actually used.")]

    void PrintProperties<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T instance)
    {
        foreach (var p in typeof(T).GetProperties())
        {
            PrintPropertyValue(p, instance);
        }
    }

```

In order to detect the warning suppression not tied to any trimmer-unsafe pattern we should run the `dotnet publish` command and pass the `--check-suppressions` option.
```shell
  dotnet publish -r win-x64 -p:PublishTrimmed=True p:_ExtraTrimmerArgs="--check-suppressions"
```

The warning should be reported in the output of the command.

```
Trim analysis warning IL2021: Program.PrintProperties<T>(T): Unused UnconditionalSuppressMessageAttribute found. Consider removing the unused warning suppression.
```

## Other solutions

The proposed solution operates by extending the functionality of the linker tool. On one hand, this allows for reusing the existing components leading to a simple implementation, on the other hand this may lead to potential problems. The linker sees only a part of code which is actually used, that means that the solution would not be able to identify the warning suppressions on the code which is trimmed away. Also, the dependencies identified by the linker may be different depending on the environment the tool is run in. Hence, the proposed solution may report different redundant suppressions on different environments.

Alternatively, we could make the analyzer do the unused warning suppressions detection. In this case, we would always process the entire codebase, not only the part visited by the linker. The reported redundant suppressions should then be complete and environment independent. Also, an advantage of such solution would be a shorter feedback loop; we would learn about the redundant suppressions way before we run the publish command. The drawback of this approach is the added complexity. We would not be able to reuse the existing components. Also, the analyzer has a different view of the code than the linker. We may not be able to identify the same set of trimmer-unsafe patterns using analyzer as we do using the linker.