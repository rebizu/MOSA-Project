// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Compiler.Linker;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Mosa.Compiler.Framework.Stages
{
	/// <summary>
	/// An compilation stage, which generates a map file of the built binary file.
	/// </summary>
	public sealed class MapFileGenerationStage : BaseCompilerStage
	{
		#region Data members

		public string MapFile { get; set; }

		/// <summary>
		/// Holds the text writer used to emit the map file.
		/// </summary>
		private TextWriter writer;

		#endregion Data members

		protected override void Setup()
		{
			MapFile = CompilerOptions.MapFile;
		}

		protected override void Run()
		{
			if (string.IsNullOrEmpty(MapFile))
				return;

			using (writer = new StreamWriter(MapFile))
			{
				// Emit map file header
				writer.WriteLine(CompilerOptions.OutputFile);
				writer.WriteLine();
				writer.WriteLine("Timestamp is {0}", DateTime.Now);
				writer.WriteLine();
				writer.WriteLine("Preferred load address is {0:x16}", Linker.BaseAddress);
				writer.WriteLine();

				// Emit the sections
				EmitSections(Linker);
				writer.WriteLine();

				// Emit all symbols
				EmitSymbols(Linker);

				writer.Close();
			}
		}

		#region Internals

		/// <summary>
		/// Emits all the section created in the binary file.
		/// </summary>
		/// <param name="linker">The linker.</param>
		private void EmitSections(BaseLinker linker)
		{
			writer.WriteLine("Offset           Virtual          Length           Name                             Class");
			foreach (var section in linker.Sections)
			{
				writer.WriteLine("{0:x16} {1:x16} {2:x16} {3} {4}", section.FileOffset, section.VirtualAddress, section.Size, section.Name.PadRight(32), section.SectionKind);
			}
		}

		/// <summary>
		/// Emits all symbols emitted in the binary file.
		/// </summary>
		/// <param name="linker">The linker.</param>
		private void EmitSymbols(BaseLinker linker)
		{
			writer.WriteLine("Virtual          Offset           Length           Symbol");

			foreach (var section in linker.Sections)
			{
				foreach (var symbol in section.Symbols)
				{
					writer.WriteLine("{0:x16} {1:x16} {2:x16} {3} {4}", symbol.VirtualAddress, symbol.SectionOffset, symbol.Size, symbol.SectionKind.ToString().PadRight(7), symbol.Name);
				}
			}

			var entryPoint = linker.EntryPoint;
			if (entryPoint != null)
			{
				writer.WriteLine();
				writer.WriteLine("Entry point is {0}", entryPoint.Name);
				writer.WriteLine("\tat Offset {0:x16}", entryPoint.SectionOffset); // TODO! add section offset too?
				writer.WriteLine("\tat virtual address {0:x16}", entryPoint.VirtualAddress);
			}

			writer.WriteLine();
			writer.WriteLine("Hash Table:");
			writer.WriteLine();
			writer.WriteLine("Virtual          Size     Pre-Hash  Post-Hash Symbol");

			var symbols = linker.Symbols.OrderBy(symbol => symbol.Name);

			foreach (var symbol in symbols)
			{
				if (symbol.SectionKind == SectionKind.Text)
				{
					writer.WriteLine("{0:x16} {1:x8} {2} {3}  {4}", symbol.VirtualAddress, symbol.Size, ExtractHash(symbol.PreHash), ExtractHash(symbol.PostHash), symbol.Name);
				}
			}

			writer.WriteLine();
			writer.WriteLine("Pre-Hash Table:");
			writer.WriteLine();
			writer.WriteLine("Hash     Size     Symbol");

			var symbols2 = linker.Symbols.OrderBy(symbol => symbol.Name);

			foreach (var symbol in symbols2)
			{
				if (symbol.SectionKind == SectionKind.Text)
				{
					writer.WriteLine("{0} {1:x8} {2}", ExtractHash(symbol.PreHash), symbol.Size, symbol.Name);
				}
			}
		}

		private string ExtractHash(string hash)
		{
			if (hash.Length > 8)
				return hash.Substring(hash.Length - 8, 8);

			return hash.PadLeft(8, '-');
		}

		#endregion Internals
	}
}
