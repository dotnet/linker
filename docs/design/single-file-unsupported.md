# Single-file unsupported attribute

Starting with .NET Core 3.0 users have the ability to bundle all files used by their applications into a single executable through the publish functionality, `PublishSingleFile`. Although most applications can be agnostic of whether they will be deployed as single-file, there exist some APIs that are known to be incompatible with this deployment mode and thus can cause single-file apps to crash.

While the existing single-file analyzer can help us emit warnings whenever incompatible runtime library APIs are called from within user's code in a project that has been marked with the intent of being published as single-file (through setting `PublishSingleFile` to true in its .csproj), developers can still take a dependency on third-party components implementing functionality that inadvertently make their application no longer compatible with single-file.

## User Experience

For library authors, the new attribute will allow them to easily annotate APIs known to be problematic when used in the context of a single-file application.

```C#
[SingleFileUnsupported("'Bar' method is not compatible with single-file", "https://help")]
void Bar()
{
    Assembly executingAssembly = Assembly.GetExecutingAssembly();
    var codeBase = executingAssembly.CodeBase;
    ...
}
```

Consumers of this library who want to publish their application as a single-file binary will commonly do it through setting the corresponding property in the application’s .csproj:

```XML
<PropertyGroup>
    <PublishSingleFile>true</PublishSingleFile>
</PropertyGroup>
```

If the user now makes use of the problematic API, a warning will be produced:

> File.cs(4,4): Single-file warning ILXXXX: Foo.CallBar(): 'Bar' method is not compatible with single-file. Url.
```C#
void CallBar()
{
    ...
    Bar();
    ...
}
```
The linker will exercise implicit suppression of all other single-file related warnings produced by code within the incompatible method.

If a user is confident that the used API does not pose a problem, the known warning suppression mechanisms can be used.

## Goal

By adding this attribute, we expect to improve users experience by giving library authors a way to annotate their single-file incompatible APIs such that consumers get useful diagnostics when using them. Developers who build applications meant to be published as single-file binaries should not have to wait until running their program to find out that they are using incompatible APIs.

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