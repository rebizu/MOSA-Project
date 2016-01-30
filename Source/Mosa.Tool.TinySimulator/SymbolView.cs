// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Compiler.Linker;
using Mosa.TinyCPUSimulator;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Mosa.Tool.TinySimulator
{
	public partial class SymbolView : SimulatorDockContent
	{
		private List<SymbolEntry> symbols;

		private class SymbolEntry
		{
			public string Name { get { return LinkerSymbol.Name; } }

			public ulong Length { get { return LinkerSymbol.Size; } }

			public string VirtualAddress { get { return MainForm.Format(LinkerSymbol.VirtualAddress, display32); } }

			public ulong Offset { get { return LinkerSymbol.SectionOffset; } }

			public string SectionKind { get; set; }

			public string SectionAddress { get { return MainForm.Format(LinkerSymbol.SectionOffset, display32); } }

			public LinkerSymbol LinkerSymbol;

			private bool display32;

			public SymbolEntry(LinkerSymbol linkerSymbol, bool display32)
			{
				LinkerSymbol = linkerSymbol;
				this.display32 = display32;
				SectionKind = linkerSymbol.SectionKind.ToString();
			}
		}

		public SymbolView(MainForm mainForm)
			: base(mainForm)
		{
			InitializeComponent();
		}

		private void SymbolView_Load(object sender, EventArgs e)
		{
			toolStripComboBox1.SelectedIndex = 0;
			toolStripComboBox2.SelectedIndex = 0;
		}

		public void CreateEntries()
		{
			if (MainForm.Compiler.Linker == null)
				return;

			symbols = new List<SymbolEntry>();

			string filter = toolStripTextBox1.Text.Trim();
			string kind = toolStripComboBox1.SelectedIndex < 1 ? string.Empty : toolStripComboBox1.SelectedItem.ToString().Trim();

			uint start = 0;
			uint end = UInt32.MaxValue;

			switch (toolStripComboBox2.SelectedIndex)
			{
				case 1: start = 1; end = 1; break;
				case 2: start = 2; end = 2; break;
				case 3: start = 4; end = 4; break;
				case 4: start = 8; end = 8; break;
				default: break;
			}

			foreach (var symbol in MainForm.Compiler.Linker.Symbols)
			{
				if (!(filter.Length == 0 || symbol.Name.Contains(filter)))
					continue;

				if (kind != string.Empty && symbol.SectionKind.ToString() != kind)
					continue;

				if (symbol.Size > end || symbol.Size < start)
					continue;

				symbols.Add(new SymbolEntry(symbol, MainForm.Display32));
			}

			dataGridView1.DataSource = symbols;
			dataGridView1.AutoResizeColumns();
			dataGridView1.Columns[0].Width = 500;
		}

		public override void UpdateDock(BaseSimState simState)
		{
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			CreateEntries();
		}

		private SymbolEntry clickedSymbolEntry;

		private void dataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.Button != MouseButtons.Right)
				return;

			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;

			var row = dataGridView1.Rows[e.RowIndex].DataBoundItem as SymbolEntry;

			clickedSymbolEntry = row;

			var relativeMousePosition = dataGridView1.PointToClient(Cursor.Position);

			MenuItem menu = new MenuItem(row.Name);
			menu.Enabled = false;
			ContextMenu m = new ContextMenu();
			m.MenuItems.Add(menu);
			m.MenuItems.Add(new MenuItem("Copy to &Clipboard", new EventHandler(MenuItem3_Click)));
			m.MenuItems.Add(new MenuItem("Add to &Watch List", new EventHandler(MenuItem1_Click)));
			m.MenuItems.Add(new MenuItem("Set &Breakpoint", new EventHandler(MenuItem2_Click)));
			m.Show(dataGridView1, relativeMousePosition);
		}

		private void MenuItem1_Click(Object sender, EventArgs e)
		{
			if (clickedSymbolEntry == null)
				return;

			MainForm.AddWatch(clickedSymbolEntry.Name, clickedSymbolEntry.LinkerSymbol.VirtualAddress, (int)clickedSymbolEntry.LinkerSymbol.Size);
		}

		private void MenuItem2_Click(Object sender, EventArgs e)
		{
			if (clickedSymbolEntry == null)
				return;

			MainForm.AddBreakpoint(clickedSymbolEntry.Name, clickedSymbolEntry.LinkerSymbol.VirtualAddress);
		}

		private void MenuItem3_Click(Object sender, EventArgs e)
		{
			if (clickedSymbolEntry == null)
				return;

			Clipboard.SetText(clickedSymbolEntry.Name);
		}

		private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			CreateEntries();
		}

		private void toolStripComboBox2_SelectedIndexChanged(object sender, EventArgs e)
		{
			CreateEntries();
		}
	}
}
