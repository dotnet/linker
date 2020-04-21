# IL Linker

The linker is a tool one can use to only ship the minimal possible set of
functions that a set of programs might require to run as opposed to the full
libraries.

## How does the linker work?

The linker analyses the intermediate code (CIL) produced by every compiler
targeting the .NET platform like mcs, csc, vbnc, booc or others. It will walk
through all the code that it is given to it, and basically, perform a mark and
sweep operations on all the code that it is referenced, to only keep what is
necessary for the source program to run.

## Usage

### Linking from a source assembly

The command:

`illink -a Program.exe`

will use the assembly Program.exe as a source. That means that the linker will
walk through all the methods of Program.exe to generate only what is necessary
for this assembly to run.

### Linking from an [XML descriptor](#syntax-of-xml-descriptor)

The command:

`illink -x desc.xml`

will use the XML descriptor as a source. That means that the linker will
use this file to decide what to link in a set of assemblies. The format of the
descriptors is described further on in this document.

### Linking from an API info file

The command:

`illink -i assembly.info`

will use a file produced by `mono-api-info` as a source. The linker will use
this file to link only what is necessary to match the public API defined in
the info file.

### Actions on the assemblies

You can specify what the linker should do exactly per assembly.

The linker can do the following things:

- Skip: skip them, and do nothing with them,
- Copy: copy them to the output directory,
- CopyUsed: copy used assemblies to the output directory,
- Link: link them, to reduce their size,
- Delete: remove them from the output,
- Save: save them in memory without linking
- AddBypassNGen: add BypassNGenAttribute to unmarked methods,
- AddBypassNgenUsed: add BypassNGenAttribute to unmarked methods in used assemblies.

You can specify an action per assembly like this:

`illink -p link Foo`

or

`illink -p skip System.Windows.Forms`

Or you can specify what to do for the core assemblies.

Core assemblies are the assemblies that belong to the base class library,
like mscorlib.dll, System.dll or System.Windows.Forms.dll.

You can specify what action to do on the core assemblies with the option:

`-c skip|copy|link`

### The output directory

By default, the linker will create an `output` directory in the current
directory where it will emit the linked files, to avoid erasing source
assemblies. You can specify the output directory with the option:

`-out PATH`

If you specify the directory `.', please ensure that you won't write over
important assemblies of yours.

### Specifying directories where the linker should look for assemblies

By default, the linker will first look for assemblies in the directories `.`
and `bin`. You can specify

Example:

`illink -d ../../libs -a program.exe`


### Using custom substitutions

An option called `--substitutions` allows external customization of any
method or field for assemblies which are linked. The syntax used for that is based on
XML files. Using substitutions with `ipconstprop` optimization (enabled by
default) can help reduce output size as any dependencies under conditional
logic which will be evaluated as unreachable will be removed.

An example of a substitution XML file

```xml
<linker>
  <assembly fullname="test">
    <type fullname="UserCode.Substitutions.Playground">
      <method signature="System.String TestMethod()" body="stub" value="abcd">
      </method>
      <field name="MyNumericField" value="5" initialize="true">
      </field>	    
    </type>
  </assembly>
</linker>
```

The `value` attribute is optional and only required when the method stub should not
return the default value or no-op for `void` like methods.

Addition to `stub` modification also removal of the implementation body is supported by
using `remove` mode the method when the method is marked. This is useful when the conditional logic
cannot be evaluated by the linker and the method will be marked but never actually reached.

A similar mechanism is available for fields where a field can be initialized with a specific
value and override the existing behaviour. The rule can also apply to static fields which
if set to default value without explicit `initialize` setting could help to elide whole
explicit static constructor.

### Adding custom steps to the linker.

You can write custom steps for the linker and tell the linker to use them.
Let's take a simple example:

```csharp
using System;

using Mono.Linker;
using Mono.Linker.Steps;

namespace Foo {

	public class FooStep : IStep {

