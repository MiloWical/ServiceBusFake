using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal interface IRequest : Microsoft.ServiceBus.Channels.IRequestBase
	{
		void SendRequest(Message message, TimeSpan timeout);

		Message WaitForReply(TimeSpan timeout);
	}
}