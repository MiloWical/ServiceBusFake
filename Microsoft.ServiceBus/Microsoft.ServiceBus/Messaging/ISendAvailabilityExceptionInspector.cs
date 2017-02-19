using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal interface ISendAvailabilityExceptionInspector
	{
		bool CausesFailover(Exception exception);
	}
}