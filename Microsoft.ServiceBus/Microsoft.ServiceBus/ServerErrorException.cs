using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[Serializable]
	public class ServerErrorException : Exception
	{
		public ServerErrorException()
		{
		}

		public ServerErrorException(string message) : base(message)
		{
		}

		public ServerErrorException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected ServerErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}