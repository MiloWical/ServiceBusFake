using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Multiple<T>
	{
		private List<T> @value;

		public Multiple()
		{
			this.@value = new List<T>();
		}

		public Multiple(IList<T> value)
		{
			this.@value = new List<T>(value);
		}

		public void Add(T item)
		{
			this.@value.Add(item);
		}

		public bool Contains(T item)
		{
			return this.@value.Contains(item);
		}

		public static Multiple<T> Decode(ByteBuffer buffer)
		{
			object obj = AmqpEncoding.DecodeObject(buffer);
			if (obj == null)
			{
				return null;
			}
			if (obj is T)
			{
				Multiple<T> multiple = new Multiple<T>();
				multiple.Add((T)obj);
				return multiple;
			}
			if (!obj.GetType().IsArray)
			{
				throw new AmqpException(AmqpError.InvalidField);
			}
			return new Multiple<T>((T[])obj);
		}

		public static void Encode(Multiple<T> multiple, ByteBuffer buffer)
		{
			if (multiple == null)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			if (multiple.@value.Count != 1)
			{
				ArrayEncoding.Encode<T>(multiple.@value.ToArray(), buffer);
				return;
			}
			AmqpEncoding.EncodeObject(multiple.@value[0], buffer);
		}

		public static int GetEncodeSize(Multiple<T> multiple)
		{
			if (multiple == null)
			{
				return 1;
			}
			if (multiple.@value.Count != 1)
			{
				return ArrayEncoding.GetEncodeSize<T>(multiple.@value.ToArray());
			}
			return AmqpEncoding.GetObjectEncodeSize(multiple.@value[0]);
		}

		public static IList<T> Intersect(Multiple<T> multiple1, Multiple<T> multiple2)
		{
			List<T> ts = new List<T>();
			if (multiple1 == null || multiple2 == null)
			{
				return ts;
			}
			foreach (T t in multiple1.@value)
			{
				if (!multiple2.@value.Contains(t))
				{
					continue;
				}
				ts.Add(t);
			}
			return ts;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("[");
			bool flag = true;
			foreach (T t in this.@value)
			{
				object obj = t;
				if (!flag)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(obj.ToString());
				flag = false;
			}
			stringBuilder.Append(']');
			return stringBuilder.ToString();
		}
	}
}