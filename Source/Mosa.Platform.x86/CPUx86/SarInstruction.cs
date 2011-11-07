﻿/*
 * (c) 2008 MOSA - The Managed Operating System Alliance
 *
 * Licensed under the terms of the New BSD License.
 *
 * Authors:
 *  Simon Wollwage (rootnode) <kintaro@think-in-co.de>
 */

using System;

using Mosa.Compiler.Framework;
using Mosa.Compiler.Framework.Operands;
using Mosa.Compiler.Metadata;
using Mosa.Compiler.Metadata.Signatures;

namespace Mosa.Platform.x86.CPUx86
{
	/// <summary>
	/// Intermediate representation of the arithmetic shift right instruction.
	/// </summary>
	public sealed class SarInstruction : TwoOperandInstruction
	{
		#region Codes
		private static readonly OpCode R = new OpCode(new byte[] { 0xD3 }, 7);
		private static readonly OpCode M = new OpCode(new byte[] { 0xD3 }, 7);
		private static readonly OpCode R_C = new OpCode(new byte[] { 0xC1 }, 7);
		private static readonly OpCode M_C = new OpCode(new byte[] { 0xC1 }, 7);
		#endregion

		#region Methods
		/// <summary>
		/// 
		/// </summary>
		/// <param name="destination"></param>
		/// <param name="source"></param>
		/// <param name="third"></param>
		/// <returns></returns>
		protected override OpCode ComputeOpCode(Operand destination, Operand source, Operand third)
		{
			if ((destination is RegisterOperand) && (source is ConstantOperand)) return R_C;
			if ((destination is MemoryOperand) && (source is ConstantOperand)) return M_C;
			if (destination is RegisterOperand) return R;
			if (destination is MemoryOperand) return M;
			throw new ArgumentException(@"No opcode for operand type.");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="emitter"></param>
		protected override void Emit(Context ctx, MachineCodeEmitter emitter)
		{
			OpCode opCode = ComputeOpCode(ctx.Result, ctx.Operand1, ctx.Operand2);
			if (ctx.Operand1 is ConstantOperand)
			{
				ConstantOperand op = ctx.Operand1 as ConstantOperand;
				op = new ConstantOperand(BuiltInSigType.Byte, op.Value);
				emitter.Emit(opCode, ctx.Result, op);
			}
			else
				emitter.Emit(opCode, ctx.Operand1, null);
		}

		/// <summary>
		/// Allows visitor based dispatch for this instruction object.
		/// </summary>
		/// <param name="visitor">The visitor object.</param>
		/// <param name="context">The context.</param>
		public override void Visit(IX86Visitor visitor, Context context)
		{
			visitor.Sar(context);
		}

		#endregion // Methods
	}
}
