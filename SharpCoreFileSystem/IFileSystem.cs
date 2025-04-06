using System.IO;
using System.Collections.Generic;
using System;
namespace SharpFileSystem
{
public interface IFileSystem: IDisposable
{
    ICollection<FileSystemPath> GetEntities(FileSystemPath path);
    bool Exists(FileSystemPath path);
    Stream CreateFile(FileSystemPath path, bool createParents = false);
    Stream OpenFile(FileSystemPath path, FileAccess access);
    void CreateDirectory(FileSystemPath path, bool createParents = false);
    void Delete(FileSystemPath path);

    ICollection<FileSystemPath> GetDirectories(FileSystemPath path);

    ICollection<FileSystemPath> GetFiles(FileSystemPath path);

    void ChRoot(FileSystemPath newRoot);

    /// <summary>
    /// Semantic of "clean" is up to the file system type (see each FS documentation):
    ///  - Only removes created files.
    ///  - Removes all files and directories.
    ///  - nothing (for RO FS).
    /// </summary>
    void CleanFS();

    bool IsReadOnly { get; }

    FileSystemPath CurrentDirectory { get;  }

    void ChDir(FileSystemPath currentDirectory);

    FileSystemPath GetAbsolutePath(FileSystemPath path);
}
}
