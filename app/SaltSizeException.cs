using System;
using System.Runtime.Serialization;

namespace TelemetrySigner
{
    [Serializable]
    public class SaltSizeException : Exception
    {

        public SaltSizeException()
        {
        }

        public SaltSizeException(string message) : base(message)
        {
        }

        public SaltSizeException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SaltSizeException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}