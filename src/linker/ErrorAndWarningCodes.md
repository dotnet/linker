# IL Linker Error Codes

### IL1001
```
Failed to process XML substitution: 'XML document location'. Feature 'feature' does not specify a "featurevalue" attribute
```

The substitution in 'XML document location' with feature value 'feature' does not use the `featurevalue` attribute. These attributes have to be used together.

---

### IL1002
```
Failed to process XML substitution: 'XML document location'. Unsupported non-boolean feature definition 'feature'
```

The substitution in 'XML document location' with feature value 'feature' sets the attribute `featurevalue` to a non-boolean value. Only boolean values are supported for this attribute.

---

### IL1003
```
Error processing 'XML document name': 'XmlException'
```

There was an error processing a resource linker descriptor. The most likely reason for this is that the descriptor file has syntactical errors.

---

### IL1004
```
Error processing 'XML document name': 'XmlException'
```

There was an error processing an embedded resource linker descriptor. The most likely reason for this is that the descriptor file has syntactical errors.

---

### IL1005
```
Error processing 'XML document name': 'XmlException'
```

There was an error processing an external substitution XML (`ILLink.Substitutions.xml`). The most likely reason for this is that the substitution file has syntactical errors.

---

### IL1006
```
Failed to process description file 'XML document location': 'XmlException'
```

There was an error processing an XML descriptor. The most likely reason for this is that the descriptor file has syntactical errors.

---

# IL Linker Warning Codes 

### IL2001
```
Type 'type' has no fields to preserve
```

The XML descriptor preserves fields on type 'type', but this type has no fields.

---

### IL2002
```
Type 'type' has no methods to preserve
```

The XML descriptor preserves methods on type 'type', but this type has no methods.

---

### IL2003
```
Could not resolve 'assembly' assembly dependency specified in a `PreserveDependency` attribute that targets method 'method'
```

The assembly 'assembly' in `PreserveDependency` attribute could not be resolved.

---

### IL2004
```
Could not resolve 'type' type dependency specified in a `PreserveDependency` attribute that targets method 'method'
```

The type 'type' in `PreserveDependency` attribute could not be resolved.

---

### IL2005
```
Could not resolve dependency member 'member' declared in type 'type' specified in a `PreserveDependency` attribute that targets method 'method'
```

The member 'member' in `PreserveDependency` attribute could not be resolved.

---

### IL2006
```
Unrecognized reflection pattern ...
```

The linker found an unrecognized reflection access pattern. The most likely reason for this is that the linker could not resolve a member that is being accessed dynamicallly. To fix this, use the `DynamicallyAccessedMemberAttribute` and specify the member kinds you're trying to access.

---

### IL2007
```
Could not match assembly 'assembly' for substitution
```

The assembly 'assembly' in the substitution XML could not be resolved.

---

### IL2008
```
Could not resolve type 'type' for substitution
```

The type 'type' in the substitution XML could not be resolved.

---

### IL2009
```
Could not find method 'method' for substitution
```

The method 'method' in the substitution XML could not be resolved.

---

### IL2010
```
Invalid value for 'signature' stub
```

The value 'value' used in the substitution XML for method 'signature' does not represent a value of a built-in type, or does not match the return type of the method.

---

### IL2011
```
Unknown body modification 'action' for 'signature'
```

The value 'action' of the body attribute used in the substitution XML is invalid (the only supported options are `remove` and `stub`).

---

### IL2012
```
Could not find field 'field' for substitution
```

The field 'field' specified in the substitution XML was not found.

---

### IL2013
```
Substituted field 'field' needs to be static field
```

The substituted field 'field' was non-static or constant. Only static non-constant fields are supported.

---

### IL2014
```
Missing 'value' attribute for field 'field'
```

A field was specified for substitution but no value to be substituted was given.

---

### IL2015
```
Invalid value for 'field': 'value'
```

The value 'value' used in the substitution XML for field 'field' is not a built-in type, or does not match the type of 'field'.
