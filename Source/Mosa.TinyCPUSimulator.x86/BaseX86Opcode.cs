﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

using System;
using System.Diagnostics;

namespace Mosa.TinyCPUSimulator.x86
{
	public class BaseX86Opcode : BaseOpcode
	{
		public override void Execute(SimCPU cpu, SimInstruction instruction)
		{
			var x86 = cpu as CPUx86;

			x86.EIP.Value = x86.EIP.Value + instruction.OpcodeSize;

			Execute(x86, instruction);
		}

		public virtual void Execute(CPUx86 cpu, SimInstruction instruction)
		{
		}

		protected static bool IsSign(ulong v, int size)
		{
			if (size == 32) return IsSign((uint)v);
			else if (size == 16) return IsSign((ushort)v);
			else if (size == 8) return IsSign((byte)v);

			throw new SimCPUException();
		}

		protected static bool IsSign(uint v)
		{
			return (((v >> 31) & 0x1) == 1);
		}

		protected static bool IsSign(ushort v)
		{
			return (((v >> 15) & 0x1) == 1);
		}

		protected static bool IsSign(byte v)
		{
			return (((v >> 7) & 0x1) == 1);
		}

		protected static bool IsAdjustAfterAdd(ulong v1, ulong v2)
		{
			return (((v1 & 0xF) + (v2 & 0xF)) > 16);
		}

		protected static bool IsAdjustAfterSub(ulong v1, ulong v2)
		{
			return ((v1 & 0xF) < (v2 & 0xF));
		}

		protected void UpdateParity(CPUx86 cpu, uint u)
		{
			bool parity = false;

			for (int i = 0; i < 8; i++)
			{
				if ((u & 0x1) == 1)
					parity = !parity;

				u = u >> 1;
			}

			cpu.EFLAGS.Parity = parity;
		}

		protected void UpdateFlags(CPUx86 cpu, int size, long s, ulong u, bool zeroFlag, bool parityParity, bool signFlag, bool carryFlag, bool overFlowFlag)
		{
			if (size == 32)
				UpdateFlags32(cpu, s, u, zeroFlag, parityParity, signFlag, carryFlag, overFlowFlag);
			else if (size == 16)
				UpdateFlags16(cpu, s, u, zeroFlag, parityParity, signFlag, carryFlag, overFlowFlag);
			else
				UpdateFlags8(cpu, s, u, zeroFlag, parityParity, signFlag, carryFlag, overFlowFlag);

			UpdateParity(cpu, (uint)u);
		}

		protected void UpdateFlags8(CPUx86 cpu, long s, ulong u, bool zeroFlag, bool parityParity, bool signFlag, bool carryFlag, bool overFlowFlag)
		{
			if (zeroFlag)
				cpu.EFLAGS.Zero = ((u & 0xFF) == 0);
			if (overFlowFlag)
				cpu.EFLAGS.Overflow = (s < byte.MinValue || s > byte.MaxValue);
			if (carryFlag)
				cpu.EFLAGS.Carry = ((u >> 8) != 0);
			if (signFlag)
				cpu.EFLAGS.Sign = (((u >> 7) & 0x01) != 0);
		}

		protected void UpdateFlags16(CPUx86 cpu, long s, ulong u, bool zeroFlag, bool parityParity, bool signFlag, bool carryFlag, bool overFlowFlag)
		{
			if (zeroFlag)
				cpu.EFLAGS.Zero = ((u & 0xFFFF) == 0);
			if (overFlowFlag)
				cpu.EFLAGS.Overflow = (s < short.MinValue || s > short.MaxValue);
			if (carryFlag)
				cpu.EFLAGS.Carry = ((u >> 16) != 0);
			if (signFlag)
				cpu.EFLAGS.Sign = (((u >> 15) & 0x01) != 0);
		}

		protected void UpdateFlags32(CPUx86 cpu, long s, ulong u, bool zeroFlag, bool parityParity, bool signFlag, bool carryFlag, bool overFlowFlag)
		{
			if (zeroFlag)
				cpu.EFLAGS.Zero = ((u & 0xFFFFFFFF) == 0);
			if (overFlowFlag)
				cpu.EFLAGS.Overflow = (s < int.MinValue || s > int.MaxValue);
			if (carryFlag)
				cpu.EFLAGS.Carry = ((u >> 32) != 0);
			if (signFlag)
				cpu.EFLAGS.Sign = (((u >> 31) & 0x01) != 0);
		}

		protected uint GetAddress(CPUx86 cpu, SimOperand operand)
		{
			Debug.Assert(operand.IsMemory);

			int address = 0;

			if (operand.Index != null)
			{
				// Memory = Register + (Base * Scale) + Displacement
				address = (int)(((operand.Index) as GeneralPurposeRegister).Value * operand.Scale);
			}

			if (operand.Register != null)
			{
				address = address + (int)((operand.Register) as GeneralPurposeRegister).Value + operand.Displacement;
			}
			else
			{
				address = (int)operand.Immediate;
			}

			return (uint)address;
		}

		protected uint ResolveLabel(CPUx86 cpu, SimOperand operand)
		{
			Debug.Assert(operand.IsLabel);

			uint address = (uint)cpu.GetSymbol(operand.Label).Address;

			if (operand.IsMemory)
			{
				return Read(cpu, address, operand.Size);
			}
			else
			{
				return address;
			}
		}

