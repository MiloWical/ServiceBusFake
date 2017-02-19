using System;

namespace Microsoft.ServiceBus.Channels
{
	internal interface IConnectionInitiator
	{
		IAsyncResult BeginConnect(Uri uri, TimeSpan timeout, AsyncCallback callback, object state);

		IConnection Connect(Uri uri, TimeSpan timeout);

		IConnection EndConnect(IAsyncResult result);
	}
}