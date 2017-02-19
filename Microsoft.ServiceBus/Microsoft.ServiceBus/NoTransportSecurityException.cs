using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[Serializable]
	internal class NoTransportSecurityException : Exception
	{
		public NoTransportSecurityException()
		{
		}

		public NoTransportSecurityException(string message) : base(message)
		{
		}

		public NoTransportSecurityException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected NoTransportSecurityException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}