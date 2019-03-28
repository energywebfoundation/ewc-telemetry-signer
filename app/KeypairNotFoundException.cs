using System;
using System.Runtime.Serialization;

namespace TelemetrySigner
{
    /// <summary>
    /// Custom Exception class for Keypair Not Found
    /// </summary>
    [Serializable]
    public class KeypairNotFoundException : Exception
    {
        /// <summary>
        /// Deafult Constructor of KeypairNotFoundException
        /// </summary>
        /// <returns>returns instance of KeypairNotFoundException</returns>
        public KeypairNotFoundException()
        {
        }

        /// <summary>
        ///  Parameterized Constructor of KeypairNotFoundException with message
        /// </summary>
        /// <param name="message">The message string for KeypairNotFoundException</param>
        /// <returns>returns instance of KeypairNotFoundException with custom message</returns>
        public KeypairNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        ///  Parameterized Constructor of KeypairNotFoundException with message and Inner Exception
        /// </summary>
        /// <param name="message">The message string for KeypairNotFoundException</param>
        /// <param name="inner">The inner Exception reference for KeypairNotFoundException</param>
        /// <returns>returns instance of KeypairNotFoundException with custom message and inner exception</returns>
        public KeypairNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        ///  Parameterized Constructor of KeypairNotFoundException with SerializationInfo and StreamingContext
        /// </summary>
        /// <param name="info">The serialization info for KeypairNotFoundException</param>
        /// <param name="context">The streaming context for KeypairNotFoundException</param>
        /// <returns>returns instance of KeypairNotFoundException with custom serialization and streaming context</returns>
        protected KeypairNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}