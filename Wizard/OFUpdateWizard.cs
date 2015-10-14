using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TemplateWizard;
using System.Windows.Forms;
using EnvDTE;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using EnvDTE80;
using System.Diagnostics;
using System.Threading.Tasks;

namespace of {
    public sealed class UpdateWizard : IWizard
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);

        public void BeforeOpeningFile(ProjectItem projectItem)
        {

        }

        public void ProjectFinishedGenerating(Project project)
        {
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {

        }

        public void collapse(UIHierarchyItem item)
        {
        }


        public void RunFinished()
        {
        }

        private static async void DoNotWait(Task task) {
            await task;
        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            dte = (EnvDTE.DTE)automationObject;
            String destinationDirectory = replacementsDictionary["$destinationdirectory$"];
            DoNotWait(Task.Run(() =>
            {
                //dte.Commands.Raise
            }));
            throw new WizardCancelledException();
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return false;
        }

        private Form1 inputForm;
        private List<string> addons;
        private EnvDTE.DTE dte;
        private string itemName;
    };
}