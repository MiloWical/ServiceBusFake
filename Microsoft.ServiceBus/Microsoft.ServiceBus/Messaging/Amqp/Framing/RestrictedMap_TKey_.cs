using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class RestrictedMap<TKey> : RestrictedMap
	{
		public object this[TKey key]
		{
			get
			{
				return base.InnerMap[new MapKey((object)key)];
			}
			set
			{
				base.InnerMap[new MapKey((object)key)] = value;
			}
		}

		public object this[MapKey key]
		{
			get
			{
				return base.InnerMap[key];
			}
			set
			{
				base.InnerMap[key] = value;
			}
		}

		protected RestrictedMap()
		{
		}

		public void Add(TKey key, object value)
		{
			base.InnerMap.Add(new MapKey((object)key), value);
		}

		public void Add(MapKey key, object value)
		{
			base.InnerMap.Add(key, value);
		}

		public void Merge(RestrictedMap<TKey> map)
		{
			foreach (KeyValuePair<MapKey, object> value in (IEnumerable<KeyValuePair<MapKey, object>>)map)
			{
				this[value.Key] = value.Value;
			}
		}

		public static implicit operator AmqpMap(RestrictedMap<TKey> restrictedMap)
		{
			if (restrictedMap == null)
			{
				return null;
			}
			return restrictedMap.InnerMap;
		}

		public bool TryGetValue<TValue>(TKey key, out TValue value)
		{
			return base.InnerMap.TryGetValue<TValue>(new MapKey((object)key), out value);
		}

		public bool TryGetValue<TValue>(MapKey key, out TValue value)
		{
			return base.InnerMap.TryGetValue<TValue>(key, out value);
		}

		public bool TryRemoveValue<TValue>(TKey key, out TValue value)
		{
			return base.InnerMap.TryRemoveValue<TValue>(new MapKey((object)key), out value);
		}
	}
}