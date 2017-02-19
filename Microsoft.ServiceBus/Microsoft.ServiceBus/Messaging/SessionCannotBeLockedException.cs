using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class SessionCannotBeLockedException : MessagingException
	{
		public SessionCannotBeLockedException(string message) : base(message)
		{
			base.IsTransient = false;
		}

		public SessionCannotBeLockedException(string message, Exception innerException) : base(message, innerException)
		{
			base.IsTransient = false;
		}

		private SessionCannotBeLockedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}
	}
}