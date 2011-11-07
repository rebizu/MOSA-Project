﻿/*
 * (c) 2008 MOSA - The Managed Operating System Alliance
 *
 * Licensed under the terms of the New BSD License.
 *
 * Authors:
 *  Simon Wollwage (rootnode) <kintaro@think-in-co.de>
 */

using System.Collections.Generic;
using Mosa.Compiler.Framework;
using Mosa.Compiler.Framework.Operands;
using Mosa.Compiler.TypeSystem;
using Mosa.Compiler.Metadata;
using Mosa.Compiler.Metadata.Signatures;

namespace Mosa.Platform.x86.Intrinsic
{
	/// <summary>
	/// Representations the x86 Lgdt instruction.
	/// </summary>
	public sealed class Lgdt : IIntrinsicMethod
	{

		#region Methods

		/// <summary>
		/// Replaces the intrinsic call site
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="typeSystem">The type system.</param>
		public void ReplaceIntrinsicCall(Context context, ITypeSystem typeSystem, IList<RuntimeParameter> parameters)
		{
			MemoryOperand operand = new MemoryOperand(BuiltInSigType.Ptr, GeneralPurposeRegister.EAX, new System.IntPtr(0));
			context.SetInstruction(CPUx86.Instruction.MovInstruction, new RegisterOperand(BuiltInSigType.Ptr, GeneralPurposeRegister.EAX), context.Operand1);
			context.AppendInstruction(CPUx86.Instruction.LgdtInstruction, null, operand);

			RegisterOperand ax = new RegisterOperand(BuiltInSigType.Int16, GeneralPurposeRegister.EAX);
			RegisterOperand ds = new RegisterOperand(BuiltInSigType.Int16, SegmentRegister.DS);
			RegisterOperand es = new RegisterOperand(BuiltInSigType.Int16, SegmentRegister.ES);
			RegisterOperand fs = new RegisterOperand(BuiltInSigType.Int16, SegmentRegister.FS);
			RegisterOperand gs = new RegisterOperand(BuiltInSigType.Int16, SegmentRegister.GS);
			RegisterOperand ss = new RegisterOperand(BuiltInSigType.Int16, SegmentRegister.SS);

			context.AppendInstruction(CPUx86.Instruction.MovInstruction, ax, new ConstantOperand(BuiltInSigType.Int32, (int)0x00000010));
			context.AppendInstruction(CPUx86.Instruction.MovInstruction, ds, ax);
			context.AppendInstruction(CPUx86.Instruction.MovInstruction, es, ax);
			context.AppendInstruction(CPUx86.Instruction.MovInstruction, fs, ax);
			context.AppendInstruction(CPUx86.Instruction.MovInstruction, gs, ax);
			context.AppendInstruction(CPUx86.Instruction.MovInstruction, ss, ax);
			context.AppendInstruction(CPUx86.Instruction.FarJmpInstruction);
		}

		#endregion // Methods

	}
}
