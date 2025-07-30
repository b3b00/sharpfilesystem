using System.Linq;
using SharpFileSystem.FileSystems;
using Xunit;

namespace SharpFileSystem.Tests
{
    public class ExtensionsTests
    {
        [Fact]
        public void testRecursiveDirectoryCreation()
        {
            var mem = new MemoryFileSystem();
            mem.CreateDirectory("/memory/deep/deeper/deepest/",true);
            Assert.True(mem.Exists("/memory/"));
            Assert.True(mem.Exists("/memory/deep/"));
            Assert.True(mem.Exists("/memory/deep/deeper/"));
            Assert.True(mem.Exists("/memory/deep/deeper/deepest/"));
        }

        [Fact]
        public void GetEntitiesRecursiveTest()
        {
            EmbeddedResourceFileSystem eFS = new EmbeddedResourceFileSystem(typeof(ExtensionsTests).Assembly);
            var entities = eFS.GetEntitiesRecursive("/");
            Assert.Equal(4,entities.Count());
        }
    }
}
