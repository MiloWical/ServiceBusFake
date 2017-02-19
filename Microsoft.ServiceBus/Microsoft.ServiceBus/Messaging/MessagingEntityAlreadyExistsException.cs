using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class MessagingEntityAlreadyExistsException : MessagingException
	{
		internal object ExistingEntityMetadata
		{
			get;
			set;
		}

		public MessagingEntityAlreadyExistsException(string entityName) : this(MessagingExceptionDetail.EntityConflict(SRClient.MessagingEntityAlreadyExists(entityName)), null)
		{
			base.IsTransient = false;
		}

		public MessagingEntityAlreadyExistsException(string entityName, TrackingContext trackingContext) : this(MessagingExceptionDetail.EntityConflict(SRClient.MessagingEntityAlreadyExists(entityName)), trackingContext, null)
		{
			base.IsTransient = false;
		}

		public MessagingEntityAlreadyExistsException(string message, TrackingContext trackingContext, Exception innerException) : base(MessagingExceptionDetail.EntityConflict(message), trackingContext, innerException)
		{
			base.IsTransient = false;
		}

		internal MessagingEntityAlreadyExistsException(MessagingExceptionDetail detail, TrackingContext trackingContext) : base(detail, trackingContext)
		{
			base.IsTransient = false;
		}

		internal MessagingEntityAlreadyExistsException(MessagingExceptionDetail detail, TrackingContext trackingContext, Exception innerException) : base(detail, trackingContext, innerException)
		{
			base.IsTransient = false;
		}

		private MessagingEntityAlreadyExistsException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
		}
	}
}