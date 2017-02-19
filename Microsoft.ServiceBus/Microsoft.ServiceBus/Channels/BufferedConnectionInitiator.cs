using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class BufferedConnectionInitiator : IConnectionInitiator
	{
		private int writeBufferSize;

		private TimeSpan flushTimeout;

		private IConnectionInitiator connectionInitiator;

		protected TimeSpan FlushTimeout
		{
			get
			{
				return this.flushTimeout;
			}
		}

		protected int WriteBufferSize
		{
			get
			{
				return this.writeBufferSize;
			}
		}

		public BufferedConnectionInitiator(IConnectionInitiator connectionInitiator, TimeSpan flushTimeout, int writeBufferSize)
		{
			this.connectionInitiator = connectionInitiator;
			this.flushTimeout = flushTimeout;
			this.writeBufferSize = writeBufferSize;
		}

		public IAsyncResult BeginConnect(Uri uri, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.connectionInitiator.BeginConnect(uri, timeout, callback, state);
		}

		public IConnection Connect(Uri uri, TimeSpan timeout)
		{
			return new BufferedConnection(this.connectionInitiator.Connect(uri, timeout), this.flushTimeout, this.writeBufferSize);
		}

		public IConnection EndConnect(IAsyncResult result)
		{
			return new BufferedConnection(this.connectionInitiator.EndConnect(result), this.flushTimeout, this.writeBufferSize);
		}
	}
}