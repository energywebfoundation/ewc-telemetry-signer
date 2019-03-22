using System;
using System.Runtime.Serialization;

namespace TelemetrySigner
{
    /// <summary>
    /// Exception that is thrown when there is an issue with the load of the RSA key
    /// </summary>
    [Serializable]
    public class KeypairNotFoundException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

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