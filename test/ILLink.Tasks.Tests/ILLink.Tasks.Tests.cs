using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Linker;

namespace ILLink.Tasks.Tests
{
	// These tests ensure that the task options correctly flow from
	// the task -> response file -> parsed arguments -> options on LinkContext
	public class TaskArgumentTests
	{
		public static IEnumerable<object[]> AssemblyPathsCases => new List<object[]> {
			new object [] {
				new ITaskItem [] {
					new TaskItem ("Assembly.dll", new Dictionary<string, string> { { "trimmode", "copy" } })
				}
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem ("Assembly.dll", new Dictionary<string, string> { { "TrimMode", "Copy" } })
				}
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem ("path with/spaces/Assembly.dll")
				}
			},
			new object [] {
				new ITaskItem [] {
					// same path
					new TaskItem ("path/to/Assembly1.dll"),
					new TaskItem ("path/to/Assembly2.dll")
				}
			},
			new object [] {
				new ITaskItem [] {
					// same assembly
					new TaskItem ("path/to/Assembly.dll"),
					new TaskItem ("path/to/Assembly.dll")
				}
			},
			new object [] {
				new ITaskItem [] {
					// same assembly name, different paths
					new TaskItem ("path1/Assembly.dll"),
					new TaskItem ("path2/Assembly.dll")
				}
			}
		};

		[Theory]
		[MemberData (nameof (AssemblyPathsCases))]
		public void TestAssemblyPaths (ITaskItem[] assemblyPaths)
		{
			var task = new MockTask () {
				AssemblyPaths = assemblyPaths
			};
			using (var driver = task.CreateDriver ()) {
				var context = driver.Context;

				var expectedReferences = assemblyPaths.Select (p => p.ItemSpec)
					.GroupBy (p => Path.GetFileNameWithoutExtension (p))
					.Select (g => g.First ());
				var actualReferences = driver.GetReferenceAssemblies ();
				Assert.Equal (expectedReferences.OrderBy (a => a), actualReferences.OrderBy (a => a));

				foreach (var item in assemblyPaths) {
					var assemblyPath = item.ItemSpec;
					var trimMode = item.GetMetadata ("TrimMode");
					if (String.IsNullOrEmpty (trimMode))
						continue;
					AssemblyAction expectedAction = (AssemblyAction) Enum.Parse (typeof (AssemblyAction), trimMode, ignoreCase: true);
					AssemblyAction actualAction = (AssemblyAction) context.Actions[Path.GetFileNameWithoutExtension (assemblyPath)];
					Assert.Equal (expectedAction, actualAction);
				}
			}
		}

		[Fact]
		public void TestAssemblyPathsWithInvalidAction ()
		{
			var task = new MockTask () {
				AssemblyPaths = new ITaskItem[] { new TaskItem ("Assembly.dll", new Dictionary<string, string> { { "TrimMode", "invalid" } }) }
			};
			Assert.Throws<ArgumentException> (() => task.CreateDriver ());
		}

		// the InlineData string [] parameters are wrapped in object [] as described in https://github.com/xunit/xunit/issues/2060

		[Theory]
		[InlineData (new object[] { new string[] { "path/to/Assembly.dll" } })]
		[InlineData (new object[] { new string[] { "path with/spaces/Assembly.dll" } })]
		[InlineData (new object[] { new string[] { "path/to/Assembly With Spaces.dll" } })]
		public void TestReferenceAssemblyPaths (string[] referenceAssemblyPaths)
		{
			var task = new MockTask () {
				ReferenceAssemblyPaths = referenceAssemblyPaths.Select (p => new TaskItem (p)).ToArray ()
			};
			using (var driver = task.CreateDriver ()) {
				var expectedReferences = referenceAssemblyPaths;
				var actualReferences = driver.GetReferenceAssemblies ();
				Assert.Equal (expectedReferences.OrderBy (a => a), actualReferences.OrderBy (a => a));
				foreach (var reference in expectedReferences) {
					var referenceName = Path.GetFileNameWithoutExtension (reference);
					var actualAction = driver.Context.Actions[referenceName];
					Assert.Equal (AssemblyAction.Skip, actualAction);
				}
			}
		}

		[Theory]
		[InlineData (new object[] { new string[] { "AssemblyName" } })]
		public void TestRootAssemblyNames (string[] rootAssemblyNames)
		{
			var task = new MockTask () {
				RootAssemblyNames = rootAssemblyNames.Select (a => new TaskItem (a)).ToArray ()
			};
			using (var driver = task.CreateDriver ()) {
				var expectedRoots = rootAssemblyNames;
				var actualRoots = driver.GetRootAssemblies ();
				Assert.Equal (rootAssemblyNames.OrderBy (r => r), actualRoots.OrderBy (r => r));
			}
		}

		[Theory]
		[InlineData ("path/to/directory")]
		[InlineData ("path with/spaces")]
		public void TestOutputDirectory (string outputDirectory)
		{
			var task = new MockTask () {
				OutputDirectory = new TaskItem (outputDirectory)
			};
			using (var driver = task.CreateDriver ()) {
				var actualOutputDirectory = driver.Context.OutputDirectory;
				Assert.Equal (outputDirectory, actualOutputDirectory);
			}
		}

		[Theory]
		[InlineData (new object[] { new string[] { "path/to/descriptor.xml" } })]
		[InlineData (new object[] { new string[] { "path with/spaces/descriptor.xml" } })]
		[InlineData (new object[] { new string[] { "descriptor with spaces.xml" } })]
		[InlineData (new object[] { new string[] { "descriptor1.xml", "descriptor2.xml" } })]
		public void TestRootDescriptorFiles (string[] rootDescriptorFiles)
		{
			var task = new MockTask () {
				RootDescriptorFiles = rootDescriptorFiles.Select (f => new TaskItem (f)).ToArray ()
			};
			using (var driver = task.CreateDriver ()) {
				var actualDescriptors = driver.GetRootDescriptors ();
				Assert.Equal (rootDescriptorFiles.OrderBy (f => f), actualDescriptors.OrderBy (f => f));
			}
		}

		public static IEnumerable<object[]> OptimizationsCases ()
		{
			foreach (var optimization in MockTask.OptimizationNames) {
				yield return new object[] { optimization, true };
				yield return new object[] { optimization, false };
			}
		}

		[Theory]
		[MemberData (nameof (OptimizationsCases))]
		public void TestGlobalOptimizations (string optimization, bool enabled)
		{
			var task = new MockTask ();
			task.SetOptimization (optimization, enabled);
			// get the corresponding CodeOptimizations value
			using (var driver = task.CreateDriver ()) {
				Assert.True (driver.GetOptimizationName (optimization, out CodeOptimizations codeOptimizations));
				var actualValue = driver.Context.Optimizations.IsEnabled (codeOptimizations, assemblyName: null);
				Assert.Equal (enabled, actualValue);
			}
		}

		public static IEnumerable<object[]> PerAssemblyOptimizationsCases ()
		{
			// test that we can individually enable/disable each optimization
			foreach (var optimization in MockTask.OptimizationNames) {
				yield return new object[] {
					new ITaskItem [] {
						new TaskItem ("path/to/Assembly.dll", new Dictionary<string, string> {
							{ optimization, "True" }
						})
					}
				};
				yield return new object[] {
					new ITaskItem [] {
						new TaskItem ("path/to/Assembly.dll", new Dictionary<string, string> {
							{ optimization, "False" }
						})
					}
				};
			}
			// complex case with multiple optimizations, assemblies
			yield return new object[] {
				new ITaskItem [] {
					new TaskItem ("path/to/Assembly1.dll", new Dictionary<string, string> {
						{ "Sealer", "True" },
						{ "BeforeFieldInit", "False" }
					}),
					new TaskItem ("path/to/Assembly2.dll", new Dictionary<string, string> {
						{ "Sealer", "False" },
						{ "BeforeFieldInit", "True" }
					})
				}
			};
		}

		[Theory]
		[MemberData (nameof (PerAssemblyOptimizationsCases))]
		public void TestPerAssemblyOptimizations (ITaskItem[] assemblyPaths)
		{
			var task = new MockTask () {
				AssemblyPaths = assemblyPaths
			};
			using (var driver = task.CreateDriver ()) {
				foreach (var item in assemblyPaths) {
					var assemblyName = Path.GetFileNameWithoutExtension (item.ItemSpec);
					foreach (var optimization in MockTask.OptimizationNames) {
						Assert.True (driver.GetOptimizationName (optimization, out CodeOptimizations codeOptimizations));
						var optimizationValue = item.GetMetadata (optimization);
						if (String.IsNullOrEmpty (optimizationValue))
							continue;
						var enabled = Boolean.Parse (optimizationValue);
						var actualValue = driver.Context.Optimizations.IsEnabled (codeOptimizations, assemblyName: assemblyName);
						Assert.Equal (enabled, actualValue);
					}
				}
			}
		}

		[Fact]
		public void TestInvalidPerAssemblyOptimizations ()
		{
			var task = new MockTask () {
				AssemblyPaths = new ITaskItem[] {
					new TaskItem ("path/to/Assembly.dll", new Dictionary<string, string> {
						{ "Sealer", "invalid" }
					})
				}
			};
			Assert.Throws<ArgumentException> (() => task.CreateDriver ());
		}

		[Fact]
		public void TestOptimizationsDefaults ()
		{
			var task = new MockTask ();
			using (var driver = task.CreateDriver ()) {
				var expectedOptimizations = driver.GetDefaultOptimizations ();
				var actualOptimizations = driver.Context.Optimizations.Global;
				Assert.Equal (expectedOptimizations, actualOptimizations);
			}
		}

		[Fact]
		public void CheckGlobalOptimizationsMatchPerAssemblyOptimizations ()
		{
			var task = new MockTask ();
			var optimizationMetadataNames = MockTask.OptimizationNames;
			var optimizationPropertyNames = MockTask.GetOptimizationPropertyNames ();
			Assert.Equal (optimizationMetadataNames.OrderBy (o => o), optimizationPropertyNames.OrderBy (o => o));
		}

		[Theory]
		[InlineData ("IL2001;IL2002;IL2003;IL2004", 4)]
		[InlineData ("IL2001 IL2002 IL2003 IL2004", 4)]
		[InlineData ("IL2001,IL2002,IL2003,IL2004", 4)]
		[InlineData ("IL2001,IL2002;IL2003 IL2004", 4)]
		[InlineData ("IL2001,IL2002,IL8000,IL1003", 4)]
		[InlineData ("IL20000,IL02000", 2)]
		public void TestValidNoWarn (string noWarn, int validNoWarns)
		{
			var task = new MockTask () {
				NoWarn = noWarn
			};
			using (var driver = task.CreateDriver ()) {
				var actualUsedNoWarns = driver.Context.NoWarn;
				Assert.Equal (validNoWarns, actualUsedNoWarns.Count);
			}
		}

		[Theory]
		[InlineData ("0", WarnVersion.ILLink0)]
		[InlineData ("5", WarnVersion.ILLink5)]
		[InlineData ("6", (WarnVersion) 6)]
		[InlineData ("9999", WarnVersion.Latest)]
		public void TestWarn (string warnArg, WarnVersion expectedVersion)
		{
			var task = new MockTask () {
				Warn = warnArg
			};
			using (var driver = task.CreateDriver ()) {
				Assert.Equal (expectedVersion, driver.Context.WarnVersion);
			}
		}

