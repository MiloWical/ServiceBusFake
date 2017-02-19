using System;

namespace Microsoft.ServiceBus.Messaging
{
	public abstract class MessageClientEntity : ClientEntity
	{
		internal MessageClientEntity()
		{
		}

		public new IAsyncResult BeginClose(AsyncCallback callback, object state)
		{
			return base.BeginClose(this.OperationTimeout, callback, state);
		}

		public new void EndClose(IAsyncResult result)
		{
			base.EndClose(result);
		}
	}
}