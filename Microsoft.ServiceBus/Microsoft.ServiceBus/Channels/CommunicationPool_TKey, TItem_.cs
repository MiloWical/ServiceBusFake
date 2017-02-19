using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class CommunicationPool<TKey, TItem>
	where TKey : class
	where TItem : class
	{
		private const int pruneThreshold = 30;

		private Dictionary<TKey, CommunicationPool<TKey, TItem>.EndpointConnectionPool> endpointPools;

		private int maxCount;

		private int openCount;

		private int pruneAccrual;

		public int MaxIdleConnectionPoolCount
		{
			get
			{
				return this.maxCount;
			}
		}

		protected object ThisLock
		{
			get
			{
				return this;
			}
		}

		protected CommunicationPool(int maxCount)
		{
			this.maxCount = maxCount;
			this.endpointPools = new Dictionary<TKey, CommunicationPool<TKey, TItem>.EndpointConnectionPool>();
			this.openCount = 1;
		}

		protected abstract void AbortItem(TItem item);

		public void AddConnection(TKey key, TItem connection, TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			CommunicationPool<TKey, TItem>.EndpointConnectionPool endpointPool = this.GetEndpointPool(key, timeoutHelper.RemainingTime());
			endpointPool.AddConnection(connection, timeoutHelper.RemainingTime());
		}

		public bool Close(TimeSpan timeout)
		{
			bool flag;
			lock (this.ThisLock)
			{
				if (this.openCount > 0)
				{
					CommunicationPool<TKey, TItem> communicationPool = this;
					communicationPool.openCount = communicationPool.openCount - 1;
					if (this.openCount != 0)
					{
						flag = false;
					}
					else
					{
						this.OnClose(timeout);
						flag = true;
					}
				}
				else
				{
					flag = true;
				}
			}
			return flag;
		}

		protected abstract void CloseItem(TItem item, TimeSpan timeout);

		protected virtual CommunicationPool<TKey, TItem>.EndpointConnectionPool CreateEndpointConnectionPool(TKey key)
		{
			return new CommunicationPool<TKey, TItem>.EndpointConnectionPool(this, key);
		}

		private CommunicationPool<TKey, TItem>.EndpointConnectionPool GetEndpointPool(TKey key, TimeSpan timeout)
		{
			CommunicationPool<TKey, TItem>.EndpointConnectionPool endpointConnectionPool = null;
			List<TItem> tItems = null;
			lock (this.ThisLock)
			{
				if (!this.endpointPools.TryGetValue(key, out endpointConnectionPool))
				{
					tItems = this.PruneIfNecessary();
					endpointConnectionPool = this.CreateEndpointConnectionPool(key);
					this.endpointPools.Add(key, endpointConnectionPool);
				}
			}
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(endpointConnectionPool != null, "EndpointPool must be non-null at this point");
			if (tItems != null && tItems.Count > 0)
			{
				TimeoutHelper timeoutHelper = new TimeoutHelper(TimeoutHelper.Divide(timeout, 2));
				for (int i = 0; i < tItems.Count; i++)
				{
					endpointConnectionPool.CloseIdleConnection(tItems[i], timeoutHelper.RemainingTime());
				}
			}
			return endpointConnectionPool;
		}

		protected abstract TKey GetPoolKey(EndpointAddress address, Uri via);

		private void OnClose(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			foreach (CommunicationPool<TKey, TItem>.EndpointConnectionPool value in this.endpointPools.Values)
			{
				try
				{
					value.Close(timeoutHelper.RemainingTime());
				}
				catch (CommunicationException communicationException1)
				{
					CommunicationException communicationException = communicationException1;
					Fx.Exception.TraceHandled(communicationException, string.Concat(this.GetType(), ".OnClose"), null);
				}
				catch (TimeoutException timeoutException1)
				{
					TimeoutException timeoutException = timeoutException1;
					Fx.Exception.TraceHandled(timeoutException, string.Concat(this.GetType(), ".OnClose"), null);
				}
			}
			this.endpointPools.Clear();
		}

		protected virtual void OnClosed()
		{
		}

		private List<TItem> PruneIfNecessary()
		{
			List<TItem> tItems = null;
			CommunicationPool<TKey, TItem> communicationPool = this;
			communicationPool.pruneAccrual = communicationPool.pruneAccrual + 1;
			if (this.pruneAccrual > 30)
			{
				this.pruneAccrual = 0;
				tItems = new List<TItem>();
				foreach (CommunicationPool<TKey, TItem>.EndpointConnectionPool value in this.endpointPools.Values)
				{
					value.Prune(tItems);
				}
				List<TKey> tKeys = null;
				foreach (KeyValuePair<TKey, CommunicationPool<TKey, TItem>.EndpointConnectionPool> endpointPool in this.endpointPools)
				{
					if (!endpointPool.Value.CloseIfEmpty())
					{
						continue;
					}
					if (tKeys == null)
					{
						tKeys = new List<TKey>();
					}
					tKeys.Add(endpointPool.Key);
				}
				if (tKeys != null)
				{
					for (int i = 0; i < tKeys.Count; i++)
					{
						this.endpointPools.Remove(tKeys[i]);
					}
				}
			}
			return tItems;
		}

		public void ReturnConnection(TKey key, TItem connection, bool connectionIsStillGood, TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			CommunicationPool<TKey, TItem>.EndpointConnectionPool endpointPool = this.GetEndpointPool(key, timeoutHelper.RemainingTime());
			endpointPool.ReturnConnection(connection, connectionIsStillGood, timeoutHelper.RemainingTime());
		}

		public TItem TakeConnection(EndpointAddress address, Uri via, TimeSpan timeout, out TKey key)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			key = this.GetPoolKey(address, via);
			CommunicationPool<TKey, TItem>.EndpointConnectionPool endpointPool = this.GetEndpointPool(key, timeoutHelper.RemainingTime());
			return endpointPool.TakeConnection(timeoutHelper.RemainingTime());
		}

		public bool TryOpen()
		{
			bool flag;
			lock (this.ThisLock)
			{
				if (this.openCount > 0)
				{
					CommunicationPool<TKey, TItem> communicationPool = this;
					communicationPool.openCount = communicationPool.openCount + 1;
					flag = true;
				}
				else
				{
					flag = false;
				}
			}
			return flag;
		}

		protected class EndpointConnectionPool
		{
			private TKey key;

			private List<TItem> busyConnections;

			private bool closed;

			private CommunicationPool<TKey, TItem>.IdleConnectionPool idleConnections;

			private CommunicationPool<TKey, TItem> parent;

			private CommunicationPool<TKey, TItem>.IdleConnectionPool IdleConnections
			{
				get
				{
					if (this.idleConnections == null)
					{
						this.idleConnections = this.GetIdleConnectionPool();
					}
					return this.idleConnections;
				}
			}

			protected TKey Key
			{
				get
				{
					return this.key;
				}
			}

			protected CommunicationPool<TKey, TItem> Parent
			{
				get
				{
					return this.parent;
				}
			}

			protected object ThisLock
			{
				get
				{
					return this;
				}
			}

			public EndpointConnectionPool(CommunicationPool<TKey, TItem> parent, TKey key)
			{
				this.key = key;
				this.parent = parent;
				this.busyConnections = new List<TItem>();
			}

			public void Abort()
			{
				if (this.closed)
				{
					return;
				}
				List<TItem> tItems = null;
				lock (this.ThisLock)
				{
					if (!this.closed)
					{
						this.closed = true;
						tItems = this.SnapshotIdleConnections();
					}
					else
					{
						return;
					}
				}
				this.AbortConnections(tItems);
			}

			private void AbortConnections(List<TItem> idleItemsToClose)
			{
				for (int i = 0; i < idleItemsToClose.Count; i++)
				{
					this.AbortItem(idleItemsToClose[i]);
				}
				for (int j = 0; j < this.busyConnections.Count; j++)
				{
					this.AbortItem(this.busyConnections[j]);
				}
				this.busyConnections.Clear();
			}

			protected virtual void AbortItem(TItem item)
			{
				this.parent.AbortItem(item);
			}

			public void AddConnection(TItem connection, TimeSpan timeout)
			{
				bool flag = false;
				lock (this.ThisLock)
				{
					if (this.closed)
					{
						flag = true;
					}
					else if (!this.IdleConnections.Add(connection))
					{
						flag = true;
					}
				}
				if (flag)
				{
					try
					{
						this.CloseItem(connection, timeout);
					}
					catch (CommunicationException communicationException1)
					{
						CommunicationException communicationException = communicationException1;
						Fx.Exception.TraceHandled(communicationException, string.Concat(this.GetType(), ".AddConnection"), null);
						this.AbortItem(connection);
					}
				}
			}

			public void Close(TimeSpan timeout)
			{
				List<TItem> tItems = null;
				lock (this.ThisLock)
				{
					if (!this.closed)
					{
						this.closed = true;
						tItems = this.SnapshotIdleConnections();
					}
					else
					{
						return;
					}
				}
				try
				{
					TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
					for (int i = 0; i < tItems.Count; i++)
					{
						this.CloseItem(tItems[i], timeoutHelper.RemainingTime());
					}
					tItems.Clear();
				}
				finally
				{
					this.AbortConnections(tItems);
				}
			}

			public void CloseIdleConnection(TItem connection, TimeSpan timeout)
			{
				bool flag = true;
				try
				{
					try
					{
						this.CloseItem(connection, timeout);
						flag = false;
					}
					catch (CommunicationException communicationException1)
					{
						CommunicationException communicationException = communicationException1;
						Fx.Exception.TraceHandled(communicationException, string.Concat(this.GetType().Name, ".CloseIdleConnection"), null);
					}
					catch (TimeoutException timeoutException1)
					{
						TimeoutException timeoutException = timeoutException1;
						Fx.Exception.TraceHandled(timeoutException, string.Concat(this.GetType().Name, ".CloseIdleConnection"), null);
					}
				}
				finally
				{
					if (flag)
					{
						this.AbortItem(connection);
					}
				}
			}

			public bool CloseIfEmpty()
			{
				bool flag;
				lock (this.ThisLock)
				{
					if (!this.closed)
					{
						if (this.busyConnections.Count > 0)
						{
							flag = false;
							return flag;
						}
						else if (this.idleConnections == null || this.idleConnections.Count <= 0)
						{
							this.closed = true;
						}
						else
						{
							flag = false;
							return flag;
						}
					}
					return true;
				}
				return flag;
			}

			protected virtual void CloseItem(TItem item, TimeSpan timeout)
			{
				this.parent.CloseItem(item, timeout);
			}

			protected virtual CommunicationPool<TKey, TItem>.IdleConnectionPool GetIdleConnectionPool()
			{
				return new CommunicationPool<TKey, TItem>.EndpointConnectionPool.PoolIdleConnectionPool(this.parent.MaxIdleConnectionPoolCount);
			}

			protected virtual void OnConnectionAborted()
			{
			}

			public virtual void Prune(List<TItem> itemsToClose)
			{
			}

			public void ReturnConnection(TItem connection, bool connectionIsStillGood, TimeSpan timeout)
			{
				bool flag = false;
				bool flag1 = false;
				lock (this.ThisLock)
				{
					if (this.closed)
					{
						flag1 = true;
					}
					else if (!this.busyConnections.Remove(connection) || !connectionIsStillGood)
					{
						flag1 = true;
					}
					else if (!this.IdleConnections.Return(connection))
					{
						flag = true;
					}
				}
				if (flag)
				{
					this.CloseIdleConnection(connection, timeout);
					return;
				}
				if (flag1)
				{
					this.AbortItem(connection);
					this.OnConnectionAborted();
				}
			}

			private List<TItem> SnapshotIdleConnections()
			{
				bool flag;
				List<TItem> tItems = new List<TItem>();
				while (true)
				{
					TItem tItem = this.IdleConnections.Take(out flag);
					if (tItem == null)
					{
						break;
					}
					tItems.Add(tItem);
				}
				return tItems;
			}

			public TItem TakeConnection(TimeSpan timeout)
			{
				bool flag;
				TItem tItem;
				TItem tItem1 = default(TItem);
				List<TItem> tItems = null;
				lock (this.ThisLock)
				{
					if (!this.closed)
					{
						while (true)
						{
							tItem1 = this.IdleConnections.Take(out flag);
							if (tItem1 == null)
							{
								break;
							}
							if (flag)
							{
								if (tItems == null)
								{
									tItems = new List<TItem>();
								}
								tItems.Add(tItem1);
							}
							else
							{
								this.busyConnections.Add(tItem1);
								break;
							}
						}
						if (tItems != null)
						{
							TimeoutHelper timeoutHelper = new TimeoutHelper(TimeoutHelper.Divide(timeout, 2));
							for (int i = 0; i < tItems.Count; i++)
							{
								this.CloseIdleConnection(tItems[i], timeoutHelper.RemainingTime());
							}
						}
						return tItem1;
					}
					else
					{
						tItem = default(TItem);
					}
				}
				return tItem;
			}

			protected class PoolIdleConnectionPool : CommunicationPool<TKey, TItem>.IdleConnectionPool
			{
				private Microsoft.ServiceBus.Channels.Pool<TItem> idleConnections;

				private int maxCount;

				public override int Count
				{
					get
					{
						return this.idleConnections.Count;
					}
				}

				public PoolIdleConnectionPool(int maxCount)
				{
					this.idleConnections = new Microsoft.ServiceBus.Channels.Pool<TItem>(maxCount);
					this.maxCount = maxCount;
				}

				public override bool Add(TItem connection)
				{
					return this.ReturnToPool(connection);
				}

				public override bool Return(TItem connection)
				{
					return this.ReturnToPool(connection);
				}

				private bool ReturnToPool(TItem connection)
				{
					bool flag = this.idleConnections.Return(connection);
					if (!flag && Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
					{
						DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
						string traceCodeConnectionPoolMaxOutboundConnectionsPerEndpointQuotaReached = Resources.TraceCodeConnectionPoolMaxOutboundConnectionsPerEndpointQuotaReached;
						object[] objArray = new object[] { this.maxCount };
						diagnosticTrace.TraceEvent(TraceEventType.Information, TraceCode.ConnectionPoolMaxOutboundConnectionsPerEndpointQuotaReached, Microsoft.ServiceBus.SR.GetString(traceCodeConnectionPoolMaxOutboundConnectionsPerEndpointQuotaReached, objArray), null, null, this);
					}
					return flag;
				}

				public override TItem Take(out bool closeItem)
				{
					closeItem = false;
					return this.idleConnections.Take();
				}
			}
		}

		protected abstract class IdleConnectionPool
		{
			public abstract int Count
			{
				get;
			}

			protected IdleConnectionPool()
			{
			}

			public abstract bool Add(TItem item);

			public abstract bool Return(TItem item);

			public abstract TItem Take(out bool closeItem);
		}
	}
}