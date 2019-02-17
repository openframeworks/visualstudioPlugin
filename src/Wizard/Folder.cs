using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace of
{
    public class Folder
    {
        public Folder(string path, IEnumerable<string> filters, string projectPath)
        {
            this.path = path;
            name = Path.GetFileName(path);
            DirectoryInfo di;
            if (Path.IsPathRooted(path))
            {
                di = new DirectoryInfo(path);
            }
            else
            {
                di = new DirectoryInfo(Path.Combine(projectPath, path));
            }
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
                    folders.Add(new Folder(Path.Combine(path, dir.Name), filters, projectPath));
                }
            }
            catch (Exception) { }
        }

        public bool toVCFilter(VCFilter parent)
        {
            bool hasFiles = files.Count > 0;
            if (files.Count > 0 || folders.Count > 0)
            {
                VCFilter filter = parent.AddFilter(name);
                foreach (var file in files)
                {
                    filter.AddFile(Path.Combine(path, file));
                }
                foreach (var folder in folders)
                {
                    hasFiles |= folder.toVCFilter(filter);
                }
                if (!hasFiles)
                {
                    parent.RemoveFilter(filter);
                }
            }
            return hasFiles;
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
}
