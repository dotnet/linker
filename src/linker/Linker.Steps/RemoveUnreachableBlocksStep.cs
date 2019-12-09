using System;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System.Collections.Generic;

namespace Mono.Linker.Steps
{
	//
	// This steps evaluates simple properties or methods for constant expressions and
	// then uses this information to remove unreachable conditional blocks. It does
	// not do any inlining-like code changes.
	//
	public class RemoveUnreachableBlocksStep : BaseStep
	{
		Dictionary<MethodDefinition, Instruction> constExprMethods;
		MethodDefinition IntPtrSize, UIntPtrSize;

		protected override void Process ()
		{
			var assemblies = Context.Annotations.GetAssemblies ().ToArray ();

			constExprMethods = new Dictionary<MethodDefinition, Instruction> ();
			foreach (var assembly in assemblies) {
				FindConstantExpressionsMethods (assembly.MainModule.Types);
			}

			if (constExprMethods.Count == 0)
				return;

			int constExprMethodsCount;
			do {
				//
				// Body rewriting can produce more methods with constant expression
				//
				constExprMethodsCount = constExprMethods.Count;

				foreach (var assembly in assemblies) {
					if (Annotations.GetAction (assembly) != AssemblyAction.Link)
						continue;

					RewriteBodies (assembly.MainModule.Types);
				}
			} while (constExprMethodsCount < constExprMethods.Count);
		}

		void FindConstantExpressionsMethods (Collection<TypeDefinition> types)
		{
			foreach (var type in types) {
				if (type.IsInterface)
					continue;

				if (!type.HasMethods)
					continue;

				foreach (var method in type.Methods) {
					if (!method.HasBody)
						continue;

					if (method.ReturnType.MetadataType == MetadataType.Void)
						continue;

					switch (Annotations.GetAction (method)) {
					case MethodAction.ConvertToThrow:
						continue;
					case MethodAction.ConvertToStub:
						var instruction = CodeRewriterStep.StubMethodWithConstant (Context, method);
						if (instruction != null)
							constExprMethods [method] = instruction;

						continue;
					}

					if (method.IsIntrinsic ())
						continue;

					if (constExprMethods.ContainsKey (method))
						continue;

					var analyzer = new ConstantExpressionMethodAnalyzer (method);
					if (analyzer.Analyze ()) {
						constExprMethods [method] = analyzer.Result;
					}
				}

				if (type.HasNestedTypes)
					FindConstantExpressionsMethods (type.NestedTypes);
			}
		}

		void RewriteBodies (Collection<TypeDefinition> types)
		{
			foreach (var type in types) {
				if (type.IsInterface)
					continue;

				if (!type.HasMethods)
					continue;

				foreach (var method in type.Methods) {
					if (!method.HasBody)
						continue;

					RewriteBody (method);
				}

				if (type.HasNestedTypes)
					RewriteBodies (type.NestedTypes);
			}
		}

		void RewriteBody (MethodDefinition method)
		{
			var reducer = new BodyReducer (method.Body);

			//
			// Temporary inlines any calls which return contant expression
			//
			if (!TryInlineBodyDependencies (ref reducer))
				return;

			//
			// This is the main step which evaluates if inlined calls can
			// produce folded branches. When it finds them the unreachable
			// branch is replaced with nops.
			//
			if (!reducer.RewriteBody ())
				return;

			Context.LogMessage (MessageImportance.Low, $"Reduced '{reducer.InstructionsReplaced}' instructions in conditional branches for [{method.DeclaringType.Module.Assembly.Name}] method {method.FullName}");

			if (method.ReturnType.MetadataType == MetadataType.Void)
				return;

			//
			// Re-run the analyzer in case body change rewrote it to constant expression
			//
			var analyzer = new ConstantExpressionMethodAnalyzer (method, reducer.FoldedInstructions);
			if (analyzer.Analyze ()) {
				constExprMethods [method] = analyzer.Result;
			}
		}

