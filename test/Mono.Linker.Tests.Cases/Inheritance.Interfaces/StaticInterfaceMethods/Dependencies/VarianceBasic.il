// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }

.assembly 'library' { }

.class interface public abstract auto ansi InterfaceScenario1`1<- T>
{
  .method public newslot static abstract virtual 
          int32  Method() cil managed noinlining
  {
  } // end of method InterfaceScenario1`1::Method

} // end of class InterfaceScenario1`1

.class public auto ansi BaseScenario1
       extends [mscorlib]System.Object
       implements class InterfaceScenario1`1<object>
{
  .method public static int32  Method() cil managed noinlining
  {
    .override  method int32 class InterfaceScenario1`1<object>::Method()
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  ldc.i4     0x1
    IL_0005:  ret
  } // end of method BaseScenario1::Method

} // end of class BaseScenario1

.class public auto ansi DerivedScenario1
       extends BaseScenario1
{
} // end of class DerivedScenario1

.class public auto ansi TestEntrypoint
       extends [mscorlib]System.Object
{
  .method public static string  Test_Scenario<T,(class InterfaceScenario1`1<!!T>) ImplType>() cil managed noinlining
  {
    // Code size       20 (0x14)
    .maxstack  8
    .locals init (int32 V_0)
    constrained. !!ImplType
    call       int32 class InterfaceScenario1`1<!!T>::Method()
    stloc.0
    ldloca.s   V_0
    call       instance string [mscorlib]System.Int32::ToString()
    ret
  } // end of method TestEntrypoint::Test_Scenario
  .method public static int32 Test() cil managed noinlining
  {
    .locals init (class [mscorlib]System.Exception V_0)
    .try {
        ldstr      "VariantDispatchToBaseTypeMethodVariantly"
        ldstr      "1"
        call       string TestEntrypoint::Test_Scenario<string,class BaseScenario1>()
        pop
        leave.s ScenarioDone
    } catch [mscorlib]System.Exception {
        stloc.0
        leave.s ScenarioDone
    }
ScenarioDone: nop
    ret
  } // end of method Test
} // end of class TestEntrypoint


