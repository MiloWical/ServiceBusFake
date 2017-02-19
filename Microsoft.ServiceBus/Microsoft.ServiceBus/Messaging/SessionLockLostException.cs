using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class SessionLockLostException : MessagingException
	{
		public SessionLockLostException(string message) : base(message)
		{
			base.IsTransient = false;
		}

		public SessionLockLostException(string message, Exception innerException) : base(message, innerException)
		{
			base.IsTransient = false;
		}

		private SessionLockLostException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}
	}
}