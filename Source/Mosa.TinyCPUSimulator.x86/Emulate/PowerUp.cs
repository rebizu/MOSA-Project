﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

namespace Mosa.TinyCPUSimulator.x86.Emulate
{
	public class PowerUp : BaseSimDevice
	{
		public readonly uint VectorReset = 0xFFFFFFF0;
		public readonly string VectorCall = "System.Void Default::StartUp()";

		public PowerUp(SimCPU simCPU)
			: base(simCPU)
		{
		}

		public override BaseSimDevice Clone(SimCPU simCPU)
		{
			return new PowerUp(simCPU);
		}

		public override void Initialize()
		{
			var x86 = simCPU as CPUx86;

			simCPU.AddMemory(0x00000000, 0x000A0000, 1); // First 640kb
			simCPU.AddMemory(VectorReset, 0x0000000F, 2); // Vector Reset

			simCPU.AddInstruction(VectorReset, new SimInstruction(Opcode.Call, 32, SimOperand.CreateLabel(32, VectorCall), 4));
		}

		public override void Reset()
		{
			var x86 = simCPU as CPUx86;

			// Start of stack
			x86.ESP.Value = 0x003FFFFC;
			x86.EBP.Value = x86.ESP.Value;

			// Start EIP
			x86.EIP.Value = VectorReset;
		}
	}
}
