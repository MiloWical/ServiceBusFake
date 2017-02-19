using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class Pool<T>
	where T : class
	{
		private T[] items;

		private int count;

		public int Count
		{
			get
			{
				return this.count;
			}
		}

		public Pool(int maxCount)
		{
			this.items = new T[maxCount];
		}

		public void Clear()
		{
			for (int i = 0; i < this.count; i++)
			{
				this.items[i] = default(T);
			}
			this.count = 0;
		}

		public bool Return(T item)
		{
			if (this.count >= (int)this.items.Length)
			{
				return false;
			}
			T[] tArray = this.items;
			Pool<T> pool = this;
			int num = pool.count;
			int num1 = num;
			pool.count = num + 1;
			tArray[num1] = item;
			return true;
		}

		public T Take()
		{
			if (this.count <= 0)
			{
				return default(T);
			}
			Pool<T> pool = this;
			int num = pool.count - 1;
			int num1 = num;
			pool.count = num;
			T t = this.items[num1];
			this.items[this.count] = default(T);
			return t;
		}
	}
}