using MajiroStringEditor;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MSEGui {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            try {
                textBox1.Text = listBox1.Items[listBox1.SelectedIndex].ToString();
            } catch { }
        }

        Obj1 Script;
        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "All Majiro Scripts|*.mjo";
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            Script = new Obj1(File.ReadAllBytes(fd.FileName));
            string[] Strings = Script.Import();

            listBox1.Items.Clear();
            listBox1.Items.AddRange(Strings);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveFileDialog fd = new SaveFileDialog();
            fd.Filter = "All Majiro Scripts|*.mjo";
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            string[] Strings = listBox1.Items.Cast<string>().ToArray();

            File.WriteAllBytes(fd.FileName, Script.Export(Strings, true));
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                e.Handled = true;
                try {
                    listBox1.Items[listBox1.SelectedIndex] = textBox1.Text;
                } catch { }
            }
        }
    }
}
