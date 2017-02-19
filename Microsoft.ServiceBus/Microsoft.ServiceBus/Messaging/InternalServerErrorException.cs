using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Tracing;
using System;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	internal sealed class InternalServerErrorException : MessagingException
	{
		public InternalServerErrorException() : this((Exception)null)
		{
		}

		public InternalServerErrorException(string message) : base(message)
		{
			this.Initialize();
		}

		public InternalServerErrorException(Exception innerException) : base(SRClient.InternalServerError, true, innerException)
		{
			this.Initialize();
		}

		public InternalServerErrorException(MessagingExceptionDetail detail, TrackingContext context) : base(detail, context)
		{
			this.Initialize();
		}

		public InternalServerErrorException(TrackingContext context) : base(MessagingExceptionDetail.UnknownDetail(SRClient.InternalServerError), context)
		{
			this.Initialize();
		}

		private void Initialize()
		{
			base.IsTransient = true;
		}
	}
}