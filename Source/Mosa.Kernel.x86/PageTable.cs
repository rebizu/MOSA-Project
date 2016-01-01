﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Kernel.Helpers;
using Mosa.Kernel.x86.Helpers;
using Mosa.Platform.Internal.x86;
using System.Runtime.InteropServices;

namespace Mosa.Kernel.x86
{
	unsafe public static class PageTable
	{
		internal static PageDirectoryEntry* pageDirectoryEntries;
		internal static PageTableEntry* pageTableEntries;

		// We can have up to 1024 PageDirectories
		public const uint PageDirectoryCount = 1024;

		public const uint PageDirectorySize = PageDirectoryCount * PageDirectoryEntry.EntrySize;

		// For each PageDirectory, we can have up to 1024 PageTables
		public const uint PageTableCount = 1024;

		public const uint PageTableSize = PageTableCount * PageTableEntry.EntrySize; // Size of 1024 PageTables

		public const uint PageTablesMaxCount = PageDirectoryCount * PageTableCount; // The sum of 1024 PageTables in 1024 PageDirectories
		public const uint PageTablesMaxSize = PageTablesMaxCount * PageTableEntry.EntrySize;  // Size of 1024 PageTables in 1024 PageDirectories

		/// <summary>
		/// Sets up the PageTable
		/// </summary>
		public static void Setup()
		{
			pageDirectoryEntries = (PageDirectoryEntry*)Address.PageDirectory;
			pageTableEntries = (PageTableEntry*)Address.PageTable;

			ClearPageDirectory();
			ClearPageTable();

			// Map the first 128MB of memory (32768 4K pages) (why 128MB?)
			for (uint index = 0; index < 1024 * 32; index++)
				AllocatePage();

			MapVirtualAddressToPhysical(0xFFC00000, 0x0);
			MapVirtualAddressToPhysical(0x0, 0x0, false);

			//Panic.DumpMemory(pageDirectoryAddress);

			// Set CR3 register on processor - sets page directory
			Native.SetCR3(Address.PageDirectory);

			// Set CR0 register on processor - turns on virtual memory
			Native.SetCR0(Native.GetCR0() | BitMask.Bit31);
		}

		public static void ClearPageDirectory()
		{
			Memory.Clear(Address.PageDirectory, PageDirectorySize);
		}

		public static void ClearPageTable()
		{
			Memory.Clear(Address.PageTable, PageTablesMaxSize);
		}

		internal static PageDirectoryEntry* GetPageDirectoryEntry(uint index)
		{
			Assert.InRange(index, PageDirectoryCount);
			return pageDirectoryEntries + index;
		}

		public static PageTableEntry* GetPageTableEntry(uint index)
		{
			Assert.InRange(index, PageTablesMaxCount);
			return pageTableEntries + index;
		}

		private static uint currentDictionaryEntryCount;
		private static uint currentTableEntryCount;
		private static uint allTableEntryCount;

		private static PageDirectoryEntry* RegisterPage(PageTableEntry* page)
		{
			if (currentDictionaryEntryCount == 0 || currentTableEntryCount == 1024)
			{
				currentDictionaryEntryCount++;
				var dicEntry = GetPageDirectoryEntry(currentDictionaryEntryCount - 1);
				dicEntry->PageTableEntry = page;
				dicEntry->Present = true;
				dicEntry->Writable = true;
				dicEntry->User = true;
				currentTableEntryCount = 0;
			}
			return GetPageDirectoryEntry(currentDictionaryEntryCount - 1);
		}

		public static void AllocatePage()
		{
			currentTableEntryCount++;
			allTableEntryCount++;

			var tabEntry = GetPageTableEntry(allTableEntryCount - 1);
			tabEntry->PhysicalAddress = (allTableEntryCount - 1) * 4096;
			tabEntry->Present = true;
			tabEntry->Writable = true;
			tabEntry->User = true;

			RegisterPage(tabEntry);
		}

		/// <summary>
		/// Maps the virtual address to physical.
		/// </summary>
		/// <param name="virtualAddress">The virtual address.</param>
		/// <param name="physicalAddress">The physical address.</param>
		public static void MapVirtualAddressToPhysical(uint virtualAddress, uint physicalAddress, bool present = true)
		{
			//FUTURE: traverse page directory from CR3 --- do not assume page table is linearly allocated
			Native.Set32(Address.PageTable + ((virtualAddress & 0xFFC00000u) >> 10), (uint)(physicalAddress & 0xFFC00000u | 0x04u | 0x02u | (present ? 0x1u : 0x0u)));
		}

		/// <summary>
		/// Gets the physical memory.
		/// </summary>
		/// <param name="virtualAddress">The virtual address.</param>
		/// <returns></returns>
		public static uint GetPhysicalAddressFromVirtual(uint virtualAddress)
		{
			//FUTURE: traverse page directory from CR3 --- do not assume page table is linearly allocated
			return Native.Get32(Address.PageTable + ((virtualAddress & 0xFFFFF000u) >> 10)) + (virtualAddress & 0xFFFu);
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 4)]
	unsafe public struct PageDirectoryEntry
	{
		[FieldOffset(0)]
		private uint data;

