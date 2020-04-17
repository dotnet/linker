using System;
using System.Collections.Generic;

namespace Mono.Linker.Dataflow
{
	struct ArrayBuilder<T>
	{
		private List<T> _list;

		public void Add (T value) => (_list ??= new List<T> ()).Add (value);

		public bool Any (Predicate<T> callback) => _list == null ? false : _list.Exists (callback);

		public T [] ToArray () => _list?.ToArray ();

		public int Count => _list?.Count ?? 0;
	}
}
