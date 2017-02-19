using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class IdlingCommunicationPool<TKey, TItem> : CommunicationPool<TKey, TItem>
	where TKey : class
	where TItem : class
	{
		private TimeSpan idleTimeout;

		private TimeSpan leaseTimeout;

		public TimeSpan IdleTimeout
		{
			get
			{
				return this.idleTimeout;
			}
		}

		protected TimeSpan LeaseTimeout
		{
			get
			{
				return this.leaseTimeout;
			}
		}

		protected IdlingCommunicationPool(int maxCount, TimeSpan idleTimeout, TimeSpan leaseTimeout) : base(maxCount)
		{
			this.idleTimeout = idleTimeout;
			this.leaseTimeout = leaseTimeout;
		}

		protected override CommunicationPool<TKey, TItem>.EndpointConnectionPool CreateEndpointConnectionPool(TKey key)
		{
			if (!(this.idleTimeout != TimeSpan.MaxValue) && !(this.leaseTimeout != TimeSpan.MaxValue))
			{
				return base.CreateEndpointConnectionPool(key);
			}
			return new IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool(this, key);
		}

		protected class IdleTimeoutEndpointConnectionPool : CommunicationPool<TKey, TItem>.EndpointConnectionPool
		{
			private IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool.IdleTimeoutIdleConnectionPool connections;

			public IdleTimeoutEndpointConnectionPool(IdlingCommunicationPool<TKey, TItem> parent, TKey key) : base(parent, key)
			{
				this.connections = new IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool.IdleTimeoutIdleConnectionPool(this, base.ThisLock);
			}

			protected override void AbortItem(TItem item)
			{
				this.connections.OnItemClosing(item);
				base.AbortItem(item);
			}

			protected override void CloseItem(TItem item, TimeSpan timeout)
			{
				this.connections.OnItemClosing(item);
				base.CloseItem(item, timeout);
			}

			protected override CommunicationPool<TKey, TItem>.IdleConnectionPool GetIdleConnectionPool()
			{
				return this.connections;
			}

			public override void Prune(List<TItem> itemsToClose)
			{
				if (this.connections != null)
				{
					this.connections.Prune(itemsToClose, false);
				}
			}

			protected class IdleTimeoutIdleConnectionPool : CommunicationPool<TKey, TItem>.EndpointConnectionPool.PoolIdleConnectionPool
			{
				private const int timerThreshold = 0;

				private IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool parent;

				private TimeSpan idleTimeout;

				private TimeSpan leaseTimeout;

				private IOThreadTimer idleTimer;

				private static Action<object> onIdle;

				private object thisLock;

				private Exception pendingException;

				private Dictionary<TItem, IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool.IdleTimeoutIdleConnectionPool.IdlingConnectionSettings> connectionMapping;

				public IdleTimeoutIdleConnectionPool(IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool parent, object thisLock) : base(parent.Parent.MaxIdleConnectionPoolCount)
				{
					this.parent = parent;
					IdlingCommunicationPool<TKey, TItem> idlingCommunicationPool = (IdlingCommunicationPool<TKey, TItem>)parent.Parent;
					this.idleTimeout = idlingCommunicationPool.idleTimeout;
					this.leaseTimeout = idlingCommunicationPool.leaseTimeout;
					this.thisLock = thisLock;
					this.connectionMapping = new Dictionary<TItem, IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool.IdleTimeoutIdleConnectionPool.IdlingConnectionSettings>();
				}

				public override bool Add(TItem connection)
				{
					this.ThrowPendingException();
					bool flag = base.Add(connection);
					if (flag)
					{
						this.connectionMapping.Add(connection, new IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool.IdleTimeoutIdleConnectionPool.IdlingConnectionSettings());
						this.StartTimerIfNecessary();
					}
					return flag;
				}

				private void CancelTimer()
				{
					if (this.idleTimer != null)
					{
						this.idleTimer.Cancel();
					}
				}

				private bool IdleOutConnection(TItem connection, DateTime now)
				{
					if (connection == null)
					{
						return false;
					}
					bool flag = false;
					IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool.IdleTimeoutIdleConnectionPool.IdlingConnectionSettings item = this.connectionMapping[connection];
					if (now > (item.LastUsage + this.idleTimeout))
					{
						this.TraceConnectionIdleTimeoutExpired();
						flag = true;
					}
					else if ((now - item.CreationTime) >= this.leaseTimeout)
					{
						this.TraceConnectionLeaseTimeoutExpired();
						flag = true;
					}
					return flag;
				}

				private static void OnIdle(object state)
				{
					((IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool.IdleTimeoutIdleConnectionPool)state).OnIdle();
				}

				private void OnIdle()
				{
					List<TItem> tItems = new List<TItem>();
					lock (this.thisLock)
					{
						try
						{
							this.Prune(tItems, true);
						}
						catch (Exception exception1)
						{
							Exception exception = exception1;
							if (Fx.IsFatal(exception))
							{
								throw;
							}
							this.pendingException = exception;
							this.CancelTimer();
						}
					}
					TimeoutHelper timeoutHelper = new TimeoutHelper(TimeoutHelper.Divide(this.idleTimeout, 2));
					for (int i = 0; i < tItems.Count; i++)
					{
						this.parent.CloseIdleConnection(tItems[i], timeoutHelper.RemainingTime());
					}
				}

				public void OnItemClosing(TItem connection)
				{
					this.ThrowPendingException();
					lock (this.thisLock)
					{
						this.connectionMapping.Remove(connection);
					}
				}

				public void Prune(List<TItem> itemsToClose, bool calledFromTimer)
				{
					bool flag;
					if (!calledFromTimer)
					{
						this.ThrowPendingException();
					}
					if (this.Count == 0)
					{
						return;
					}
					DateTime utcNow = DateTime.UtcNow;
					bool count = false;
					lock (this.thisLock)
					{
						TItem[] tItemArray = new TItem[this.Count];
						for (int i = 0; i < (int)tItemArray.Length; i++)
						{
							tItemArray[i] = base.Take(out flag);
							DiagnosticUtility.DebugAssert(tItemArray[i] != null, "IdleConnections should only be modified under thisLock");
							if (flag || this.IdleOutConnection(tItemArray[i], utcNow))
							{
								itemsToClose.Add(tItemArray[i]);
								tItemArray[i] = default(TItem);
							}
						}
						for (int j = 0; j < (int)tItemArray.Length; j++)
						{
							if (tItemArray[j] != null)
							{
								DiagnosticUtility.DebugAssert(base.Return(tItemArray[j]), "IdleConnections should only be modified under thisLock");
							}
						}
						count = this.Count > 0;
					}
					if (calledFromTimer && count)
					{
						this.idleTimer.Set(this.idleTimeout);
					}
				}

				public override bool Return(TItem connection)
				{
					this.ThrowPendingException();
					if (!this.connectionMapping.ContainsKey(connection))
					{
						return false;
					}
					bool flag = base.Return(connection);
					if (flag)
					{
						this.connectionMapping[connection].LastUsage = DateTime.UtcNow;
						this.StartTimerIfNecessary();
					}
					return flag;
				}

				private void StartTimerIfNecessary()
				{
					if (this.Count > 0)
					{
						if (this.idleTimer == null)
						{
							if (IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool.IdleTimeoutIdleConnectionPool.onIdle == null)
							{
								IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool.IdleTimeoutIdleConnectionPool.onIdle = new Action<object>(IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool.IdleTimeoutIdleConnectionPool.OnIdle);
							}
							this.idleTimer = new IOThreadTimer(IdlingCommunicationPool<TKey, TItem>.IdleTimeoutEndpointConnectionPool.IdleTimeoutIdleConnectionPool.onIdle, this, false);
						}
						this.idleTimer.Set(this.idleTimeout);
					}
				}

				public override TItem Take(out bool closeItem)
				{
					this.ThrowPendingException();
					DateTime utcNow = DateTime.UtcNow;
					TItem tItem = base.Take(out closeItem);
					if (!closeItem)
					{
						closeItem = this.IdleOutConnection(tItem, utcNow);
					}
					return tItem;
				}

				private void ThrowPendingException()
				{
					if (this.pendingException != null)
					{
						lock (this.thisLock)
						{
							if (this.pendingException != null)
							{
								Exception exception = this.pendingException;
								this.pendingException = null;
								throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(exception);
							}
						}
					}
				}

				private void TraceConnectionIdleTimeoutExpired()
				{
				}

				private void TraceConnectionLeaseTimeoutExpired()
				{
				}

				private class IdlingConnectionSettings
				{
					private DateTime creationTime;

					private DateTime lastUsage;

					public DateTime CreationTime
					{
						get
						{
							return this.creationTime;
						}
					}

					public DateTime LastUsage
					{
						get
						{
							return this.lastUsage;
						}
						set
						{
							this.lastUsage = value;
						}
					}

					public IdlingConnectionSettings()
					{
						this.creationTime = DateTime.UtcNow;
						this.lastUsage = this.creationTime;
					}
				}
			}
		}
	}
}