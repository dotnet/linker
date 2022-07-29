# Interface Implementation Methods Marking
#### (Does this method need to be kept due to the interface method it overrides)

The following behavior is expected for interface methods. This logic could be used to begin marking and sweeping the `.Override` of a method since if the method isn't a dependency due to the interface/base type, we should be able to remove the methodImpl. Right now, the methodImpl is always kept if both the interface method and overriding method is kept, but that isn't always necessary.

Whether or not a method implementing an interface method is required due to the _interface_ is affected by the following cases / possibilities (the method could still be kept for other reasons):
- Base method is abstract or has a default implementation (`virtual` in C#)
- Method is Instance or Static
- Linker is in library mode or exe mode
- Implementing type is relevant to variant casting or not
  - Relevant to variant casting means the type token appears, the type is passed as a type argument or array type, or is reflected over.
- Base method is marked as used or not
- Base method is from preserved scope or not
- Implementing type is marked as instantiated or not
- Interface Implementation is marked or not

Note that in library mode, interface methods that can be accessed by COM or native code are marked by the linker.

### If Linker is in library mode, mark the implementation method
All interfaces and interface methods should be kept for library mode. COM in the runtime library may expect the interfaces to exist, so we should keep them.

Cases left (bold means we know it is only one of the possible options now):
- Base method is abstract or has a default implementation
- Method is Instance or Static
- Linker is in library mode or exe mode
- Implementing type is relevant to variant casting or not
- Base method is marked as used or not
- Base method from preserved scope or not
- Implementing type is marked as instantiated or not
- Interface Implementation is marked

### If the interface implementation is not marked, do not mark the implementation method
A type that doesn't implement the interface isn't required to have methods that implement the interface.

Cases left (bold means we know it is only one of the possible options now):
- Base method is abstract or has a default implementation
- Method is Instance or Static
- Linker is in library mode or exe mode
- Implementing type is relevant to variant casting or not
- Base method is marked as used or not
- Base method from preserved scope or not
- Implementing type is marked as instantiated or not
- __Interface Implementation is marked__

### If the interface method is not marked and the interface doesn't come from a preserved scope, do not mark the implementation method
Unmarked interface methods from `link` assemblies will be removed so the implementing method does not need to be kept.

Cases left:
- Base method is abstract or has a default implementation
- Method is Instance or Static
- Linker is in library mode or exe mode
- Implementing type is relevant to variant casting or not
- ~~Base method is marked as used or not~~
- ~~Base method from preserved scope or not~~
- _Base method is either marked as used or from preserved scope (combine above)_
- Implementing type is marked as instantiated or not
- __Interface Implementation is marked__

### If the interface method is abstract, mark the implementation method
The method is needed for valid IL.

Cases left:
- __Base method has a default implementation__
- Method is Instance or Static
- Linker is in library mode or exe mode
- Implementing type is relevant to variant casting or not
- Base method is marked as used or from preserved scope
- Implementing type is marked as instantiated or not
- __Interface Implementation is marked__

### If the interface is from a preserved scope and the linker is in library mode, we should treat the base method as marked
#### If the method is static, mark the implementation method
An application may use this method through a constrained type parameter
#### If the method is an instance method, and the type is instantiated or has a non-private constructor, mark the implementation method
An application can create an instance and call the method through the interface

All other behaviors are the same regardless of whether or not the linker is in library or exe mode, or whether or not the interface is in a preserved scope.

Cases left:
- __Base method has a default implementation__
- Method is Instance or Static
- ~~Linker is in library mode or exe mode~~
- Implementing type is relevant to variant casting or not
- Base method is marked as used _or not_ ~~or from preserved scope~~
- Implementing type is marked as instantiated or not
- __Interface Implementation is marked__

### If the interface method is not marked, do not mark the implementation method
We know the method cannot be called if it is not marked.

Cases left:
- __Base method has a default implementation__
- Method is Instance or Static
- Implementing type is relevant to variant casting or not
- __Base method is marked as used__
- Implementing type is marked as instantiated or not
- __Interface Implementation is marked__

### If the method is static and the implementing type is relevant to variant casting, mark the implementation method. If the method is static and the implementing type is not relevant to variant casting, do not mark the implementation method.
A static method may only be called through a constrained call if the type is relevant to variant casting.

Cases left:
- __Base method has a default implementation__
- __Method is Instance__
- Implementing type is relevant to variant casting or not
- __Base method is marked as used__
- Implementing type is marked as instantiated or not
- __Interface Implementation is marked__

Instance methods are not affected by whether or not it's relevant to variant casting

Cases left:
- __Base method has a default implementation__
- __Method is Instance__
-~~Implementing type is relevant to variant casting or not~~
- __Base method is marked as used__
- Implementing type is marked as instantiated or not
- __Interface Implementation is marked__


### If the implementing type is marked as not instantiated, do not mark the implemetation method. If the implementing type is marked as instantiated, mark the implementation method.

This should cover all the cases, but let me know if there are cases I don't mention or factors that should affect this. I don't think all of this behavior are all implemented and more tests need to be made to check each of these cases.

Summary:

if __Interface Implementation is not marked__ then do not mark the implementation method.

else if __Base method is marked as not used__ AND __Interface is not from preserved scope__ do not mark the implementation method

else if __Base method does not have a default implementation__ then mark the implementation method

else if __Interface is from preserved scope__ AND __Linker is in library mode__ AND __Method is static__ then mark the implementation method

else if __Interface is from preserved scope__ AND __Linker is in library mode__ AND __Method is instance__ AND __Implementing type is marked as instantiated__ then mark the implementing method

else if __Interface is from preserved scope__ AND __Base method is marked as not used__ then do not mark the implementation method

else if __Method is Static__ AND __Implementing type is relevant to variant casting__ then mark the implementation method

else if __Method is Static__ AND __Implementing type is not relevant to variant casting__ then do not mark the implementation method

else if __Method is marked as instantiated__ then mark the implementing method

else do not mark the implementation method