		bool TryInlineBodyDependencies (ref BodyReducer reducer)
		{
			bool changed = false;
			var instructions = reducer.Body.Instructions;
			Instruction targetResult;

			for (int i = 0; i < instructions.Count; ++i) {
				var instr = instructions [i];
				switch (instr.OpCode.Code) {

				case Code.Call:
					var target = (MethodReference)instr.Operand;
					var md = target.Resolve ();
					if (md == null)
						break;

					if (!md.IsStatic)
						break;

					if (!constExprMethods.TryGetValue (md, out targetResult))
						break;

					if (md.HasParameters)
						break;

					reducer.Rewrite (i, targetResult);
					changed = true;
					break;

				case Code.Sizeof:
					//
					// sizeof (IntPtr) and sizeof (UIntPtr) are just aliases for IntPtr.Size and UIntPtr.Size
					// which are simple static properties commonly overwritten. Instead of forcing C# code style
					// we handle both via static Size property
					//
					MethodDefinition sizeOfImpl = null;

					var operand = (TypeReference) instr.Operand;
					if (operand.MetadataType == MetadataType.UIntPtr) {
						sizeOfImpl = UIntPtrSize ?? (UIntPtrSize = FindSizeMethod (operand.Resolve ()));
					}

					if (operand.MetadataType == MetadataType.IntPtr) {
						sizeOfImpl = IntPtrSize ?? (IntPtrSize = FindSizeMethod (operand.Resolve ()));
					}

					if (sizeOfImpl != null && constExprMethods.TryGetValue (sizeOfImpl, out targetResult)) {
						reducer.Rewrite (i, targetResult);
						changed = true;
					}

					break;
				}
			}

			return changed;
		}

		static MethodDefinition FindSizeMethod (TypeDefinition type)
		{
			if (type == null)
				return null;

			return type.Methods.First (l => !l.HasParameters && l.IsStatic && l.Name == "get_Size");
		}

		struct BodyReducer
		{
			Dictionary<Instruction, int> mapping;

			//
			// Sorted list of body instruction indexes which were
			// replaced pass-through nop
			// 
			List<int> conditionInstrsToRemove;

			public BodyReducer (MethodBody body)
			{
				Body = body;
				FoldedInstructions = null;
				mapping = null;
				conditionInstrsToRemove = null;
				InstructionsReplaced = 0;
			}

			public MethodBody Body { get; }

			public int InstructionsReplaced { get; set; }

			public Collection<Instruction> FoldedInstructions { get; private set; }

			public void Rewrite (int index, Instruction newInstruction)
			{
				if (FoldedInstructions == null) {
					FoldedInstructions = new Collection<Instruction> (Body.Instructions);
					mapping = new Dictionary<Instruction, int> ();
				}

				// Tracks mapping for replaced instructions for easier
				// branch targets resolution later
				mapping [Body.Instructions [index]] = index;

				FoldedInstructions [index] = newInstruction;
			}

			void RewriteConditionToNop (int index)
			{
				if (conditionInstrsToRemove == null)
					conditionInstrsToRemove = new List<int> ();

				conditionInstrsToRemove.Add (index);
				RewriteToNop (index);
			}

			void RewriteToNop (int index)
			{
				Rewrite (index, Instruction.Create (OpCodes.Nop));
			}

