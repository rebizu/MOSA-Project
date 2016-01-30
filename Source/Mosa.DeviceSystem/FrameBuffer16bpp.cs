﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

namespace Mosa.DeviceSystem
{
	/// <summary>
	/// Implementation of FrameBuffer with 16 Bits Per Pixel
	/// </summary>
	public sealed class FrameBuffer16bpp : FrameBuffer, IFrameBuffer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FrameBuffer16bpp"/> class.
		/// </summary>
		/// <param name="memory">The memory.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="depth">The depth.</param>
		public FrameBuffer16bpp(IMemory memory, uint width, uint height, uint offset, uint depth)
		{
			this.memory = memory;
			this.width = width;
			this.height = height;
			this.offset = offset;
			this.depth = depth;
		}

		/// <summary>
		/// Gets the offset.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns></returns>
		protected override uint GetOffset(uint x, uint y)
		{
			return offset + (y * depth) + (x << 1);
		}

		/// <summary>
		/// Gets the pixel.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns></returns>
		public override uint GetPixel(uint x, uint y)
		{
			return memory.Read16(GetOffset(x, y));
		}

		/// <summary>
		/// Sets the pixel.
		/// </summary>
		/// <param name="color">The color.</param>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		public override void SetPixel(uint color, uint x, uint y)
		{
			memory.Write16(GetOffset(x, y), (ushort)color);
		}
	}
}
