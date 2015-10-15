using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace of
{
    public class Folder
    {
        public Folder(string path, IEnumerable<string> filters)
        {
            this.path = path;
            name = Path.GetFileName(path);
            DirectoryInfo di = new DirectoryInfo(path);
            var files = new List<FileInfo>();
            foreach (var filter in filters)
            {
                try
                {
                    files.AddRange(di.GetFiles(filter, SearchOption.TopDirectoryOnly));
                }
                catch (Exception) { }
            }
            foreach (var file in files)
            {
                this.files.Add(file.FullName);
            }
            try
            {
                foreach (var dir in di.GetDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    folders.Add(new Folder(Path.Combine(path, dir.FullName), filters));
                }
            }
            catch (Exception) { }
        }

        public void toVCFilter(VCFilter parent)
        {
            VCFilter filter = parent.AddFilter(name);
            foreach(var file in files)
            {
                filter.AddFile(Path.Combine(path,file));
            }
            foreach(var folder in folders)
            {
                folder.toVCFilter(filter);
            }
        }

        public string getRecursiveFolderList()
        {
            string list = "";
            foreach(var folder in folders)
            {
                list += "\"" + path + "\";";
                list += folder.getRecursiveFolderList();
            }
            return list;
        }

        public List<string> getRecursiveFoldersPaths()
        {
            List<string> list = new List<string>();
            list.Add(path);
            foreach (var folder in folders)
            {
                list.AddRange(folder.getRecursiveFoldersPaths());
            }
            return list;
        }

        public string path;
        public string name;
        public List<string> files = new List<string>();
        public List<Folder> folders = new List<Folder>();
    }

    public class Addon
    {
        public Addon(string ofRoot, string nameOrPath)
        {
            // check if the addon is in addons or is local
            DirectoryInfo di = new DirectoryInfo(nameOrPath);
            if (di.Exists)
            {
                addonName = di.FullName;
                addonPath = nameOrPath;
            }
            else
            {
                addonName = nameOrPath;
                addonPath = Path.Combine(ofRoot, "addons", nameOrPath);
            }

            // find all sources and headers
            String[] srcFilters = { "*.h", "*.hpp", "*.cpp", "*.c", "*.s", "*.S", "*.cc", "*.cxx", "*.c++" };
            srcFolder = new Folder(Path.Combine(addonPath, "src"), srcFilters);
            libsFolder = new Folder(Path.Combine(addonPath, "libs"), srcFilters);

            // find binary libs
            di = new DirectoryInfo(Path.Combine(addonPath, "libs"));
            String[] libsFilters = { "*.lib", "*.dll" };
            if (di.Exists) {
                foreach (var dir in di.GetDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    var vsLibsPath = Path.Combine(addonPath, "libs", dir.FullName, "lib", "vs");
                    var libFolder = new DirectoryInfo(vsLibsPath);
                    if (libFolder.Exists) {
                        var libsWin32 = new DirectoryInfo(Path.Combine(vsLibsPath, "Win32"));
                        if (libsWin32.Exists)
                        {
                            var libsWin32Debug = new DirectoryInfo(Path.Combine(libsWin32.FullName, "Debug"));
                            if (libsWin32Debug.Exists)
                            {
                                AddLibs(libsWin32Debug, libs32Debug, libsFilters);
                            }

                            var libsWin32Release = new DirectoryInfo(Path.Combine(libsWin32.FullName, "Release"));
                            if (libsWin32Release.Exists)
                            {
                                AddLibs(libsWin32Release, libs32Release, libsFilters);
                            }

                            AddLibs(libsWin32, libs32Debug, libsFilters);
                            AddLibs(libsWin32, libs32Release, libsFilters);
                        }

                        var libsWin64 = new DirectoryInfo(Path.Combine(vsLibsPath, "x64"));
                        if (libsWin64.Exists)
                        {
                            var libsWin64Debug = new DirectoryInfo(Path.Combine(libsWin64.FullName, "Debug"));
                            if (libsWin64Debug.Exists)
                            {
                                AddLibs(libsWin64Debug, libs64Debug, libsFilters);
                            }

                            var libsWin64Release = new DirectoryInfo(Path.Combine(libsWin64.FullName, "Release"));
                            if (libsWin64Release.Exists)
                            {
                                AddLibs(libsWin64Release, libs64Release, libsFilters);
                            }

                            AddLibs(libsWin64, libs64Debug, libsFilters);
                            AddLibs(libsWin64, libs64Release, libsFilters);
                        }
                    }

                    AddLibs(libFolder, libs32Debug, libsFilters);
                    AddLibs(libFolder, libs32Release, libsFilters);
                }
            }

            // find include search paths:
            //
            // - add src recursively as search path
            // - if libs has folders, for each
            //   · if theres include folder add recursively
            //   . else add all the folder recursively
            // - else if has files add libs to search path
            //
            includePaths.AddRange(srcFolder.getRecursiveFoldersPaths());
            if (libsFolder.folders.Count > 0)
            {
                foreach (var libfolder in libsFolder.folders)
                {
                    var includeFolder = libfolder.folders.Find((folder) => { return folder.name == "include"; });
                    if (includeFolder != null)
                    {
                        includePaths.AddRange(includeFolder.getRecursiveFoldersPaths());
                    }
                    else
                    {
                        var srcFolder = libfolder.folders.Find((folder) => { return folder.name == "src"; });
                        if (srcFolder != null)
                        {

                            includePaths.AddRange(srcFolder.getRecursiveFoldersPaths());
                        }
                        else
                        {
                            includePaths.AddRange(libfolder.getRecursiveFoldersPaths());
                        }
                    }
                }
            }
            else
            {
                if (libsFolder.files.Count>0)
                {
                    includePaths.Add(libsFolder.path);
                }
            }

            // Parse addons_config if it exists:
            var addonConfigPath = Path.Combine(addonPath, "addon_config.mk");
            if (File.Exists(addonConfigPath))
            {
                var totalLibsExclude = new List<string>();
                var totalSourcesExclude = new List<string>();
                var totalIncludesExclude = new List<string>();
                var section = "meta";
                var addonConfig = File.OpenText(addonConfigPath);
                while (!addonConfig.EndOfStream) {
                    var line = addonConfig.ReadLine().Trim();
                    if (line.Count() == 0)
                    {
                        continue;
                    }
                    if(line.First() == '#')
                    {
                        continue;
                    }
                    if(line.Last() == ':')
                    {
                        section = line.Substring(0,line.Count()-1);
                        continue;
                    }
                    if (section == "common" || section == "vs")
                    {
                        string variable="";
                        string value="";
                        bool add = false;
                        if (line.Contains("+="))
                        {
                            String[] sep = { "+=" };
                            var varValue = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                            if (varValue.Count() == 2)
                            {
                                variable = varValue[0].Trim();
                                value = varValue[1].Trim();
                                add = true;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else if (line.Contains("="))
                        {
                            var varValue = line.Split('=');
                            if (varValue.Count() == 2)
                            {
                                variable = varValue[0].Trim();
                                value = varValue[1].Trim();
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }

                        char[] splitChar = { ' ' };
                        switch (variable)
                        {
                            case "ADDON_INCLUDES":
                                var includes = value.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                                if (!add)
                                {
                                    includePaths.Clear();
                                }
                                includePaths.AddRange(includes);
                                break;
                            case "ADDON_LIBS":
                                // TODO: there should be a way to specify architecture and target
                                var libs = value.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                                if (!add)
                                {
                                    libs32Release.Clear();
                                    libs32Debug.Clear();
                                }
                                libs32Debug.AddRange(libs);
                                libs32Release.AddRange(libs);
                                break;
                            case "ADDON_CFLAGS":
                                var flags = value.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                                if (!add)
                                {
                                    cflags.Clear();
                                }
                                cflags.AddRange(flags);
                                break;
                            case "ADDON_LDFLAGS":
                                flags = value.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                                if (!add)
                                {
                                    ldflags.Clear();
                                }
                                cflags.AddRange(flags);
                                break;
                            case "ADDON_SOURCES":
                                break;
                            case "ADDON_LIBS_EXCLUDE":
                                var libsExclude = value.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                                if (!add)
                                {
                                    totalLibsExclude.Clear();
                                }
                                totalLibsExclude.AddRange(libsExclude.Select((libExclude)=> { return Path.Combine(addonPath, libExclude); }));
                                break;
                            case "ADDON_INCLUDES_EXCLUDE":
                                var includesExclude = value.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                                if (!add)
                                {
                                    totalIncludesExclude.Clear();
                                }
                                totalIncludesExclude.AddRange(includesExclude.Select((includeExclude) => { return Path.Combine(addonPath, includeExclude); }));
                                break;
                            case "ADDON_SOURCES_EXCLUDE":
                                var sourcesExclude = value.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                                if (!add)
                                {
                                    totalSourcesExclude.Clear();
                                }
                                totalSourcesExclude.AddRange(sourcesExclude.Select((sourceExclude) => { return Path.Combine(addonPath, sourceExclude); }));
                                break;
                        }
                    }
                }

                libs32Debug = libs32Debug.Where((lib) => { return !totalLibsExclude.Contains(lib); }).ToList();
                libs32Release = libs32Release.Where((lib) => { return !totalLibsExclude.Contains(lib); }).ToList();
                libs64Debug = libs64Debug.Where((lib) => { return !totalLibsExclude.Contains(lib); }).ToList();
                libs64Release = libs64Release.Where((lib) => { return !totalLibsExclude.Contains(lib); }).ToList();
                includePaths = includePaths.Where((include) => { return !totalIncludesExclude.Contains(include); }).ToList();
                filterSources(srcFolder,totalSourcesExclude);
                filterSources(libsFolder, totalSourcesExclude);
            }
        }

        private void filterSources(Folder folder, List<string> sourceExcludes)
        {
            folder.files = folder.files.Where((file) => { return !sourceExcludes.Contains(file); }).ToList();
            foreach(var subfolder in folder.folders)
            {
                filterSources(subfolder, sourceExcludes);
            }
        }

        private void AddLibs(DirectoryInfo src, List<string> dst, String[] libsFilters)
        {
            foreach(var filter in libsFilters)
            {
                try
                {
                    foreach (var lib in src.GetFiles(filter, SearchOption.TopDirectoryOnly))
                    {
                        dst.Add(lib.FullName);
                    }
                }
                catch (Exception e) { }
            }
        }

        public void addFilesToVCFilter(VCFilter parent)
        {
            srcFolder.toVCFilter(parent);
            libsFolder.toVCFilter(parent);
        }

        public void addIncludePathsToVCProject(VCProject project)
        {
            var includes = includePaths.Aggregate((sum, value) =>
            {
                return sum + ";\n\"" + value + "\"";
            });
            foreach (VCConfiguration config in project.Configurations)
            {
                IVCCollection tools = config.Tools;

                foreach (Object tool in tools)
                {
                    if (tool is VCCLCompilerTool)
                    {
                        VCCLCompilerTool compilerTool = (VCCLCompilerTool)tool;
                        var currentIncludes = compilerTool.AdditionalIncludeDirectories;
                        compilerTool.AdditionalIncludeDirectories = currentIncludes + ";\n" + includes;
                    }
                }
            }
        }

        public void addLibsToVCProject(VCProject project)
        {

            foreach (VCConfiguration config in project.Configurations)
            {
                IVCCollection tools = config.Tools;

                foreach (Object tool in tools)
                {
                    if (tool is VCLinkerTool)
                    {
                        VCLinkerTool linker = (VCLinkerTool)tool;
                        if (config.Name == "Debug|Win32" && libs32Debug.Count > 0)
                        {
                            linker.AdditionalDependencies += ";\n" + libs32Debug.Aggregate((sum, value) =>
                            {
                                return sum + ";\n\"" + value + "\"";
                            });
                        }
                        else if (config.Name == "Debug|x64" && libs64Debug.Count > 0)
                        {
                            linker.AdditionalDependencies += ";\n" + libs64Debug.Aggregate((sum, value) =>
                            {
                                return sum + ";\n\"" + value + "\"";
                            });
                        }
                        else if (config.Name == "Release|Win32" && libs32Release.Count > 0)
                        {
                            linker.AdditionalDependencies += ";\n" + libs32Release.Aggregate((sum, value) =>
                            {
                                return sum + ";\n\"" + value + "\"";
                            });
                        }
                        else if (config.Name == "Release|x64" && libs64Release.Count > 0)
                        {
                            linker.AdditionalDependencies += ";\n" + libs64Debug.Aggregate((sum, value) =>
                            {
                                return sum + ";\n\"" + value + "\"";
                            });
                        }
                    }
                }
            }
        }

        public List<string> getIncludes()
        {
            return includePaths;
        }

        public List<string> getLibs32Debug()
        {
            return libs32Debug;
        }

        public List<string> getLibs32Release()
        {
            return libs32Release;
        }

        public List<string> getLibs64Debug()
        {
            return libs64Debug;
        }

        public List<string> getLibs64Release()
        {
            return libs32Release;
        }

        public List<string> getCFlags()
        {
            return cflags;
        }

        public List<string> getLDFlags()
        {
            return ldflags;
        }

        private string addonName;
        private string addonPath;
        private Folder srcFolder;
        private Folder libsFolder;
        private List<string> libs32Debug = new List<string>();
        private List<string> libs32Release = new List<string>();
        private List<string> libs64Debug = new List<string>();
        private List<string> libs64Release = new List<string>();
        private List<string> includePaths = new List<string>();
        private List<string> cflags = new List<string>();
        private List<string> ldflags = new List<string>();
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);
    }
}
