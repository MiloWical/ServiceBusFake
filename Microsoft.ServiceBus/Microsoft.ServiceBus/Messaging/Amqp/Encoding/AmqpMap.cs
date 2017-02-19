using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class AmqpMap : IEnumerable<KeyValuePair<MapKey, object>>, IEnumerable
	{
		private IDictionary<MapKey, object> @value;

		public int Count
		{
			get
			{
				return this.@value.Count;
			}
		}

		public object this[MapKey key]
		{
			get
			{
				object obj;
				if (this.@value.TryGetValue(key, out obj))
				{
					return obj;
				}
				return null;
			}
			set
			{
				this.@value[key] = value;
			}
		}

		public int ValueSize
		{
			get
			{
				return MapEncoding.GetValueSize(this);
			}
		}

		public AmqpMap()
		{
			this.@value = new Dictionary<MapKey, object>();
		}

		public AmqpMap(IDictionary<MapKey, object> value)
		{
			this.@value = value;
		}

		public AmqpMap(IDictionary value) : this()
		{
			foreach (DictionaryEntry dictionaryEntry in value)
			{
				this.@value.Add(new MapKey(dictionaryEntry.Key), dictionaryEntry.Value);
			}
		}

		public void Add(MapKey key, object value)
		{
			this.@value.Add(key, value);
		}

		IEnumerator<KeyValuePair<MapKey, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<Microsoft.ServiceBus.Messaging.Amqp.Encoding.MapKey,System.Object>>.GetEnumerator()
		{
			return this.@value.GetEnumerator();
		}

		IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.@value.GetEnumerator();
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append('[');
			bool flag = true;
			foreach (KeyValuePair<MapKey, object> keyValuePair in this.@value)
			{
				if (!flag)
				{
					stringBuilder.Append(',');
				}
				else
				{
					flag = false;
				}
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] key = new object[] { keyValuePair.Key, keyValuePair.Value };
				stringBuilder.AppendFormat(invariantCulture, "{0}:{1}", key);
			}
			stringBuilder.Append(']');
			return stringBuilder.ToString();
		}

		public bool TryGetValue<TValue>(MapKey key, out TValue value)
		{
			object obj;
			if (this.@value.TryGetValue(key, out obj))
			{
				if (obj == null)
				{
					value = default(TValue);
					return true;
				}
				if (obj is TValue)
				{
					value = (TValue)obj;
					return true;
				}
			}
			value = default(TValue);
			return false;
		}

		public bool TryRemoveValue<TValue>(MapKey key, out TValue value)
		{
			if (!this.TryGetValue<TValue>(key, out value))
			{
				return false;
			}
			this.@value.Remove(key);
			return true;
		}
	}
}