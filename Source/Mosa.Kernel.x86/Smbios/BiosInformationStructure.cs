// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Runtime.x86;

namespace Mosa.Kernel.x86.Smbios
{
	/// <summary>
	///
	/// </summary>
	public class BiosInformationStructure : SmbiosStructure
	{
		/// <summary>
		///
		/// </summary>
		private string biosVendor = string.Empty;

		/// <summary>
		///
		/// </summary>
		private string biosVersion = string.Empty;

		/// <summary>
		///
		/// </summary>
		private string biosDate = string.Empty;

		/// <summary>
		///
		/// </summary>
		public BiosInformationStructure()
			: base(SmbiosManager.GetStructureOfType(0x00))
		{
			biosVendor = GetStringFromIndex(Native.Get8(address + 0x04u));
			biosVersion = GetStringFromIndex(Native.Get8(address + 0x05u));
			biosDate = GetStringFromIndex(Native.Get8(address + 0x08u));
		}

		/// <summary>
		///
		/// </summary>
		public string BiosVendor
		{
			get { return biosVendor; }
		}

		/// <summary>
		///
		/// </summary>
		public string BiosVersion
		{
			get { return biosVersion; }
		}

		/// <summary>
		///
		/// </summary>
		public string BiosDate
		{
			get { return biosDate; }
		}
	}
}
