﻿/*
 * (c) 2013 MOSA - The Managed Operating System Alliance
 *
 * Licensed under the terms of the New BSD License.
 *
 * Authors:
 *  Phil Garcia (tgiphil) <phil@thinkedge.com>
 *
 */

using Mosa.TinyCPUSimulator.TestSystem.xUnit;

namespace Mosa.TinyCPUSimulator.Debug
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			Test2();
		}

		private static void Test2()
		{
			var int32Fixture = new Int32Fixture();

			int32Fixture.AddI4I4(1, 1);
		}

		private static void Test1()
		{
			var test = new TestCPUx86();

			test.RunTest();
		}
	}
}