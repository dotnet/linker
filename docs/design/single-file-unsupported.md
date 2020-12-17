# Single-file unsupported attribute

Starting with .NET Core 3.0 users have the ability to bundle all files used by their applications into a single executable through the publish functionality, `PublishSingleFile`. Although most applications can be agnostic of whether they will be deployed as single-file, there exist some APIs that are known to be incompatible with this deployment mode and thus can cause single-file apps to crash.

While the existing single-file analyzer can help us emit warnings whenever incompatible runtime library APIs are called from within user's code in a project that has been marked with the intent of being published as single-file (through setting `PublishSingleFile` to true in its .csproj), developers can still take a dependency on third-party components implementing functionality that inadvertently make their application no longer compatible with single-file.

## User Experience

The new attribute will allow developers to easily annotate their APIs known to be problematic when used in the context of a single-file application. This new attribute will first be used by the runtime team to annotate low level [APIs known to be incompatible with single-file](https://docs.microsoft.com/en-us/dotnet/core/deploying/single-file#api-incompatibility). Library authors will then rely on analyzers to know which parts of their code would either need to be fixed or marked with `SingleFileUnsupportedAttribute`.

### Example: API writer

Ben is writing a library which he reckons some of its consumers might want to use in the context of a single-file app, so he decides to see if any warning is produced when `PublishSingleFile` is set in the .csproj file. After doing this, the following code gets a diagnostic produced by the single-file analyzer: 

> FileTable.cs(3,12): Single-file warning ILXXXX: 'System.Reflection.Assembly.GetFiles()' will throw for assemblies embedded in a single-file app.

```C#
public int FileTableLength(Assembly assembly)
{
    return assembly.GetFiles().Length;
}
```

He then decides to annotate the method so that users who intent to deploy their app as a single-file binary know that `FileTableLength` will not be safe to use, and can either wrap the  method call in a try-catch block or work around using it.

```C#
[SingleFileUnsupported("This method will throw for single-file apps.", "https://helpurl")]
public int FileTableLength(Assembly assembly)
```

### Example: API consumer

Maria is writing an application which keeps track of all times it has ran in a log next to the running assembly. For this purpose she uses `Assembly.Location`, which is marked with the new `SingleFileUnsupportedAttribute`.

```C#
void RecordApplicationRun()
{
    var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase), "runs.log");
    using (StreamWriter sw = File.AppendText(path))
        sw.WriteLine(DateTime.Now.ToString());
}
```

After some time she decides to make her application a single-file app, so that she can easily share it with friends. She then adds the `PublishSingleFile` property to the .csproj.

```XML
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net5.0</TargetFramework>
    <PublishSingleFile>true</PublishSingleFile>
  </PropertyGroup>

</Project>
```

Now that she has expressed the intent to deploy the app as single-file binary, the following diagnostic is shown in her code:

> Logger.cs(x,y): Single-file warning ILXXXX: Logger.RecordApplicationRun(): 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.

She now has the option to either suppress the warning or work around the highlighted method.

### Detecting incompatible APIs

Annotated public APIs that are directly called by the user's code will trigger the single-file analyzer to produce a diagnostic visible both in console and IDE. However, Roslyn analyzers have limited visibility of external code and cannot see methods which are indirectly called. For these cases a global analyzer is needed in order to report the diagnostics, this analyzer must be capable of seeing the entirety of the code used by the application. 

### Suppressing the warning

For a method causing a diagnostic to be produced, the user can choose between following the recommended guidance given by the diagnostic message to fix the problem or suppress the warning via the known warning suppression mechanisms.

To give an example, if a hypothetical consumer of Ben's library (shown [above](#example-api-writer)) decides to use the `FileTableLength` method even when publishing as single-file, the method call could be put inside a try-catch and the warning suppressed:

```C#
[UnconditionalSuppressMessage("ILXXXX")]
void Method(Assembly assembly)
{
    try
    {
        Ben.FileTableLength(assembly);
    }
    catch
    {
        GetTheFileTableLengthSomeOtherWay(assembly);
    }
}
```

The single-file analyzer as well as the global analyzer will exercise implicit suppression of all single-file related warnings which are produced by annotated methods called within an incompatible method.

```C#
// Program.cs(3,4): Single-file warning ILXXXX: This method cannot be used in the context of a single-file application.
void A()
{
    B();
}

// No diagnostic will be produced here, since the caller already produced a single-file diagnostic.
[SingleFileUnsupported("This method cannot be used in the context of a single-file application.")]
void B()
{
    C();
}

[SingleFileUnsupported("This method cannot be used in the context of a single-file application.")]
void C() { }
```



## Goal

By adding this attribute, we expect to improve users experience by giving developers a way to annotate their single-file incompatible APIs such that consumers get useful diagnostics when using them. Developers who build applications meant to be published as single-file binaries should not have to wait until running their program to find out that they are using APIs that might cause their apps to break.

## Design

```C#
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    public sealed class SingleFileUnsupportedAttribute : Attribute
    {
        public SingleFileUnsupportedAttribute(string message)
        {
            Message = message;
        }

        public string Message { get; }

        public string Url { get; set; }
    }
}
```

## Q & A
---

### How are library authors going to know where to add the attribute?

Initially, the runtime team will annotate existing APIs known to be incompatible with single-file. Library developers will have to use analyzers to get diagnostics in their code pinpointing all method calls that are problematic when used with single-file.

### Which tool is responsible for emiting the warnings related with the attribute?

The single-file analyzer as well as a global analyzer.

### Is suppression the only way to mitigate the warning?

Currently, yes. In the future, a new API can be introduced to guard the calls to methods marked as incompatible with single-file.