using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class MessageNotFoundException : MessagingException
	{
		public MessageNotFoundException(string message) : base(message)
		{
			base.IsTransient = false;
		}

		public MessageNotFoundException(string message, Exception innerException) : base(message, innerException)
		{
			base.IsTransient = false;
		}

		private MessageNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}
	}
}