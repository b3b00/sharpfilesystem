using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using SharpFileSystem;
using SharpFileSystem.FileSystems;
using Xunit;

namespace TestSFS
{
    class Program
    {


        static void Main(string[] args)
        {
            // embeddedFS();
            // CreateMemoryFile();
            // MergeEmbeddedAndMemoryFS();
            //PhysicalFS();
            // MergeEmbeddedAndPhysicalFS();
            chrootedPhysicalFS();
            // ReadZipFS();
            // WriteZipFS();
            // Read7ZipFS();

        }

        static void CreateMemoryFile()
        {
            FileSystemPath MemRootFilePath = FileSystemPath.Root.AppendFile("x");
            var MemFileSystem = new MemoryFileSystem();
            // File shouldn’t exist prior to creation.
            Assert.False(MemFileSystem.Exists(MemRootFilePath));

            var content = new byte[] { 0xde, 0xad, 0xbe, 0xef, };
            using (var xStream = MemFileSystem.CreateFile(MemRootFilePath))
            {
                // File now should exist.
                Assert.True(MemFileSystem.Exists(MemRootFilePath));

                xStream.Write(content, 0, content.Length);
            }

            // File should still exist and have content.
            Assert.True(MemFileSystem.Exists(MemRootFilePath));
            using (var xStream = MemFileSystem.OpenFile(MemRootFilePath, FileAccess.Read))
            {
                var readContent = new byte[2 * content.Length];
                Assert.Equal(content.Length, xStream.Read(readContent, 0, readContent.Length));
                Assert.Equal(
                    content,
                    // trim to the length that was read.
                    readContent.Take(content.Length).ToArray());

                // Trying to read beyond end of file should return 0.
                Assert.Equal(0, xStream.Read(readContent, 0, readContent.Length));
            }
        }


        static void embeddedFS()
        {
            string content = "test embedded resource";
            string deepContent = "deep file";
            var filePath = FileSystemPath.Root.AppendFile("test.txt");
            var resDir = FileSystemPath.Root.AppendDirectory("resDir");
            var deepFilePath = resDir.AppendFile("deepFile.txt");
            EmbeddedResourceFileSystem eRscFS = new EmbeddedResourceFileSystem(Assembly.GetAssembly(typeof(Program)));
            Assert.True(eRscFS.Exists(filePath));
            using (var stream = eRscFS.OpenFile(filePath, FileAccess.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    string value = reader.ReadToEnd();
                    Assert.Equal(content, value);
                }
            }

            Assert.True(eRscFS.Exists(deepFilePath));
            using (var stream = eRscFS.OpenFile(deepFilePath, FileAccess.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    string value = reader.ReadToEnd();
                    Assert.Equal(deepContent, value);
                }
            }

            var entities = eRscFS.GetEntities(FileSystemPath.Root);
            var recentities = eRscFS.GetEntitiesRecursive(FileSystemPath.Root);
            ;
        }

        static void MergeEmbeddedAndMemoryFS()
        {
            string newContent = "new content";
            string newContentPath = "/resDir/new.txt";
            MemoryFileSystem memFS = new MemoryFileSystem();
            memFS.ChRoot("/");
            string content = "test embedded resource";
            string deepContent = "deep file";
            var filePath = FileSystemPath.Root.AppendFile("test.txt");
            var resDir = FileSystemPath.Root.AppendDirectory("resDir");
            var deepFilePath = resDir.AppendFile("deepFile.txt");

            EmbeddedResourceFileSystem embeddedFS =
                new EmbeddedResourceFileSystem(Assembly.GetAssembly(typeof(Program)));
            embeddedFS.ChRoot("/");

            MergedFileSystem mergedFS = new MergedFileSystem(embeddedFS, memFS);
            mergedFS.ChRoot("/");

            Assert.True(mergedFS.Exists(filePath));
            using (var stream = mergedFS.OpenFile(filePath, FileAccess.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    string value = reader.ReadToEnd();
                    Assert.Equal(content, value);
                }
            }

            Assert.True(mergedFS.Exists(deepFilePath));
            using (var stream = mergedFS.OpenFile(deepFilePath, FileAccess.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    string value = reader.ReadToEnd();
                    Assert.Equal(deepContent, value);
                }
            }

            mergedFS.WriteAllText(newContentPath, newContent);
            Assert.True(mergedFS.Exists(filePath));
            var foundContent = mergedFS.ReadAllText(newContentPath);
            Assert.Equal(newContent, foundContent);

            mergedFS.CleanFS();
            Assert.False(mergedFS.Exists(newContentPath));
        }

