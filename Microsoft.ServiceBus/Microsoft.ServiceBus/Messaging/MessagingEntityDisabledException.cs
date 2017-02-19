using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class MessagingEntityDisabledException : MessagingException
	{
		public MessagingEntityDisabledException(string entityName) : this(SRClient.MessagingEntityIsDisabledException(entityName), null)
		{
			base.IsTransient = false;
		}

		public MessagingEntityDisabledException(string message, Exception innerException) : base(message, innerException)
		{
			base.IsTransient = false;
		}

		internal MessagingEntityDisabledException(MessagingExceptionDetail detail, TrackingContext trackingContext) : base(detail, trackingContext)
		{
			base.IsTransient = false;
		}

		internal MessagingEntityDisabledException(MessagingExceptionDetail detail, TrackingContext trackingContext, Exception innerException) : base(detail, trackingContext, innerException)
		{
			base.IsTransient = false;
		}

		private MessagingEntityDisabledException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}
	}
}