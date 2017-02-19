using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.Caching;

namespace Microsoft.ServiceBus.Messaging
{
	internal static class RuntimeEntityDescriptionCache
	{
		private readonly static MemoryCache entityDescriptionCache;

		private readonly static TimeSpan cacheEntryTtl;

		static RuntimeEntityDescriptionCache()
		{
			RuntimeEntityDescriptionCache.cacheEntryTtl = TimeSpan.FromMinutes(10);
			string str = string.Concat(typeof(RuntimeEntityDescriptionCache).Name, "entityDescriptionCache");
			NameValueCollection nameValueCollection = new NameValueCollection()
			{
				{ "CacheMemoryLimitMegabytes", "1" }
			};
			RuntimeEntityDescriptionCache.entityDescriptionCache = new MemoryCache(str, nameValueCollection);
		}

		public static void AddOrUpdate(string entityAddress, RuntimeEntityDescription newEntityDescription)
		{
			if (string.IsNullOrWhiteSpace(entityAddress))
			{
				throw new ArgumentException(SRCore.ArgumentNullOrEmpty("entityAddress"));
			}
			if (newEntityDescription == null)
			{
				throw new ArgumentNullException("newEntityDescription");
			}
			MemoryCache memoryCaches = RuntimeEntityDescriptionCache.entityDescriptionCache;
			string upperInvariant = entityAddress.ToUpperInvariant();
			DateTimeOffset utcNow = DateTimeOffset.UtcNow;
			RuntimeEntityDescription enableMessagePartitioning = (RuntimeEntityDescription)memoryCaches.AddOrGetExisting(upperInvariant, newEntityDescription, utcNow.Add(RuntimeEntityDescriptionCache.cacheEntryTtl), null);
			if (enableMessagePartitioning != null)
			{
				enableMessagePartitioning.EnableMessagePartitioning = newEntityDescription.EnableMessagePartitioning;
			}
		}

		public static bool TryGet(string entityAddress, out RuntimeEntityDescription entityDescription)
		{
			entityDescription = (RuntimeEntityDescription)RuntimeEntityDescriptionCache.entityDescriptionCache.Get(entityAddress.ToUpperInvariant(), null);
			return entityDescription != null;
		}
	}
}