			public bool RewriteBody ()
			{
				if (FoldedInstructions == null)
					return false;

				if (!RemoveConditions ())
					return false;

				var reachableMap = GetReachableInstructionsMap ();
				if (reachableMap == null)
					return false;

				bool changed = false;
				var instrs = Body.Instructions;
				for (int i = 0; i < instrs.Count; ++i) {
					if (reachableMap [i])
						continue;

					if (instrs [i].OpCode.Code == Code.Nop)
						continue;

					instrs [i] = Instruction.Create (OpCodes.Nop);
					changed = true;
					InstructionsReplaced++;
				}

				//
				// Process list of conditional jump which should be removed. They cannot be
				// replaced with nops as they alter the stack
				//
				if (conditionInstrsToRemove != null) {
					int bodyExpansion = 0;

					foreach (int instrIndex in conditionInstrsToRemove) {
						var index = instrIndex + bodyExpansion;
						var instr = instrs [index];

						if (instr.OpCode == OpCodes.Nop)
							continue;

						switch (instr.OpCode.StackBehaviourPop) {
						case StackBehaviour.Pop1_pop1:

							InstructionsReplaced += 2;

							//
							// One of the operands is most likely constant and could just be removed instead of additional pop
							//
							if (index > 0 && IsSideEffectFreeLoad (instrs [index - 1])) {
								instrs [index - 1] = Instruction.Create (OpCodes.Pop);
								instrs [index] = Instruction.Create (OpCodes.Nop);
							} else {
								instrs [index] = Instruction.Create (OpCodes.Pop);
								instrs.Insert (index, Instruction.Create (OpCodes.Pop));

								//
								// conditionInstrsToRemove is always sorted and instead of
								// increasing remaining indexes we introduce index delta value
								//
								bodyExpansion++;
							}
							break;
						case StackBehaviour.Popi:
							instrs [index] = Instruction.Create (OpCodes.Pop);
							InstructionsReplaced++;
							break;
						default:
							// Should never be reachedc
							throw new NotImplementedException ();
						}
					}

					changed = true;
				}

				return changed;
			}

			bool RemoveConditions ()
			{
				bool changed = false;
				object left, right;

				//
				// Finds any branchable instruction and checks if the operand or operands
				// can be evaluated as constant result.
				//
				// The logic does not remove any instructions but replaces them with nops for
				// easier processing later (makes the mapping straigh-forward).
				//
				for (int i = 0; i < FoldedInstructions.Count; ++i) {
					var instr = FoldedInstructions [i];
					var opcode = instr.OpCode;

					if (opcode.FlowControl == FlowControl.Cond_Branch) {
						if (opcode.StackBehaviourPop == StackBehaviour.Pop1_pop1) {
							if (!GetOperandsConstantValues (i, out left, out right))
								continue;

							if (left is int lint && right is int rint) {
								RewriteToNop (i - 2);
								RewriteToNop (i - 1);

								if (IsComparisonAlwaysTrue (opcode, lint, rint)) {
									Rewrite (i, Instruction.Create (OpCodes.Br, (Instruction)instr.Operand));
								} else {
									RewriteConditionToNop (i);
								}

								changed = true;
								continue;
							}

							continue;
						}

						if (opcode.StackBehaviourPop == StackBehaviour.Popi) {
							if (i > 0 && GetConstantValue (FoldedInstructions [i - 1], out var operand)) {
								if (operand is int opint) {
									RewriteToNop (i - 1);

									if (IsConstantBranch (opcode, opint)) {
										Rewrite (i, Instruction.Create (OpCodes.Br, (Instruction)instr.Operand));
									} else {
										RewriteConditionToNop (i);
									}

									changed = true;
									continue;
								}

								if (operand is null && (opcode.Code == Code.Brfalse || opcode.Code == Code.Brfalse_S)) {
									RewriteToNop (i - 1);
									Rewrite (i, Instruction.Create (OpCodes.Br, (Instruction)instr.Operand));
									changed = true;
									continue;
								}
							}

							// Common pattern generated by C# compiler in debug mode
							if (i > 3 && GetConstantValue (FoldedInstructions [i - 3], out operand) && operand is int opint2 && IsPairedStlocLdloc (FoldedInstructions [i - 2], FoldedInstructions [i - 1])) {
								RewriteToNop (i - 3);
								RewriteToNop (i - 2);
								RewriteToNop (i - 1);

								if (IsConstantBranch (opcode, opint2)) {
									Rewrite (i, Instruction.Create (OpCodes.Br, (Instruction)instr.Operand));
								} else {
									RewriteConditionToNop (i);
								}

								changed = true;
								continue;
							}

							continue;
						}

						throw new NotImplementedException ();
					}

					// Mode special for csc in debug mode
					switch (instr.OpCode.Code) {
					case Code.Ceq:
					case Code.Clt:
					case Code.Cgt:
						if (!GetOperandsConstantValues (i, out left, out right))
							continue;

						if (left is int lint && right is int rint) {
							RewriteToNop (i - 2);
							RewriteToNop (i - 1);

							if (IsComparisonAlwaysTrue (instr.OpCode, lint, rint)) {
								Rewrite (i, Instruction.Create (OpCodes.Ldc_I4_1));
							} else {
								Rewrite (i, Instruction.Create (OpCodes.Ldc_I4_0));
							}

							changed = true;
						}

						break;

					case Code.Cgt_Un:
						if (!GetOperandsConstantValues (i, out left, out right))
							continue;

						if (left == null && right == null) {
							Rewrite (i, Instruction.Create (OpCodes.Ldc_I4_0));
						}

						changed = true;
						break;
					}
				}

				return changed;
			}

