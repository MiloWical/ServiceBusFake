using System;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	internal sealed class SoapActionNotSupportedException : MessagingException
	{
		public SoapActionNotSupportedException(string message, Exception exception) : base(message, exception)
		{
			base.IsTransient = false;
		}
	}
}