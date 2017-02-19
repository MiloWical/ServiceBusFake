using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class MessageSizeExceededException : MessagingException
	{
		public MessageSizeExceededException(string message) : base(message)
		{
			base.IsTransient = false;
		}

		public MessageSizeExceededException(string message, Exception innerException) : base(message, innerException)
		{
			base.IsTransient = false;
		}

		private MessageSizeExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}
	}
}