			BitArray GetReachableInstructionsMap ()
			{
				var reachable = new BitArray (FoldedInstructions.Count);

				Stack<int> conditional = null;

				//
				// Mark all handlers all the time as we are not interested in optimizing handlers 
				//
				if (Body.HasExceptionHandlers) {
					conditional = new Stack<int> ();
					foreach (var handler in Body.ExceptionHandlers) {
						conditional.Push (GetInstructionIndex (handler.HandlerStart));

						if (handler.FilterStart != null)
							conditional.Push (GetInstructionIndex (handler.FilterStart));
					}
				}

				Instruction target;
				int i = 0;
				while (true) {
					while (i < FoldedInstructions.Count) {
						if (reachable [i])
							break;

						reachable [i] = true;
						var instr = FoldedInstructions [i++];

						switch (instr.OpCode.FlowControl) {
						case FlowControl.Branch:
							target = (Instruction)instr.Operand;
							i = GetInstructionIndex (target);
							continue;

						case FlowControl.Cond_Branch:
							if (conditional == null)
								conditional = new Stack<int> ();

							switch (instr.Operand) {
							case Instruction starget:
								conditional.Push (GetInstructionIndex (starget));
								continue;
							case Instruction[] mtargets:
								foreach (var t in mtargets)
									conditional.Push (GetInstructionIndex (t));
								continue;
							default:
								throw new NotImplementedException ();
							}

						case FlowControl.Next:
						case FlowControl.Call:
						case FlowControl.Meta:
							continue;

						case FlowControl.Return:
						case FlowControl.Throw:
							break;

						default:
							throw new NotImplementedException ();
						}

						break;
					}

					if (conditional?.Count > 0) {
						i = conditional.Pop ();
						continue;
					}

					return reachable;
				}
			}

			//
			// Returns index of instruction in folded instruction body
			//
			int GetInstructionIndex (Instruction instruction)
			{
				int idx;
				if (mapping.TryGetValue (instruction, out idx))
					return idx;

				idx = FoldedInstructions.IndexOf (instruction);
				Debug.Assert (idx >= 0);
				return idx;
			}

			bool GetOperandsConstantValues (int index, out object left, out object right)
			{
				left = default;
				right = default;

				if (index < 2)
					return false;

				return GetConstantValue (FoldedInstructions [index - 2], out left) &&
					GetConstantValue (FoldedInstructions [index - 1], out right);
			}

			static bool GetConstantValue (Instruction instruction, out object value)
			{
				switch (instruction.OpCode.Code) {
				case Code.Ldc_I4_0:
					value = 0;
					return true;
				case Code.Ldc_I4_1:
					value = 1;
					return true;
				case Code.Ldc_I4_2:
					value = 2;
					return true;
				case Code.Ldc_I4_3:
					value = 3;
					return true;
				case Code.Ldc_I4_4:
					value = 4;
					return true;
				case Code.Ldc_I4_5:
					value = 5;
					return true;
				case Code.Ldc_I4_6:
					value = 6;
					return true;
				case Code.Ldc_I4_7:
					value = 7;
					return true;
				case Code.Ldc_I4_8:
					value = 8;
					return true;
				case Code.Ldc_I4_M1:
					value = -1;
					return true;
				case Code.Ldc_I4:
					value = (int)instruction.Operand;
					return true;
				case Code.Ldc_I4_S:
					value = (int)(sbyte)instruction.Operand;
					return true;
				case Code.Ldc_I8:
					value = (long)instruction.Operand;
					return true;
				case Code.Ldnull:
					value = null;
					return true;
				default:
					value = null;
					return false;
				}
			}

