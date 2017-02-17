using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IPA.Tests
{
    public class ProgramTest
    {
        [Theory]
        // Unrelated path
        [InlineData("test/from.dll", "test/to.dll", "native", false, new string[] { "test/to.dll" })]

        // Flat -> Not-Flat
        [InlineData("native/from.dll", "native/to.dll", "native", false, new string[] { "native/x86/to.dll", "native/x86_64/to.dll" })]

        // Flat -> Flat
        [InlineData("native/from.dll", "native/to.dll", "native", true, new string[] { "native/to.dll" })]

        // Not-Flat -> Flat
        [InlineData("native/x86/from.dll", "native/x86/to.dll", "native", true, new string[] { })]
        [InlineData("native/x86_64/from.dll", "native/x86_64/to.dll", "native", true, new string[] { "native/to.dll" })]

        // Not-flat -> Not-Flat
        [InlineData("native/x86/from.dll", "native/x86/to.dll", "native", false, new string[] { "native/x86/to.dll" })]
        [InlineData("native/x86_64/from.dll", "native/x86_64/to.dll", "native", false, new string[] { "native/x86_64/to.dll"  })]

        public void CopiesCorrectly(string from, string to, string nativeFolder, bool isFlat, string[] expected)
        {
            var outcome = Program.NativePluginInterceptor(new FileInfo(from), new FileInfo(to), new DirectoryInfo(nativeFolder), isFlat).Select(f => f.FullName).ToList();

            var expectedPaths = expected.Select(e => new FileInfo(e)).Select(f => f.FullName).ToList();
            Assert.Equal(expectedPaths, outcome);
        }
    }
}
