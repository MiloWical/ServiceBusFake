using System;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal interface IMessageSource
	{
		Microsoft.ServiceBus.Channels.AsyncReceiveResult BeginReceive(TimeSpan timeout, WaitCallback callback, object state);

		Microsoft.ServiceBus.Channels.AsyncReceiveResult BeginWaitForMessage(TimeSpan timeout, WaitCallback callback, object state);

		Message EndReceive();

		bool EndWaitForMessage();

		Message Receive(TimeSpan timeout);

		bool WaitForMessage(TimeSpan timeout);
	}
}