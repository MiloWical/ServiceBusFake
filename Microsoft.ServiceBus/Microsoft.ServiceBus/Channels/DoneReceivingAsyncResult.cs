using Microsoft.ServiceBus.Common;
using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal class DoneReceivingAsyncResult : CompletedAsyncResult
	{
		internal DoneReceivingAsyncResult(AsyncCallback callback, object state) : base(callback, state)
		{
		}

		internal static bool End(Microsoft.ServiceBus.Channels.DoneReceivingAsyncResult result, out Message message)
		{
			message = null;
			return true;
		}

		internal static bool End(Microsoft.ServiceBus.Channels.DoneReceivingAsyncResult result, out RequestContext requestContext)
		{
			requestContext = null;
			return true;
		}

		internal static bool End(Microsoft.ServiceBus.Channels.DoneReceivingAsyncResult result)
		{
			return true;
		}
	}
}