using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class ReceiverDisconnectedException : MessagingException
	{
		public ReceiverDisconnectedException(string message) : base(message)
		{
			base.IsTransient = false;
		}

		public ReceiverDisconnectedException(string message, Exception innerException) : base(message, innerException)
		{
			base.IsTransient = false;
		}

		internal ReceiverDisconnectedException(MessagingExceptionDetail detail, TrackingContext trackingContext) : base(detail, trackingContext)
		{
			base.IsTransient = false;
		}

		internal ReceiverDisconnectedException(MessagingExceptionDetail detail, TrackingContext trackingContext, Exception innerException) : base(detail, trackingContext, innerException)
		{
			base.IsTransient = false;
		}

		private ReceiverDisconnectedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}
	}
}