using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	public class ExceptionalDataFlow
	{
		public static void Main ()
		{
			TryFlowsToFinally ();
			TryFlowsToAfterFinally ();

			TryFlowsToCatch ();

			CatchFlowsToFinally ();
			CatchFlowsToAfterTry ();
			CatchFlowsToAfterFinally ();

			FinallyFlowsToAfterFinally ();

			TryFlowsToMultipleCatchAndFinally ();
			NestedWithFinally ();
			ControlFlowsOutOfMultipleFinally ();
			NestedWithCatch ();

			CatchInTry ();
			CatchInTryWithFinally ();
			TestCatchesHaveSeparateState ();

			FinallyWithBranchToFirstBlock ();
			FinallyWithBranchToFirstBlockAndEnclosingTryCatchState ();
			CatchWithBranchToFirstBlock ();
			CatchWithBranchToFirstBlockAndReassignment ();
			CatchWithNonSimplePredecessor ();
			FinallyWithNonSimplePredecessor ();
			NestedFinallyWithNonSimplePredecessor ();
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void TryFlowsToFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			} finally {
				// methods/fields/properties
				RequireAll (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void TryFlowsToAfterFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			} finally { }
			// properties
			RequireAll (t);
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void TryFlowsToCatch ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			} catch {
				// methods/fields/properties
				RequireAll (t);
			}
		}


		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void CatchFlowsToFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			} finally {
				// methods/fields/properties
				RequireAll (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void CatchFlowsToAfterTry ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			}
			// methods/properties (not fields)
			RequireAll (t);
		}



		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void CatchFlowsToAfterFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			} finally { }
			// methods/properties, not fields
			RequireAll (t);
		}


		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void FinallyFlowsToAfterFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} finally {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
			}
			// properties only
			RequireAll (t);
		}

		public class Exception1 : Exception { }
		public class Exception2 : Exception { }


		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll4) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll4) + "(Type)", nameof (GetWithPublicFields) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll5) + "(Type)", nameof (GetWithPublicEvents) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll6) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll6) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll6) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll6) + "(Type)", nameof (GetWithPublicEvents) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll7) + "(Type)", nameof (GetWithPublicConstructors) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicConstructors) + "()")]


		public static void TryFlowsToMultipleCatchAndFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				RequireAll1 (t); // fields only
			} catch (Exception1) {
				RequireAll2 (t); // methods/fields
				t = GetWithPublicProperties ();
				RequireAll3 (t); // properties only
			} catch (Exception2) {
				RequireAll4 (t); // methods/fields
				t = GetWithPublicEvents ();
				RequireAll5 (t); // events only
			} finally {
				RequireAll6 (t); // methods/fields/properties/events
				t = GetWithPublicConstructors ();
				RequireAll7 (t); // ctors only
			}
			RequireAll (t);
		}


		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicConstructors) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicEvents) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicConstructors) + "()")]

		public static void NestedWithFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				try {
					// fields
					t = GetWithPublicProperties ();
				} finally {
					// fields/properties
					RequireAll1 (t);
					t = GetWithPublicEvents ();
					t = GetWithPublicConstructors ();
				}
				// ctors
				RequireAll2 (t);
			} finally {
				// methods/fields/properties/events/constructors
				RequireAll3 (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicEvents) + "()")]
		public static void ControlFlowsOutOfMultipleFinally ()
		{
			Type t = GetWithPublicMethods ();
			try {
				try {
					try {
						t = GetWithPublicFields ();
					} finally {
						// methods/fields
						RequireAll1 (t);
						t = GetWithPublicProperties ();
					}
				} finally {
					// methods/fields/properties
					RequireAll2 (t);
					t = GetWithPublicEvents ();
				}
			} finally {
				// methods/fields/propreties/events
				RequireAll3 (t);
			}
		}


		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicConstructors) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicEvents) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll3) + "(Type)", nameof (GetWithPublicConstructors) + "()")]

		public static void NestedWithCatch ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				try {
					// fields
					t = GetWithPublicProperties ();
				} catch {
					// fields/properties
					RequireAll1 (t);
					t = GetWithPublicEvents ();
					t = GetWithPublicConstructors ();
				}
				// properties/ctors
				RequireAll2 (t);
			} catch {
				// methods/fields/properties/events/constructors
				RequireAll3 (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()")]
		public static void CatchInTry ()
		{
			try {
				Type t = GetWithPublicMethods ();
				try {
				} catch {
					t = GetWithPublicFields ();
					RequireAll (t);
				}
			} catch {
			}
		}

		// This tests a case where the catch state was being merged with the containing try state incorrectly.
		// In the bug, the exceptional catch state, which is used in the finally, had too much in it.
		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicFields) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]
		// The bug was producing this warning:
		// [ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicConstructors) + "()")]
		public static void CatchInTryWithFinally ()
		{
			Type t = GetWithPublicConstructors ();
			try {
				t = GetWithPublicMethods ();
				// methods
				// ex: ctors/methods
				try {
					// methods
					// ex: methods
				} catch {
					// methods
					t = GetWithPublicFields ();
					// fields
					// ex: methods/fields
					RequireAll1 (t);
				} finally {
					// normal state: fields
					// exceptional state: methods/fields
					RequireAll2 (t);
				}
			} catch {
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]

		public static void TestCatchesHaveSeparateState ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch (Exception1) {
				t = GetWithPublicFields ();
			} catch (Exception2) {
				// methods only!
				RequireAll (t);
			} finally {
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()")]
		public static void FinallyWithBranchToFirstBlock ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} finally {
			FinallyStart:
				RequireAll (t);
				t = GetWithPublicFields ();
				goto FinallyStart;
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()")]
		public static void FinallyWithBranchToFirstBlockAndEnclosingTryCatchState ()
		{
			try {
				Type t = GetWithPublicProperties ();
				t = GetWithPublicMethods ();
				try {
				} finally {
				FinallyStart:
					// methods/fields
					RequireAll (t);
					t = GetWithPublicFields ();
					goto FinallyStart;
				}
			} finally {
				// An operation just to prevent optimizing away
				// the try/finally.
				_ = String.Empty;
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()")]
		public static void CatchWithBranchToFirstBlock ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch {
			CatchStart:
				RequireAll (t);
				t = GetWithPublicFields ();
				goto CatchStart;
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll) + "(Type)", nameof (GetWithPublicFields) + "()")]
		public static void CatchWithBranchToFirstBlockAndReassignment ()
		{
			Type t = GetWithPublicMethods ();
			try {
			} catch {
			CatchStart:
				RequireAll (t); // methods/fields, but not properties!
				t = GetWithPublicProperties ();
				t = GetWithPublicFields ();
				goto CatchStart;
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void CatchWithNonSimplePredecessor ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
				try {
					// properties only
				} catch {
					// properties only.
					RequireAll1 (t);
				}
			} catch {
				// methods/fields/properties
				RequireAll2 (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void FinallyWithNonSimplePredecessor ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
				try {
					// properties only
				} catch {
					// properties only.
					RequireAll1 (t);
				}
			} finally {
				// methods/fields/properties
				RequireAll2 (t);
			}
		}

		[ExpectedWarning ("IL2072", nameof (RequireAll1) + "(Type)", nameof (GetWithPublicProperties) + "()")]

		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicMethods) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicFields) + "()")]
		[ExpectedWarning ("IL2072", nameof (RequireAll2) + "(Type)", nameof (GetWithPublicProperties) + "()")]
		public static void NestedFinallyWithNonSimplePredecessor ()
		{
			Type t = GetWithPublicMethods ();
			try {
				t = GetWithPublicFields ();
				t = GetWithPublicProperties ();
				try {
					// properties only
				} finally {
					// properties only.
					RequireAll1 (t);
				}
			} finally {
				// methods/fields/properties
				RequireAll2 (t);
			}
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		public static Type GetWithPublicMethods ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		public static Type GetWithPublicFields ()
		{
			return null;
		}
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
		public static Type GetWithPublicProperties ()
		{
			return null;
		}
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)]
		public static Type GetWithPublicEvents ()
		{
			return null;
		}
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		public static Type GetWithPublicConstructors ()
		{
			return null;
		}

		public static void RequirePublicMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			Type type)
		{
		}

		public static void RequirePublicFields (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
			Type type)
		{
		}
		public static void RequirePublicProperties (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
			Type type)
		{
		}


		public static void RequireFieldsAndProperties (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
			Type type)
		{
		}

		public static void RequireAll (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll1 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll2 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll3 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll4 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll5 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll6 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
		public static void RequireAll7 (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
			Type type)
		{
		}
	}
}