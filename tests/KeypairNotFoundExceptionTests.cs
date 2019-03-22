using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using FluentAssertions;
using TelemetrySigner;
using Xunit;

namespace tests
{
    public class KeypairNotFoundExceptionTests : KeypairNotFoundException
    {
        [Fact]
        public void EmptyConstructorShouldPass()
        {
            KeypairNotFoundException ex = new KeypairNotFoundException();
            Assert.NotNull(ex);

        }

        [Fact]
        public void OneParamConstructorShouldPass()
        {
            const string msg1 = "outer message 1";
            KeypairNotFoundException ex = new KeypairNotFoundException(msg1);
            ex.Should()
                .NotBeNull()
                .And.BeAssignableTo<Exception>()
                .And.Match<KeypairNotFoundException>(obj =>
                    obj.Message == msg1);

        }

        [Fact]
        public void TwoParamConstructorShouldPass()
        {
            const string msg1 = "outer message 1";
            const string msg2 = "innter message 2";
            KeypairNotFoundException ex = new KeypairNotFoundException(msg1, new Exception(msg2));

            ex.Should()
                .NotBeNull()
                .And.BeAssignableTo<Exception>()
                .And.Match<KeypairNotFoundException>(obj =>
                    obj.Message == msg1);

            ex.InnerException.Should()
                .NotBeNull()
                .And.BeAssignableTo<Exception>()
                .And.Match<Exception>(o => o.Message == msg2);

        }

        [Fact]
        public void SerializationDeserializationShouldPass()
        {

            Exception innerEx = new Exception("inner message");
            KeypairNotFoundException originalException = new KeypairNotFoundException("file exc message", innerEx);

            byte[] buffer = new byte[4096];
            MemoryStream memoryStream = new MemoryStream(buffer);
            MemoryStream memoryStream2 = new MemoryStream(buffer);
            BinaryFormatter formatterObj = new BinaryFormatter();

            // Act
            formatterObj.Serialize(memoryStream, originalException);
            KeypairNotFoundException deserializedException = (KeypairNotFoundException)formatterObj.Deserialize(memoryStream2);

            Assert.Equal(originalException.InnerException.Message, deserializedException.InnerException.Message);
            Assert.Equal(originalException.Message, deserializedException.Message);
        }

    }

}