			static bool IsPairedStlocLdloc (Instruction first, Instruction second)
			{
				switch (first.OpCode.Code) {
				case Code.Stloc_0:
					return second.OpCode.Code == Code.Ldloc_0;
				case Code.Stloc_1:
					return second.OpCode.Code == Code.Ldloc_1;
				case Code.Stloc_2:
					return second.OpCode.Code == Code.Ldloc_2;
				case Code.Stloc_3:
					return second.OpCode.Code == Code.Ldloc_3;
				case Code.Stloc_S:
				case Code.Stloc:
					if (second.OpCode.Code == Code.Ldloc_S || second.OpCode.Code == Code.Ldloc)
						return ((VariableDefinition)first.Operand).Index == ((VariableDefinition)second.Operand).Index;

					break;
				}

				return false;
			}

			static bool IsSideEffectFreeLoad (Instruction instr)
			{
				switch (instr.OpCode.Code) {
				case Code.Ldarg:
				case Code.Ldloc:
				case Code.Ldloc_0:
				case Code.Ldloc_1:
				case Code.Ldloc_2:
				case Code.Ldloc_3:
				case Code.Ldloc_S:
				case Code.Ldc_I4_0:
				case Code.Ldc_I4_1:
				case Code.Ldc_I4_2:
				case Code.Ldc_I4_3:
				case Code.Ldc_I4_4:
				case Code.Ldc_I4_5:
				case Code.Ldc_I4_6:
				case Code.Ldc_I4_7:
				case Code.Ldc_I4_8:
				case Code.Ldc_I4:
				case Code.Ldc_I4_S:
				case Code.Ldc_I4_M1:
				case Code.Ldc_I8:
				case Code.Ldc_R4:
				case Code.Ldc_R8:
				case Code.Ldnull:
				case Code.Ldstr:
					return true;
				}

				return false;
			}

			static bool IsComparisonAlwaysTrue (OpCode opCode, int left, int right)
			{
				switch (opCode.Code) {
				case Code.Beq:
				case Code.Beq_S:
				case Code.Ceq:
					return left == right;
				case Code.Bne_Un:
				case Code.Bne_Un_S:
					return left != right;
				case Code.Bge:
				case Code.Bge_S:
				case Code.Bge_Un:
				case Code.Bge_Un_S:
					return left >= right;
				case Code.Bgt:
				case Code.Bgt_S:
				case Code.Bgt_Un:
				case Code.Bgt_Un_S:
				case Code.Cgt:
					return left > right;
				case Code.Ble:
				case Code.Ble_S:
				case Code.Ble_Un:
				case Code.Ble_Un_S:
					return left <= right;
				case Code.Blt:
				case Code.Blt_S:
				case Code.Blt_Un:
				case Code.Blt_Un_S:
				case Code.Clt:
					return left < right;
				}

				throw new NotImplementedException (opCode.ToString ());
			}

			static bool IsConstantBranch (OpCode opCode, int operand)
			{
				switch (opCode.Code) {
				case Code.Brfalse:
				case Code.Brfalse_S:
					return operand == 0;
				case Code.Brtrue:
				case Code.Brtrue_S:
					return operand != 0;
				}

				throw new NotImplementedException (opCode.ToString ());
			}
		}

		struct ConstantExpressionMethodAnalyzer
		{
			readonly MethodDefinition method;
			readonly Collection<Instruction> instructions;

			Stack<Instruction> stack_instr;
			Dictionary<int, Instruction> locals;

			public ConstantExpressionMethodAnalyzer (MethodDefinition method)
			{
				this.method = method;
				instructions = method.Body.Instructions;
				stack_instr = null;
				locals = null;
				Result = null;
			}

