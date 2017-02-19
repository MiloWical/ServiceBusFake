using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[Serializable]
	internal class ConnectionLostException : Exception
	{
		public ConnectionLostException()
		{
		}

		public ConnectionLostException(string message) : base(message)
		{
		}

		public ConnectionLostException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected ConnectionLostException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}