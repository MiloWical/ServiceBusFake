using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class MessageClientEntityManager
	{
		private readonly object syncLock;

		private readonly EventHandler onInnerObjectClosed;

		internal List<ClientEntity> clientObjects;

		private bool closed;

		public MessageClientEntityManager()
		{
			this.clientObjects = new List<ClientEntity>();
			this.onInnerObjectClosed = new EventHandler(this.OnInnerObjectClosed);
			this.syncLock = new object();
		}

		public void Abort()
		{
			List<ClientEntity> clientEntities = null;
			lock (this.syncLock)
			{
				if (!this.closed)
				{
					this.closed = true;
					clientEntities = this.clientObjects;
					this.clientObjects = new List<ClientEntity>();
				}
			}
			if (clientEntities != null)
			{
				clientEntities.ForEach((ClientEntity m) => m.Abort());
			}
		}

		public void Add(ClientEntity clientEntity)
		{
			bool flag = false;
			lock (this.syncLock)
			{
				if (!this.closed)
				{
					this.clientObjects.Add(clientEntity);
					clientEntity.SafeAddClosed(this.onInnerObjectClosed);
				}
				else
				{
					flag = true;
				}
			}
			if (flag)
			{
				clientEntity.Abort();
				throw FxTrace.Exception.AsError(new OperationCanceledException(SRClient.MessageEntityDisposed), null);
			}
		}

		public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			List<ClientEntity> clientEntities;
			lock (this.syncLock)
			{
				if (this.closed)
				{
					clientEntities = new List<ClientEntity>();
				}
				else
				{
					this.closed = true;
					clientEntities = this.clientObjects;
					this.clientObjects = new List<ClientEntity>();
				}
			}
			return new CloseEntityCollectionAsyncResult(clientEntities, timeout, callback, state);
		}

		public void EndClose(IAsyncResult result)
		{
			CloseEntityCollectionAsyncResult.End(result);
		}

		private void OnInnerObjectClosed(object sender, EventArgs args)
		{
			if (!this.closed)
			{
				lock (this.syncLock)
				{
					if (!this.closed)
					{
						ClientEntity clientEntity = (ClientEntity)sender;
						this.clientObjects.Remove(clientEntity);
						clientEntity.Closed -= this.onInnerObjectClosed;
					}
				}
			}
		}

		public void UpdateRetryPolicy(RetryPolicy policy)
		{
			lock (this.syncLock)
			{
				this.clientObjects.ForEach((ClientEntity entity) => {
					if (entity.ShouldLinkRetryPolicy)
					{
						entity.RetryPolicy = policy;
					}
				});
			}
		}
	}
}