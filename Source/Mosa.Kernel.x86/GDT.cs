﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Runtime.x86;

namespace Mosa.Kernel.x86
{
	/// <summary>
	///
	/// </summary>
	public static class GDT
	{
		#region Data members

		internal struct Offset
		{
			internal const byte LimitLow = 0x00;
			internal const byte BaseLow = 0x02;
			internal const byte BaseMiddle = 0x04;
			internal const byte Access = 0x05;
			internal const byte Granularity = 0x06;
			internal const byte BaseHigh = 0x07;
			internal const byte TotalSize = 0x08;
		}

		#endregion Data members

		public static void Setup()
		{
			Memory.Clear(Address.GDTTable, 6);
			Native.Set16(Address.GDTTable, (Offset.TotalSize * 3) - 1);
			Native.Set32(Address.GDTTable + 2, Address.GDTTable + 6);

			Set(0, 0, 0, 0, 0);                // Null segment
			Set(1, 0, 0xFFFFFFFF, 0x9A, 0xCF); // Code segment
			Set(2, 0, 0xFFFFFFFF, 0x92, 0xCF); // Data segment

			Native.Lgdt(Address.GDTTable);
		}

		private static void Set(uint index, uint address, uint limit, byte access, byte granularity)
		{
			uint entry = GetEntryLocation(index);
			Native.Set16(entry + Offset.BaseLow, (ushort)(address & 0xFFFF));
			Native.Set8(entry + Offset.BaseMiddle, (byte)((address >> 16) & 0xFF));
			Native.Set8(entry + Offset.BaseHigh, (byte)((address >> 24) & 0xFF));
			Native.Set16(entry + Offset.LimitLow, (ushort)(limit & 0xFFFF));
			Native.Set8(entry + Offset.Granularity, (byte)(((byte)(limit >> 16) & 0x0F) | (granularity & 0xF0)));
			Native.Set8(entry + Offset.Access, access);
		}

		/// <summary>
		/// Gets the GTP entry location.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		private static uint GetEntryLocation(uint index)
		{
			return Address.GDTTable + 6 + (index * Offset.TotalSize);
		}
	}
}
