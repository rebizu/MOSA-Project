// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.TinyCPUSimulator;
using System;

namespace Mosa.Tool.TinySimulator
{
	public partial class RegisterView : SimulatorDockContent
	{
		public RegisterView(MainForm mainForm)
			: base(mainForm)
		{
			InitializeComponent();
		}

		private void GeneralPurposeRegistersView_Load(object sender, EventArgs e)
		{
			dataGridView1.Rows.Clear();
		}

		public override void UpdateDock(BaseSimState simState)
		{
			dataGridView1.Rows.Clear();

			foreach (var name in simState.RegisterList)
			{
				dataGridView1.Rows.Add(name, MainForm.Format(simState.GetRegister(name)));
			}

			Refresh();
		}
	}
}
