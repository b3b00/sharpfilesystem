using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpFileSystem.FileSystems
{
    public class MergedFileSystem: AbstractFileSystem
    {
        public override bool IsReadOnly => FileSystems.All(x => x.IsReadOnly);

        public IEnumerable<IFileSystem> FileSystems { get; private set; }
        public MergedFileSystem(IEnumerable<IFileSystem> fileSystems)
        {
            FileSystems = fileSystems.ToArray();
        }

        public MergedFileSystem(params IFileSystem[] fileSystems)
        {
            FileSystems = fileSystems.ToArray();
        }

        public override void Dispose()
        {
            foreach(var fs in FileSystems)
                fs.Dispose();
        }

        public override ICollection<FileSystemPath> GetEntities(FileSystemPath path)
        {
            var entities = new SortedList<FileSystemPath, FileSystemPath>();
            foreach (var fs in FileSystems.Where(fs => fs.Exists(path)))
            {
                foreach(var entity in fs.GetEntities(path))
                    if (!entities.ContainsKey(entity))
                        entities.Add(entity, entity);
            }
            return entities.Values;
        }

        public override bool Exists(FileSystemPath path)
        {
            return FileSystems.Any(fs => fs.Exists(path));
        }

        public IFileSystem GetFirst(FileSystemPath path, FileAccess access = FileAccess.Read)
        {
            return FileSystems.FirstOrDefault(fs => fs.Exists(path) &&
                (access == FileAccess.Read) ||
                (access == FileAccess.Write && !fs.IsReadOnly) ||
                (access == FileAccess.ReadWrite && !fs.IsReadOnly));
        }

        public IFileSystem GetFirstRW(FileSystemPath path)
        {
            for (int i = 0; i < FileSystems.Count(); i++)
            {
                var fs = FileSystems.ElementAt(i);
                if (!fs.IsReadOnly)
                {
                    if (path == null)
                    {
                        return fs;
                    }

                    if (fs.Exists(path))
                    {
                        return fs;
                    }
                }
            }
            return FileSystems.FirstOrDefault(fs => !fs.IsReadOnly && (path == null || fs.Exists(path)));
        }

        public IFileSystem GetFirstRW()
        {
            return FileSystems.FirstOrDefault(fs => !fs.IsReadOnly);
        }

        public override Stream CreateFile(FileSystemPath path, bool createParents = false)
        {
            if (Exists(path))
                throw new ArgumentException("The specified file already exists.");
            var fs = GetFirstRW() ?? FileSystems.First();
            return fs.CreateFile(path, createParents);
        }


        public override void CleanFS()
        {
            foreach (var fileSystem in FileSystems)
            {
                fileSystem.CleanFS();
            }
        }

        public override Stream OpenFile(FileSystemPath path, FileAccess access)
        {
            var fs = GetFirst(path, access);
            if (fs == null)
            {
                throw new FileNotFoundException();
            }

            if (fs.Exists(path))
            {
                return fs.OpenFile(path, access);
            }
            else if (access == FileAccess.Write || access == FileAccess.ReadWrite)
            {
                fs.CreateFile(path,true);
                return fs.OpenFile(path, access);
            }
            throw new FileNotFoundException();
        }

        public override void CreateDirectory(FileSystemPath path, bool createParents = false)
        {
            if (Exists(path))
                throw new ArgumentException("The specified directory already exists.");
            IFileSystem fs = null;
            if (createParents)
            {
                fs = GetFirstRW();
            }
            else
            {
                fs = GetFirstRW(path.ParentPath);
            }

            if (fs == null)
                throw new ArgumentException("The directory-parent does not exist.");
            fs.CreateDirectory(path, createParents);
        }

        public override void Delete(FileSystemPath path)
        {
            foreach(var fs in FileSystems.Where(fs => fs.Exists(path)))
                fs.Delete(path);
        }

        public override void ChRoot(FileSystemPath root)
        {
            Root = root;
            foreach (var fileSystem in FileSystems)
            {
                fileSystem.ChRoot(root);
            }
        }

        public virtual ICollection<FileSystemPath> GetFiles(FileSystemPath path)
        {
            var files = FileSystems.SelectMany(x => x.GetFiles(path)).ToList();
            return files.Distinct().ToList();
        }


        public virtual ICollection<FileSystemPath> GetDirectories(FileSystemPath path)
        {
            var directories = FileSystems.SelectMany(x => x.GetDirectories(path)).ToList();
            return directories.Distinct().ToList();
        }
    }
}
