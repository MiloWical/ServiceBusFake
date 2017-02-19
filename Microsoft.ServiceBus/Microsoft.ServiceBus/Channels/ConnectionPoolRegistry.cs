using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class ConnectionPoolRegistry
	{
		private Dictionary<string, List<ConnectionPool>> registry;

		private object ThisLock
		{
			get
			{
				return this.registry;
			}
		}

		protected ConnectionPoolRegistry()
		{
			this.registry = new Dictionary<string, List<ConnectionPool>>();
		}

		protected abstract ConnectionPool CreatePool(IConnectionOrientedTransportChannelFactorySettings settings);

		public ConnectionPool Lookup(IConnectionOrientedTransportChannelFactorySettings settings)
		{
			List<ConnectionPool> connectionPools;
			ConnectionPool item = null;
			string connectionPoolGroupName = settings.ConnectionPoolGroupName;
			lock (this.ThisLock)
			{
				if (!this.registry.TryGetValue(connectionPoolGroupName, out connectionPools))
				{
					connectionPools = new List<ConnectionPool>();
					this.registry.Add(connectionPoolGroupName, connectionPools);
				}
				else
				{
					int num = 0;
					while (num < connectionPools.Count)
					{
						if (!connectionPools[num].IsCompatible(settings) || !connectionPools[num].TryOpen())
						{
							num++;
						}
						else
						{
							item = connectionPools[num];
							goto Label0;
						}
					}
				}
			Label0:
				if (item == null)
				{
					item = this.CreatePool(settings);
					connectionPools.Add(item);
				}
			}
			return item;
		}

		public void Release(ConnectionPool pool, TimeSpan timeout)
		{
			lock (this.ThisLock)
			{
				if (pool.Close(timeout))
				{
					List<ConnectionPool> item = this.registry[pool.Name];
					int num = 0;
					while (num < item.Count)
					{
						if (!object.ReferenceEquals(item[num], pool))
						{
							num++;
						}
						else
						{
							item.RemoveAt(num);
							break;
						}
					}
					if (item.Count == 0)
					{
						this.registry.Remove(pool.Name);
					}
				}
			}
		}
	}
}