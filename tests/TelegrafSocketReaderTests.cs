using System;
using System.Collections.Concurrent;
using System.IO;
using FluentAssertions;
using TelemetrySigner;
using Xunit;

namespace tests
{
    public class TelegrafSocketReaderTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("    ")]
        [InlineData("foobar")]
        public void ShouldThrowOnInvalidFilename(string path)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                TelegrafSocketReader tsr = new TelegrafSocketReader(path);
            });
        }

        [Fact]
        public void ShouldThrowOnNullQueue()
        {
            string testFile = Path.GetTempFileName();
            TelegrafSocketReader tsr = new TelegrafSocketReader(testFile);
            Assert.Throws<ArgumentNullException>(() => tsr.Read(null));
        }

        [Fact]
        public void ShouldEnqueueLinesFromFile()
        {
            string testFile = Path.GetTempFileName();

            var testLines = new[]
            {
                "line-1",
                "line-2",
                "line-3",
                "line-4"
            };
            
            ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
            
            // Write some lines into TestFile
            File.WriteAllLines(testFile,testLines);
            TelegrafSocketReader tsr = new TelegrafSocketReader(testFile);
            tsr.Read(queue);
            
            // dequeue and see if matches
            var actualArray = queue.ToArray();
            actualArray.Should().NotBeEmpty()
                .And.HaveCount(testLines.Length)
                .And.ContainInOrder(testLines)
                .And.ContainItemsAssignableTo<string>();


        }
    }
}