using Microsoft.ServiceBus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[CollectionDataContract(Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	internal sealed class PropertyDictionary : IDictionary<string, object>, ICollection<KeyValuePair<string, object>>, IEnumerable<KeyValuePair<string, object>>, IEnumerable
	{
		private readonly IDictionary<string, object> inner;

		public int Count
		{
			get
			{
				return this.inner.Count;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return this.inner.IsReadOnly;
			}
		}

		public object this[string key]
		{
			get
			{
				return this.inner[key];
			}
			set
			{
				this.inner[key] = value;
			}
		}

		public ICollection<string> Keys
		{
			get
			{
				return this.inner.Keys;
			}
		}

		public ICollection<object> Values
		{
			get
			{
				return this.inner.Values;
			}
		}

		public PropertyDictionary()
		{
			this.inner = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}

		public PropertyDictionary(IDictionary<string, object> container)
		{
			this.inner = container;
		}

		public void Add(string key, object value)
		{
			if (value != null)
			{
				Type type = value.GetType();
				if (!SerializationUtilities.IsSupportedPropertyType(type))
				{
					throw new ArgumentException(SRClient.NotSupportedPropertyType(type), "value");
				}
			}
			this.inner.Add(key, value);
		}

		public void Add(KeyValuePair<string, object> item)
		{
			this.inner.Add(item);
		}

		public void Clear()
		{
			this.inner.Clear();
		}

		public bool Contains(KeyValuePair<string, object> item)
		{
			return this.inner.Contains(item);
		}

		public bool ContainsKey(string key)
		{
			return this.inner.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
		{
			this.inner.CopyTo(array, arrayIndex);
		}

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			return this.inner.GetEnumerator();
		}

		public bool Remove(string key)
		{
			return this.inner.Remove(key);
		}

		public bool Remove(KeyValuePair<string, object> item)
		{
			return this.inner.Remove(item);
		}

		IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.inner.GetEnumerator();
		}

		public bool TryGetValue(string key, out object value)
		{
			return this.inner.TryGetValue(key, out value);
		}
	}
}