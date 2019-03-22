using System;
using System.Runtime.Serialization;

namespace TelemetrySigner
{
    [Serializable]
    public class KeypairNotFoundException : Exception
    {

        public KeypairNotFoundException()
        {
        }

        public KeypairNotFoundException(string message) : base(message)
        {
        }

        public KeypairNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }

        protected KeypairNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}