		public void Process (LinkContext context)
		{
			foreach (IStep step in context.Pipeline.GetSteps ()) {
				Console.WriteLine (step.GetType ().Name);
			}
		}
	}
}
```

That is compiled against the linker to `Foo.dll` assembly.

To tell the linker where this assembly is located, you have to append its full path after a comma which separates the custom step's name from the custom assembly's path:

`--custom-step [custom step],[custom assembly]`

You can now ask the linker to add the custom step at the end of the pipeline:

`illink --custom-step Foo.FooStep,D:\Bar\Foo.dll`

Or you can ask the linker to add it after a specific step:

`illink --custom-step +MarkStep:Foo.FooStep,D:\Bar\Foo.dll -a program.exe`

Or before a specific step:

`illink --custom-step -MarkStep:Foo.FooStep,D:\Bar\Foo.dll -a program.exe`

### Passing data to custom steps

For advanced custom steps which needs interaction with external values (for example for the custom step configuration), there is `--custom-data KEY=VALUE` option. The key
data are stored inside a linker context and can be obtained in the custom step using `context.TryGetCustomData` method. Each key can have a simple value assigned which means
if you need to store multiple values for the same key use custom separator for the values and pass them as one key-value pair.

## MonoLinker specific options

### The i18n Assemblies

Mono has a few assemblies which contains everything region specific:

    I18N.CJK.dll
    I18N.MidEast.dll
    I18N.Other.dll
    I18N.Rare.dll
    I18N.West.dll

By default, they will all be copied to the output directory. But you can
specify which one you want using the command:

`illink -l choice`

Where choice can either be: none, all, cjk, mideast, other, rare or west. You can
combine the values with a comma.

Example:

`illink -a assembly -l mideast,cjk`

## Syntax of XML Descriptor

Here is an example that shows all the possibilities of this format:

```xml
<linker>
	<assembly fullname="Library">
		<type fullname="Foo" />
		<type fullname="Bar" preserve="nothing" required="false" />
		<type fullname="Baz" preserve="fields" required="false" />
		<type fullname="Gazonk">
			<method signature="System.Void .ctor(System.String)" />
			<field signature="System.String _blah" />
		</type>
	</assembly>
</linker>
```

In this example, the linker will link the types Foo, Bar, Baz and Gazonk.

The fullname attribute specifies the fullname of the type in the format
specified by ECMA-335. This is in certain cases not the same
as the one reported by Type.FullName (nested classes e.g.).

The preserve attribute ensures that all the fields of the type Baz will be
always be linked, not matter if they are used or not, but that neither the
fields or the methods of Bar will be linked if they are not used. Not
specifying a preserve attribute implies that we are preserving everything in
the specified type.

The required attribute specifies that if the type is not marked, during the
mark operation, it will not be linked.

The type Gazonk will be linked, as well as its constructor taking a string as a
parameter, and it's _blah field.

You can have multiple assembly nodes.

More comprehensive example is bellow which show more advanced configuration options.

```xml
<linker>
  <!--
  Preserve types and members in an assembly
  -->
  <assembly fullname="Assembly1">
    <!--Preserve an entire type-->
    <type fullname="Assembly1.A" preserve="all"/>
    <!--No "preserve" attribute and no members specified means preserve all members-->
    <type fullname="Assembly1.B"/>
    <!--Preserve all fields on a type-->
    <type fullname="Assembly1.C" preserve="fields"/>
    <!--Preserve all methods on a type-->
    <type fullname="Assembly1.D" preserve="methods"/>
    <!--Preserve the type only-->
    <type fullname="Assembly1.E" preserve="nothing"/>
    <!--Preserving only specific members of a type-->
    <type fullname="Assembly1.F">
      <!--
      Fields
      -->
      <field signature="System.Int32 field1" />
      <!--Preserve a field by name rather than signature-->
      <field name="field2" />
      <!--
      Methods
      -->
      <method signature="System.Void Method1()" />
      <!--Preserve a method with parameters-->
      <method signature="System.Void Method2(System.Int32,System.String)" />
      <!--Preserve a method by name rather than signature-->
      <method name="Method3" />
      <!--
      Properties
      -->
      <!--Preserve a property, it's backing field (if present), getter, and setter methods-->
      <property signature="System.Int32 Property1" />
      <property signature="System.Int32 Property2" accessors="all" />
      <!--Preserve a property, it's backing field (if present), and getter method-->
      <property signature="System.Int32 Property3" accessors="get" />
      <!--Preserve a property, it's backing field (if present), and setter method-->
      <property signature="System.Int32 Property4" accessors="set" />
      <!--Preserve a property by name rather than signature-->
      <property name="Property5" />
      <!--
      Events
      -->
      <!--Preserve an event, it's backing field (if present), add, and remove methods-->
      <event signature="System.EventHandler Event1" />
      <!--Preserve an event by name rather than signature-->
      <event name="Event2" />
    </type>
    <!--Examples with generics-->
    <type fullname="Assembly1.G`1">
      <!--Preserve a field with generics in the signature-->
      <field signature="System.Collections.Generic.List`1&lt;System.Int32&gt; field1" />
      <field signature="System.Collections.Generic.List`1&lt;T&gt; field2" />
      <!--Preserve a method with generics in the signature-->
      <method signature="System.Void Method1(System.Collections.Generic.List`1&lt;System.Int32&gt;)" />
      <!--Preserve an event with generics in the signature-->
      <event signature="System.EventHandler`1&lt;System.EventArgs&gt; Event1" />
    </type>
    <!--Preserve a nested type-->
    <type fullname="Assembly1.H/Nested" preserve="all"/>
    <!--Preserve all fields of a type if the type is used.  If the type is not used it will be removed-->
    <type fullname="Assembly1.I" preserve="fields" required="0"/>
    <!--Preserve all methods of a type if the type is used.  If the type is not used it will be removed-->
    <type fullname="Assembly1.J" preserve="methods" required="0"/>
    <!--Preserve all types in a namespace-->
    <type fullname="Assembly1.SomeNamespace*" />
    <!--Preserve all types with a common prefix in their name-->
    <type fullname="Prefix*" />
  </assembly>
  <!--
  Preserve an entire assembly
  -->
  <assembly fullname="Assembly2" preserve="all"/>
  <!--No "preserve" attribute and no types specified means preserve all-->
  <assembly fullname="Assembly3"/>
  <!--
  Fully qualified assembly name
  -->
  <assembly fullname="Assembly4, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null">
    <type fullname="Assembly4.Foo" preserve="all"/>
  </assembly>
