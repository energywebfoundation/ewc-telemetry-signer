using System;
using System.Runtime.Serialization;

namespace TelemetrySigner
{
    /// <summary>
    /// Custom Exception class for Keypair Not Found
    /// </summary>
    [Serializable]
    public class SaltSizeException : Exception
    {

        /// <summary>
        /// Deafult Constructor of SaltSizeException
        /// </summary>
        /// <returns>returns instance of SaltSizeException</returns>
        public SaltSizeException()
        {
        }

        /// <summary>
        ///  Parameterized Constructor of SaltSizeException with message
        /// </summary>
        /// <param name="message">The message string for SaltSizeException</param>
        /// <returns>returns instance of SaltSizeException with custom message</returns>
        public SaltSizeException(string message) : base(message)
        {
        }

        /// <summary>
        ///  Parameterized Constructor of SaltSizeException with message and Inner Exception
        /// </summary>
        /// <param name="message">The message string for SaltSizeException</param>
        /// <param name="inner">The inner Exception reference for SaltSizeException</param>
        /// <returns>returns instance of SaltSizeException with custom message and inner exception</returns>
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