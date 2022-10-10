// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ILLink.Shared.TypeSystemProxy
{
	internal struct ParameterCollection
	{
		readonly int _start;

		readonly int _end;

		readonly MethodProxy _method;

		public ParameterCollection (int start, int end, MethodProxy method)
		{
			_start = start;
			_end = end;
			_method = method;
		}

		public ParameterEnumerator GetEnumerator () => new ParameterEnumerator (_start, _end, _method);

		public struct ParameterEnumerator
		{
			int _current;
			readonly int _end;
			readonly MethodProxy _method;
			public ParameterEnumerator (int start, int end, MethodProxy method)
			{
				_current = start - 1;
				_end = end;
				_method = method;
			}
			public ParameterProxy Current => new ParameterProxy (_method, (ParameterIndex) _current);

			public bool MoveNext () => ++_current < _end;
		}
	}
}
