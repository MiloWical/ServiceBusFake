using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using System;

namespace Microsoft.ServiceBus
{
	internal class DemuxSocketListener : IConnectionListener
	{
		private InputQueue<IConnection> connectionQueue;

		private DemuxSocketManager demuxManager;

		private bool isClosed;

		private bool listening;

		private object mutex;

		private string type;

		private System.Uri uri;

		public string Type
		{
			get
			{
				return this.type;
			}
		}

		public System.Uri Uri
		{
			get
			{
				return this.uri;
			}
		}

		public DemuxSocketListener(System.Uri uri, string type, DemuxSocketManager demuxManager)
		{
			this.uri = uri;
			this.type = type;
			this.demuxManager = demuxManager;
			this.mutex = new object();
			this.connectionQueue = new InputQueue<IConnection>();
		}

		public void Abort()
		{
			lock (this.mutex)
			{
				if (!this.isClosed)
				{
					this.isClosed = true;
					if (this.listening)
					{
						this.demuxManager.UnregisterListener(TimeSpan.Zero, this);
					}
				}
			}
		}

		public IAsyncResult BeginAccept(AsyncCallback callback, object state)
		{
			return this.connectionQueue.BeginDequeue(TimeSpan.MaxValue, callback, state);
		}

		public void Close(TimeSpan timeout)
		{
			lock (this.mutex)
			{
				if (!this.isClosed)
				{
					this.isClosed = true;
					if (this.listening)
					{
						this.demuxManager.UnregisterListener(timeout, this);
					}
				}
			}
		}

		public IConnection EndAccept(IAsyncResult result)
		{
			return this.connectionQueue.EndDequeue(result);
		}

		public void EnqueueConnection(IConnection socket, Action dequeuedCallback)
		{
			this.connectionQueue.EnqueueAndDispatch(socket, dequeuedCallback);
		}

		public void Open(TimeSpan timeout)
		{
			lock (this.mutex)
			{
				this.demuxManager.RegisterListener(timeout, this);
				this.listening = true;
			}
		}
	}
}