using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	internal abstract class WebStreamConnectionListener : IConnectionListener
	{
		private readonly InputQueue<IConnection> connectionQueue;

		public WebStreamConnectionListener()
		{
			this.connectionQueue = new InputQueue<IConnection>();
		}

		public abstract void Abort();

		public IAsyncResult BeginAccept(AsyncCallback callback, object state)
		{
			return this.connectionQueue.BeginDequeue(TimeSpan.MaxValue, callback, state);
		}

		public abstract void Close(TimeSpan timeout);

		public IConnection EndAccept(IAsyncResult result)
		{
			IConnection connection;
			try
			{
				connection = this.connectionQueue.EndDequeue(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && !(exception is CommunicationException))
				{
					throw Fx.Exception.AsError(new CommunicationException(exception.Message, exception), null);
				}
				throw;
			}
			return connection;
		}

		public void EnqueueConnection(IConnection connection, bool transportProtectionEnabled)
		{
			this.connectionQueue.EnqueueAndDispatch(connection);
		}

		public abstract void Open(TimeSpan timeout);
	}
}