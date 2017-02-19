using System;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public class SendAvailabilityBacklogException : Exception
	{
		public SendAvailabilityBacklogException(string message) : base(message)
		{
		}
	}
}