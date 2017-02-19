using Microsoft.ServiceBus;
using System;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public class SendAvailabilityMessagingException : MessagingException
	{
		public SendAvailabilityMessagingException(Exception innerException) : base(SRClient.PairedNamespacePrimaryEntityUnreachable, innerException)
		{
			base.IsTransient = false;
		}
	}
}