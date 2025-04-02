using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpFileSystem.Collections;

namespace SharpFileSystem.FileSystems
{
    public class PhysicalFileSystem : AbstractFileSystem
    {
        public override bool IsReadOnly => false;

        #region Internals

        private HashSet<FileSystemPath> CreatedDirectories = new HashSet<FileSystemPath>();
        private HashSet<FileSystemPath> CreatedFiles = new HashSet<FileSystemPath>();
        public string PhysicalRoot { get; private set; }

        public PhysicalFileSystem(string physicalRoot)
        {
            if (!Path.IsPathRooted(physicalRoot))
                physicalRoot = Path.GetFullPath(physicalRoot);
            if (physicalRoot[physicalRoot.Length - 1] != Path.DirectorySeparatorChar)
                physicalRoot = physicalRoot + Path.DirectorySeparatorChar;
            PhysicalRoot = physicalRoot;
        }

        public string GetPhysicalPath(FileSystemPath path)
        {
            string root = PhysicalRoot;
            if (!Root.IsRoot)
            {
                root = Path.Combine(PhysicalRoot, Root.PathWithoutLeadingSlash);
            }
            return Path.Combine(root, path.ToString().Remove(0, 1).Replace(FileSystemPath.DirectorySeparator, Path.DirectorySeparatorChar));
        }

        public FileSystemPath GetVirtualFilePath(string physicalPath)
        {
            if (!physicalPath.StartsWith(PhysicalRoot, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("The specified path is not member of the PhysicalRoot.", "physicalPath");
            string virtualPath = FileSystemPath.DirectorySeparator + physicalPath.Remove(0, PhysicalRoot.Length).Replace(Path.DirectorySeparatorChar, FileSystemPath.DirectorySeparator);
            return FileSystemPath.Parse(virtualPath);
        }

        public FileSystemPath GetVirtualDirectoryPath(string physicalPath)
        {
            if (!physicalPath.StartsWith(PhysicalRoot, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("The specified path is not member of the PhysicalRoot.", "physicalPath");
            string virtualPath = FileSystemPath.DirectorySeparator + physicalPath.Remove(0, PhysicalRoot.Length).Replace(Path.DirectorySeparatorChar, FileSystemPath.DirectorySeparator);
            if (virtualPath[virtualPath.Length - 1] != FileSystemPath.DirectorySeparator)
                virtualPath += FileSystemPath.DirectorySeparator;
            return FileSystemPath.Parse(virtualPath);
        }

        #endregion

        public override ICollection<FileSystemPath> GetEntities(FileSystemPath path)
        {
            string physicalPath = GetPhysicalPath(path);
            string[] directories = System.IO.Directory.GetDirectories(physicalPath);
            string[] files = System.IO.Directory.GetFiles(physicalPath);
            var virtualDirectories =
                directories.Select(p => GetVirtualDirectoryPath(p));
            var virtualFiles =
                files.Select(p => GetVirtualFilePath(p));
            return new EnumerableCollection<FileSystemPath>(virtualDirectories.Concat(virtualFiles), directories.Length + files.Length);
        }

        public override  bool Exists(FileSystemPath path)
        {
            return path.IsFile ? System.IO.File.Exists(GetPhysicalPath(path)) : System.IO.Directory.Exists(GetPhysicalPath(path));
        }

        public override Stream CreateFile(FileSystemPath path, bool createParents = false)
        {
            if (!path.IsFile)
                throw new ArgumentException("The specified path is not a file.", "path");
            var physicalPath = GetPhysicalPath(path);
            var parent = GetPhysicalPath(path.ParentPath);
            if (createParents && !Path.Exists(parent))
            {
                CreatedDirectories.Add(path.ParentPath);
                System.IO.Directory.CreateDirectory(parent);
            }
            CreatedFiles.Add(path);
            return System.IO.File.Create(GetPhysicalPath(path));
        }

        /// <summary>
        /// Removes all created files and leaves the physical file system clean.
        /// </summary>
        public override void CleanFS()
        {
            foreach (var createdFile in CreatedFiles)
            {
                if (System.IO.File.Exists(GetPhysicalPath(createdFile)))
                {
                    System.IO.File.Delete(GetPhysicalPath(createdFile));
                }
            }

            foreach (var createdDirectory in CreatedDirectories)
            {
                if (System.IO.Directory.Exists(GetPhysicalPath(createdDirectory)))
                {
                    System.IO.Directory.Delete(GetPhysicalPath(createdDirectory), true);
                }
            }
            //TODO removes
        }

        public override Stream OpenFile(FileSystemPath path, FileAccess access)
        {
            if (!path.IsFile)
                throw new ArgumentException("The specified path is not a file.", "path");
            return System.IO.File.Open(GetPhysicalPath(path), FileMode.Open, access);
        }

        public override void CreateDirectory(FileSystemPath path, bool createParents = false)
        {
            if (!path.IsDirectory)
                throw new ArgumentException("The specified path is not a directory.", "path");
            CreatedDirectories.Add(path);
            System.IO.Directory.CreateDirectory(GetPhysicalPath(path));
        }

        public override void Delete(FileSystemPath path)
        {
            if (path.IsFile)
                System.IO.File.Delete(GetPhysicalPath(path));
            else
                System.IO.Directory.Delete(GetPhysicalPath(path), true);
        }

        public new ICollection<FileSystemPath> GetDirectories(FileSystemPath path)
        {
            return System.IO.Directory.GetDirectories(path).Select(x => (FileSystemPath)x).ToList();
        }

        public new ICollection<FileSystemPath> GetFiles(FileSystemPath path)
        {
            return System.IO.Directory.GetFiles(path).Select(x => (FileSystemPath)x).ToList();
        }

        public override  void Dispose()
        {
        }
    }
}


