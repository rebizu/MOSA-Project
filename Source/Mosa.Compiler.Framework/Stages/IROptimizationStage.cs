// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Compiler.Common;
using Mosa.Compiler.Framework.IR;
using Mosa.Compiler.MosaTypeSystem;
using Mosa.Compiler.Trace;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mosa.Compiler.Framework.Stages
{
	/// <summary>
	///
	/// </summary>
	public sealed class IROptimizationStage : BaseMethodCompilerStage
	{
		private int instructionsRemovedCount = 0;
		private int simplifyExtendedMoveWithConstantCount = 0;
		private int arithmeticSimplificationSubtractionCount = 0;
		private int arithmeticSimplificationMultiplicationCount = 0;
		private int arithmeticSimplificationDivisionCount = 0;
		private int arithmeticSimplificationAdditionAndSubstractionCount = 0;
		private int arithmeticSimplificationLogicalOperatorsCount = 0;
		private int arithmeticSimplificationShiftOperators = 0;
		private int simpleConstantPropagationCount = 0;
		private int simpleForwardCopyPropagationCount = 0;
		private int deadCodeEliminationCount = 0;
		private int reduceTruncationAndExpansionCount = 0;
		private int constantFoldingIntegerOperationsCount = 0;
		private int constantFoldingIntegerCompareCount = 0;
		private int constantFoldingAdditionAndSubstractionCount = 0;
		private int constantFoldingMultiplicationCount = 0;
		private int constantFoldingDivisionCount = 0;
		private int constantFoldingPhiCount = 0;
		private int blockRemovedCount = 0;
		private int foldIntegerCompareBranchCount = 0;
		private int reduceZeroExtendedMoveCount = 0;
		private int foldIntegerCompareCount = 0;
		private int simplifyExtendedMoveCount = 0;
		private int foldLoadStoreOffsetsCount = 0;
		private int constantMoveToRightCount = 0;
		private int simplifyPhiCount = 0;
		private int deadCodeEliminationPhi = 0;
		private int reduce64BitOperationsTo32BitCount = 0;
		private int promoteLocalVariableCount = 0;
		private int constantFoldingLogicalOrCount = 0;
		private int constantFoldingLogicalAndCount = 0;
		private int combineIntegerCompareBranchCount = 0;
		private int removeUselessIntegerCompareBranch = 0;
		private int changeCount = 0;

		private Stack<InstructionNode> worklist = new Stack<InstructionNode>();

		private HashSet<Operand> virtualRegisters = new HashSet<Operand>();

		private TraceLog trace;

		private delegate void Transformation(InstructionNode node);

		private List<Transformation> transformations;

		private int debugRestrictOptimizationByCount = 0;

		protected override void Run()
		{
			// Method is empty - must be a plugged method
			if (!HasCode)
				return;

			trace = CreateTraceLog();

			debugRestrictOptimizationByCount = MethodCompiler.Compiler.CompilerOptions.DebugRestrictOptimizationByCount;

			transformations = CreateTransformationList();

			Optimize();

			UpdateCounter("IROptimizations.IRInstructionRemoved", instructionsRemovedCount);
			UpdateCounter("IROptimizations.ConstantFoldingIntegerOperations", constantFoldingIntegerOperationsCount);
			UpdateCounter("IROptimizations.ConstantFoldingIntegerCompare", constantFoldingIntegerCompareCount);
			UpdateCounter("IROptimizations.ConstantFoldingAdditionAndSubstraction", constantFoldingAdditionAndSubstractionCount);
			UpdateCounter("IROptimizations.ConstantFoldingMultiplication", constantFoldingMultiplicationCount);
			UpdateCounter("IROptimizations.ConstantFoldingDivision", constantFoldingDivisionCount);
			UpdateCounter("IROptimizations.ConstantFoldingLogicalOr", constantFoldingLogicalOrCount);
			UpdateCounter("IROptimizations.ConstantFoldingLogicalAnd", constantFoldingLogicalAndCount);
			UpdateCounter("IROptimizations.ConstantFoldingPhi", constantFoldingPhiCount);
			UpdateCounter("IROptimizations.ConstantMoveToRight", constantMoveToRightCount);
			UpdateCounter("IROptimizations.ArithmeticSimplificationSubtraction", arithmeticSimplificationSubtractionCount);
			UpdateCounter("IROptimizations.ArithmeticSimplificationMultiplication", arithmeticSimplificationMultiplicationCount);
			UpdateCounter("IROptimizations.ArithmeticSimplificationDivision", arithmeticSimplificationDivisionCount);
			UpdateCounter("IROptimizations.ArithmeticSimplificationAdditionAndSubstraction", arithmeticSimplificationAdditionAndSubstractionCount);
			UpdateCounter("IROptimizations.ArithmeticSimplificationLogicalOperators", arithmeticSimplificationLogicalOperatorsCount);
			UpdateCounter("IROptimizations.ArithmeticSimplificationShiftOperators", arithmeticSimplificationShiftOperators);
			UpdateCounter("IROptimizations.SimpleConstantPropagation", simpleConstantPropagationCount);
			UpdateCounter("IROptimizations.SimpleForwardCopyPropagation", simpleForwardCopyPropagationCount);
			UpdateCounter("IROptimizations.FoldIntegerCompareBranch", foldIntegerCompareBranchCount);
			UpdateCounter("IROptimizations.FoldIntegerCompare", foldIntegerCompareCount);
			UpdateCounter("IROptimizations.FoldLoadStoreOffsets", foldLoadStoreOffsetsCount);
			UpdateCounter("IROptimizations.DeadCodeElimination", deadCodeEliminationCount);
			UpdateCounter("IROptimizations.DeadCodeEliminationPhi", deadCodeEliminationPhi);
			UpdateCounter("IROptimizations.ReduceTruncationAndExpansion", reduceTruncationAndExpansionCount);
			UpdateCounter("IROptimizations.CombineIntegerCompareBranch", combineIntegerCompareBranchCount);
			UpdateCounter("IROptimizations.ReduceZeroExtendedMove", reduceZeroExtendedMoveCount);
			UpdateCounter("IROptimizations.SimplifyExtendedMove", simplifyExtendedMoveCount);
			UpdateCounter("IROptimizations.SimplifyExtendedMoveWithConstant", simplifyExtendedMoveWithConstantCount);
			UpdateCounter("IROptimizations.SimplifyPhi", simplifyPhiCount);
			UpdateCounter("IROptimizations.BlockRemoved", blockRemovedCount);
			UpdateCounter("IROptimizations.PromoteLocalVariable", promoteLocalVariableCount);
			UpdateCounter("IROptimizations.Reduce64BitOperationsTo32Bit", reduce64BitOperationsTo32BitCount);
			UpdateCounter("IROptimizations.RemoveUselessIntegerCompareBranch", removeUselessIntegerCompareBranch);

			worklist = null;
		}

		private void Optimize()
		{
			if (MethodCompiler.Compiler.CompilerOptions.EnableVariablePromotion)
				PromoteLocalVariable();

			foreach (var block in BasicBlocks)
			{
				for (var node = block.First; !node.IsBlockEndInstruction; node = node.Next)
				{
					if (node.IsEmpty)
						continue;

					if (node.ResultCount == 0 && node.OperandCount == 0)
						continue;

					Do(node);

					if (ShouldStop)
						return;

					ProcessWorkList();

					if (ShouldStop)
						return;

					// Collect virtual registers
					if (node.IsEmpty)
						continue;

					// add virtual registers
					foreach (var op in node.Results)
					{
						if (op.IsVirtualRegister)
							virtualRegisters.AddIfNew(op);
					}

					foreach (var op in node.Operands)
					{
						if (op.IsVirtualRegister)
							virtualRegisters.AddIfNew(op);
					}
				}
			}

			bool change = true;
			while (change)
			{
				change = false;

				if (MethodCompiler.Compiler.CompilerOptions.EnableVariablePromotion)
					if (PromoteLocalVariable())
						change = true;

				if (ShouldStop)
					return;

				//if (Reduce64BitOperationsTo32Bit())
				//	change = true;

				if (change)
				{
					ProcessWorkList();
				}

				if (ShouldStop)
					return;
			}
		}

		private List<Transformation> CreateTransformationList()
		{
			return new List<Transformation>()
			{
				SimpleConstantPropagation,
				SimpleForwardCopyPropagation,
				DeadCodeElimination,
				ConstantFoldingIntegerOperations,
				ConstantMoveToRight,
				ArithmeticSimplificationSubtraction,
				ArithmeticSimplificationMultiplication,
				ArithmeticSimplificationDivision,
				ArithmeticSimplificationAdditionAndSubstraction,
				ArithmeticSimplificationLogicalOperators,
				ArithmeticSimplificationShiftOperators,
				ReduceZeroExtendedMove,
				ConstantFoldingAdditionAndSubstraction,
				ConstantFoldingMultiplication,
				ConstantFoldingDivision,
				ConstantFoldingIntegerCompare,
				ConstantFoldingLogicalOr,
				ConstantFoldingLogicalAnd,
				CombineIntegerCompareBranch,
				FoldIntegerCompare,
				RemoveUselessIntegerCompareBranch,
				FoldIntegerCompareBranch,
				ReduceTruncationAndExpansion,
				SimplifyExtendedMoveWithConstant,
				SimplifyExtendedMove,
				FoldLoadStoreOffsets,
				ConstantFoldingPhi,
				SimplifyPhi,
				DeadCodeEliminationPhi,
				NormalizeConstantTo32Bit
			};
		}

		private bool ShouldStop
		{
			get { return ((debugRestrictOptimizationByCount != 0) && (changeCount > debugRestrictOptimizationByCount)); }
		}

		/// <summary>
		/// Finishes this instance.
		/// </summary>
		protected override void Finish()
		{
			virtualRegisters = null;
			worklist = null;
		}

		private void ProcessWorkList()
		{
			while (worklist.Count != 0)
			{
				var node = worklist.Pop();
				Do(node);
			}
		}

		private void Do(InstructionNode node)
		{
			foreach (var method in transformations)
			{
				if (node.IsEmpty)
					return;

				method.Invoke(node);

				if (ShouldStop)
					return;
			}
		}

		private void AddToWorkList(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			// work list stays small, so the check is inexpensive
			if (worklist.Contains(node))
				return;

			worklist.Push(node);
		}

		/// <summary>
		/// Adds the operand usage and definitions to work list.
		/// </summary>
		/// <param name="operand">The operand.</param>
		private void AddOperandUsageToWorkList(Operand operand)
		{
			if (!operand.IsVirtualRegister)
				return;

			foreach (var index in operand.Uses)
			{
				AddToWorkList(index);
			}

			foreach (var index in operand.Definitions)
			{
				AddToWorkList(index);
			}
		}

		/// <summary>
		/// Adds the all the operands usage and definitions to work list.
		/// </summary>
		/// <param name="node">The node.</param>
		private void AddOperandUsageToWorkList(InstructionNode node)
		{
			if (node.Result != null)
			{
				AddOperandUsageToWorkList(node.Result);
			}
			foreach (var operand in node.Operands)
			{
				AddOperandUsageToWorkList(operand);
			}
		}

		private bool ContainsAddressOf(Operand local)
		{
			foreach (var node in local.Uses)
			{
				if (node.Instruction == IRInstruction.AddressOf)
					return true;
			}

			return false;
		}

		private bool PromoteLocalVariable()
		{
			bool change = false;

			foreach (var local in MethodCompiler.LocalVariables)
			{
				if (local.IsVirtualRegister)
					continue;

				if (local.IsParameter)
					continue;

				if (!local.IsStackLocal)
					continue;

				if (local.IsPinned)
					continue;

				if (local.Definitions.Count != 1)
					continue;

				if (!local.IsReferenceType && !local.IsInteger && !local.IsR && !local.IsChar && !local.IsBoolean && !local.IsPointer && !local.IsI && !local.IsU)
					continue;

				if (ContainsAddressOf(local))
					continue;

				var v = MethodCompiler.CreateVirtualRegister(local.Type.GetStackType());

				if (trace.Active) trace.Log("*** PromoteLocalVariable");

				ReplaceVirtualRegister(local, v, true);

				promoteLocalVariableCount++;
				changeCount++;
				change = true;
			}

			return change;
		}

		/// <summary>
		/// Removes the useless move and dead code
		/// </summary>
		/// <param name="node">The node.</param>
		private void DeadCodeElimination(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.ResultCount != 1)
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (node.Result.Definitions.Count != 1)
				return;

			if (node.Instruction == IRInstruction.Call || node.Instruction == IRInstruction.IntrinsicMethodCall)
				return;

			if (node.Instruction == IRInstruction.Move && node.Operand1.IsVirtualRegister && node.Operand1 == node.Result)
			{
				if (trace.Active) trace.Log("*** DeadCodeElimination");
				if (trace.Active) trace.Log("REMOVED:\t" + node.ToString());
				AddOperandUsageToWorkList(node);
				node.SetInstruction(IRInstruction.Nop);
				instructionsRemovedCount++;
				deadCodeEliminationCount++;
				changeCount++;
				return;
			}

			if (node.Result.Uses.Count != 0)
				return;

			if (trace.Active) trace.Log("*** DeadCodeElimination");
			if (trace.Active) trace.Log("REMOVED:\t" + node.ToString());
			AddOperandUsageToWorkList(node);
			node.SetInstruction(IRInstruction.Nop);
			instructionsRemovedCount++;
			deadCodeEliminationCount++;
			changeCount++;
			return;
		}

		/// <summary>
		/// Simple constant propagation.
		/// </summary>
		/// <param name="node">The node.</param>
		private void SimpleConstantPropagation(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.Move)
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (node.Result.Definitions.Count != 1)
				return;

			if (!node.Operand1.IsConstant)
				return;

			Debug.Assert(node.Result.Definitions.Count == 1);

			Operand destination = node.Result;
			Operand source = node.Operand1;

			// for each statement T that uses operand, substituted c in statement T
			foreach (var useNode in destination.Uses.ToArray())
			{
				if (useNode.Instruction == IRInstruction.AddressOf)
					continue;

				bool propogated = false;

				for (int i = 0; i < useNode.OperandCount; i++)
				{
					var operand = useNode.GetOperand(i);

					if (operand == node.Result)
					{
						propogated = true;

						if (trace.Active) trace.Log("*** SimpleConstantPropagation");
						if (trace.Active) trace.Log("BEFORE:\t" + useNode.ToString());
						AddOperandUsageToWorkList(operand);
						useNode.SetOperand(i, source);
						simpleConstantPropagationCount++;
						changeCount++;
						if (trace.Active) trace.Log("AFTER: \t" + useNode.ToString());
					}
				}

				if (propogated)
				{
					AddToWorkList(useNode);
				}
			}
		}

		private bool CanCopyPropagation(Operand source, Operand destination)
		{
			if (source.IsReferenceType && destination.IsReferenceType)
				return true;

			if (source.IsUnmanagedPointer && destination.IsUnmanagedPointer)
				return true;

			if (source.IsManagedPointer && destination.IsManagedPointer)
				return true;

			if (source.IsFunctionPointer && destination.IsFunctionPointer)
				return true;

			if (source.Type.IsArray & destination.Type.IsArray & source.Type.ElementType == destination.Type.ElementType)
				return true;

			if (source.Type.IsUI1 & destination.Type.IsUI1)
				return true;

			if (source.Type.IsUI2 & destination.Type.IsUI2)
				return true;

			if (source.Type.IsUI4 & destination.Type.IsUI4)
				return true;

			if (source.Type.IsUI8 & destination.Type.IsUI8)
				return true;

			if (NativePointerSize == 4 && (destination.IsI || destination.IsU || destination.IsUnmanagedPointer) && (source.IsI4 || source.IsU4))
				return true;

			if (NativePointerSize == 4 && (source.IsI || source.IsU || source.IsUnmanagedPointer) && (destination.IsI4 || destination.IsU4))
				return true;

			if (NativePointerSize == 8 && (destination.IsI || destination.IsU || destination.IsUnmanagedPointer) && (source.IsI8 || source.IsU8))
				return true;

			if (NativePointerSize == 8 && (source.IsI || source.IsU || source.IsUnmanagedPointer) && (destination.IsI8 || destination.IsU8))
				return true;

			if (source.IsR4 && destination.IsR4)
				return true;

			if (source.IsR8 && destination.IsR8)
				return true;

			if (((source.IsI || source.IsU) && (destination.IsI || destination.IsU)))
				return true;

			if (source.IsValueType || destination.IsValueType)
				return false;

			if (source.Type == destination.Type)
				return true;

			return false;
		}

		/// <summary>
		/// Simple copy propagation.
		/// </summary>
		/// <param name="node">The node.</param>
		private void SimpleForwardCopyPropagation(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.Move)
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (node.Result.Definitions.Count != 1)
				return;

			if (node.Operand1.Definitions.Count != 1)
				return;

			if (node.Operand1.IsConstant)
				return;

			if (!node.Operand1.IsVirtualRegister)
				return;

			// If the pointer or reference types are different, we can not copy propagation because type information would be lost.
			// Also if the operand sign is different, we cannot do it as it requires a signed/unsigned extended move, not a normal move
			if (!CanCopyPropagation(node.Result, node.Operand1))
				return;

			Operand destination = node.Result;
			Operand source = node.Operand1;

			if (ContainsAddressOf(destination))
				return;

			// for each statement T that uses operand, substituted c in statement T
			AddOperandUsageToWorkList(node);

			foreach (var useNode in destination.Uses.ToArray())
			{
				for (int i = 0; i < useNode.OperandCount; i++)
				{
					var operand = useNode.GetOperand(i);

					if (destination == operand)
					{
						if (trace.Active) trace.Log("*** SimpleForwardCopyPropagation");
						if (trace.Active) trace.Log("BEFORE:\t" + useNode.ToString());
						useNode.SetOperand(i, source);
						simpleForwardCopyPropagationCount++;
						changeCount++;
						if (trace.Active) trace.Log("AFTER: \t" + useNode.ToString());
					}
				}
			}

			Debug.Assert(destination.Uses.Count == 0);

			if (trace.Active) trace.Log("REMOVED:\t" + node.ToString());
			AddOperandUsageToWorkList(node);
			node.SetInstruction(IRInstruction.Nop);
			instructionsRemovedCount++;
			changeCount++;
		}

		/// <summary>
		/// Folds an integer operation on constants
		/// </summary>
		/// <param name="node">The node.</param>
		private void ConstantFoldingIntegerOperations(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.AddSigned || node.Instruction == IRInstruction.AddUnsigned ||
				  node.Instruction == IRInstruction.SubSigned || node.Instruction == IRInstruction.SubUnsigned ||
				  node.Instruction == IRInstruction.LogicalAnd || node.Instruction == IRInstruction.LogicalOr ||
				  node.Instruction == IRInstruction.LogicalXor ||
				  node.Instruction == IRInstruction.MulSigned || node.Instruction == IRInstruction.MulUnsigned ||
				  node.Instruction == IRInstruction.DivSigned || node.Instruction == IRInstruction.DivUnsigned ||
				  node.Instruction == IRInstruction.ArithmeticShiftRight ||
				  node.Instruction == IRInstruction.ShiftLeft || node.Instruction == IRInstruction.ShiftRight))
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			Operand result = node.Result;
			Operand op1 = node.Operand1;
			Operand op2 = node.Operand2;

			if (!op1.IsConstant || !op2.IsConstant)
				return;

			// Divide by zero!
			if ((node.Instruction == IRInstruction.DivSigned || node.Instruction == IRInstruction.DivUnsigned) && op2.IsConstant && op2.IsConstantZero)
				return;

			Operand constant = null;

			if (node.Instruction == IRInstruction.AddSigned || node.Instruction == IRInstruction.AddUnsigned)
			{
				constant = Operand.CreateConstant(result.Type, op1.ConstantUnsignedLongInteger + op2.ConstantUnsignedLongInteger);
			}
			else if (node.Instruction == IRInstruction.SubSigned || node.Instruction == IRInstruction.SubUnsigned)
			{
				constant = Operand.CreateConstant(result.Type, op1.ConstantUnsignedLongInteger - op2.ConstantUnsignedLongInteger);
			}
			else if (node.Instruction == IRInstruction.LogicalAnd)
			{
				constant = Operand.CreateConstant(result.Type, op1.ConstantUnsignedLongInteger & op2.ConstantUnsignedLongInteger);
			}
			else if (node.Instruction == IRInstruction.LogicalOr)
			{
				constant = Operand.CreateConstant(result.Type, op1.ConstantUnsignedLongInteger | op2.ConstantUnsignedLongInteger);
			}
			else if (node.Instruction == IRInstruction.LogicalXor)
			{
				constant = Operand.CreateConstant(result.Type, op1.ConstantUnsignedLongInteger ^ op2.ConstantUnsignedLongInteger);
			}
			else if (node.Instruction == IRInstruction.MulSigned || node.Instruction == IRInstruction.MulUnsigned)
			{
				constant = Operand.CreateConstant(result.Type, op1.ConstantUnsignedLongInteger * op2.ConstantUnsignedLongInteger);
			}
			else if (node.Instruction == IRInstruction.DivUnsigned)
			{
				constant = Operand.CreateConstant(result.Type, op1.ConstantUnsignedLongInteger / op2.ConstantUnsignedLongInteger);
			}
			else if (node.Instruction == IRInstruction.DivSigned)
			{
				constant = Operand.CreateConstant(result.Type, op1.ConstantSignedLongInteger / op2.ConstantSignedLongInteger);
			}
			else if (node.Instruction == IRInstruction.ArithmeticShiftRight)
			{
				constant = Operand.CreateConstant(result.Type, ((long)op1.ConstantUnsignedLongInteger) >> (int)op2.ConstantUnsignedLongInteger);
			}
			else if (node.Instruction == IRInstruction.ShiftRight)
			{
				constant = Operand.CreateConstant(result.Type, op1.ConstantUnsignedLongInteger >> (int)op2.ConstantUnsignedLongInteger);
			}
			else if (node.Instruction == IRInstruction.ShiftLeft)
			{
				constant = Operand.CreateConstant(result.Type, op1.ConstantUnsignedLongInteger << (int)op2.ConstantUnsignedLongInteger);
			}

			if (constant == null)
				return;

			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("*** ConstantFoldingIntegerOperations");
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.SetInstruction(IRInstruction.Move, node.Result, constant);
			constantFoldingIntegerOperationsCount++;
			changeCount++;
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
		}

		/// <summary>
		/// Folds the integer compare on constants
		/// </summary>
		/// <param name="node">The node.</param>
		private void ConstantFoldingIntegerCompare(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.IntegerCompare)
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			Operand result = node.Result;
			Operand op1 = node.Operand1;
			Operand op2 = node.Operand2;

			if (!op1.IsConstant || !op2.IsConstant)
				return;

			if (!op1.IsValueType || !op2.IsValueType)
				return;

			bool compareResult = true;

			switch (node.ConditionCode)
			{
				case ConditionCode.Equal: compareResult = (op1.ConstantUnsignedLongInteger == op2.ConstantUnsignedLongInteger); break;
				case ConditionCode.NotEqual: compareResult = (op1.ConstantUnsignedLongInteger != op2.ConstantUnsignedLongInteger); break;
				case ConditionCode.GreaterOrEqual: compareResult = (op1.ConstantUnsignedLongInteger >= op2.ConstantUnsignedLongInteger); break;
				case ConditionCode.GreaterThan: compareResult = (op1.ConstantUnsignedLongInteger > op2.ConstantUnsignedLongInteger); break;
				case ConditionCode.LessOrEqual: compareResult = (op1.ConstantUnsignedLongInteger <= op2.ConstantUnsignedLongInteger); break;
				case ConditionCode.LessThan: compareResult = (op1.ConstantUnsignedLongInteger < op2.ConstantUnsignedLongInteger); break;

				// TODO: Add more
				default: return;
			}

			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("*** ConstantFoldingIntegerCompare");
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.SetInstruction(IRInstruction.Move, result, Operand.CreateConstant(result.Type, compareResult ? 1 : 0));
			constantFoldingIntegerCompareCount++;
			changeCount++;
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
		}

		/// <summary>
		/// Strength reduction for integer addition when one of the constants is zero
		/// </summary>
		/// <param name="node">The node.</param>
		private void ArithmeticSimplificationAdditionAndSubstraction(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.AddSigned || node.Instruction == IRInstruction.AddUnsigned
				|| node.Instruction == IRInstruction.SubSigned || node.Instruction == IRInstruction.SubUnsigned))
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			Operand result = node.Result;
			Operand op1 = node.Operand1;
			Operand op2 = node.Operand2;

			if (op2.IsConstant && !op1.IsConstant && op2.IsConstantZero)
			{
				AddOperandUsageToWorkList(node);
				if (trace.Active) trace.Log("*** ArithmeticSimplificationAdditionAndSubstraction");
				if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
				node.SetInstruction(IRInstruction.Move, result, op1);
				if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
				arithmeticSimplificationAdditionAndSubstractionCount++;
				changeCount++;
				return;
			}
		}

		/// <summary>
		/// Strength reduction for multiplication when one of the constants is zero or one
		/// </summary>
		/// <param name="node">The node.</param>
		private void ArithmeticSimplificationMultiplication(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.MulSigned || node.Instruction == IRInstruction.MulUnsigned))
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (!node.Operand2.IsConstant)
				return;

			Operand result = node.Result;
			Operand op1 = node.Operand1;
			Operand op2 = node.Operand2;

			if (op2.IsConstantZero)
			{
				AddOperandUsageToWorkList(node);
				if (trace.Active) trace.Log("*** ArithmeticSimplificationMultiplication");
				if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
				node.SetInstruction(IRInstruction.Move, result, Operand.CreateConstant(node.Result.Type, 0));
				arithmeticSimplificationMultiplicationCount++;
				if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
				changeCount++;
				return;
			}

			if (op2.IsConstantOne)
			{
				AddOperandUsageToWorkList(node);
				if (trace.Active) trace.Log("*** ArithmeticSimplificationMultiplication");
				if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
				node.SetInstruction(IRInstruction.Move, result, op1);
				arithmeticSimplificationMultiplicationCount++;
				if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
				changeCount++;
				return;
			}

			if (IsPowerOfTwo(op2.ConstantUnsignedLongInteger))
			{
				uint shift = GetPowerOfTwo(op2.ConstantUnsignedLongInteger);

				if (shift < 32)
				{
					AddOperandUsageToWorkList(node);
					if (trace.Active) trace.Log("*** ArithmeticSimplificationMultiplication");
					if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
					node.SetInstruction(IRInstruction.ShiftLeft, result, op1, Operand.CreateConstant(TypeSystem, (int)shift));
					arithmeticSimplificationMultiplicationCount++;
					if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
					changeCount++;
					return;
				}
			}
		}

		/// <summary>
		/// Strength reduction for division when one of the constants is zero or one
		/// </summary>
		/// <param name="node">The node.</param>
		private void ArithmeticSimplificationDivision(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.DivSigned || node.Instruction == IRInstruction.DivUnsigned))
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			Operand result = node.Result;
			Operand op1 = node.Operand1;
			Operand op2 = node.Operand2;

			if (!op2.IsConstant || op2.IsConstantZero)
			{
				// Possible divide by zero
				return;
			}

			if (op1.IsConstant && op1.IsConstantZero)
			{
				AddOperandUsageToWorkList(node);
				if (trace.Active) trace.Log("*** ArithmeticSimplificationDivision");
				if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
				node.SetInstruction(IRInstruction.Move, result, Operand.CreateConstant(result.Type, 0));
				arithmeticSimplificationDivisionCount++;
				changeCount++;
				if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
				return;
			}

			if (op2.IsConstant && op2.IsConstantOne)
			{
				AddOperandUsageToWorkList(node);
				if (trace.Active) trace.Log("*** ArithmeticSimplificationDivision");
				if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
				node.SetInstruction(IRInstruction.Move, result, op1);
				arithmeticSimplificationDivisionCount++;
				changeCount++;
				if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
				return;
			}

			if (node.Instruction == IRInstruction.DivUnsigned && IsPowerOfTwo(op2.ConstantUnsignedLongInteger))
			{
				uint shift = GetPowerOfTwo(op2.ConstantUnsignedLongInteger);

				if (shift < 32)
				{
					AddOperandUsageToWorkList(node);
					if (trace.Active) trace.Log("*** ArithmeticSimplificationDivision");
					if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
					node.SetInstruction(IRInstruction.ShiftRight, result, op1, Operand.CreateConstant(TypeSystem, (int)shift));
					arithmeticSimplificationDivisionCount++;
					changeCount++;
					if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
					return;
				}
			}
		}

		/// <summary>
		/// Simplifies extended moves with a constant
		/// </summary>
		/// <param name="node">The node.</param>
		private void SimplifyExtendedMoveWithConstant(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.ZeroExtendedMove || node.Instruction == IRInstruction.SignExtendedMove))
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (node.Result.Definitions.Count != 1)
				return;

			if (!node.Operand1.IsConstant)
				return;

			Operand result = node.Result;
			Operand op1 = node.Operand1;

			Operand newOperand;

			if (node.Instruction == IRInstruction.ZeroExtendedMove && result.IsUnsigned && op1.IsSigned)
			{
				var newConstant = Unsign(op1.Type, op1.ConstantSignedLongInteger);
				newOperand = Operand.CreateConstant(node.Result.Type, newConstant);
			}
			else
			{
				newOperand = Operand.CreateConstant(node.Result.Type, op1.ConstantUnsignedLongInteger);
			}

			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("*** SimplifyExtendedMoveWithConstant");
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.SetInstruction(IRInstruction.Move, result, newOperand);
			simplifyExtendedMoveWithConstantCount++;
			changeCount++;
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
		}

		static private ulong Unsign(MosaType type, long value)
		{
			if (type.IsI1) return (ulong)((sbyte)value);
			else if (type.IsI2) return (ulong)((short)value);
			else if (type.IsI4) return (ulong)((int)value);
			else if (type.IsI8) return (ulong)value;
			else return (ulong)value;
		}

		/// <summary>
		/// Simplifies subtraction where both operands are the same
		/// </summary>
		/// <param name="node">The node.</param>
		private void ArithmeticSimplificationSubtraction(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.SubSigned || node.Instruction == IRInstruction.SubUnsigned))
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			Operand result = node.Result;
			Operand op1 = node.Operand1;
			Operand op2 = node.Operand2;

			if (op1 != op2)
				return;

			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("*** ArithmeticSimplificationSubtraction");
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.SetInstruction(IRInstruction.Move, result, Operand.CreateConstant(node.Result.Type, 0));
			arithmeticSimplificationSubtractionCount++;
			changeCount++;
		}

		/// <summary>
		/// Strength reduction for logical operators
		/// </summary>
		/// <param name="node">The node.</param>
		private void ArithmeticSimplificationLogicalOperators(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.LogicalAnd || node.Instruction == IRInstruction.LogicalOr))
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (!node.Operand2.IsConstant)
				return;

			Operand result = node.Result;
			Operand op1 = node.Operand1;
			Operand op2 = node.Operand2;

			if (node.Instruction == IRInstruction.LogicalOr)
			{
				if (op2.IsConstantZero)
				{
					AddOperandUsageToWorkList(node);
					if (trace.Active) trace.Log("*** ArithmeticSimplificationLogicalOperators");
					if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
					node.SetInstruction(IRInstruction.Move, result, op1);
					arithmeticSimplificationLogicalOperatorsCount++;
					changeCount++;
					if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
					return;
				}
			}
			else if (node.Instruction == IRInstruction.LogicalAnd)
			{
				if (op2.IsConstantZero)
				{
					AddOperandUsageToWorkList(node);
					if (trace.Active) trace.Log("*** ArithmeticSimplificationLogicalOperators");
					if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
					node.SetInstruction(IRInstruction.Move, result, Operand.CreateConstant(node.Result.Type, 0));
					arithmeticSimplificationLogicalOperatorsCount++;
					changeCount++;
					if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
					return;
				}

				if (((result.IsI4 || result.IsU4 || result.IsI || result.IsU) && op2.ConstantUnsignedInteger == 0xFFFFFFFF)
					|| ((result.IsI8 || result.IsU8) && op2.ConstantUnsignedLongInteger == 0xFFFFFFFFFFFFFFFF))
				{
					AddOperandUsageToWorkList(node);
					if (trace.Active) trace.Log("*** ArithmeticSimplificationLogicalOperators");
					if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
					node.SetInstruction(IRInstruction.Move, result, op1);
					arithmeticSimplificationLogicalOperatorsCount++;
					changeCount++;
					if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
					return;
				}
			}

			// TODO: Add more strength reductions especially for AND w/ 0xFF, 0xFFFF, 0xFFFFFFFF, etc when source or destination are same or smaller
		}

		/// <summary>
		/// Strength reduction shift operators.
		/// </summary>
		/// <param name="node">The node.</param>
		private void ArithmeticSimplificationShiftOperators(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.ShiftLeft || node.Instruction == IRInstruction.ShiftRight || node.Instruction == IRInstruction.ArithmeticShiftRight))
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (!node.Operand2.IsConstant)
				return;

			Operand result = node.Result;
			Operand op1 = node.Operand1;
			Operand op2 = node.Operand2;

			if (op2.IsConstantZero || op1.IsConstantZero)
			{
				AddOperandUsageToWorkList(node);
				if (trace.Active) trace.Log("*** ArithmeticSimplificationShiftOperators");
				if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
				node.SetInstruction(IRInstruction.Move, result, op1);
				arithmeticSimplificationShiftOperators++;
				changeCount++;
				if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
				return;
			}
		}

		/// <summary>
		/// Removes the unless integer compare branch.
		/// </summary>
		/// <param name="node">The node.</param>
		private void RemoveUselessIntegerCompareBranch(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.IntegerCompareBranch)
				return;

			if (node.Block.NextBlocks.Count != 1)
				return;

			if (trace.Active) trace.Log("REMOVED:\t" + node.ToString());
			AddOperandUsageToWorkList(node);
			node.SetInstruction(IRInstruction.Nop);
			instructionsRemovedCount++;
			removeUselessIntegerCompareBranch++;
			changeCount++;
		}

		/// <summary>
		/// Folds integer compare branch.
		/// </summary>
		/// <param name="node">The node.</param>
		private void FoldIntegerCompareBranch(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.IntegerCompareBranch)
				return;

			Debug.Assert(node.OperandCount == 2);

			Operand op1 = node.Operand1;
			Operand op2 = node.Operand2;

			if (!op1.IsConstant || !op2.IsConstant)
				return;

			Operand result = node.Result;
			InstructionNode nextNode = node.Next;

			if (nextNode.Instruction != IRInstruction.Jmp)
				return;

			if (node.BranchTargets[0] == nextNode.BranchTargets[0])
			{
				if (trace.Active) trace.Log("*** FoldIntegerCompareBranch-Useless");
				if (trace.Active) trace.Log("REMOVED:\t" + node.ToString());
				AddOperandUsageToWorkList(node);
				node.SetInstruction(IRInstruction.Nop);
				instructionsRemovedCount++;
				foldIntegerCompareBranchCount++;
				changeCount++;
				return;
			}

			bool compareResult = true;

			switch (node.ConditionCode)
			{
				case ConditionCode.Equal: compareResult = (op1.ConstantUnsignedLongInteger == op2.ConstantUnsignedLongInteger); break;
				case ConditionCode.NotEqual: compareResult = (op1.ConstantUnsignedLongInteger != op2.ConstantUnsignedLongInteger); break;
				case ConditionCode.GreaterOrEqual: compareResult = (op1.ConstantUnsignedLongInteger >= op2.ConstantUnsignedLongInteger); break;
				case ConditionCode.GreaterThan: compareResult = (op1.ConstantUnsignedLongInteger > op2.ConstantUnsignedLongInteger); break;
				case ConditionCode.LessOrEqual: compareResult = (op1.ConstantUnsignedLongInteger <= op2.ConstantUnsignedLongInteger); break;
				case ConditionCode.LessThan: compareResult = (op1.ConstantUnsignedLongInteger < op2.ConstantUnsignedLongInteger); break;

				// TODO: Add more
				default: return;
			}

			BasicBlock notTaken;
			InstructionNode notUsed;

			if (trace.Active) trace.Log("*** FoldIntegerCompareBranch");

			if (compareResult)
			{
				notTaken = nextNode.BranchTargets[0];
				if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
				node.SetInstruction(IRInstruction.Jmp, node.BranchTargets[0]);
				if (trace.Active) trace.Log("AFTER:\t" + node.ToString());

				notUsed = nextNode;
			}
			else
			{
				notTaken = node.BranchTargets[0];
				notUsed = node;
			}

			if (trace.Active) trace.Log("REMOVED:\t" + node.ToString());
			AddOperandUsageToWorkList(notUsed);
			notUsed.SetInstruction(IRInstruction.Nop);
			instructionsRemovedCount++;
			foldIntegerCompareBranchCount++;
			changeCount++;

			// if target block no longer has any predecessors (or the only predecessor is itself), remove all instructions from it.
			CheckAndClearEmptyBlock(notTaken);
		}

		private void CheckAndClearEmptyBlock(BasicBlock block)
		{
			if (block.PreviousBlocks.Count != 0 || BasicBlocks.HeadBlocks.Contains(block))
				return;

			if (trace.Active) trace.Log("*** RemoveBlock: " + block.ToString());

			blockRemovedCount++;

			var nextBlocks = block.NextBlocks.ToArray();

			EmptyBlockOfAllInstructions(block);

			UpdatePhiList(block, nextBlocks);

			Debug.Assert(block.NextBlocks.Count == 0);
			Debug.Assert(block.PreviousBlocks.Count == 0);
		}

		private void ConstantMoveToRight(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.AddSigned || node.Instruction == IRInstruction.AddUnsigned
				|| node.Instruction == IRInstruction.MulSigned || node.Instruction == IRInstruction.MulUnsigned
				|| node.Instruction == IRInstruction.LogicalAnd || node.Instruction == IRInstruction.LogicalOr
				|| node.Instruction == IRInstruction.LogicalXor))
				return;

			if (node.Operand2.IsConstant)
				return;

			if (!node.Operand1.IsConstant)
				return;

			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("*** ConstantMoveToRight");
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			var op1 = node.Operand1;
			node.Operand1 = node.Operand2;
			node.Operand2 = op1;
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
			constantMoveToRightCount++;
			changeCount++;
		}

		private void ConstantFoldingAdditionAndSubstraction(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.AddSigned || node.Instruction == IRInstruction.AddUnsigned
				|| node.Instruction == IRInstruction.SubSigned || node.Instruction == IRInstruction.SubUnsigned))
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (node.Result.Definitions.Count != 1)
				return;

			if (!node.Operand2.IsConstant)
				return;

			if (node.Result.Uses.Count != 1)
				return;

			var node2 = node.Result.Uses[0];

			if (!(node2.Instruction == IRInstruction.AddSigned || node2.Instruction == IRInstruction.AddUnsigned
				|| node2.Instruction == IRInstruction.SubSigned || node2.Instruction == IRInstruction.SubUnsigned))
				return;

			if (!node2.Result.IsVirtualRegister)
				return;

			if (!node2.Operand2.IsConstant)
				return;

			Debug.Assert(node2.Result.Definitions.Count == 1);

			bool add = true;

			if ((node.Instruction == IRInstruction.AddSigned || node.Instruction == IRInstruction.AddUnsigned) &&
				(node2.Instruction == IRInstruction.SubSigned || node2.Instruction == IRInstruction.SubUnsigned))
			{
				add = false;
			}
			else if ((node.Instruction == IRInstruction.SubSigned || node.Instruction == IRInstruction.SubUnsigned) &&
				(node2.Instruction == IRInstruction.AddSigned || node2.Instruction == IRInstruction.AddUnsigned))
			{
				add = false;
			}

			ulong r = add ? node.Operand2.ConstantUnsignedLongInteger + node2.Operand2.ConstantUnsignedLongInteger :
				node.Operand2.ConstantUnsignedLongInteger - node2.Operand2.ConstantUnsignedLongInteger;

			Debug.Assert(node2.Result.Definitions.Count == 1);

			if (trace.Active) trace.Log("*** ConstantFoldingAdditionAndSubstraction");
			AddOperandUsageToWorkList(node2);
			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("BEFORE:\t" + node2.ToString());
			node2.SetInstruction(IRInstruction.Move, node2.Result, node.Result);
			if (trace.Active) trace.Log("AFTER: \t" + node2.ToString());
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.Operand2 = Operand.CreateConstant(node.Operand2.Type, r);
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
			constantFoldingAdditionAndSubstractionCount++;
			changeCount++;
		}

		private void ConstantFoldingLogicalOr(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.LogicalOr)
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (node.Result.Definitions.Count != 1)
				return;

			if (!node.Operand2.IsConstant)
				return;

			if (node.Result.Uses.Count != 1)
				return;

			var node2 = node.Result.Uses[0];

			if (node2.Instruction != IRInstruction.LogicalOr)
				return;

			if (!node2.Result.IsVirtualRegister)
				return;

			if (!node2.Operand2.IsConstant)
				return;

			Debug.Assert(node2.Result.Definitions.Count == 1);

			ulong r = node.Operand2.ConstantUnsignedLongInteger | node2.Operand2.ConstantUnsignedLongInteger;

			if (trace.Active) trace.Log("*** ConstantFoldingLogicalOr");
			AddOperandUsageToWorkList(node2);
			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("BEFORE:\t" + node2.ToString());
			node2.SetInstruction(IRInstruction.Move, node2.Result, node.Result);
			if (trace.Active) trace.Log("AFTER: \t" + node2.ToString());
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.Operand2 = Operand.CreateConstant(node.Operand2.Type, r);
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
			constantFoldingLogicalOrCount++;
			changeCount++;
		}

		private void ConstantFoldingLogicalAnd(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.LogicalAnd)
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (node.Result.Definitions.Count != 1)
				return;

			if (!node.Operand2.IsConstant)
				return;

			if (node.Result.Uses.Count != 1)
				return;

			var node2 = node.Result.Uses[0];

			if (node2.Instruction != IRInstruction.LogicalAnd)
				return;

			if (!node2.Result.IsVirtualRegister)
				return;

			if (!node2.Operand2.IsConstant)
				return;

			Debug.Assert(node2.Result.Definitions.Count == 1);

			ulong r = node.Operand2.ConstantUnsignedLongInteger & node2.Operand2.ConstantUnsignedLongInteger;

			if (trace.Active) trace.Log("*** ConstantFoldingLogicalOr");
			AddOperandUsageToWorkList(node2);
			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("BEFORE:\t" + node2.ToString());
			node2.SetInstruction(IRInstruction.Move, node2.Result, node.Result);
			if (trace.Active) trace.Log("AFTER: \t" + node2.ToString());
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.Operand2 = Operand.CreateConstant(node.Operand2.Type, r);
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
			constantFoldingLogicalAndCount++;
			changeCount++;
		}

		private void ConstantFoldingMultiplication(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.MulSigned || node.Instruction == IRInstruction.MulUnsigned))
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (node.Result.Definitions.Count != 1)
				return;

			if (!node.Operand2.IsConstant)
				return;

			if (node.Result.Uses.Count != 1)
				return;

			var node2 = node.Result.Uses[0];

			if (!(node2.Instruction == IRInstruction.MulSigned || node2.Instruction == IRInstruction.MulUnsigned))
				return;

			if (!node2.Result.IsVirtualRegister)
				return;

			if (!node2.Operand2.IsConstant)
				return;

			Debug.Assert(node2.Result.Definitions.Count == 1);

			ulong r = node.Operand2.ConstantUnsignedLongInteger * node2.Operand2.ConstantUnsignedLongInteger;

			if (trace.Active) trace.Log("*** ConstantFoldingMultiplication");
			AddOperandUsageToWorkList(node2);
			if (trace.Active) trace.Log("BEFORE:\t" + node2.ToString());
			node2.SetInstruction(IRInstruction.Move, node2.Result, node.Result);
			if (trace.Active) trace.Log("AFTER: \t" + node2.ToString());
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.Operand2 = Operand.CreateConstant(node.Operand2.Type, r);
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
			constantFoldingMultiplicationCount++;
			changeCount++;
		}

		private void ConstantFoldingDivision(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.DivSigned || node.Instruction == IRInstruction.DivUnsigned))
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (node.Result.Definitions.Count != 1)
				return;

			if (!node.Operand2.IsConstant)
				return;

			if (node.Result.Uses.Count != 1)
				return;

			var node2 = node.Result.Uses[0];

			if (!(node2.Instruction == IRInstruction.DivSigned || node2.Instruction == IRInstruction.DivUnsigned))
				return;

			if (!node2.Result.IsVirtualRegister)
				return;

			if (!node2.Operand2.IsConstant)
				return;

			Debug.Assert(node2.Result.Definitions.Count == 1);

			ulong r = (node2.Instruction == IRInstruction.DivSigned) ?
				(ulong)(node.Operand2.ConstantSignedLongInteger / node2.Operand2.ConstantSignedLongInteger) :
				node.Operand2.ConstantUnsignedLongInteger / node2.Operand2.ConstantUnsignedLongInteger;

			if (trace.Active) trace.Log("*** ConstantFoldingDivision");
			AddOperandUsageToWorkList(node2);
			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("BEFORE:\t" + node2.ToString());
			node2.SetInstruction(IRInstruction.Move, node2.Result, node.Result);
			if (trace.Active) trace.Log("AFTER: \t" + node2.ToString());
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.Operand2 = Operand.CreateConstant(node.Operand2.Type, r);
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
			constantFoldingDivisionCount++;
			changeCount++;
		}

		private void ReduceZeroExtendedMove(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.ZeroExtendedMove)
				return;

			if (!node.Operand1.IsVirtualRegister)
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (trace.Active) trace.Log("*** ReduceZeroExtendedMove");
			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.SetInstruction(IRInstruction.Move, node.Result, node.Operand1);
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
			reduceZeroExtendedMoveCount++;
			changeCount++;
		}

		private void ReduceTruncationAndExpansion(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.ZeroExtendedMove)
				return;

			if (!node.Operand1.IsVirtualRegister)
				return;

			if (!node.Result.IsVirtualRegister)
				return;

			if (node.Result.Uses.Count != 1)
				return;

			if (node.Result.Definitions.Count != 1)
				return;

			if (node.Operand1.Uses.Count != 1 || node.Operand1.Definitions.Count != 1)
				return;

			var node2 = node.Operand1.Definitions[0];

			if (node2.Instruction != IRInstruction.Move)
				return;

			if (node2.Result.Uses.Count != 1)
				return;

			Debug.Assert(node2.Result.Definitions.Count == 1);

			if (node2.Operand1.Type != node.Result.Type)
				return;

			if (trace.Active) trace.Log("*** ReduceTruncationAndExpansion");
			AddOperandUsageToWorkList(node2);
			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("BEFORE:\t" + node2.ToString());
			node2.Result = node.Result;
			if (trace.Active) trace.Log("AFTER: \t" + node2.ToString());
			if (trace.Active) trace.Log("REMOVED:\t" + node2.ToString());
			node.SetInstruction(IRInstruction.Nop);
			reduceTruncationAndExpansionCount++;
			instructionsRemovedCount++;
			changeCount++;
		}

		private void CombineIntegerCompareBranch(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.IntegerCompareBranch)
				return;

			if (!(node.ConditionCode == ConditionCode.NotEqual || node.ConditionCode == ConditionCode.Equal))
				return;

			if (!((node.Operand1.IsVirtualRegister && node.Operand2.IsConstant && node.Operand2.IsConstantZero) ||
				(node.Operand2.IsVirtualRegister && node.Operand1.IsConstant && node.Operand1.IsConstantZero)))
				return;

			var operand = (node.Operand2.IsConstant && node.Operand2.IsConstantZero) ? node.Operand1 : node.Operand2;

			if (operand.Uses.Count != 1)
				return;

			if (operand.Definitions.Count != 1)
				return;

			var node2 = operand.Definitions[0];

			if (node2.Instruction != IRInstruction.IntegerCompare)
				return;

			AddOperandUsageToWorkList(node2);
			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("*** CombineIntegerCompareBranch");
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.ConditionCode = node.ConditionCode == ConditionCode.NotEqual ? node2.ConditionCode : node2.ConditionCode.GetOpposite();
			node.Operand1 = node2.Operand1;
			node.Operand2 = node2.Operand2;
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
			if (trace.Active) trace.Log("REMOVED:\t" + node2.ToString());
			node2.SetInstruction(IRInstruction.Nop);
			combineIntegerCompareBranchCount++;
			instructionsRemovedCount++;
			changeCount++;
		}

		private void FoldIntegerCompare(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.IntegerCompare)
				return;

			if (!(node.ConditionCode == ConditionCode.NotEqual || node.ConditionCode == ConditionCode.Equal))
				return;

			if (!((node.Operand1.IsVirtualRegister && node.Operand2.IsConstant && node.Operand2.IsConstantZero) ||
				(node.Operand2.IsVirtualRegister && node.Operand1.IsConstant && node.Operand1.IsConstantZero)))
				return;

			var operand = (node.Operand2.IsConstant && node.Operand2.IsConstantZero) ? node.Operand1 : node.Operand2;

			if (operand.Uses.Count != 1)
				return;

			if (operand.Definitions.Count != 1)
				return;

			Debug.Assert(operand.Definitions.Count == 1);

			var node2 = operand.Definitions[0];

			if (node2.Instruction != IRInstruction.IntegerCompare)
				return;

			AddOperandUsageToWorkList(node2);
			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("*** FoldIntegerCompare");
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.ConditionCode = node.ConditionCode == ConditionCode.NotEqual ? node2.ConditionCode : node2.ConditionCode.GetOpposite();
			node.Operand1 = node2.Operand1;
			node.Operand2 = node2.Operand2;
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
			if (trace.Active) trace.Log("REMOVED:\t" + node2.ToString());
			node2.SetInstruction(IRInstruction.Nop);
			foldIntegerCompareCount++;
			instructionsRemovedCount++;
			changeCount++;
		}

		/// <summary>
		/// Simplifies sign/zero extended move
		/// </summary>
		/// <param name="node">The node.</param>
		private void SimplifyExtendedMove(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.ZeroExtendedMove || node.Instruction == IRInstruction.SignExtendedMove))
				return;

			if (!node.Result.IsVirtualRegister || !node.Operand1.IsVirtualRegister)
				return;

			if (!((NativePointerSize == 4 && node.Result.IsInt && (node.Operand1.IsInt || node.Operand1.IsU || node.Operand1.IsI)) ||
				(NativePointerSize == 4 && node.Operand1.IsInt && (node.Result.IsInt || node.Result.IsU || node.Result.IsI)) ||
				(NativePointerSize == 8 && node.Result.IsLong && (node.Operand1.IsLong || node.Operand1.IsU || node.Operand1.IsI)) ||
				(NativePointerSize == 8 && node.Operand1.IsLong && (node.Result.IsLong || node.Result.IsU || node.Result.IsI))))
				return;

			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("*** SimplifyExtendedMove");
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.SetInstruction(IRInstruction.Move, node.Result, node.Operand1);
			simplifyExtendedMoveCount++;
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
			changeCount++;
		}

		private void FoldLoadStoreOffsets(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (!(node.Instruction == IRInstruction.Load || node.Instruction == IRInstruction.Store
				|| node.Instruction == IRInstruction.LoadSignExtended || node.Instruction == IRInstruction.LoadZeroExtended))
				return;

			if (!node.Operand2.IsConstant)
				return;

			if (!node.Operand1.IsVirtualRegister)
				return;

			if (node.Operand1.Uses.Count != 1)
				return;

			var node2 = node.Operand1.Definitions[0];

			if (!(node2.Instruction == IRInstruction.AddSigned || node2.Instruction == IRInstruction.SubSigned ||
				node2.Instruction == IRInstruction.AddUnsigned || node2.Instruction == IRInstruction.SubUnsigned))
				return;

			if (!node2.Operand2.IsConstant)
				return;

			Operand constant;

			if (node2.Instruction == IRInstruction.AddUnsigned || node2.Instruction == IRInstruction.AddSigned)
			{
				constant = Operand.CreateConstant(node.Operand2.Type, node2.Operand2.ConstantSignedLongInteger + node.Operand2.ConstantSignedLongInteger);
			}
			else
			{
				constant = Operand.CreateConstant(node.Operand2.Type, node.Operand2.ConstantSignedLongInteger - node2.Operand2.ConstantSignedLongInteger);
			}

			if (trace.Active) trace.Log("*** FoldLoadStoreOffsets");
			AddOperandUsageToWorkList(node);
			AddOperandUsageToWorkList(node2);
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			node.Operand1 = node2.Operand1;
			node.Operand2 = constant;
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
			if (trace.Active) trace.Log("REMOVED:\t" + node2.ToString());
			node2.SetInstruction(IRInstruction.Nop);
			foldLoadStoreOffsetsCount++;
			instructionsRemovedCount++;
			changeCount++;
		}

		/// <summary>
		/// Folds the constant phi instruction.
		/// </summary>
		/// <param name="node">The node.</param>
		private void ConstantFoldingPhi(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.Phi)
				return;

			if (node.Result.Definitions.Count != 1)
				return;

			if (!node.Result.IsInteger)
				return;

			Operand operand1 = node.Operand1;
			Operand result = node.Result;

			foreach (var operand in node.Operands)
			{
				if (!operand.IsConstant)
					return;

				if (operand.ConstantUnsignedLongInteger != operand1.ConstantUnsignedLongInteger)
					return;
			}

			if (trace.Active) trace.Log("*** FoldConstantPhiInstruction");
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			AddOperandUsageToWorkList(node);
			node.SetInstruction(IRInstruction.Move, result, operand1);
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
			constantFoldingPhiCount++;
			changeCount++;
		}

		/// <summary>
		/// Simplifies the phi.
		/// </summary>
		/// <param name="node">The node.</param>
		private void SimplifyPhi(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.Phi)
				return;

			if (node.OperandCount != 1)
				return;

			if (node.Result.Definitions.Count != 1)
				return;

			if (trace.Active) trace.Log("*** SimplifyPhiInstruction");
			if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
			AddOperandUsageToWorkList(node);
			node.SetInstruction(IRInstruction.Move, node.Result, node.Operand1);
			if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
			simplifyPhiCount++;
			changeCount++;
		}

		private void DeadCodeEliminationPhi(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.Instruction != IRInstruction.Phi)
				return;

			if (node.Result.Definitions.Count != 1)
				return;

			var result = node.Result;

			foreach (var use in result.Uses)
			{
				if (use != node)
					return;
			}

			AddOperandUsageToWorkList(node);
			if (trace.Active) trace.Log("*** DeadCodeEliminationPhi");
			if (trace.Active) trace.Log("REMOVED:\t" + node.ToString());
			node.SetInstruction(IRInstruction.Nop);
			deadCodeEliminationPhi++;
			changeCount++;
		}

		private static bool IsPowerOfTwo(ulong n)
		{
			return (n & (n - 1)) == 0;
		}

		private static uint GetPowerOfTwo(ulong n)
		{
			uint bits = 0;
			while (n > 0)
			{
				bits++;
				n >>= 1;
			}

			return bits - 1;
		}

		private bool Reduce64BitOperationsTo32Bit()
		{
			if (Architecture.NativeIntegerSize != 32)
				return false;

			bool change = false;

			foreach (var register in virtualRegisters)
			{
				Debug.Assert(register.IsVirtualRegister);

				if (register.Definitions.Count != 1)
					continue;

				if (!register.IsLong)
					continue;

				if (!CanReduceTo32Bit(register))
					continue;

				var v = MethodCompiler.CreateVirtualRegister(TypeSystem.BuiltIn.U4);

				if (trace.Active) trace.Log("*** Reduce64BitOperationsTo32Bit");

				ReplaceVirtualRegister(register, v, true);

				reduce64BitOperationsTo32BitCount++;
				changeCount++;
				change = true;
			}

			return change;
		}

		private bool CanReduceTo32Bit(Operand local)
		{
			foreach (var node in local.Uses)
			{
				if (node.Instruction == IRInstruction.AddressOf)
					return false;

				if (node.Instruction == IRInstruction.Call)
					return false;

				if (node.Instruction == IRInstruction.Return)
					return false;

				if (node.Instruction == IRInstruction.SubSigned)
					return false;

				if (node.Instruction == IRInstruction.SubUnsigned)
					return false;

				if (node.Instruction == IRInstruction.DivSigned)
					return false;

				if (node.Instruction == IRInstruction.DivUnsigned)
					return false;

				if (node.Instruction == IRInstruction.Load || node.Instruction == IRInstruction.LoadSignExtended || node.Instruction == IRInstruction.LoadZeroExtended)
					if (node.Result == local)
						return false;

				if (node.Instruction == IRInstruction.ShiftRight || node.Instruction == IRInstruction.ArithmeticShiftRight)
					if (node.Operand1 == local)
						return false;

				if (node.Instruction == IRInstruction.Phi)
					return false;

				if (node.Instruction == IRInstruction.IntegerCompareBranch)
					return false;

				if (node.Instruction == IRInstruction.IntegerCompare)
					return false;

				if (node.Instruction == IRInstruction.RemSigned)
					return false;

				if (node.Instruction == IRInstruction.RemUnsigned)
					return false;

				if (node.Instruction == IRInstruction.Move)
					if (node.Operand1.IsParameter)
						return false;
			}

			return true;
		}

		private void ReplaceVirtualRegister(Operand local, Operand replacement, bool updateMove)
		{
			if (trace.Active) trace.Log("Replacing: " + local.ToString() + " with " + replacement.ToString());

			foreach (var node in local.Uses.ToArray())
			{
				AddOperandUsageToWorkList(node);

				for (int i = 0; i < node.OperandCount; i++)
				{
					var operand = node.GetOperand(i);

					if (local == operand)
					{
						if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
						node.SetOperand(i, replacement);

						if (updateMove)
						{
							if (node.Instruction == IRInstruction.ZeroExtendedMove)
							{
								node.Instruction = IRInstruction.Move;
								node.Size = InstructionSize.None;
							}
						}

						if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
					}
				}
			}

			foreach (var node in local.Definitions.ToArray())
			{
				AddOperandUsageToWorkList(node);

				for (int i = 0; i < node.ResultCount; i++)
				{
					var operand = node.GetResult(i);

					if (local == operand)
					{
						if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
						node.SetResult(i, replacement);

						if (updateMove)
						{
							if (node.Instruction == IRInstruction.ZeroExtendedMove)
							{
								node.Instruction = IRInstruction.Move;
								node.Size = InstructionSize.None;
							}
						}

						if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
					}
				}
			}
		}

		private void NormalizeConstantTo32Bit(InstructionNode node)
		{
			if (node.IsEmpty)
				return;

			if (node.ResultCount != 1)
				return;

			if (!node.Result.IsInt)
				return;

			if (node.Instruction == IRInstruction.LogicalAnd ||
				node.Instruction == IRInstruction.LogicalOr ||
				node.Instruction == IRInstruction.LogicalXor ||
				node.Instruction == IRInstruction.LogicalNot)
			{
				if (node.Operand1.IsConstant && node.Operand1.IsLong)
				{
					if (trace.Active) trace.Log("*** NormalizeConstantTo32Bit");

					if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
					node.Operand1 = Operand.CreateConstant(TypeSystem, (int)(node.Operand1.ConstantUnsignedLongInteger & uint.MaxValue));
					changeCount++;
					if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
					AddOperandUsageToWorkList(node);
				}
				if (node.OperandCount >= 2 && node.Operand2.IsConstant && node.Operand2.IsLong)
				{
					if (trace.Active) trace.Log("*** NormalizeConstantTo32Bit");

					if (trace.Active) trace.Log("BEFORE:\t" + node.ToString());
					node.Operand2 = Operand.CreateConstant(TypeSystem, (int)(node.Operand2.ConstantUnsignedLongInteger & uint.MaxValue));
					changeCount++;
					if (trace.Active) trace.Log("AFTER: \t" + node.ToString());
					AddOperandUsageToWorkList(node);
				}
			}
		}
	}
}
