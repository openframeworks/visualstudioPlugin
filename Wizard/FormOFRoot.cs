using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace of
{
    public partial class FormOFRoot : Form
    {
        public FormOFRoot()
        {
            InitializeComponent();
        }

        private void searchfolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.Cancel)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            var correct = Wizard.isOFDirectory(textBox1.Text);
            okButton.Enabled = correct;
            errorLabel.Visible = !correct;
            labelOk.Visible = correct;
        }

        public string getOFRoot()
        {
            return textBox1.Text;
        }
    }
}
