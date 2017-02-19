using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class MessagingEntityNotFoundException : MessagingException
	{
		public MessagingEntityNotFoundException(string entityName) : this(MessagingExceptionDetail.EntityNotFound(SRClient.MessagingEntityCouldNotBeFound(entityName)), null)
		{
			base.IsTransient = false;
		}

		public MessagingEntityNotFoundException(string message, Exception innerException) : base(MessagingExceptionDetail.EntityNotFound(message), null, innerException)
		{
			base.IsTransient = false;
		}

		internal MessagingEntityNotFoundException(MessagingExceptionDetail detail, TrackingContext trackingContext) : base(detail, trackingContext)
		{
			base.IsTransient = false;
		}

		internal MessagingEntityNotFoundException(MessagingExceptionDetail detail, TrackingContext trackingContext, Exception innerException) : base(detail, trackingContext, innerException)
		{
			base.IsTransient = false;
		}

		private MessagingEntityNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}

		public override string ToString()
		{
			return this.Message;
		}
	}
}