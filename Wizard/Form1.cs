using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace of
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);

        public Form1()
        {
            InitializeComponent();

            OutputDebugString("Listing directories in C:\\Users\\arturo\\Code\\openFrameworks\\addons ");
            DirectoryInfo di = new DirectoryInfo(getOFRoot() + "\\addons");
            var directories = di.GetDirectories("ofx*", SearchOption.TopDirectoryOnly);

            foreach (DirectoryInfo d in directories)
            {
                checkedListBox1.Items.Add(d.Name);
            }
        }

        public List<string> getAddons()
        {
            List<string> addons = new List<string>();
            foreach (var item in this.checkedListBox1.CheckedItems)
            {
                addons.Add(item.ToString());
            }
            return addons;
        }

        public string getOFRoot()
        {
            return "C:\\Users\\arturo\\Code\\openFrameworks";
        }
    }
}
