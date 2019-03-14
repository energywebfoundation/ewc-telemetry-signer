using System;
using System.Runtime.Serialization;

namespace TelemetrySigner
{
    [Serializable]
    public class SaltSizeException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

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