         static void MergeEmbeddedAndPhysicalFS()
        {
            string newContent = "new content";
            string newContentPath = "/resDir/new.txt";
            PhysicalFileSystem physicalFS = new PhysicalFileSystem(".");
            physicalFS.ChRoot("/");
            string content = "test embedded resource";
            string deepContent = "deep file";
            var filePath = FileSystemPath.Root.AppendFile("test.txt");
            var resDir = FileSystemPath.Root.AppendDirectory("resDir");
            var deepFilePath = resDir.AppendFile("deepFile.txt");

            EmbeddedResourceFileSystem embeddedFS =
                new EmbeddedResourceFileSystem(Assembly.GetAssembly(typeof(Program)));
            embeddedFS.ChRoot("/");

            MergedFileSystem mergedFS = new MergedFileSystem(embeddedFS, physicalFS);
            mergedFS.ChRoot("/");

            Assert.True(mergedFS.Exists(filePath));
            using (var stream = mergedFS.OpenFile(filePath, FileAccess.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    string value = reader.ReadToEnd();
                    Assert.Equal(content, value);
                }
            }

            Assert.True(mergedFS.Exists(deepFilePath));
            using (var stream = mergedFS.OpenFile(deepFilePath, FileAccess.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    string value = reader.ReadToEnd();
                    Assert.Equal(deepContent, value);
                }
            }

            mergedFS.WriteAllText(newContentPath, newContent);
            Assert.True(mergedFS.Exists(filePath));
            var foundContent = mergedFS.ReadAllText(newContentPath);
            Assert.Equal(newContent, foundContent);

            var entities = mergedFS.GetEntities(FileSystemPath.Root);
            foreach (var entity in entities)
            {
                Console.WriteLine(entity.Path);
            }

            var recentities = mergedFS.GetEntitiesRecursive(FileSystemPath.Root);
            foreach (var entity in recentities)
            {
                Console.WriteLine(entity.Path);
            }

            mergedFS.CleanFS();
            Assert.False(mergedFS.Exists(newContentPath));
        }


        static void PhysicalFS()
        {
            PhysicalFileSystem physicalFS = new PhysicalFileSystem(".");

            physicalFS.WriteAllText("/dir/subDir/file.txt", "content");
            Assert.True(physicalFS.Exists("/dir/subDir/file.txt"));
            var foundContent = physicalFS.ReadAllText("/dir/subdir/file.txt");
            Assert.Equal("content", foundContent);

            var entities = physicalFS.GetEntities(FileSystemPath.Root);
            var recentities = physicalFS.GetEntitiesRecursive(FileSystemPath.Root);

            physicalFS.CleanFS();
        }

        static void chrootedPhysicalFS()
        {
            PhysicalFileSystem fileSystem;
            var fileName = "x";
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(root);
            fileSystem = new PhysicalFileSystem(root);
            fileSystem.CreateDirectory("/root/");
            fileSystem.ChRoot("/root");
            fileSystem.WriteAllText("/test.root.txt","test");
            var physicalPath = Path.Combine(fileSystem.PhysicalRoot, "root", "test.root.txt");
            Assert.True(System.IO.File.Exists(physicalPath));
            var content = System.IO.File.ReadAllText(physicalPath);
            Assert.Equal("test",content);
        }
    }

}

