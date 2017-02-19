using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal interface IRelayedOnewaySender : ICommunicationObject, IConnectionStatus
	{
		Microsoft.ServiceBus.TokenProvider TokenProvider
		{
			get;
		}

		IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state);

		void EndSend(IAsyncResult result);

		void Send(Message message, TimeSpan timeout);
	}
}