using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class MessageLockLostException : MessagingException
	{
		public MessageLockLostException(string message) : base(message)
		{
			base.IsTransient = false;
		}

		public MessageLockLostException(string message, Exception innerException) : base(message, innerException)
		{
			base.IsTransient = false;
		}

		private MessageLockLostException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}
	}
}