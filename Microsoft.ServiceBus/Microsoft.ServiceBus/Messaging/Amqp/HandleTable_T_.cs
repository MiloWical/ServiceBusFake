using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class HandleTable<T>
	where T : class
	{
		private const int fastSegmentCount = 32;

		private uint maxHandle;

		private T[] fastSegment;

		private Dictionary<uint, T> slowSegment;

		public IEnumerable<T> Values
		{
			get
			{
				List<T> ts = new List<T>();
				T[] tArray = this.fastSegment;
				for (int i = 0; i < (int)tArray.Length; i++)
				{
					T t = tArray[i];
					if (t != null)
					{
						ts.Add(t);
					}
				}
				if (this.slowSegment != null)
				{
					ts.AddRange(this.slowSegment.Values);
				}
				return ts;
			}
		}

		public HandleTable(uint maxHandle)
		{
			this.maxHandle = maxHandle;
			this.fastSegment = new T[32];
		}

		public uint Add(T value)
		{
			for (int i = 0; i < (int)this.fastSegment.Length; i++)
			{
				if (this.fastSegment[i] == null)
				{
					this.fastSegment[i] = value;
					return (uint)i;
				}
			}
			if (this.slowSegment == null)
			{
				this.slowSegment = new Dictionary<uint, T>();
			}
			uint length = (uint)this.fastSegment.Length;
			while (length < this.maxHandle && this.slowSegment.ContainsKey(length))
			{
				length++;
			}
			if (length == this.maxHandle)
			{
				throw new AmqpException(AmqpError.ResourceLimitExceeded, SRAmqp.AmqpHandleExceeded(this.maxHandle));
			}
			this.slowSegment.Add(length, value);
			return length;
		}

		public void Add(uint handle, T value)
		{
			T t;
			if ((ulong)handle < (long)((int)this.fastSegment.Length))
			{
				if (this.fastSegment[handle] != null)
				{
					throw new AmqpException(AmqpError.HandleInUse, SRAmqp.AmqpHandleInUse(handle, this.fastSegment[handle]));
				}
				this.fastSegment[handle] = value;
				return;
			}
			if (this.slowSegment == null)
			{
				this.slowSegment = new Dictionary<uint, T>();
			}
			else if (this.slowSegment.TryGetValue(handle, out t))
			{
				throw new AmqpException(AmqpError.HandleInUse, SRAmqp.AmqpHandleInUse(handle, t));
			}
			this.slowSegment.Add(handle, value);
		}

		public void Clear()
		{
			for (int i = 0; i < (int)this.fastSegment.Length; i++)
			{
				this.fastSegment[i] = default(T);
			}
			this.slowSegment = null;
		}

		public void Remove(uint handle)
		{
			if ((ulong)handle < (long)((int)this.fastSegment.Length))
			{
				this.fastSegment[handle] = default(T);
				return;
			}
			if (this.slowSegment != null)
			{
				this.slowSegment.Remove(handle);
			}
		}

		public bool TryGetObject(uint handle, out T value)
		{
			value = default(T);
			if ((ulong)handle < (long)((int)this.fastSegment.Length))
			{
				value = this.fastSegment[handle];
			}
			else if (this.slowSegment != null)
			{
				this.slowSegment.TryGetValue(handle, out value);
			}
			return value != null;
		}
	}
}