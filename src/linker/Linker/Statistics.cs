// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mono.Linker
{
	public class Statistics
	{
		const string StatisticSuffix = "Statistic";
		readonly Dictionary<string, List<NamedValue>> _trackedValues = new Dictionary<string, List<NamedValue>> (StringComparer.Ordinal);

		public NamedValue GetValue(string category, string name)
		{
			if (name.EndsWith (StatisticSuffix))
				name = name.Substring (0, name.Length - StatisticSuffix.Length);

			if (!_trackedValues.TryGetValue(category, out var values)) {
				values = new List<NamedValue> ();
				_trackedValues.Add (category, values);
			}

			var value = values.FirstOrDefault (v => string.Equals (v.Name, name, StringComparison.Ordinal));
			if (value == null) {
				value = new NamedValue (category, name);
				values.Add (value);
			}

			return value;
		}

		public void Log(LinkContext context)
		{
			using var writer = new StringWriter ();
			writer.WriteLine ("Statistics:");
			foreach (var category in _trackedValues.Keys) {
				writer.WriteLine ($"\t{category}");
				foreach (var value in _trackedValues[category]) {
					writer.WriteLine ($"\t\t{value.Name}: {value.Value}");
				}
			}

			context.LogDiagnostic (writer.ToString ());
		}

		public class NamedValue
		{
			public string Category { get; private set; }
			public string Name { get; private set; }
			public int Value { get; set; }

			public NamedValue(string category, string name)
			{
				Category = category;
				Name = name;
				Value = 0;
			}

			public static NamedValue operator+(NamedValue value, int increment)
			{
				value.Value += increment;
				return value;
			}

			public static NamedValue operator++(NamedValue value)
			{
				value.Value++;
				return value;
			}
		}
	}
}
