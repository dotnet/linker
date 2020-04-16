# IL Linker Warning Codes 

### IL2001
A type whose fields were specified to be preserved via a XML descriptor has none.

---

### IL2002
A type whose methods were specified to be preserved via a XML descriptor has none.

---

### IL2003
An assembly dependency that was specified via the `PreserveDependency` attribute could not be resolved.

---

### IL2004
A type dependency that was specified via the `PreserveDependency` attribute could not be resolved.

---

### IL2005
A dependency member that was specified via the `PreserveDependency` attribute could not be resolved.

---

### IL2006
The linker found an unrecognized reflection access pattern.

---

### IL2007
An assembly that was specified for external customization was not found.

---

### IL2008
A type that was specified for external customization was not found.

---

### IL2009
A method which was specified for custom substitution was not found.

---

### IL2010
The value specified for a method to be substituted for could not be casted to the required type.

---

### IL2011
The value of the body attribute used for a custom substitution was invalid (currently the only supported options are `remove` and `stub`).

---

### IL2012
A field which was specified for custom substitution was not found.

---

### IL2013
A field which was specified for substitution was found to be either non static or constant. Fields substituted via custom substitution must comply with being static and non constant.

---

### IL2014
A field was specified for substitution but no value to be substituted for was given.

---

### IL2015
The value specified for a field to be substituted for could not be casted to the required type.
