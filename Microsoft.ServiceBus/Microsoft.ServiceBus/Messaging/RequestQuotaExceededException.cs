using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	internal class RequestQuotaExceededException : QuotaExceededException
	{
		public RequestQuotaExceededException(string message) : base(message)
		{
		}

		public RequestQuotaExceededException(string message, Exception innerException) : base(message, innerException)
		{
		}

		internal RequestQuotaExceededException(MessagingExceptionDetail detail, TrackingContext trackingContext) : base(detail, trackingContext)
		{
		}

		internal RequestQuotaExceededException(MessagingExceptionDetail detail, TrackingContext trackingContext, Exception innerException) : base(detail, trackingContext, innerException)
		{
		}

		protected RequestQuotaExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}
	}
}