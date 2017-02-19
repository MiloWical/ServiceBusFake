using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public class MessagingException : Exception
	{
		public sealed override IDictionary Data
		{
			get
			{
				return base.Data;
			}
		}

		public MessagingExceptionDetail Detail
		{
			get;
			private set;
		}

		public new bool IsTransient
		{
			get;
			protected set;
		}

		public DateTime Timestamp
		{
			get;
			private set;
		}

		internal TrackingContext Tracker
		{
			get;
			private set;
		}

		public MessagingException(string message) : base(message)
		{
			MessagingExceptionDetail messagingExceptionDetail = MessagingExceptionDetail.UnknownDetail(message);
			this.Initialize(messagingExceptionDetail, null, DateTime.UtcNow);
		}

		public MessagingException(string message, Exception innerException) : base(message, innerException)
		{
			MessagingExceptionDetail messagingExceptionDetail = MessagingExceptionDetail.UnknownDetail(message);
			this.Initialize(messagingExceptionDetail, null, DateTime.UtcNow);
		}

		public MessagingException(string message, bool isTransientError, Exception innerException) : base(message, innerException)
		{
			MessagingExceptionDetail messagingExceptionDetail = MessagingExceptionDetail.UnknownDetail(message);
			this.Initialize(messagingExceptionDetail, null, DateTime.UtcNow);
			this.IsTransient = isTransientError;
		}

		internal MessagingException(MessagingExceptionDetail detail, TrackingContext trackingContext) : base(detail.Message)
		{
			this.Initialize(detail, trackingContext, DateTime.UtcNow);
		}

		internal MessagingException(MessagingExceptionDetail detail, TrackingContext trackingContext, Exception innerException) : base(detail.Message, innerException)
		{
			this.Initialize(detail, trackingContext, DateTime.UtcNow);
		}

		protected MessagingException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			this.Initialize((MessagingExceptionDetail)info.GetValue("Detail", typeof(MessagingExceptionDetail)), TrackingContext.GetInstance((string)info.GetValue("TrackingId", typeof(string)), (string)info.GetValue("SubsystemId", typeof(string)), false), (DateTime)info.GetValue("Timestamp", typeof(DateTime)));
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Detail", this.Detail);
			info.AddValue("TrackingId", this.Tracker.TrackingId);
			info.AddValue("SubsystemId", this.Tracker.SystemTracker);
			info.AddValue("Timestamp", this.Timestamp.ToString());
		}

		private void Initialize(MessagingExceptionDetail detail, TrackingContext currentTracker, DateTime timestamp)
		{
			this.IsTransient = true;
			this.Detail = detail;
			this.Tracker = currentTracker ?? TrackingContext.GetInstance(Guid.NewGuid());
			this.Timestamp = timestamp;
			if (base.GetType() != typeof(MessagingException))
			{
				this.DisablePrepareForRethrow();
			}
		}
	}
}