		public const byte EntrySize = 4;

		private class Offset
		{
			public const byte Present = 0;
			public const byte Readonly = 1;
			public const byte User = 2;
			public const byte WriteThrough = 3;
			public const byte DisableCache = 4;
			public const byte Accessed = 5;
			private const byte UNKNOWN6 = 6;
			public const byte PageSize4Mib = 7;
			private const byte IGNORED8 = 8;
			public const byte Custom = 9;
			public const byte Address = 12;
		}

		private const byte AddressBitSize = 20;
		private const uint AddressMask = 0xFFFFF000;

		private uint PageTableAddress
		{
			get { return data & AddressMask; }
			set
			{
				Assert.True(value << AddressBitSize == 0, "PageDirectoryEntry.Address needs to be 4k aligned");
				data = data.SetBits(Offset.Address, AddressBitSize, value, Offset.Address);
			}
		}

		internal PageTableEntry* PageTableEntry
		{
			get { return (PageTableEntry*)PageTableAddress; }
			set { PageTableAddress = (uint)value; }
		}

		public bool Present
		{
			get { return data.IsBitSet(Offset.Present); }
			set { data = data.SetBit(Offset.Present, value); }
		}

		public bool Writable
		{
			get { return data.IsBitSet(Offset.Readonly); }
			set { data = data.SetBit(Offset.Readonly, value); }
		}

		public bool User
		{
			get { return data.IsBitSet(Offset.User); }
			set { data = data.SetBit(Offset.User, value); }
		}

		public bool WriteThrough
		{
			get { return data.IsBitSet(Offset.WriteThrough); }
			set { data = data.SetBit(Offset.WriteThrough, value); }
		}

		public bool DisableCache
		{
			get { return data.IsBitSet(Offset.DisableCache); }
			set { data = data.SetBit(Offset.DisableCache, value); }
		}

		public bool Accessed
		{
			get { return data.IsBitSet(Offset.Accessed); }
			set { data = data.SetBit(Offset.Accessed, value); }
		}

		public bool PageSize4Mib
		{
			get { return data.IsBitSet(Offset.PageSize4Mib); }
			set { data = data.SetBit(Offset.PageSize4Mib, value); }
		}

		public byte Custom
		{
			get { return (byte)data.GetBits(Offset.Custom, 2); }
			set { data = data.SetBits(Offset.Custom, 2, value); }
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 4)]
	public struct PageTableEntry
	{
		[FieldOffset(0)]
		private uint Value;

		public const byte EntrySize = 4;

		private class Offset
		{
			public const byte Present = 0;
			public const byte Readonly = 1;
			public const byte User = 2;
			public const byte WriteThrough = 3;
			public const byte DisableCache = 4;
			public const byte Accessed = 5;
			public const byte Dirty = 6;
			private const byte SIZE0 = 7;
			public const byte Global = 8;
			public const byte Custom = 9;
			public const byte Address = 12;
		}

		private const byte AddressBitSize = 20;
		private const uint AddressMask = 0xFFFFF000;

		/// <summary>
		/// 4k aligned physical address
		/// </summary>
		public uint PhysicalAddress
		{
			get { return Value & AddressMask; }
			set
			{
				Assert.True(value << AddressBitSize == 0, "PageTableEntry.PhysicalAddress needs to be 4k aligned");
				Value = Value.SetBits(Offset.Address, AddressBitSize, value, Offset.Address);
			}
		}

		public bool Present
		{
			get { return Value.IsBitSet(Offset.Present); }
			set { Value = Value.SetBit(Offset.Present, value); }
		}

		public bool Writable
		{
			get { return Value.IsBitSet(Offset.Readonly); }
			set { Value = Value.SetBit(Offset.Readonly, value); }
		}

		public bool User
		{
			get { return Value.IsBitSet(Offset.User); }
			set { Value = Value.SetBit(Offset.User, value); }
		}

		public bool WriteThrough
		{
			get { return Value.IsBitSet(Offset.WriteThrough); }
			set { Value = Value.SetBit(Offset.WriteThrough, value); }
		}

		public bool DisableCache
		{
			get { return Value.IsBitSet(Offset.DisableCache); }
			set { Value = Value.SetBit(Offset.DisableCache, value); }
		}

		public bool Accessed
		{
			get { return Value.IsBitSet(Offset.Accessed); }
			set { Value = Value.SetBit(Offset.Accessed, value); }
		}

		public bool Global
		{
			get { return Value.IsBitSet(Offset.Global); }
			set { Value = Value.SetBit(Offset.Global, value); }
		}

		public bool Dirty
		{
			get { return Value.IsBitSet(Offset.Dirty); }
			set { Value = Value.SetBit(Offset.Dirty, value); }
		}

		public byte Custom
		{
			get { return (byte)Value.GetBits(Offset.Custom, 2); }
			set { Value = Value.SetBits(Offset.Custom, 2, value); }
		}
	}
}
