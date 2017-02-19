using Microsoft.ServiceBus;
using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class MessagingCommunicationException : MessagingException
	{
		public MessagingCommunicationException(string communicationPath) : this(SRClient.MessagingEndpointCommunicationError(communicationPath), null)
		{
		}

		public MessagingCommunicationException(string message, Exception innerException) : base(message, innerException)
		{
		}

		private MessagingCommunicationException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}