using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class MessageStoreLockLostException : MessagingException
	{
		public MessageStoreLockLostException(string message) : base(message)
		{
			base.IsTransient = false;
		}

		public MessageStoreLockLostException(string message, Exception innerException) : base(message, innerException)
		{
			base.IsTransient = false;
		}

		internal MessageStoreLockLostException(MessagingExceptionDetail detail, TrackingContext trackingContext) : base(detail, trackingContext)
		{
			base.IsTransient = false;
		}

		internal MessageStoreLockLostException(MessagingExceptionDetail detail, TrackingContext trackingContext, Exception innerException) : base(detail, trackingContext, innerException)
		{
			base.IsTransient = false;
		}

		private MessageStoreLockLostException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}
	}
}