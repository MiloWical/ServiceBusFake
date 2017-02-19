using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class ServerBusyException : MessagingException
	{
		public ServerBusyException(string message) : this(message, null, null)
		{
		}

		public ServerBusyException(string message, Exception innerException) : this(message, null, innerException)
		{
		}

		internal ServerBusyException(string message, TrackingContext trackingContext) : this(message, trackingContext, null)
		{
		}

		internal ServerBusyException(MessagingExceptionDetail detail, TrackingContext trackingContext) : base(detail, trackingContext)
		{
		}

		internal ServerBusyException(string message, TrackingContext trackingContext, Exception innerException) : base(MessagingExceptionDetail.ServerBusy(message), trackingContext, innerException)
		{
		}

		private ServerBusyException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}