		protected uint LoadValue(CPUx86 cpu, SimOperand operand)
		{
			if (operand.IsImmediate)
			{
				return (uint)operand.Immediate;
			}

			if (operand.IsRegister)
			{
				return ((operand.Register) as Register32Bit).Value;
			}

			if (operand.IsLabel)
			{
				return ResolveLabel(cpu, operand);
			}

			if (operand.IsMemory)
			{
				uint address = GetAddress(cpu, operand);

				return Read(cpu, address, operand.Size);
			}

			throw new SimCPUException();
		}

		protected void StoreValue(CPUx86 cpu, SimOperand operand, uint value, int size)
		{
			Debug.Assert(!operand.IsImmediate);

			if (operand.IsRegister)
			{
				((operand.Register) as Register32Bit).Value = value;
			}
			else if (operand.IsLabel)
			{
				uint address = (uint)cpu.GetSymbol(operand.Label).Address;

				Write(cpu, address, value, size);

				return;
			}
			else if (operand.IsMemory)
			{
				uint address = GetAddress(cpu, operand);

				Write(cpu, address, value, size);
			}
		}

		protected FloatingValue LoadFloatValue(CPUx86 cpu, SimOperand operand, int size)
		{
			if (operand.IsRegister)
			{
				return ((operand.Register) as RegisterFloatingPoint).Value;
			}

			if (operand.IsLabel)
			{
				uint address = (uint)cpu.GetSymbol(operand.Label).Address;

				return ReadFloat(cpu, address, size);
			}

			if (operand.IsMemory)
			{
				uint address = GetAddress(cpu, operand);

				return ReadFloat(cpu, address, size);
			}

			throw new SimCPUException();
		}

		protected void StoreFloatValue(CPUx86 cpu, SimOperand operand, float value, int size)
		{
			var val = new FloatingValue()
			{
				LowF = value
			};
			StoreFloatValue(cpu, operand, val, size);
		}

		protected void StoreFloatValue(CPUx86 cpu, SimOperand operand, double value, int size)
		{
			var val = new FloatingValue()
			{
				Low = value
			};
			StoreFloatValue(cpu, operand, val, size);
		}

		protected void StoreFloatValue(CPUx86 cpu, SimOperand operand, FloatingValue value, int size)
		{
			Debug.Assert(!operand.IsImmediate);

			if (operand.IsRegister)
			{
				var newValue = ((operand.Register) as RegisterFloatingPoint).Value;
				switch (size)
				{
					case 128:
						newValue = value;
						break;

					case 64:
						newValue.Low = value.Low;
						break;

					case 32:
						newValue.LowF = value.LowF;
						break;
				}
				((operand.Register) as RegisterFloatingPoint).Value = newValue;
			}

			if (operand.IsLabel)
			{
				uint address = (uint)cpu.GetSymbol(operand.Label).Address;

				WriteFloat(cpu, address, value, size);
			}

			if (operand.IsMemory)
			{
				uint address = GetAddress(cpu, operand);

				WriteFloat(cpu, address, value, size);
			}
		}

		protected uint Read(CPUx86 cpu, uint address, int size)
		{
			if (size == 32) return cpu.Read32(address);
			else if (size == 16) return cpu.Read16(address);
			else if (size == 8) return cpu.Read8(address);

			throw new SimCPUException();
		}

		protected void Write(CPUx86 cpu, uint address, uint value, int size)
		{
			if (size == 32) cpu.Write32(address, value);
			else if (size == 16) cpu.Write16(address, (ushort)value);
			else if (size == 8) cpu.Write8(address, (byte)value);
		}

		protected FloatingValue ReadFloat(CPUx86 cpu, uint address, int size)
		{
			if (size == 128)
			{
				return new FloatingValue()
				{
					ULow = cpu.Read64(address),
					UHigh = cpu.Read64(address + 0x8)
				};
			}
			else if (size == 64)
			{
				return new FloatingValue()
				{
					ULow = cpu.Read64(address)
				};
			}
			else if (size == 32)
			{
				var val = new FloatingValue();
				var b = BitConverter.GetBytes(cpu.Read32(address));
				val.LowF = BitConverter.ToSingle(b, 0);
				return val;
			}

			throw new SimCPUException();
		}

		protected void WriteFloat(CPUx86 cpu, uint address, FloatingValue value, int size)
		{
			if (size == 128)
			{
				cpu.Write64(address, value.ULow);
				cpu.Write64(address + 0x08, value.UHigh);
			}
			else if (size == 64)
			{
				cpu.Write64(address, value.ULow);
			}
			else if (size == 32)
			{
				var b = BitConverter.GetBytes(value.LowF);
				uint val = BitConverter.ToUInt32(b, 0);
				cpu.Write32(address, val);
			}
		}

		protected uint ResolveBranch(CPUx86 cpu, SimOperand operand)
		{
			if (operand.IsLabel)
				return ResolveLabel(cpu, operand);
			else if (operand.IsImmediate)
				return (uint)(cpu.EIP.Value + (long)operand.Immediate);

			throw new InvalidProgramException();
		}
	}
}
