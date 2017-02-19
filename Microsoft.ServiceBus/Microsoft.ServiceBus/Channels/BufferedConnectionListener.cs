using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class BufferedConnectionListener : IConnectionListener
	{
		private int writeBufferSize;

		private TimeSpan flushTimeout;

		private IConnectionListener connectionListener;

		public BufferedConnectionListener(IConnectionListener connectionListener, TimeSpan flushTimeout, int writeBufferSize)
		{
			this.connectionListener = connectionListener;
			this.flushTimeout = flushTimeout;
			this.writeBufferSize = writeBufferSize;
		}

		public void Abort()
		{
			this.connectionListener.Abort();
		}

		public IAsyncResult BeginAccept(AsyncCallback callback, object state)
		{
			return this.connectionListener.BeginAccept(callback, state);
		}

		public void Close(TimeSpan timeout)
		{
			this.connectionListener.Close(timeout);
		}

		public IConnection EndAccept(IAsyncResult result)
		{
			IConnection connection = this.connectionListener.EndAccept(result);
			if (connection == null)
			{
				return connection;
			}
			return new BufferedConnection(connection, this.flushTimeout, this.writeBufferSize);
		}

		public void Open(TimeSpan timeout)
		{
			this.connectionListener.Open(timeout);
		}
	}
}