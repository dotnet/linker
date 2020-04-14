using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.Build.Framework;
using Mono.Linker;
using Mono.Linker.Steps;
using Xunit;

namespace ILLink.Tasks.Tests
{
	public class MockTask : ILLink
	{
		public MockTask () {
			// Ensure that [Required] members are non-null
			AssemblyPaths = new ITaskItem [0];
			RootAssemblyNames = new ITaskItem [0];
		}

		public MockDriver CreateDriver () {
			string [] responseFileLines = GenerateResponseFileCommands ().Split (Environment.NewLine);
			var arguments = new Queue<string> ();
			Driver.ParseResponseFileLines (responseFileLines, arguments);
			return new MockDriver (arguments);
		}

		public static string [] OptimizationNames {
			get {
				var field = typeof (ILLink).GetField ("_optimizationNames", BindingFlags.NonPublic | BindingFlags.Static);
				var value = (string []) (field.GetValue (null));
				return value;
			}
		}

		public void SetOptimization (string optimization, bool enabled) {
			var property = typeof (ILLink).GetProperty (optimization);
			var setter = property.GetSetMethod ();
			property.GetSetMethod ().Invoke (this, new object [] { enabled });
		}

		static readonly string [] nonOptimizationBooleanProperties = new string [] {
			"DumpDependencies",
			"LinkSymbols"
		};

		public static IEnumerable<string> GetOptimizationPropertyNames () {
			foreach (var property in typeof (ILLink).GetProperties (BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)) {
				if (property.PropertyType != typeof (bool))
					continue;
				if (nonOptimizationBooleanProperties.Contains (property.Name))
					continue;
				yield return property.Name;
			}
		}
	}

	public class MockDriver : Driver
	{
		public MockDriver (Queue<string> arguments) : base (arguments) {
			// Always add a dummy root assembly for testing purposes (otherwise Driver fails without roots).
			arguments.Enqueue ("-a");
			arguments.Enqueue ("DummyRootAssembly");
			// Always set up the context early on.
			Assert.Equal (0, SetupContext ());
		}

		public LinkContext Context => context;

		public IEnumerable<string> GetRootAssemblies () {
			foreach (var step in context.Pipeline.GetSteps ()) {
				if (!(step is ResolveFromAssemblyStep))
					continue;

				var assemblyName = (string)(typeof (ResolveFromAssemblyStep).GetField ("_file", BindingFlags.NonPublic | BindingFlags.Instance).GetValue (step));
				if (assemblyName == "DummyRootAssembly")
					continue;
				
				yield return assemblyName;
			}
		}

		public IEnumerable<string> GetRootDescriptors () {
			foreach (var step in context.Pipeline.GetSteps ()) {
				if (!(step is ResolveFromXmlStep))
					continue;

				var descriptor = (string)(typeof (ResolveFromXmlStep).GetField ("_xmlDocumentLocation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue (step));

				yield return descriptor;
			}
		}

		public IEnumerable<string> GetReferenceAssemblies () {
			return (IEnumerable<string>)(typeof (AssemblyResolver).GetField ("_references", BindingFlags.NonPublic | BindingFlags.Instance).GetValue (context.Resolver));
		}

		protected override void AddResolveFromXmlStep (Pipeline pipeline, string file)
		{
			// Don't try to load an xml file - just pretend it exists.
			pipeline.PrependStep (new ResolveFromXmlStep (document: null, file));
		}

		protected override void AddXmlDependencyRecorder (LinkContext context, string file)
		{
			// Don't try to open the output file for writing - just pretend it exists.
			Context.Tracer.AddRecorder (MockXmlDependencyRecorder.Singleton);
		}

		public IEnumerable<IDependencyRecorder> GetDependencyRecorders () {
			return (IEnumerable<IDependencyRecorder>)(typeof (Tracer).GetField ("recorders", BindingFlags.NonPublic | BindingFlags.Instance).GetValue (context.Tracer));
		}

		public static bool GetOptimizationName (string optimization, out CodeOptimizations codeOptimizations) {
			var method =  typeof (Driver).GetMethod ("GetOptimizationName", BindingFlags.NonPublic | BindingFlags.Static);
			var parameters = new object [] { optimization, null };
			var ret = (bool) (method.Invoke (null, parameters));
			codeOptimizations = (CodeOptimizations) (parameters [1]);
			return ret;
		}

		public CodeOptimizations GetDefaultOptimizations () {
			var context = base.GetDefaultContext (null);
			return context.Optimizations.Global;
		}
	}

	public class MockXmlDependencyRecorder : IDependencyRecorder
	{
		public static MockXmlDependencyRecorder Singleton = new MockXmlDependencyRecorder ();
		public void RecordDependency (object source, object arget, bool marked) {}
		public void RecordDependency (object target, in DependencyInfo reason, bool marked) {}
	}

	public class MockCustomStep : IStep
	{
		public void Process (LinkContext context) {}
	}

}