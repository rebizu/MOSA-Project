﻿/*
 * (c) 2008 MOSA - The Managed Operating System Alliance
 *
 * Licensed under the terms of the New BSD License.
 *
 * Authors:
 *  Phil Garcia (tgiphil) <phil@thinkedge.com>
 */

using Mosa.Runtime;
using Mosa.Runtime.x86;

namespace Mosa.Kernel.x86
{
	/// <summary>
	/// Static class of helpful memory functions
	/// </summary>
	public static class Multiboot
	{
		private static uint multibootptr = 0x200004;
		private static uint multibootsignature = 0x200000;

		/// <summary>
		/// Location of the Multiboot Structure
		/// </summary>
		public static uint MultibootAddress { get; private set; }

		/// <summary>
		/// Magic value that indicates that kernel was loaded by a Multiboot-compliant boot loader
		/// </summary>
		public const uint MultibootMagic = 0x2BADB002;

		/// <summary>
		///
		/// </summary>
		private static uint memoryMapCount = 0;

		/// <summary>
		/// Gets the memory map count.
		/// </summary>
		/// <value>The memory map count.</value>
		public static uint MemoryMapCount { get { return memoryMapCount; } }

		/// <summary>
		/// Setups this multiboot.
		/// </summary>
		public static void Setup()
		{
			uint magic = Native.GetMultibootEAX();
			uint address = Native.GetMultibootEBX();

			if (magic == MultibootMagic)
			{
				MultibootAddress = address;
				CountMemoryMap();
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is multiboot enabled.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is multiboot enabled; otherwise, <c>false</c>.
		/// </value>
		public static bool IsMultibootEnabled { get { return (MultibootAddress != 0x0); } }

		/// <summary>
		/// Gets the flags.
		/// </summary>
		/// <value>The flags.</value>
		public static uint Flags { get { return Intrinsic.Load32(MultibootAddress); } }

		/// <summary>
		/// Gets the memory lower.
		/// </summary>
		/// <value>The lower memory.</value>
		public static uint MemoryLower { get { return Intrinsic.Load32(MultibootAddress, 4); } }

		/// <summary>
		/// Gets the memory upper.
		/// </summary>
		/// <value>The memory upper.</value>
		public static uint MemoryUpper { get { return Intrinsic.Load32(MultibootAddress, 8); } }

		/// <summary>
		/// Gets the boot device.
		/// </summary>
		/// <value>The boot device.</value>
		public static uint BootDevice { get { return Intrinsic.Load32(MultibootAddress, 12); } }

		/// <summary>
		/// Gets the CMD line address.
		/// </summary>
		/// <value>The CMD line address.</value>
		public static uint CmdLineAddress { get { return Intrinsic.Load32(MultibootAddress, 16); } }

		/// <summary>
		/// Gets the modules start.
		/// </summary>
		/// <value>The modules start.</value>
		public static uint ModulesStart { get { return Intrinsic.Load32(MultibootAddress, 20); } }

		/// <summary>
		/// Gets the modules count.
		/// </summary>
		/// <value>The modules count.</value>
		public static uint ModulesCount { get { return Intrinsic.Load32(MultibootAddress, 24); } }

		/// <summary>
		/// Gets the length of the memory map.
		/// </summary>
		/// <value>The length of the memory map.</value>
		public static uint MemoryMapLength { get { return Intrinsic.Load32(MultibootAddress, 44); } }

		/// <summary>
		/// Gets the memory map start.
		/// </summary>
		/// <value>The memory map start.</value>
		public static uint MemoryMapStart { get { return Intrinsic.Load32(MultibootAddress, 48); } }

		/// <summary>
		/// Counts the memory map.
		/// </summary>
		private static void CountMemoryMap()
		{
			memoryMapCount = 0;
			uint location = MemoryMapStart;

			while (location < (MemoryMapStart + MemoryMapLength))
			{
				memoryMapCount++;
				location = Intrinsic.Load32(location) + location + 4;
			}
		}

		/// <summary>
		/// Gets the memory map index location.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		private static uint GetMemoryMapIndexLocation(uint index)
		{
			uint location = MemoryMapStart;

			for (uint i = 0; i < index; i++)
			{
				location = location + Intrinsic.Load32(location) + 4;
			}

			return location;
		}

		/// <summary>
		/// Gets the memory map base.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		public static uint GetMemoryMapBase(uint index)
		{
			return Intrinsic.Load32(GetMemoryMapIndexLocation(index), 4);
		}

		/// <summary>
		/// Gets the length of the memory map.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		public static uint GetMemoryMapLength(uint index)
		{
			return Intrinsic.Load32(GetMemoryMapIndexLocation(index), 12);
		}

		/// <summary>
		/// Gets the type of the memory map.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		public static byte GetMemoryMapType(uint index)
		{
			return Native.Get8(GetMemoryMapIndexLocation(index) + 20);
		}

		/// <summary>
		/// Gets the length of the drive.
		/// </summary>
		/// <value>The length of the drive.</value>
		public static uint DriveLength { get { return Intrinsic.Load32(MultibootAddress, 52); } }

		/// <summary>
		/// Gets the drive start.
		/// </summary>
		/// <value>The drive start.</value>
		public static uint DriveStart { get { return Intrinsic.Load32(MultibootAddress, 56); } }

		/// <summary>
		/// Gets the configuration table.
		/// </summary>
		/// <value>The configuration table.</value>
		public static uint ConfigurationTable { get { return Intrinsic.Load32(MultibootAddress, 60); } }

		/// <summary>
		/// Gets the name of the boot loader.
		/// </summary>
		/// <value>The name of the boot loader.</value>
		public static uint BootLoaderName { get { return Intrinsic.Load32(MultibootAddress, 64); } }

		/// <summary>
		/// Gets the APM table.
		/// </summary>
		/// <value>The APM table.</value>
		public static uint APMTable { get { return Intrinsic.Load32(MultibootAddress, 68); } }

		/// <summary>
		/// Gets the VBE control information.
		/// </summary>
		/// <value>The VBE control information.</value>
		public static uint VBEControlInformation { get { return Intrinsic.Load32(MultibootAddress, 72); } }

		/// <summary>
		/// Gets the VBE mode info.
		/// </summary>
		/// <value>The VBE mode info.</value>
		public static uint VBEModeInfo { get { return Intrinsic.Load32(MultibootAddress, 72); } }

		/// <summary>
		/// Gets the VBE mode.
		/// </summary>
		/// <value>The VBE mode.</value>
		public static uint VBEMode { get { return Intrinsic.Load32(MultibootAddress, 76); } }

		/// <summary>
		/// Gets the VBE interface seg.
		/// </summary>
		/// <value>The VBE interface seg.</value>
		public static uint VBEInterfaceSeg { get { return Intrinsic.Load32(MultibootAddress, 80); } }

		/// <summary>
		/// Gets the VBE interface off.
		/// </summary>
		/// <value>The VBE interface off.</value>
		public static uint VBEInterfaceOff { get { return Intrinsic.Load32(MultibootAddress, 84); } }

		/// <summary>
		/// Gets the VBE interface len.
		/// </summary>
		/// <value>The VBE interface len.</value>
		public static uint VBEInterfaceLen { get { return Intrinsic.Load32(MultibootAddress, 86); } }
	}
}
