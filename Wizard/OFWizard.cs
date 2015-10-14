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

namespace of {
    public class Wizard : IWizard
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);

        public void BeforeOpeningFile(ProjectItem projectItem)
        {

        }

        public void ProjectFinishedGenerating(Project project)
        {
            VCProject vcproject = null;
            vcproject = project.Object as VCProject;
            addons = inputForm.getAddons();
            itemName = vcproject.ItemName;
            Wizard.addAddons(vcproject, inputForm.getOFRoot(), addons);
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {

        }

        public static void addAddons(VCProject vcproject, String ofRoot, List<String> addons)
        {
            VCFilter addonsFolder = null;
            try {
                addonsFolder = vcproject.AddFilter("addons");
            }catch(Exception e)
            {
                IVCCollection filters = vcproject.Filters;
                foreach(var filter in filters)
                {
                    if(filter is VCFilter)
                    {
                        if(((VCFilter)filter).Name == "addons")
                        {
                            addonsFolder = ((VCFilter)filter);
                            break;
                        }
                    }
                }
            }
            if (addonsFolder!=null)
            {
                foreach (var addon in addons)
                {
                    VCFilter addonFolder = addonsFolder.AddFilter(addon);
                    var addonObj = new Addon(ofRoot, addon);

                    addonObj.addFilesToVCFilter(addonFolder);
                    addonObj.addIncludePathsToVCProject(vcproject);
                    addonObj.addLibsToVCProject(vcproject);
                }
                vcproject.Save();
            }
            else
            {
                throw new Exception("Couldn't create or find addonsFolder");
            }
        }

        public static void collapse(UIHierarchyItem item, string projectName)
        {
            foreach(UIHierarchyItem subitem in item.UIHierarchyItems)
            {
                if(subitem.UIHierarchyItems.Expanded && subitem.UIHierarchyItems.Count > 0)
                {
                    collapse(subitem, projectName);
                }
            }
            if (item.Name != projectName)
            {
                item.UIHierarchyItems.Expanded = false;
            }
        }

        public static void CollapseAllFolders(UIHierarchy solExplorer, string projectName)
        {
            if (solExplorer.UIHierarchyItems.Count > 0)
            {
                UIHierarchyItem rootNode = solExplorer.UIHierarchyItems.Item(1);
                rootNode.DTE.SuppressUI = true;
                foreach (UIHierarchyItem uihitem in rootNode.UIHierarchyItems)
                {
                    if (uihitem.UIHierarchyItems.Expanded)
                    {
                        collapse(uihitem, projectName);
                    }

                }
                rootNode.Select(vsUISelectionType.vsUISelectionTypeSelect);
                rootNode.DTE.SuppressUI = false;
            }
        }

        public void RunFinished()
        {
            DTE2 application = dte.Application as DTE2;
            UIHierarchy solExplorer = application.ToolWindows.SolutionExplorer;
            Wizard.CollapseAllFolders(solExplorer, itemName);
        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            dte = (EnvDTE.DTE)automationObject;
            String destinationDirectory = replacementsDictionary["$destinationdirectory$"];
            try
            {
                // Display a form to the user. The form collects 
                // input for the custom message.
                inputForm = new Form1();
                var result = inputForm.ShowDialog();
                if(result == DialogResult.Cancel)
                {
                    throw new WizardBackoutException();
                }

                // Add custom parameters.
                //replacementsDictionary.Add("$custommessage$", customMessage);
            }
            catch (Exception ex)
            {
                if (Directory.Exists(destinationDirectory))
                {
                    Directory.Delete(destinationDirectory, true);
                }

                Debug.WriteLine(ex);

                throw;
            }

        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        private Form1 inputForm;
        private List<string> addons;
        private EnvDTE.DTE dte;
        private string itemName;
    };
}