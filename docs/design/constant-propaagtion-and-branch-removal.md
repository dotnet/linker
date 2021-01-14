# Constant propagation and unreachable branch removal

ILLink implements optimization which can propagate constant values across methods and based on these constants determine unreachable branches in code and remove those. This means that the code in the removed branch is not scanned for its dependencies which in turn won't be marked and can potentially be trimmed as well.

## Desired behavior

### Constant propagation

Method can return constant value if its code will always return the same value (and it's possible to statically analyze that as a fact), for example:

```csharp
    public bool Is32Bit { get => false; }
```

On 64bit platforms the property is compiled with constant value, and ILLInk can determine this. It's also possible to use substitutions to overwrite method's return value to a constant via the [substitutions XML file](../data-formats.md#substitution-format). 

If such method is used in another method and it influences its return value, it can mean that the caller method will itself always return the same value. For example:

```csharp
    public int SizeOfIntPtr { 
        get {
            if (Is32Bit)
                return 4;
            else
                return 8;
        }
    }
```

ILLink will be able to determine that the call to `Is32Bit` getter will always return `false` and thus the `SizeOfIntPtr` will in turn always return `8`.

### Unreachable branch removal

If some method's return value is detected as constant, it can be possible to optimize conditions in which the return value is used and potentially even remove entire branches of code. For example:

```csharp
    public void CopyMemory ()
    {
        if (Is32Bit)
        {
            CopyUsingDWords ();
        }
        else
        {
            CopyUsingQWords ();
        }
    }
```

In this case if building for 64bit platform the condition will be evaluated as `false` always, and thus the `true` branch of the `if` can be removed. This will in turn lead to also trimming `CopyUsingDWords` method (assuming it's not used from some other place).

### Explicit non-goals

For now ILLink will not inline any method calls. It's relatively tricky to determine if it's possible without breaking the application and leaving the actual calls in place makes debugging more predictable and easier (it's possible to set a breakpoint into the callee's body and it will be hit always).

## Algorithm

The implementation of this optimization is relatively complex since it's solving a potentially global problem in that results of optimization of one method potentially influence results of all methods which call it and so on. But we need the algorithm to work locally without global view. This is necessary because of lazy loading of assemblies, which means that before and during marking it's not guaranteed that all assemblies were discovered and loaded. At the same time this optimization must be complete before a given method is processed by `MarkStep` since we want to not mark dependencies from removed branches.

### Used data structures

* Dictionary of method -> value for all visited methods. The value of a method can be several things depending on the state of processing and the result of the analysis:
  * Pointer to the enqueued processing node if the method is still being processed
  * Sentinel value "Processed but not changed" which means the method has been processed and no optimization was done on it. It's unknown if the method returns a constant value or not (yet, analysis hasn't occurred). If nothing needs to know the return value of the method then this can be a final state.
  * Sentinel value "Processed and is not constant" which means the method has been processed and its return value was not detected as constant. This is a final state.
  * Instruction which represents the constant return value of the method if it was detected as returning constant value. This is a final state.

* Processing stack which stores ordered list of processing node, each node representing a method and addition data about it. The stack is processed by always taking the top of the stack and attempting to process that node. Nodes are always added to the top of the stack and are always removed from the top of the stack. In some cases nodes are "moved", that is a node which is not on the top of the stack is moved to the top of the stack. For this reason the stack is implemented as a linked list (so that it's easy to point to nodes in it as well as moves nodes around).

### Processing methods

It starts by placing the requested method on top of the stack and then processing the stack until it's empty (at which point the requested method is guaranteed to be processed).

Processing the stack is a loop where:

* The top of the stack is peeked (not actually popped) and the method there is processed
  * The last attempt version of the method is set to the current version of the stack (for loop detection, see below)
  * The method's body is scanned and all callees which can be used for constant propagation are detected
    * If the called method is already processed its value is used (if it has one)
      * There's an optimization here where methods are only marked as processed without analyzing for their return value. If such method is encountered here, the return value analyzer will run in-place to determine the value of the method (and the result is stored)
    * If the called method is not yet processed and is not on the stack, it's added to the top of the stack
    * If the called method is not yet processed but it's already on the stack, it's moved to the top of the stack - this makes it efficient since this promotes processing of dependencies before the dependents and thus reduces the number of times the dependents must be re-scanned.
  * If the scan was not fully done because some callees are not yet processed, give up on this method and loop (pick up the new top of the stack)
  * If the scan was successful
    * If there were not callees with constant values detected, mark the method as "Processed and unchanged" and remove it from the stack - loop
  * If the method had any constants detected, run the branch removal logic to remove unused branches
  * Regardless of branch removal results (even if nothing happened) use the new method body and the detected constants to analyze the method if it returns a constant itself - store the result
  * Mark the method as processed and remove it from the stack - loop

## Alternatives and improvements

### Use actual recursion in the analyzer

The processing of methods is recursive in nature since callers needs to know results of processing callees. To avoid actual recursion in the analyzer, the nodes are stored in the processing stack. If the necessary results are not yet known for a given method, the current method is postponed (moves down on the stack) and it will be retried later on. This is potentially expensive. An optimization would be to allow a limited recursion within the analyzer and only rely on the processing stack in cases a recursion limit is reached.
