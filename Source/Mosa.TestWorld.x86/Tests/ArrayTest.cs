﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

using System;
using System.Collections.Generic;

namespace Mosa.TestWorld.x86.Tests
{
	public class ArrayTest : KernelTest
	{
		public ArrayTest()
			: base("Array")
		{
			testMethods.AddLast(GenericInterfaceTest);
			testMethods.AddLast(ArrayBoundsCheck);

			// TODO: add more tests
		}

		public static bool GenericInterfaceTest()
		{
			int[] list = new int[] { 1, 3, 5 };
			IList<int> iList = list;

			int result = 0;
			foreach (var i in iList)
				result += i;
			return (result == 9);
		}

		public static bool ArrayBoundsCheck()
		{
			int[] myArray = new int[1];
			try
			{
				myArray[1] = 20;
				return false;
			}
			catch (IndexOutOfRangeException ex)
			{
				return true;
			}
		}
	}
}
