﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Runtime.Plug;

namespace Mosa.Runtime.x86
{
	internal static class SpinLock
	{
		[Method("System.Threading.SpinLock.InternalEnter")]
		public static void Enter(ref bool spinlock)
		{
			while (!Native.SyncCompareAndSwap(ref spinlock, 0, 1))
			{
				Native.Pause();
			}
		}

		[Method("System.Threading.SpinLock.InternalExit")]
		public static void Exit(ref bool spinlock)
		{
			Native.SyncSet(ref spinlock, 0);
		}
	}
}
