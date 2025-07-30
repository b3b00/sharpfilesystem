using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NFluent;
using NUnit.Framework;
using SharpFileSystem.FileSystems;
using Xunit;
using Assert = Xunit.Assert;

namespace SharpFileSystem.Tests.FileSystems
{
    public class MergeFileSystemTests
    {
        [Fact]
        public void TestMerge()
        {
            var memFs = new MemoryFileSystem();
            var embedFS = new EmbeddedResourceFileSystem(typeof(MergeFileSystemTests).Assembly);
            var merge = new MergedFileSystem(memFs, embedFS);
            merge.CreateDirectory("/memory/", true);
            using (var stream = merge.CreateFile("/memory/test.txt", true))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write("hello");
                }
            }


            Assert.True(merge.Exists("/resDir/deepFile.txt"));
            Assert.True(merge.Exists("/memory/test.txt"));
            var content = merge.ReadAllText("/memory/test.txt");
            Assert.Equal("hello",content);
            content = merge.ReadAllText("/resDir/deepFile.txt");
            Assert.Equal("deep file", content);

            var entities = merge.GetEntities("/");
            Assert.Equal(5,entities.Count);


            merge.CreateDirectory("/resDir/",true);
            using (var stream = merge.CreateFile("/resDir/memoryFileThatMatchAnEmbeddedPath.txt", true))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write("hello embed !");
                }
            }
            Assert.True(merge.Exists("/resDir/memoryFileThatMatchAnEmbeddedPath.txt"));
            content = merge.ReadAllText("/resDir/memoryFileThatMatchAnEmbeddedPath.txt");
            Check.That(content).IsEqualTo("hello embed !");


            var files = merge.GetFiles("/resDir/");
            var expectedFiles = new List<FileSystemPath>()
                { "/resDir/deepFile.txt", "/resDir/memoryFileThatMatchAnEmbeddedPath.txt" };
            Check.That(files).IsEquivalentTo(expectedFiles);


            var directories = merge.GetDirectories("/");
            var expectedDirectories = new List<FileSystemPath>() { "/memory", "/resDir" };
            Check.That(directories).IsEquivalentTo(expectedDirectories);


            embedFS.ChRoot("/resDir/");

            files = merge.GetFiles("/");
            expectedFiles = new List<FileSystemPath>() { "/deepFile.txt" };
            Check.That(files).IsEquivalentTo(expectedFiles);

            directories = merge.GetDirectories("/");
            expectedDirectories = new List<FileSystemPath>() { "/deep","/memory","/resDir" };
            Check.That(directories).IsEquivalentTo(expectedDirectories);

            directories = merge.GetDirectories("/deep/");
            expectedDirectories = new List<FileSystemPath>() { "/deeper"};
            Check.That(directories).IsEquivalentTo(expectedDirectories);

            files = merge.GetFiles("/deep/");
            expectedFiles = new List<FileSystemPath>() { "/deep/deep.txt"};
            Check.That(files).IsEquivalentTo(expectedFiles);
        }

        [Fact]
        public void TestCreateOver()
        {
            var memFs = new MemoryFileSystem();
            var embedFS = new EmbeddedResourceFileSystem(typeof(MergeFileSystemTests).Assembly);
            var merge = new MergedFileSystem(memFs, embedFS);
            Assert.True(merge.Exists("/resDir/deep/deep.txt"));
            var exception = Assert.Throws<ArgumentException>(() =>
            {
                using (var stream = merge.CreateFile("/resDir/deep/deep.txt", true))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write("create over");
                    }
                }
            });
            Assert.Contains("file already exists.", exception.Message);
        }

        [Fact]
        public void TestWriteOver()
        {
            var memFs = new MemoryFileSystem();
            var embedFS = new EmbeddedResourceFileSystem(typeof(MergeFileSystemTests).Assembly);
            var merge = new MergedFileSystem(memFs, embedFS);
            Assert.True(merge.Exists("/resDir/deep/deep.txt"));

            using (var stream = merge.OpenFile("/resDir/deep/deep.txt", FileAccess.Write))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write("write over");
                }
            }

            Assert.Equal("write over", merge.ReadAllText("/resDir/deep/deep.txt"));

        }
    }
}
