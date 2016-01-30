﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.AppSystem;
using System;
using Mosa.Kernel.x86;

namespace Mosa.Application
{
	/// <summary>
	///
	/// </summary>
	public class ShowMem : BaseApplication, IConsoleApp
	{
		public override int Start()
		{
			Console.WriteLine("*** Memory ****");
			Console.WriteLine();
			Console.Write("Total Pages : ");
			Console.WriteLine(PageFrameAllocator.TotalPages.ToString());
			Console.Write("Used Pages  : ");
			Console.WriteLine(PageFrameAllocator.TotalPagesInUse.ToString());
			Console.Write("Page Size   : ");
			Console.WriteLine(PageFrameAllocator.PageSize.ToString());
			Console.Write("Free Memory : ");
			Console.Write(((PageFrameAllocator.TotalPages - PageFrameAllocator.TotalPagesInUse) * PageFrameAllocator.PageSize / (1024 * 1024)).ToString());
			Console.WriteLine(" KB");

			return 0;
		}
	}
}
