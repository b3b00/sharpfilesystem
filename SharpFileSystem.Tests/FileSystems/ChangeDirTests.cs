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
            fileSystem.WriteAllText("/root.txt","");
            fileSystem.WriteAllText("/sub/sub.txt","");
            fileSystem.WriteAllText("/sub/sub/subsub.txt","");
            fileSystem.WriteAllText("/sub/sub/subsub2.txt","");
            fileSystem.ChDir("sub");
            Check.That(fileSystem.Exists("sub.txt"));
            Check.That(fileSystem.Exists("sub/sub/subsub.txt"));
            fileSystem.ChDir("sub");
            Check.That(fileSystem.CurrentDirectory.ToString()).IsEqualTo("/sub/sub/");
            Check.That(fileSystem.Exists("subsub.txt")).IsTrue();
            Check.That(fileSystem.Exists("subsub2.txt")).IsTrue();
            fileSystem.Delete("subsub2.txt");
            Check.That(fileSystem.Exists("subsub2.txt")).IsFalse();
            Check.That(fileSystem.Exists("sub.txt")).IsFalse();
            fileSystem.ChDir("/");
            Check.That(fileSystem.Exists("root.txt"));


        }
    }
}