</linker>
```

## Syntax of supplementary custom attribute annotations

Linker supports specifying a JSON file that has a set of custom attributes that the linker will pretend are applied to members in the input assemblies.

The structure is:

```json
{
  "[assembly-name]": {
    "[namespace-name]": {
      "[type-name]": {
        "[field-or-property-name]": {
          "[attribute-name]": "[attribute-value]"
        },

        "[method-name]([method-signature])": {
          "[parameter-name]": {
            "[attribute-name]": "[attribute-value]"
          }
        }
      }
    }
  }
}
```

# Inside the linker

The linker is a quite small piece of code, and it pretty simple to address.
Its only dependency is `Mono.Cecil`, that is used to read, modify and write back
the assemblies.

Everything is located in the namespace Linker, or in sub namespaces.
Being a command line utility, its entry point function is in the class Driver.

This class is in charge of analyzing the command line, and to instantiate two
important objects, a LinkContext, and a Pipeline.

The LinkContext contains all the information that will be used during the
linking process, such as the assemblies involved, the output directory and
probably other useful stuff.

The Pipeline is simply a queue of actions (steps), to be applied to the current
context. The whole process of linking is split into those different steps
that are all located in the Linker.Steps namespace.

Here are the current steps that are implemented, in the order they are used:

## ResolveFromAssembly or ResolveFromXml

Those steps are used to initialize the context and pre-mark the root code
that will be used as a source for the linker.

Resolving from an assembly or resolving from an xml descriptor is a decision
taken in the command line parsing.

## LoadReferences

This step will load all the references of all the assemblies involved in the
current context.

## Blacklist

This step is used if and only if you have specified that the core should be
linked. It will load a bunch of resources from the assemblies, that are
actually a few XML descriptors, that will ensure that some types and methods
that are used from inside the runtime are properly linked and not removed.

It is doing so by inserting a ResolveFromXml step per blacklist in the
pipeline.

## Mark

This is the most complex step. The linker will get from the context the list
of types, fields and methods that have been pre-marked in the resolve steps,
and walk through all of them. For every method, it will analyse the CIL stream,
to find references to other fields, types, or methods.

When it encounters such a reference, it will resolve the original definition of
this reference, and add this to the queue of items to be processed. For
instance, if have in a source assembly a call to Console.WriteLine, the linker
will resolve the appropriate method WriteLine in the Console type from the
mscorlib assembly, and add it to the queue. When this WriteLine method will be
dequeued, and processed, the linker will go through everything that is used in
it, and add it to the queue, if they have not been processed already.

To know if something has been marked to be linked, or processed, the linker
is using a functionality of Cecil called annotations. Almost everything in
Cecil can be annotated. Concretely, it means that almost everything owns an
Hashtable in which you can add what you want, using the keys and the values you
want.

So the linker will annotate assemblies, types, methods and fields to know
what should be linked or not, and what has been processed, and how it should
process them.

This is really useful as we don't have to recreate a full hierarchy of classes
to encapsulate the different Cecil types to add the few pieces of information we want.

## Sweep

This simple step will walk through all the elements of an assembly, and based
on their annotations, remove them or keep them.

## Clean

This step will clean parts of the assemblies, like properties. If a property
used to have a getter and a setter, and that after the mark & sweep steps,
only the getter is linked, it will update the property to reflect that.

There are a few things to keep clean like properties we've seen, events,
nested classes, and probably a few others.

## Output

For each assembly in the context, this step will act on the action associated
with the assembly. If the assembly is marked as skip, it won't do anything,
if it's marked as copy, it will copy the assembly to the output directory,
and if it's link, it will save the modified assembly to the output directory.

# Reporting a bug

If you face a bug in the linker, please report it using GitHub issues
