using System;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public class PairedMessagingFactoryException : MessagingException
	{
		public PairedMessagingFactoryException(string message) : base(message)
		{
			base.IsTransient = false;
		}
	}
}