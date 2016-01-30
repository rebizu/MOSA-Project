// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Compiler.Framework.IR;
using Mosa.Compiler.MosaTypeSystem;
using Mosa.Compiler.Trace;

namespace Mosa.Compiler.Framework.Stages
{
	/// <summary>
	///
	/// </summary>
	public class PromoteLocalVariablesStage : BaseMethodCompilerStage
	{
		protected TraceLog trace;

		protected override void Run()
		{
			if (!HasCode)
				return;

			trace = CreateTraceLog();

			foreach (var local in MethodCompiler.LocalVariables)
			{
				if (local.IsVirtualRegister)
					continue;

				if (local.Definitions.Count != 1)
					continue;

				if (local.IsPinned)
					continue;

				if (!local.IsReferenceType && !local.IsInteger && !local.IsR && !local.IsChar && !local.IsBoolean && !local.IsPointer)
					continue;

				if (ContainsAddressOf(local))
					continue;

				Promote(local);
			}
		}

		protected bool ContainsAddressOf(Operand local)
		{
			foreach (var node in local.Uses)
			{
				if (node.Instruction == IRInstruction.AddressOf)
					return true;
			}

			return false;
		}

		protected void Promote(Operand local)
		{
			var stacktype = local.Type.GetStackType();

			var v = MethodCompiler.CreateVirtualRegister(stacktype);

			if (trace.Active) trace.Log("*** Replacing: " + local.ToString() + " with " + v.ToString());

			foreach (var node in local.Uses.ToArray())
			{
				for (int i = 0; i < node.OperandCount; i++)
				{
					var operand = node.GetOperand(i);

					if (local == operand)
					{
						if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
						node.SetOperand(i, v);

						if (node.Instruction == IRInstruction.ZeroExtendedMove)
						{
							node.Instruction = IRInstruction.Move;
							node.Size = InstructionSize.None;
						}

						if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
					}
				}
			}

			foreach (var node in local.Definitions.ToArray())
			{
				for (int i = 0; i < node.OperandCount; i++)
				{
					var operand = node.GetResult(i);

					if (local == operand)
					{
						if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
						node.SetResult(i, v);

						if (node.Instruction == IRInstruction.ZeroExtendedMove)
						{
							node.Instruction = IRInstruction.Move;
							node.Size = InstructionSize.None;
						}

						if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
					}
				}
			}
		}
	}
}
