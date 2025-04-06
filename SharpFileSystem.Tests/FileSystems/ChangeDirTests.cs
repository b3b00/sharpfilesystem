using System;
using System.IO;
using System.Reflection;
using NFluent;
using SharpFileSystem.FileSystems;
using Xunit;

namespace SharpFileSystem.Tests.FileSystems
{
    public class ChangeDirTests
    {
        [Fact]
        public void ChangeDirEmbeddedTest()
        {
            EmbeddedResourceFileSystem embedded = new EmbeddedResourceFileSystem(Assembly.GetAssembly(typeof(EmbeddedRessourceFileSystemTests)));
            Check.That(embedded.Exists(FileSystemPath.Root.AppendFile("test.txt"))).IsTrue();
            embedded.ChDir("resDir");
            Check.That(embedded.CurrentDirectory.ToString()).IsEqualTo("/resDir/");
            Check.That(embedded.Exists("test.txt")).IsFalse();
            var deepExists = embedded.Exists("deepFile.txt");
            Check.That(deepExists).IsTrue();
        }

        [Fact]
        public void ChangeDirPhysicalTest()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(root);
            var fileSystem = new PhysicalFileSystem(root);
            fileSystem.CreateFile("/root.txt",true);
            fileSystem.CreateFile("/sub/sub.txt",true);
            fileSystem.CreateFile("/sub/sub/subsub.txt",true);
            fileSystem.CreateFile("/sub/sub/subsub2.txt",true);
            fileSystem.ChDir("sub");
            Check.That(fileSystem.Exists("sub.txt"));
            Check.That(fileSystem.Exists("sub/subsub/subsub.txt"));
            fileSystem.ChDir("subsub");
            Check.That(fileSystem.CurrentDirectory.ToString()).IsEqualTo("/sub/subsub/");
            Check.That(fileSystem.Exists("subsub.txt")).IsTrue();
            Check.That(fileSystem.Exists("subsub2.txt")).IsTrue();
            Check.That(fileSystem.Exists("sub.txt")).IsFalse();
            fileSystem.ChDir("/");
            Check.That(fileSystem.Exists("root.txt"));


        }
    }
}