			public ConstantExpressionMethodAnalyzer (MethodDefinition method, Collection<Instruction> instructions)
			{
				this.method = method;
				this.instructions = instructions;
				stack_instr = null;
				locals = null;
				Result = null;
			}

			public Instruction Result { get; private set; }

			public bool Analyze ()
			{
				var body = method.Body;
				if (body.HasExceptionHandlers)
					return false;

				VariableReference vr;
				Instruction jmpTarget = null;

				foreach (var instr in instructions) {
					if (jmpTarget != null) {
						if (instr != jmpTarget)
							continue;

						jmpTarget = null;
					}

					switch (instr.OpCode.Code) {
					case Code.Nop:
						continue;
					case Code.Pop:
						stack_instr.Pop ();
						continue;

					case Code.Br_S:
					case Code.Br:
						jmpTarget = (Instruction)instr.Operand;
						continue;

					case Code.Ldc_I4:
					case Code.Ldc_I4_S:
					case Code.Ldc_I4_0:
					case Code.Ldc_I4_1:
					case Code.Ldc_I4_2:
					case Code.Ldc_I4_3:
					case Code.Ldc_I4_4:
					case Code.Ldc_I4_5:
					case Code.Ldc_I4_6:
					case Code.Ldc_I4_7:
					case Code.Ldc_I4_8:
					case Code.Ldc_I4_M1:
					case Code.Ldc_I8:
					case Code.Ldnull:
					case Code.Ldstr:
					case Code.Ldtoken:
						PushOnStack (instr);
						continue;

					case Code.Ldloc_0:
						PushOnStack (locals [0]);
						continue;
					case Code.Ldloc_1:
						PushOnStack (locals [1]);
						continue;
					case Code.Ldloc_2:
						PushOnStack (locals [2]);
						continue;
					case Code.Ldloc_3:
						PushOnStack (locals [3]);
						continue;
					case Code.Ldloc:
					case Code.Ldloc_S:
						vr = (VariableReference)instr.Operand;
						PushOnStack (locals [vr.Index]);
						continue;

					case Code.Stloc_0:
						StoreToLocals (0);
						continue;
					case Code.Stloc_1:
						StoreToLocals (1);
						continue;
					case Code.Stloc_2:
						StoreToLocals (2);
						continue;
					case Code.Stloc_3:
						StoreToLocals (3);
						continue;
					case Code.Stloc_S:
					case Code.Stloc:
						vr = (VariableReference)instr.Operand;
						StoreToLocals (vr.Index);
						continue;

					// TODO: handle simple conversions
					//case Code.Conv_I:

					case Code.Ret:
						if (ConvertStackToResult ())
							return true;

						break;
					}

					return false;
				}

				return false;
			}

			bool ConvertStackToResult ()
			{
				if (stack_instr == null)
					return false;

				if (stack_instr.Count != 1)
					return false;

				var instr = stack_instr.Pop ();

				switch (instr.OpCode.Code) {
				case Code.Ldc_I4_0:
				case Code.Ldc_I4_1:
				case Code.Ldc_I4_2:
				case Code.Ldc_I4_3:
				case Code.Ldc_I4_4:
				case Code.Ldc_I4_5:
				case Code.Ldc_I4_6:
				case Code.Ldc_I4_7:
				case Code.Ldc_I4_8:
				case Code.Ldc_I4:
				case Code.Ldc_I4_S:
				case Code.Ldc_I4_M1:
				case Code.Ldc_I8:
				case Code.Ldnull:
				case Code.Ldstr:
					Result = instr;
					return true;
				}

				return false;
			}

			void PushOnStack (Instruction instruction)
			{
				if (stack_instr == null)
					stack_instr = new Stack<Instruction> ();

				stack_instr.Push (instruction);
			}

			void StoreToLocals (int index)
			{
				if (locals == null)
					locals = new Dictionary<int, Instruction> ();

				locals [index] = stack_instr.Pop ();
			}
		}
	}
}
