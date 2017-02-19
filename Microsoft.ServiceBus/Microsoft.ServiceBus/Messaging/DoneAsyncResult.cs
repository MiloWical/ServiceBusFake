using Microsoft.ServiceBus.Common;
using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal class DoneAsyncResult : CompletedAsyncResult<bool>
	{
		internal DoneAsyncResult(bool data, AsyncCallback callback, object state) : base(data, callback, state)
		{
		}
	}
}