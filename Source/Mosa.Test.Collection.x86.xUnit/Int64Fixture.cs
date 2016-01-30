﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Xunit;
using Xunit.Extensions;

namespace Mosa.Test.Collection.x86.xUnit
{
	public class Int64Fixture : X86TestFixture
	{
		[Theory]

		//[MemberData("I8I8", DisableDiscoveryEnumeration = true)]
		[InlineData((long)1, (long)2)]
		public void AddI8I8(long a, long b)
		{
			Assert.Equal(Int64Tests.AddI8I8(a, b), Run<long>("Mosa.Test.Collection.Int64Tests.AddI8I8", a, b));
		}

		[Theory]
		[MemberData("I8I8", DisableDiscoveryEnumeration = true)]
		public void SubI8I8(long a, long b)
		{
			Assert.Equal(Int64Tests.SubI8I8(a, b), Run<long>("Mosa.Test.Collection.Int64Tests.SubI8I8", a, b));
		}

		[Theory]
		[MemberData("I8I8", DisableDiscoveryEnumeration = true)]
		public void MulI8I8(long a, long b)
		{
			Assert.Equal(Int64Tests.MulI8I8(a, b), Run<long>("Mosa.Test.Collection.Int64Tests.MulI8I8", a, b));
		}

		[Theory]
		[MemberData("I8I8", DisableDiscoveryEnumeration = true)]
		public void DivI8I8(long a, long b)
		{
			if (a == long.MinValue && b == -1)
			{
				//	Assert.Inconclusive("TODO: Overflow exception not implemented");
				return;
			}

			if (b == 0)
			{
				return;
			}

			Assert.Equal(Int64Tests.DivI8I8(a, b), Run<long>("Mosa.Test.Collection.Int64Tests.DivI8I8", a, b));
		}

		//[Theory]
		//[ExpectedException(typeof(DivideByZeroException))]
		public void DivI8I8DivideByZeroException(long a)
		{
			Assert.Equal(Int64Tests.DivI8I8(a, 0), Run<long>("Mosa.Test.Collection.Int64Tests.DivI8I8", a, (long)0));
		}

		[Theory]
		[MemberData("I8I8", DisableDiscoveryEnumeration = true)]
		public void RemI8I8(long a, long b)
		{
			if (b == 0)
			{
				return;
			}

			// C# can't handle this case and throws an OverflowException, but MOSA can handle it
			if (a == -9223372036854775808 && b == -1)
			{
				Assert.Equal(0, Run<long>("Mosa.Test.Collection.Int64Tests.RemI8I8", a, b));
				return;
			}

			Assert.Equal(Int64Tests.RemI8I8(a, b), Run<long>("Mosa.Test.Collection.Int64Tests.RemI8I8", a, b));
		}

		//[Theory]
		//[ExpectedException(typeof(DivideByZeroException))]
		public void RemI8I8DivideByZeroException(long a)
		{
			Assert.Equal(Int64Tests.RemI8I8(a, 0), Run<long>("Mosa.Test.Collection.Int64Tests.RemI8I8", a, (long)0));
		}

		[Theory]
		[MemberData("I8", DisableDiscoveryEnumeration = true)]
		public void RetI8(long a)
		{
			Assert.Equal(Int64Tests.RetI8(a), Run<long>("Mosa.Test.Collection.Int64Tests.RetI8", a));
		}

		[Theory]
		[MemberData("I8I8", DisableDiscoveryEnumeration = true)]
		public void AndI8I8(long a, long b)
		{
			Assert.Equal(Int64Tests.AndI8I8(a, b), Run<long>("Mosa.Test.Collection.Int64Tests.AndI8I8", a, b));
		}

		[Theory]
		[MemberData("I8I8", DisableDiscoveryEnumeration = true)]
		public void OrI8I8(long a, long b)
		{
			Assert.Equal(Int64Tests.OrI8I8(a, b), Run<long>("Mosa.Test.Collection.Int64Tests.OrI8I8", a, b));
		}

