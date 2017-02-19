using System;

namespace Microsoft.ServiceBus.Channels
{
	internal interface IConnectionListener
	{
		void Abort();

		IAsyncResult BeginAccept(AsyncCallback callback, object state);

		void Close(TimeSpan timeout);

		IConnection EndAccept(IAsyncResult result);

		void Open(TimeSpan timeout);
	}
}