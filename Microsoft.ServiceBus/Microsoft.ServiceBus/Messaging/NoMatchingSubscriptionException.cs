using System;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class NoMatchingSubscriptionException : MessagingException
	{
		public NoMatchingSubscriptionException(string message) : base(message)
		{
			base.IsTransient = false;
		}

		public NoMatchingSubscriptionException(string message, Exception innerException) : base(message, innerException)
		{
			base.IsTransient = false;
		}

		public override string ToString()
		{
			return this.Message;
		}
	}
}