using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal interface ISingletonChannelListener
	{
		TimeSpan ReceiveTimeout
		{
			get;
		}

		void ReceiveRequest(RequestContext requestContext, Action callback, bool canDispatchOnThisThread);
	}
}