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
using System.Linq;

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
            Wizard.saveAddonsMake(vcproject, addons);
            var ofProject = inputForm.getOFRoot() + "\\libs\\openFrameworksCompiled\\project\\vs\\openFrameworksLib.vcxproj";
            dte.Solution.AddFromFile(ofProject);
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {

        }

        private static void ParseProjectConfig(Object tool, List<string> includes, List<string> libs)
        {
            if (tool is VCCLCompilerTool)
            {
                VCCLCompilerTool compilerTool = (VCCLCompilerTool)tool;
                includes.AddRange(compilerTool.AdditionalIncludeDirectories.Split(';').Select((include) =>
                {
                    include = include.Replace("\n", "");
                    return include.Trim();
                }));
            }
            else if (tool is VCLinkerTool)
            {
                VCLinkerTool linkerTool = (VCLinkerTool)tool;
                libs.AddRange(linkerTool.AdditionalDependencies.Split(';').Select((lib) =>
                {
                    lib = lib.Replace("\n", "");
                    return lib.Trim();
                }));
            }
        }

        private static void ListToProjectConfig(Object tool, List<string> includes, List<string> libs)
        {
            if (tool is VCCLCompilerTool)
            {
                VCCLCompilerTool compilerTool = (VCCLCompilerTool)tool;
                compilerTool.AdditionalIncludeDirectories = includes.Aggregate((sum, value) =>
                {
                    return sum + ";\n" + value;
                });
            }
            else if (tool is VCLinkerTool)
            {
                VCLinkerTool linkerTool = (VCLinkerTool)tool;
                linkerTool.AdditionalDependencies = libs.Aggregate((sum, value) =>
                {
                    return sum + ";\n" + value;
                });
            }
        }

        public static void removeAddons(VCProject vcproject, string ofRoot, IEnumerable<string> addonsNames)
        {
            // Parse current settings in the project
            List<string> includes32Debug = new List<string>();
            List<string> includes32Release = new List<string>();
            List<string> includes64Debug = new List<string>();
            List<string> includes64Release = new List<string>();
            List<string> libs32Debug = new List<string>();
            List<string> libs32Release = new List<string>();
            List<string> libs64Debug = new List<string>();
            List<string> libs64Release = new List<string>();

            foreach (VCConfiguration config in vcproject.Configurations)
            {
                IVCCollection tools = config.Tools;

                foreach (Object tool in tools)
                {
                    if (config.Name == "Debug|Win32")
                    {
                        ParseProjectConfig(tool, includes32Debug, libs32Debug);
                    }
                    else if(config.Name == "Release|Win32")
                    {
                        ParseProjectConfig(tool, includes32Release, libs32Release);
                    }
                    else if (config.Name == "Debug|x64")
                    {
                        ParseProjectConfig(tool, includes64Debug, libs64Debug);
                    }
                    else if (config.Name == "Release|x64")
                    {
                        ParseProjectConfig(tool, includes64Release, libs64Release);
                    }
                }
            }

            // Retrieve all addons config
            var addons = addonsNames.Select((addonName) =>
            {
                return new Addon(ofRoot, addonName);
            });

            // Filter out the addons to remove config from the existing one
            foreach(var addon in addons)
            {
                libs32Debug = libs32Debug.Except(addon.getLibs32Debug()).ToList();
                libs32Release = libs32Release.Except(addon.getLibs32Release()).ToList();
                libs64Debug = libs64Debug.Except(addon.getLibs64Debug()).ToList();
                libs64Release = libs32Release.Except(addon.getLibs64Release()).ToList();

                includes32Debug = includes32Debug.Except(addon.getIncludes()).ToList();
                includes32Release = includes32Release.Except(addon.getIncludes()).ToList();
                includes64Debug = libs64Debug.Except(addon.getIncludes()).ToList();
                includes64Release = includes32Release.Except(addon.getIncludes()).ToList();
            }

            // Add the config back to the project:
            foreach (VCConfiguration config in vcproject.Configurations)
            {
                IVCCollection tools = config.Tools;

                foreach (Object tool in tools)
                {
                    if (config.Name == "Debug|Win32")
                    {
                        ListToProjectConfig(tool, includes32Debug, libs32Debug);
                    }
                    else if (config.Name == "Release|Win32")
                    {
                        ListToProjectConfig(tool, includes32Release, libs32Release);
                    }
                    else if (config.Name == "Debug|x64")
                    {
                        ListToProjectConfig(tool, includes64Debug, libs64Debug);
                    }
                    else if (config.Name == "Release|x64")
                    {
                        ListToProjectConfig(tool, includes64Release, libs64Release);
                    }
                }
            }


            // Find addons filter and remove the specified addons from it
            VCFilter addonsFolder = null;
            List<VCFilter> addonsFiltersToRemove = new List<VCFilter>();
            IVCCollection filters = vcproject.Filters;
            foreach (var filter in filters)
            {
                if (filter is VCFilter)
                {
                    if (((VCFilter)filter).Name == "addons")
                    {
                        addonsFolder = ((VCFilter)filter);
                        foreach(var addon in addonsFolder.Filters)
                        {
                            if(addon is VCFilter && addonsNames.Contains(((VCFilter)addon).Name))
                            {
                                addonsFiltersToRemove.Add((VCFilter)addon);
                            }
                        }
                        break;
                    }
                }
            }

            foreach(var addon in addonsFiltersToRemove)
            {
                addonsFolder.RemoveFilter(addon);
            }
        }

        public static void addAddons(VCProject vcproject, String ofRoot, IEnumerable<String> addons)
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
            if (addonsFolder != null)
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

        public static void saveAddonsMake(VCProject vcproject, IEnumerable<string> addons)
        {
            var addonsMake = new FileInfo(vcproject.ProjectDirectory + "\\addons.make");
            var addonsMakeStrm = new StreamWriter(addonsMake.FullName);
            foreach (var addon in addons)
            {
                addonsMakeStrm.WriteLine(addon);
            }
            addonsMakeStrm.Close();
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
                inputForm = new Form1(destinationDirectory);
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