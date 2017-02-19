using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal class SendAvailabilityExceptionInspector : ISendAvailabilityExceptionInspector
	{
		public SendAvailabilityExceptionInspector()
		{
		}

		public bool CausesFailover(Exception exception)
		{
			TimeoutException timeoutException = exception as TimeoutException;
			MessagingException messagingException = exception as MessagingException;
			bool isTransient = false;
			if (timeoutException != null)
			{
				isTransient = true;
			}
			else if (messagingException != null)
			{
				isTransient = messagingException.IsTransient;
			}
			return isTransient;
		}
	}
}