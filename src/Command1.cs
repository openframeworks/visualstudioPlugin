//------------------------------------------------------------------------------
// <copyright file="Command1.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.VCProjectEngine;
using of;
using System.Windows.Forms;
using EnvDTE80;
using EnvDTE;
using System.IO;
using System.Linq;

namespace VSIXopenFrameworks
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Command1
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("f15ccc3b-e45e-4fbd-8ff2-cc9a1022e6f1");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command1"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private Command1(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Command1 Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new Command1(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        { 
            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return;
            }

            IVsMultiItemSelect multiItemSelect = null;
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainerPtr = IntPtr.Zero;
            uint itemid;
            int hr = VSConstants.S_OK;

            try
            {
                hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);
                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    // there is no selection
                    return;
                }
                IVsHierarchy hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null) return;

                string doc;
                ((IVsProject)hierarchy).GetMkDocument(itemid,out doc);

                object pvar;
                hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out pvar);
                var project = (EnvDTE.Project)pvar;
                var vcproject = (VCProject)project.Object;


                string ofRoot = "..\\..\\..";
                foreach (VCConfiguration config in vcproject.Configurations)
                {
                    ofRoot = config.GetEvaluatedPropertyValue("OF_ROOT");
                    if (ofRoot != "")
                    {
                        break;
                    }
                }

                var ofDi = new DirectoryInfo(ofRoot);
                if (new DirectoryInfo(Path.Combine(vcproject.ProjectDirectory, "..\\..\\..")).Equals(ofDi))
                {
                    ofRoot = "..\\..\\..";
                }
                else
                {
                    ofRoot = Path.GetFullPath(ofRoot);
                }

                var inputForm = new FormAddons(Path.GetDirectoryName(doc), ofRoot);
                var result = inputForm.ShowDialog();
                if (result == DialogResult.Cancel)
                {
                    return;
                }

                var addons = inputForm.getAddons();
                var currentAddons = inputForm.getProjectCurrentAddons();

                var addonsToRemove = currentAddons.Except(addons);
                var remainingAddons = currentAddons.Except(addonsToRemove);
                var newAddons = addons.Except(remainingAddons);

                Wizard.removeAddons(vcproject, ofRoot, addonsToRemove);
                Wizard.addAddons(vcproject, ofRoot, newAddons);
                Wizard.saveAddonsMake(vcproject, addons);

                DTE2 application = project.DTE.Application as DTE2;
                UIHierarchy solExplorer = application.ToolWindows.SolutionExplorer;
                Wizard.CollapseAllFolders(solExplorer, project.Name);
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPtr);
                }

                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }
    }
}
