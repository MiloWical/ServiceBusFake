using System;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	internal sealed class InvalidLinkTypeException : MessagingException
	{
		public InvalidLinkTypeException(string message, Exception innerException) : base(message, innerException)
		{
			base.IsTransient = false;
		}
	}
}