#nullable enable
		[Theory]
		[InlineData (true, null, null, new int[] { }, new int[] { })]
		[InlineData (false, "IL1001,IL2000,IL2054,IL2022", null,
			new int[] { 1001, 2000, 2054, 2022 }, new int[] { })]
		[InlineData (false, "IL2023,IL6000;IL5042 IL2040", "IL4000,IL4001;IL4002 IL4003",
			new int[] { 2023, 2040, 5042, 6000 }, new int[] { 4000, 4001, 4002, 4003 })]
		[InlineData (false, "IL3000;IL3000;ABCD", "IL2005 IL3005 IL2005",
			new int[] { 3000 }, new int[] { 2005, 3005 })]
		[InlineData (true, null, "IL2067", new int[] { }, new int[] { 2067 })]
		[InlineData (true, "IL2001", "IL2001", new int[] { }, new int[] { 2001 })]
		public void TestWarningsAsErrors (bool treatWarningsAsErrors, string? warningsAsErrors, string? warningsNotAsErrors, int[] warnAsError, int[] warnNotAsError)
		{
			var task = new MockTask () {
				TreatWarningsAsErrors = treatWarningsAsErrors,
				WarningsAsErrors = warningsAsErrors,
				WarningsNotAsErrors = warningsNotAsErrors
			};

			using (var driver = task.CreateDriver ()) {
				var actualWarnAsError = driver.Context.WarnAsError;
				var actualGeneralWarnAsError = driver.Context.GeneralWarnAsError;
				Assert.Equal (warnAsError.Count () + warnNotAsError.Count (), actualWarnAsError.Count);
				Assert.Equal (treatWarningsAsErrors, actualGeneralWarnAsError);
				if (warnAsError.Length > 0) {
					foreach (var warningCode in warnAsError)
						Assert.True (actualWarnAsError.ContainsKey (warningCode) && actualWarnAsError[warningCode] == true);
				}

				if (warnNotAsError.Length > 0) {
					foreach (var warningCode in warnNotAsError)
						Assert.True (actualWarnAsError.ContainsKey (warningCode) && actualWarnAsError[warningCode] == false);
				}
			}
		}
