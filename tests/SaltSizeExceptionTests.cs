using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TelemetrySigner;
using Xunit;

namespace tests
{
    public class SaltSizeExceptionTests : SaltSizeException
    {
            [Fact]
        public void EmptyConstructorShouldPass()
        {
            SaltSizeException ex = new SaltSizeException();
            Assert.NotNull(ex);

        }

        [Fact]
        public void OneParamConstructorShouldPass()
        {
            string msg1 = "outer message 1";
            SaltSizeException ex = new SaltSizeException(msg1);
            Assert.NotNull(ex);
            Assert.Equal(ex.Message, msg1);

        }

        [Fact]
        public void TwoParamConstructorShouldPass()
        {
            string msg1 = "outer message 1";
            string msg2 = "innter message 2";
            SaltSizeException ex = new SaltSizeException(msg1, new Exception(msg2));
            Assert.NotNull(ex);
            Assert.Equal(ex.Message, msg1);
            Assert.Equal(ex.InnerException.Message, msg2);

        }

        [Fact]
        public void SerializationDeserializationShouldPass()
        {

            var innerEx = new Exception("inner message");
            var originalException = new SaltSizeException("file exc message", innerEx);

            var buffer = new byte[4096];
            var memoryStream = new MemoryStream(buffer);
            var memoryStream2 = new MemoryStream(buffer);
            var formatterObj = new BinaryFormatter();

            // Act
            formatterObj.Serialize(memoryStream, originalException);
            var deserializedException = (SaltSizeException)formatterObj.Deserialize(memoryStream2);

            Assert.Equal(originalException.InnerException.Message, deserializedException.InnerException.Message);
            Assert.Equal(originalException.Message, deserializedException.Message);
        }

    }

}