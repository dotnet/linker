﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpAnalyzerVerifier<
	ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class DynamicallyAccessedMembersAnalyzerTests
	{
		static Task VerifyDynamicallyAccessedMembersAnalyzer (string source, params DiagnosticResult[] expected)
		{
			return VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer),
				expected);
		}

		#region SourceParametter
		[Fact]
		public Task SourceParametterDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class T
{
}

class C
{
	public static void Main()
	{
		M(typeof(T));
	}

	private static void NeedsPublicMethodsOnParameter(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type parameter)
	{
	}

	private static void M(Type type)
	{
		NeedsPublicMethodsOnParameter(type);
	}
}";

			// (23,33): warning IL2067: 'parameter' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'NeedsPublicMethodsOnParameter'.
			// The parameter 'type' of method 'M' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2067)
				.WithSpan (23, 33, 23, 37)
				.WithArguments ("parameter", "NeedsPublicMethodsOnParameter", "type", "M", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceParametterDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class T
{
}

class C
{
    public static void Main()
    {
        M(typeof(T));
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M(Type type)
    {
        return type;
    }
}";

			// (19,9): warning IL2068: 'M' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The parameter 'type' of method 'M' does not have matching annotations.The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2068)
				.WithSpan (19, 9, 19, 21)
				.WithArguments ("M", "type", "M", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceParametterDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class T
{
}

class C
{
    public static void Main()
    {
        M(typeof(T));
    }

    private static void M(Type type)
    {
        f = type;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f = typeof(T);
}";

			// (18,9): warning IL2069: value stored in field 'f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The parameter 'type' of method 'M' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2069)
				.WithSpan (18, 9, 18, 17)
				.WithArguments ("f", "type", "M", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceParameterDoesNotMatchTargetMethodAnnotations ()
		{
			var TargetMethodWithAnnotations = @"
using System;

public class T
{
}

class C
{
    public static void Main()
    {
        M(typeof(T));
    }

    private static void M(Type type)
    {
        type.GetMethod(""Foo"");

	}
}";

			// (18,9): warning IL2070: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'GetMethod'.
			// The parameter 'type' of method 'M' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2070)
				.WithSpan (17, 9, 17, 23)
				.WithArguments ("GetMethod", "type", "M", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}
		#endregion

		#region SourceMethodReturnType
		[Fact]
		public Task SourceMethodReturnTypeDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class T
{
}

class C
{
    public static void Main()
    {
        NeedsPublicMethodsOnParameter(GetT());
    }

    private static void NeedsPublicMethodsOnParameter(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
    }

    private static Type GetT()
    {
        return typeof(T);
    }
}";

			// (13,39): warning IL2072: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'NeedsPublicMethodsOnParameter'.
			// The return value of method 'GetT' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2072)
				.WithSpan (13, 39, 13, 45)
				.WithArguments ("type", "NeedsPublicMethodsOnParameter", "GetT", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodReturnTypeDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class T
{
}

class C
{
    public static void Main()
    {
        M();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M()
    {
        return GetT();
    }

    private static Type GetT()
    {
        return typeof(T);
    }
}";

			// (19,9): warning IL2073: 'M' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The return value of method 'GetT' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2073)
				.WithSpan (19, 9, 19, 23)
				.WithArguments ("M", "GetT", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodReturnTypeDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class T
{
}

class C
{
    public static void Main()
    {
        f = M();
    }

    private static Type M()
    {
        return typeof(T);
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f;
}";

			// (13,9): warning IL2074: value stored in field 'f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The return value of method 'M' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2074)
				.WithSpan (13, 9, 13, 16)
				.WithArguments ("f", "M", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodReturnTypeDoesNotMatchTargetMethod ()
		{
			var TargetMethodWithAnnotations = @"
using System;

public class T
{
}

class C
{
    public static void Main()
    {
        GetT().GetMethod(""Foo"");

	}

	private static Type GetT ()
	{
		return typeof (T);
	}
}";

			// (12,9): warning IL2075: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'GetMethod'.
			// The return value of method 'GetT' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2075)
				.WithSpan (12, 9, 12, 25)
				.WithArguments ("GetMethod", "GetT", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}
		#endregion

		#region SourceField
		[Fact]
		public Task SourceFieldDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class T
{
}

class C
{
    private static Type f = typeof(T);

    public static void Main()
    {
        NeedsPublicMethods(f);
    }

    private static void NeedsPublicMethods(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
	{
	}
}";

			// (15,28): warning IL2077: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'NeedsPublicMethods'.
			// The field 'f' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2077)
				.WithSpan (15, 28, 15, 29)
				.WithArguments ("type", "NeedsPublicMethods", "f", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceFieldDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class T
{
}

class C
{
    private static Type f = typeof(T);

    public static void Main()
    {
        M();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M()
	{
        return f;
	}
}";

			// (21,9): warning IL2078: 'M' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The field 'f' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2078)
				.WithSpan (21, 9, 21, 18)
				.WithArguments ("M", "f", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceFieldDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class T
{
}

class C
{
    private static Type f1 = typeof(T);

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f2 = typeof(T);

    public static void Main()
    {
        f2 = f1;
    }
}";

			// (18,9): warning IL2079: value stored in field 'f2' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The field 'f1' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2079)
				.WithSpan (18, 9, 18, 16)
				.WithArguments ("f2", "f1", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceFieldDoesNotMatchTargetMethodAnnotations ()
		{
			var TargetMethodWithAnnotations = @"
using System;

public class T
{
}

class C
{
    private static Type f = typeof(T);

    public static void Main()
    {
        f.GetMethod(""Foo"");

	}
}";

			// (14,9): warning IL2080: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'GetMethod'.
			// The field 'f' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2080)
				.WithSpan (14, 9, 14, 20)
				.WithArguments ("GetMethod", "f", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}
		#endregion

		#region SourceMethod

		static string GetSystemTypeBase ()
		{
			return @"
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;

namespace System
{
	public class TestSystemTypeBase : Type
	{
		public override Assembly Assembly => throw new NotImplementedException ();

		public override string AssemblyQualifiedName => throw new NotImplementedException ();

		public override Type BaseType => throw new NotImplementedException ();

		public override string FullName => throw new NotImplementedException ();

		public override Guid GUID => throw new NotImplementedException ();

		public override Module Module => throw new NotImplementedException ();

		public override string Namespace => throw new NotImplementedException ();

		public override Type UnderlyingSystemType => throw new NotImplementedException ();

		public override string Name => throw new NotImplementedException ();

		public override ConstructorInfo[] GetConstructors (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override object[] GetCustomAttributes (bool inherit)
		{
			throw new NotImplementedException ();
		}

		public override object[] GetCustomAttributes (Type attributeType, bool inherit)
		{
			throw new NotImplementedException ();
		}

		public override Type GetElementType ()
		{
			throw new NotImplementedException ();
		}

		public override EventInfo GetEvent (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override EventInfo[] GetEvents (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override FieldInfo GetField (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override FieldInfo[] GetFields (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override Type GetInterface (string name, bool ignoreCase)
		{
			throw new NotImplementedException ();
		}

		public override Type[] GetInterfaces ()
		{
			throw new NotImplementedException ();
		}

		public override MemberInfo[] GetMembers (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override MethodInfo[] GetMethods (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override Type GetNestedType (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override Type[] GetNestedTypes (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override PropertyInfo[] GetProperties (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override object InvokeMember (string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
		{
			throw new NotImplementedException ();
		}

		public override bool IsDefined (Type attributeType, bool inherit)
		{
			throw new NotImplementedException ();
		}

		protected override TypeAttributes GetAttributeFlagsImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override ConstructorInfo GetConstructorImpl (BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException ();
		}

		protected override MethodInfo GetMethodImpl (string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException ();
		}

		protected override PropertyInfo GetPropertyImpl (string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException ();
		}

		protected override bool HasElementTypeImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsArrayImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsByRefImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsCOMObjectImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsPointerImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsPrimitiveImpl ()
		{
			throw new NotImplementedException ();
		}
	}
}";
		}

		[Fact]
		public Task SourceMethodDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = @"
namespace System
{
    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M1();
        }

        private void M1()
        {
            M2(this);
        }

        private static void M2(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
				System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
        }
    }
}";
			// (178,16): warning IL2082: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'M2'. The implicit 'this' argument of method 'M1' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), TargetParameterWithAnnotations),
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2082)
				.WithSpan (178, 16, 178, 20)
				.WithArguments ("type", "M2", "M1", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = @"
namespace System
{
    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M();
        }

        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
                System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        private Type M()
        {
            return this;
        }
    }
}";

			// (180,13): warning IL2083: 'M' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The implicit 'this' argument of method 'M' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), TargetMethodReturnTypeWithAnnotations),
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2083)
				.WithSpan (180, 13, 180, 25)
				.WithArguments ("M", "M", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = @"
namespace System
{
    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M();
        }

        private void M()
        {
            f = this;
        }

        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
                System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        private static Type f;
    }
}";

			// (178,13): warning IL2084: value stored in field 'f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The implicit 'this' argument of method 'M' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), TargetFieldWithAnnotations),
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2084)
				.WithSpan (178, 13, 178, 21)
				.WithArguments ("f", "M", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodDoesNotMatchTargetMethodAnnotations ()
		{
			var TargetMethodWithAnnotations = @"
namespace System
{
    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M();
        }

        private void M()
        {
            this.GetMethods();
        }
    }
}";

			// (178,13): warning IL2085: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'GetMethods'.
			// The implicit 'this' argument of method 'M' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), TargetMethodWithAnnotations),
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2085)
				.WithSpan (178, 13, 178, 28)
				.WithArguments ("GetMethods", "M", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}
		#endregion

		[Fact]
		public Task SourceGenericParameterDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    public static void Main()
    {
        M2<int>();
    }

    private static void M1(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
    }

    private static void M2<T>()
    {
        M1(typeof(T));
    }
}";

			// (23,12): warning IL2087: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'M1'.
			// The generic parameter 'T' of 'M2' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2087)
				.WithSpan (19, 12, 19, 21)
				.WithArguments ("type", "M1", "T", "M2", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceGenericParameterDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    public static void Main()
    {
        M<int>();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M<T>()
    {
        return typeof(T);
    }
}";

			// (15,9): warning IL2088: 'M' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The generic parameter 'T' of 'M' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2088)
				.WithSpan (15, 9, 15, 26)
				.WithArguments ("M", "T", "M", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceGenericParameterDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = @"
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    public static void Main()
    {
        M<int>();
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f;

    private static void M<T>()
    {
        f = typeof(T);
    }
}";

			// (17,9): warning IL2089: value stored in field 'f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The generic parameter 'T' of 'M' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2089)
				.WithSpan (17, 9, 17, 22)
				.WithArguments ("f", "T", "M", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceGenericParameterDoesNotMatchTargetGenericParameterAnnotations ()
		{
			var TargetGenericParameterWithAnnotations = @"
using System.Diagnostics.CodeAnalysis;

class C
{
    public static void Main()
    {
        M2<int>();
    }

    private static void M1<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
    {
    }

    private static void M2<S>()
    {
        M1<S>();
    }
}";

			return VerifyDynamicallyAccessedMembersAnalyzer (TargetGenericParameterWithAnnotations,
				VerifyCS.Diagnostic (DynamicallyAccessedMembersAnalyzer.IL2091)
				.WithSpan (17, 9, 17, 16)
				.WithArguments ("T", "M1", "S", "M2", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}
	}
}
