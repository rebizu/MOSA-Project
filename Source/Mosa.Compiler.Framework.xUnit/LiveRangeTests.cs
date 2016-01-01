﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Compiler.Framework.IR;
using Mosa.Compiler.Framework.RegisterAllocator;
using System.Collections.Generic;
using Xunit;

namespace Mosa.Compiler.Framework.xUnit
{
	public class LiveRangeTests
	{
		private static BasicBlocks CreateBasicBlockInstructionSet()
		{
			var basicBlocks = new BasicBlocks();

			var block = basicBlocks.CreateBlock();
			basicBlocks.AddHeadBlock(block);

			var context = new Context(block);

			context.AppendInstruction(IRInstruction.Nop);
			context.AppendInstruction(IRInstruction.Nop);
			context.AppendInstruction(IRInstruction.Nop);
			context.AppendInstruction(IRInstruction.Nop);
			context.AppendInstruction(IRInstruction.Nop);
			context.AppendInstruction(IRInstruction.Nop);

			return basicBlocks;
		}

		[Fact]
		public void LiveRangeTest()
		{
			var basicBlocks = CreateBasicBlockInstructionSet();

			GreedyRegisterAllocator.NumberInstructions(basicBlocks);

			var liveRange = new LiveRange(
				new SlotIndex(basicBlocks[0].First),
				new SlotIndex(basicBlocks[0].Last),
				new List<SlotIndex>(),
				new List<SlotIndex>()
			);

			Assert.True(liveRange.IsEmpty);

			//liveRange.SplitAt(basicBlocks.)
		}
	}
}
