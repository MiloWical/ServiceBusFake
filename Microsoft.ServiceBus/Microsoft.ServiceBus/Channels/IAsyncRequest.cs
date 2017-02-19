using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal interface IAsyncRequest : IAsyncResult, Microsoft.ServiceBus.Channels.IRequestBase
	{
		void BeginSendRequest(Message message, TimeSpan timeout);

		Message End();
	}
}