		[Theory]
		[MemberData("I8I8", DisableDiscoveryEnumeration = true)]
		public void XorI8I8(long a, long b)
		{
			Assert.Equal(Int64Tests.XorI8I8(a, b), Run<long>("Mosa.Test.Collection.Int64Tests.XorI8I8", a, b));
		}

		[Theory]
		[MemberData("I8", DisableDiscoveryEnumeration = true)]
		public void CompI8(long a)
		{
			Assert.Equal(Int64Tests.CompI8(a), Run<long>("Mosa.Test.Collection.Int64Tests.CompI8", a));
		}

		[Theory]
		[MemberData("I8U1UpTo32", DisableDiscoveryEnumeration = true)]
		public void ShiftLeftI8I8(long a, byte b)
		{
			Assert.Equal(Int64Tests.ShiftLeftI8U1(a, b), Run<long>("Mosa.Test.Collection.Int64Tests.ShiftLeftI8U1", a, b));
		}

		[Theory]
		[MemberData("I8U1UpTo32", DisableDiscoveryEnumeration = true)]
		public void ShiftRightI8I8(long a, byte b)
		{
			Assert.Equal(Int64Tests.ShiftRightI8U1(a, b), Run<long>("Mosa.Test.Collection.Int64Tests.ShiftRightI8U1", a, b));
		}

		[Theory]
		[MemberData("I8I8", DisableDiscoveryEnumeration = true)]
		public void CeqI8I8(long a, long b)
		{
			Assert.Equal(Int64Tests.CeqI8I8(a, b), Run<bool>("Mosa.Test.Collection.Int64Tests.CeqI8I8", a, b));
		}

		[Theory]
		[MemberData("I8I8", DisableDiscoveryEnumeration = true)]
		public void CltI8I8(long a, long b)
		{
			Assert.Equal(Int64Tests.CltI8I8(a, b), Run<bool>("Mosa.Test.Collection.Int64Tests.CltI8I8", a, b));
		}

		[Theory]
		[MemberData("I8I8", DisableDiscoveryEnumeration = true)]
		public void CgtI8I8(long a, long b)
		{
			Assert.Equal(Int64Tests.CgtI8I8(a, b), Run<bool>("Mosa.Test.Collection.Int64Tests.CgtI8I8", a, b));
		}

		[Theory]
		[MemberData("I8I8", DisableDiscoveryEnumeration = true)]
		public void CleI8I8(long a, long b)
		{
			Assert.Equal(Int64Tests.CleI8I8(a, b), Run<bool>("Mosa.Test.Collection.Int64Tests.CleI8I8", a, b));
		}

		[Theory]
		[MemberData("I8I8", DisableDiscoveryEnumeration = true)]
		public void CgeI8I8(long a, long b)
		{
			Assert.Equal(Int64Tests.CgeI8I8(a, b), Run<bool>("Mosa.Test.Collection.Int64Tests.CgeI8I8", a, b));
		}

		[Fact]
		public void Newarr()
		{
			Assert.True(Run<bool>("Mosa.Test.Collection.Int64Tests.Newarr"));
		}

		[Theory]
		[MemberData("I4Small", DisableDiscoveryEnumeration = true)]
		public void Ldlen(int length)
		{
			Assert.True(Run<bool>("Mosa.Test.Collection.Int64Tests.Ldlen", length));
		}

		[Theory]
		[MemberData("I4SmallI8", DisableDiscoveryEnumeration = true)]
		public void StelemI8(int index, long value)
		{
			Assert.True(Run<bool>("Mosa.Test.Collection.Int64Tests.Stelem", index, value));
		}

		[Theory]
		[MemberData("I4SmallI8", DisableDiscoveryEnumeration = true)]
		public void LdelemI8(int index, long value)
		{
			Assert.Equal(Int64Tests.Ldelem(index, value), Run<long>("Mosa.Test.Collection.Int64Tests.Ldelem", index, value));
		}

		[Theory]
		[MemberData("I4SmallI8", DisableDiscoveryEnumeration = true)]
		public void LdelemaI8(int index, long value)
		{
			Assert.Equal(Int64Tests.Ldelema(index, value), Run<long>("Mosa.Test.Collection.Int64Tests.Ldelema", index, value));
		}
	}
}