#nullable disable

		public static IEnumerable<object[]> CustomDataCases => new List<object[]> {
			new object [] {
				new ITaskItem [] {
					new TaskItem ("DataName", new Dictionary<string, string> { { "Value", "DataValue" } })
				},
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem ("DataName", new Dictionary<string, string> { { "Value", "DataValue" } }),
					new TaskItem ("DataName", new Dictionary<string, string> { { "Value", "DataValue2" } })
				},
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem ("DataName1", new Dictionary<string, string> { { "Value", "DataValue1" } }),
					new TaskItem ("DataName2", new Dictionary<string, string> { { "Value", "DataValue2" } })
				},
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem ("DataName", new Dictionary<string, string> { { "Value", "data value with spaces" } })
				},
			},
		};

		[Theory]
		[MemberData (nameof (CustomDataCases))]
		public void TestCustomDta (ITaskItem[] customData)
		{
			var task = new MockTask () {
				CustomData = customData
			};
			using (var driver = task.CreateDriver ()) {
				var expectedCustomData = customData.Select (c => new { Key = c.ItemSpec, Value = c.GetMetadata ("Value") })
					.GroupBy (c => c.Key)
					.Select (c => c.Last ())
					.ToDictionary (c => c.Key, c => c.Value);
				var actualCustomData = driver.GetCustomData ();
				Assert.Equal (expectedCustomData, actualCustomData);
			}
		}

		public static IEnumerable<object[]> FeatureSettingsCases => new List<object[]> {
			new object [] {
				new ITaskItem [] {
					new TaskItem ("FeatureName", new Dictionary<string, string> { { "Value", "true" } })
				},
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem ("FeatureName", new Dictionary<string, string> { { "Value", "true" } }),
					new TaskItem ("FeatureName", new Dictionary<string, string> { { "Value", "false" } })
				},
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem ("FeatureName1", new Dictionary<string, string> { { "value", "true" } }),
					new TaskItem ("FeatureName2", new Dictionary<string, string> { { "value", "false" } }),
				},
			},
		};

		[Theory]
		[MemberData (nameof (FeatureSettingsCases))]
		public void TestFeatureSettings (ITaskItem[] featureSettings)
		{
			var task = new MockTask () {
				FeatureSettings = featureSettings
			};
			using (var driver = task.CreateDriver ()) {
				var expectedSettings = featureSettings.Select (f => new { Feature = f.ItemSpec, Value = f.GetMetadata ("Value") })
					.GroupBy (f => f.Feature)
					.Select (f => f.Last ())
					.ToDictionary (f => f.Feature, f => bool.Parse (f.Value));
				var actualSettings = driver.Context.FeatureSettings;
				Assert.Equal (expectedSettings, actualSettings);
			}
		}

		[Fact]
		public void TestInvalidFeatureSettings ()
		{
			var task = new MockTask () {
				FeatureSettings = new ITaskItem[] { new TaskItem ("FeatureName") }
			};
			Assert.Throws<ArgumentException> (() => task.CreateDriver ());
		}

		[Fact]
		public void TestExtraArgs ()
		{
			var task = new MockTask () {
				TrimMode = "copy",
				ExtraArgs = "-c link"
			};
			using (var driver = task.CreateDriver ()) {
				Assert.Equal (AssemblyAction.Copy, driver.Context.UserAction);
				// Check that ExtraArgs can override TrimMode
				Assert.Equal (AssemblyAction.Link, driver.Context.CoreAction);
			}
		}

		[Theory]
		[InlineData (true)]
		[InlineData (false)]
		public void TestDumpDependencies (bool dumpDependencies)
		{
			var task = new MockTask () {
				DumpDependencies = dumpDependencies
			};
			using (var driver = task.CreateDriver ()) {
				Assert.Equal (dumpDependencies, driver.GetDependencyRecorders ()?.Single () == MockXmlDependencyRecorder.Singleton);
			}
		}

		[Theory]
		[InlineData (true)]
		[InlineData (false)]
		public void TestRemoveSymbols (bool removeSymbols)
		{
			var task = new MockTask () {
				RemoveSymbols = removeSymbols
			};
			using (var driver = task.CreateDriver ()) {
				Assert.NotEqual (removeSymbols, driver.Context.LinkSymbols);
			}
		}

		[Fact]
		public void TestRemoveSymbolsDefault ()
		{
			var task = new MockTask ();
			using (var driver = task.CreateDriver ()) {
				Assert.False (driver.Context.LinkSymbols);
			}
		}

		[Theory]
		[InlineData ("copy")]
		[InlineData ("link")]
		[InlineData ("copyused")]
		public void TestGlobalTrimMode (string trimMode)
		{
			var task = new MockTask () {
				TrimMode = trimMode
			};
			using (var driver = task.CreateDriver ()) {
				var expectedAction = (AssemblyAction) Enum.Parse (typeof (AssemblyAction), trimMode, ignoreCase: true);
				Assert.Equal (expectedAction, driver.Context.CoreAction);
				Assert.Equal (expectedAction, driver.Context.UserAction);
			}
		}

		[Fact]
		public void TestInvalidDefaultAction ()
		{
			var task = new MockTask () {
				TrimMode = "invalid"
			};
			Assert.Throws<ArgumentException> (() => task.CreateDriver ());
		}

		public static IEnumerable<object[]> CustomStepsCases => new List<object[]> {
			new object [] {
				new ITaskItem [] {
					new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
						{ "Type", "ILLink.Tasks.Tests.MockCustomStep" }
					})
				},
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
						{ "Type", "ILLink.Tasks.Tests.MockCustomStep" },
						{ "BeforeStep", "MarkStep" }
					})
				},
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
						{ "type", "ILLink.Tasks.Tests.MockCustomStep" },
						{ "beforebtep", "MarkStep" }
					})
				},
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
						{ "Type", "ILLink.Tasks.Tests.MockCustomStep" },
						{ "AfterStep", "MarkStep" }
					})
				},
			},
			new object [] {
				new ITaskItem [] {
					new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
						{ "Type", "ILLink.Tasks.Tests.MockCustomStep" },
						{ "BeforeStep", "MarkStep" }
					}),
					new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
						{ "Type", "ILLink.Tasks.Tests.MockCustomStep" },
						{ "AfterStep", "MarkStep" }
					})
				},
			}
		};

		[Theory]
		[MemberData (nameof (CustomStepsCases))]
		public void TestCustomSteps (ITaskItem[] customSteps)
		{
			var task = new MockTask () {
				CustomSteps = customSteps
			};
			using (var driver = task.CreateDriver ()) {
				foreach (var customStep in customSteps) {
					var stepType = customStep.GetMetadata ("Type");
					var stepName = stepType.Substring (stepType.LastIndexOf (Type.Delimiter) + 1);
					var beforeStepName = customStep.GetMetadata ("BeforeStep");
					var afterStepName = customStep.GetMetadata ("AfterStep");
					Assert.True (String.IsNullOrEmpty (beforeStepName) || String.IsNullOrEmpty (afterStepName));

					var actualStepNames = driver.Context.Pipeline.GetSteps ().Select (s => s.GetType ().Name);
					if (!String.IsNullOrEmpty (beforeStepName)) {
						Assert.Contains (beforeStepName, actualStepNames);
						Assert.Equal (stepName, actualStepNames.TakeWhile (s => s != beforeStepName).Last ());
					} else if (!String.IsNullOrEmpty (afterStepName)) {
						Assert.Contains (afterStepName, actualStepNames);
						Assert.Equal (stepName, actualStepNames.SkipWhile (s => s != afterStepName).ElementAt (1));
					} else {
						Assert.Equal (stepName, actualStepNames.Last ());
					}
				}
			}
		}

		[Fact]
		public void TestCustomStepsWithBeforeAndAfterSteps ()
		{
			var customSteps = new ITaskItem[] {
				new TaskItem (Assembly.GetExecutingAssembly ().Location, new Dictionary<string, string> {
					{ "Type", "ILLink.Tasks.Tests.MockCustomStep" },
					{ "BeforeStep", "MarkStep" },
					{ "AfterStep", "MarkStep" }
				})
			};
			var task = new MockTask () {
				CustomSteps = customSteps
			};
			Assert.Throws<ArgumentException> (() => task.CreateDriver ());
		}

		[Fact]
		public void TestCustomStepsMissingType ()
		{
			var customSteps = new ITaskItem[] {
				new TaskItem (Assembly.GetExecutingAssembly ().Location)
			};
			var task = new MockTask () {
				CustomSteps = customSteps
			};
			Assert.Throws<ArgumentException> (() => task.CreateDriver ());
		}

		[Fact]
		public void TestUnhandledException ()
		{
			var task = new MockTask () {
				RootAssemblyNames = new ITaskItem[] { new TaskItem ("MissingAssembly.dll") }
			};
			task.BuildEngine = new MockBuildEngine ();
			task.Execute ();
			Assert.Contains (task.Messages, message =>
				message.Importance == MessageImportance.High &&
				message.Line.Contains ("Unable to find 'MissingAssembly.dll.dll'"));
		}
	}
}
