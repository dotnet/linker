// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Mono.Linker.Dataflow
{
	static class ScannerExtensions
	{
		public static bool IsControlFlowInstruction (in this OpCode opcode)
		{
			return opcode.FlowControl == FlowControl.Branch
				|| opcode.FlowControl == FlowControl.Cond_Branch
				|| (opcode.FlowControl == FlowControl.Return && opcode.Code != Code.Ret);
		}

		public static HashSet<int> ComputeBranchTargets (this MethodBody methodBody)
		{
			HashSet<int> branchTargets = new HashSet<int> ();
			foreach (Instruction operation in methodBody.Instructions) {
				if (!operation.OpCode.IsControlFlowInstruction ())
					continue;
				Object value = operation.Operand;
				if (value is Instruction inst) {
					branchTargets.Add (inst.Offset);
				} else if (value is Instruction[] instructions) {
					foreach (Instruction switchLabel in instructions) {
						branchTargets.Add (switchLabel.Offset);
					}
				}
			}
			foreach (ExceptionHandler einfo in methodBody.ExceptionHandlers) {
				if (einfo.HandlerType == ExceptionHandlerType.Filter) {
					branchTargets.Add (einfo.FilterStart.Offset);
				}
				branchTargets.Add (einfo.HandlerStart.Offset);
			}
			return branchTargets;
		}

		public static HashSet<int> GetInitialBasicBlockInstructions (this MethodBody methodBody)
		{
			var leaders = new HashSet<int> ();

			foreach (Instruction operation in methodBody.Instructions) {

				// first instruction is a leader
				if (leaders.Count == 0)
					leaders.Add (operation.Offset);

				// targets of control flow instructions are leaders
				if (operation.OpCode.IsControlFlowInstruction ()) {
					var jumpTargets = operation.GetJumpTargets ();

					foreach (var target in jumpTargets) {
						leaders.Add (target.Offset);
					}

					if (operation.OpCode.FlowControl == FlowControl.Cond_Branch && operation.Next != null) {
						leaders.Add (operation.Next.Offset);
					}
				}
			}

			return leaders;
		}

		public static IEnumerable<Instruction> GetJumpTargets (this Instruction operation)
		{
			Object value = operation.Operand;
			if (value is Instruction inst)
				return new Instruction[1] { inst };

			if (value is Instruction[] instructions) {
				return instructions;
			}

			return Array.Empty<Instruction> ();
		}
	}

}
