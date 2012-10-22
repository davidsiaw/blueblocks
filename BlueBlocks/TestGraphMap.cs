using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using BlueBlocksLib.Controls;

namespace BlueBlocks
{
    public partial class TestGraphMap : Form
    {
        GraphMap<int> gm = new GraphMap<int>();

        public TestGraphMap()
        {
            InitializeComponent();
            gm.Dock = DockStyle.Fill;
            splitContainer1.Panel2.Controls.Add(gm);
        }

        int a = 0;

        private void btn_add_Click(object sender, EventArgs e)
        {
            var box = gm.AddBox("Node " + a, Color.LightGreen, a);
            box.AddAction("Link", self => {
                gm.SelectNode(node => {
                    self.LinkTo(node);
                });
            });
            
            a++;
        }

        private void btn_remove_Click(object sender, EventArgs e)
        {
            gm.SelectNode(node => {
                gm.Delete(node);
            });
        }
    }
}
