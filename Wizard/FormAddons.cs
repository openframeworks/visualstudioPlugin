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
    public partial class FormAddons : Form
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);

        public FormAddons(string projectPath, string ofRoot)
        {
            InitializeComponent();

            string[] officialAddons =
            {
                "ofx3DModelLoader",
                "ofxAssimpModelLoader",
                "ofxGui",
                "ofxKinect",
                "ofxNetwork",
                "ofxOpenCv",
                "ofxOsc",
                "ofxSvg",
                "ofxThreadedImageLoader",
                "ofxUnitTests",
                "ofxVectorGraphics",
                "ofxXmlSettings",
            };

            string[] specialAddons =
            {
                "ofxAccelerometer",
                "ofxAndroid",
                "ofxEmscripten",
                "ofxMultiTouch",
                "ofxiOS",
            };

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
            DirectoryInfo di;
            if (Path.IsPathRooted(ofRoot)){
                di = new DirectoryInfo(ofRoot + "\\addons");
            }
            else
            {
                string[] paths = { projectPath, ofRoot, "addons" };
                di = new DirectoryInfo(Path.Combine(paths));
            }
            var directories = di.GetDirectories("ofx*", SearchOption.TopDirectoryOnly);

            var presentOfficialAddons = directories.Select((dir) => { return dir.Name; })
                .Intersect(officialAddons);
            foreach (var addon in presentOfficialAddons)
            {
                var idx = officialAddonsList.Items.Add(addon);
                if (currentAddons.Contains(addon))
                {
                    officialAddonsList.SetItemChecked(idx, true);
                }
            }

            var presentCommunityAddons = directories.Select((dir) => { return dir.Name; })
                .Except(officialAddons)
                .Except(specialAddons);
            foreach (var addon in presentCommunityAddons)
            {
                var idx = communityAddonsList.Items.Add(addon);
                if (currentAddons.Contains(addon))
                {
                    communityAddonsList.SetItemChecked(idx, true);
                }
            }
        }

        public List<string> getAddons()
        {
            List<string> addons = new List<string>();
            foreach (var item in officialAddonsList.CheckedItems)
            {
                addons.Add(item.ToString());
            }
            foreach (var item in communityAddonsList.CheckedItems)
            {
                addons.Add(item.ToString());
            }
            return addons;
        }

        public List<string> getProjectCurrentAddons()
        {
            return currentAddons;
        }

        List<string> currentAddons = new List<string>();
    }
}
