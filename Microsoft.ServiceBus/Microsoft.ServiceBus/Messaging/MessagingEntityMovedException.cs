using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	internal sealed class MessagingEntityMovedException : MessagingException
	{
		public MessagingEntityMovedException(string entityName) : base(SRClient.MessagingEntityMoved(entityName), null)
		{
			base.IsTransient = false;
		}

		public MessagingEntityMovedException(string mesage, Exception innerException) : base(mesage, innerException)
		{
			base.IsTransient = false;
		}

		internal MessagingEntityMovedException(MessagingExceptionDetail detail, TrackingContext trackingContext) : base(detail, trackingContext)
		{
			base.IsTransient = false;
		}

		internal MessagingEntityMovedException(MessagingExceptionDetail detail, TrackingContext trackingContext, Exception innerException) : base(detail, trackingContext, innerException)
		{
			base.IsTransient = false;
		}

		private MessagingEntityMovedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}
	}
}