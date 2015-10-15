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

        public Form1(string projectPath)
        {
            InitializeComponent();

            var addonsMake = new FileInfo(projectPath + "\\addons.make");
            if (addonsMake.Exists)
            {
                var addonsMakeStrm = addonsMake.OpenText();
                while (!addonsMakeStrm.EndOfStream)
                {
                    var line = addonsMakeStrm.ReadLine().Trim();
                    if (line.Count() == 0)
                    {
                        continue;
                    }
                    if (line.First() == '#')
                    {
                        continue;
                    }
                    if (line.Contains("#"))
                    {
                        line = line.Split('#')[0].Trim();
                    }
                    currentAddons.Add(line);
                }
            }

            DirectoryInfo di = new DirectoryInfo(getOFRoot() + "\\addons");
            var directories = di.GetDirectories("ofx*", SearchOption.TopDirectoryOnly);

            foreach (DirectoryInfo d in directories)
            {
                var idx = checkedListBox1.Items.Add(d.Name);
                if (currentAddons.Contains(d.Name))
                {
                    checkedListBox1.SetItemChecked(idx, true);
                }
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

        public List<string> getProjectCurrentAddons()
        {
            return currentAddons;
        }

        public string getOFRoot()
        {
            return "C:\\Users\\arturo\\Code\\openFrameworks";
        }

        List<string> currentAddons = new List<string>();
    }
}
