using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpFileSystem.FileSystems
{
    public class MemoryFileSystem : AbstractFileSystem
    {
        public override bool IsReadOnly => false;

        private IDictionary<FileSystemPath, ISet<FileSystemPath>> _directories =
    new Dictionary<FileSystemPath, ISet<FileSystemPath>>();
        private IDictionary<FileSystemPath, MemoryFile> _files =
            new Dictionary<FileSystemPath, MemoryFile>();
        public MemoryFileSystem()
        {
            _directories.Add(FileSystemPath.Root, new HashSet<FileSystemPath>());
        }

        public override ICollection<FileSystemPath> GetEntities(FileSystemPath path)
        {
            if (!path.IsDirectory)
                throw new ArgumentException("The specified path is no directory.", "path");
            ISet<MemoryFile> subentities;

            var files = _files.Where(x => x.Key.IsRootedBy(path)).Select(x => x.Key).ToList();

            return files;
        }

        public override bool Exists(FileSystemPath path)
        {
            return path.IsDirectory ? _directories.ContainsKey(path) : _files.ContainsKey(path);
        }

        public override Stream CreateFile(FileSystemPath path, bool createParents = false)
        {
            if (!path.IsFile)
                throw new ArgumentException("The specified path is no file.", "path");
            if (!createParents && !_directories.ContainsKey(path.ParentPath))
                throw new DirectoryNotFoundException();
            else if (createParents && !_directories.ContainsKey(path.ParentPath))
            {
                CreateDirectory(path.ParentPath, true);
                _directories[path.ParentPath].Add(path);
            }

            return new MemoryFileStream(_files[path] = new MemoryFile());
        }

        public override Stream OpenFile(FileSystemPath path, FileAccess access)
        {
            if (!path.IsFile)
                throw new ArgumentException("The specified path is no file.", "path");
            MemoryFile file;
            if (!_files.TryGetValue(path, out file))
                throw new FileNotFoundException();
            return new MemoryFileStream(file);
        }

        public override void CreateDirectory(FileSystemPath path, bool createParents = false)
        {
            if (!path.IsDirectory)
                throw new ArgumentException("The specified path is no directory.", "path");
            ISet<FileSystemPath> subentities;
            if (_directories.ContainsKey(path))
                throw new ArgumentException("The specified directory-path already exists.", "path");
            if (!_directories.TryGetValue(path.ParentPath, out subentities) && !createParents)
                throw new DirectoryNotFoundException();
            else if (!_directories.TryGetValue(path.ParentPath, out subentities) && createParents)
            {
                CreateDirectory(path.ParentPath, createParents);
                var subs = new HashSet<FileSystemPath>();
                _directories[path] = subs;
                subs.Add(path);
            }
            else if (_directories.TryGetValue(path.ParentPath, out subentities) && createParents)
            {
                subentities.Add(path);
                _directories[path] = new HashSet<FileSystemPath>();
            }


        }

        public override void Delete(FileSystemPath path)
        {
            if (path.IsRoot)
                throw new ArgumentException("The root cannot be deleted.");
            bool removed;
            if (path.IsDirectory)
                removed = _directories.Remove(path);
            else
                removed = _files.Remove(path);
            if (!removed)
                throw new ArgumentException("The specified path does not exist.");
            var parent = _directories[path.ParentPath];
            parent.Remove(path);
        }

        public override void Dispose()
        {
        }

        /// <summary>
        /// Removes all directories and files.
        /// </summary>
        public override void CleanFS()
        {
            _directories.Clear();
            _files.Clear();
        }

        public class MemoryFile
        {
            public byte[] Content { get; set; }

            public MemoryFile()
                : this(new byte[0])
            {
            }

            public MemoryFile(byte[] content)
            {
                Content = content;
            }
        }

        public class MemoryFileStream : Stream
        {
            private readonly MemoryFile _file;

            public byte[] Content
            {
                get { return _file.Content; }
                set { _file.Content = value; }
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return true; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override long Length
            {
                get { return _file.Content.Length; }
            }

            public override long Position { get; set; }

            public MemoryFileStream(MemoryFile file)
            {
                _file = file;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (origin == SeekOrigin.Begin)
                    return Position = offset;
                if (origin == SeekOrigin.Current)
                    return Position += offset;
                return Position = Length - offset;
            }

            public override void SetLength(long value)
            {
                int newLength = (int)value;
                byte[] newContent = new byte[newLength];
                Buffer.BlockCopy(Content, 0, newContent, 0, Math.Min(newLength, (int)Length));
                Content = newContent;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int mincount = Math.Min(count, Math.Abs((int)(Length - Position)));
                Buffer.BlockCopy(Content, (int)Position, buffer, offset, mincount);
                Position += mincount;
                return mincount;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (Length - Position < count)
                    SetLength(Position + count);
                Buffer.BlockCopy(buffer, offset, Content, (int)Position, count);
                Position += count;
            }


        }
    }
}
