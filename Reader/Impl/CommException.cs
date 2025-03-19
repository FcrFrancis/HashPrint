using System;
using System.Runtime.Serialization;

namespace Reader
{
    [Serializable]
    public class CommException : Exception
    {
        public readonly CommExceptionCode ErrCode;

        public CommException()
        {
        }

        public CommException(CommExceptionCode dataRecvError) :
            base(ReaderUtils.FormatCommException(dataRecvError))
        {
            this.ErrCode = dataRecvError;
        }

        public CommException(string message) : base(message)
        {
        }

        public CommException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public CommException(CommExceptionCode dataRecvError, string message) : base(message)
        {
            this.ErrCode = dataRecvError;
        }

        protected CommException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}