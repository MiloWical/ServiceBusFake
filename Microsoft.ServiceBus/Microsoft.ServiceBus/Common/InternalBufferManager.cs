using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.ServiceBus.Common
{
	internal abstract class InternalBufferManager
	{
		protected InternalBufferManager()
		{
		}

		public static byte[] AllocateByteArray(int size)
		{
			return new byte[size];
		}

		public abstract void Clear();

		public static InternalBufferManager Create(long maxBufferPoolSize, int maxBufferSize, bool isTransportBufferPool)
		{
			if (maxBufferPoolSize == (long)0)
			{
				return InternalBufferManager.GCBufferManager.Value;
			}
			if (isTransportBufferPool)
			{
				return new InternalBufferManager.PreallocatedBufferManager(maxBufferPoolSize, maxBufferSize);
			}
			return new InternalBufferManager.PooledBufferManager(maxBufferPoolSize, maxBufferSize);
		}

		public abstract void ReturnBuffer(byte[] buffer);

		public abstract byte[] TakeBuffer(int bufferSize);

		private class GCBufferManager : InternalBufferManager
		{
			private static InternalBufferManager.GCBufferManager @value;

			public static InternalBufferManager.GCBufferManager Value
			{
				get
				{
					return InternalBufferManager.GCBufferManager.@value;
				}
			}

			static GCBufferManager()
			{
				InternalBufferManager.GCBufferManager.@value = new InternalBufferManager.GCBufferManager();
			}

			private GCBufferManager()
			{
			}

			public override void Clear()
			{
			}

			public override void ReturnBuffer(byte[] buffer)
			{
			}

			public override byte[] TakeBuffer(int bufferSize)
			{
				return InternalBufferManager.AllocateByteArray(bufferSize);
			}
		}

		private class PooledBufferManager : InternalBufferManager
		{
			private const int minBufferSize = 128;

			private const int maxMissesBeforeTuning = 8;

			private const int initialBufferCount = 1;

			private readonly object tuningLock;

			private int[] bufferSizes;

			private InternalBufferManager.PooledBufferManager.BufferPool[] bufferPools;

			private long remainingMemory;

			private bool areQuotasBeingTuned;

			private int totalMisses;

			public PooledBufferManager(long maxMemoryToPool, int maxBufferSize)
			{
				this.tuningLock = new object();
				this.remainingMemory = maxMemoryToPool;
				List<InternalBufferManager.PooledBufferManager.BufferPool> bufferPools = new List<InternalBufferManager.PooledBufferManager.BufferPool>();
				int num = 128;
				while (true)
				{
					long num1 = this.remainingMemory / (long)num;
					int num2 = (num1 > (long)2147483647 ? 2147483647 : (int)num1);
					if (num2 > 1)
					{
						num2 = 1;
					}
					bufferPools.Add(InternalBufferManager.PooledBufferManager.BufferPool.CreatePool(num, num2));
					InternalBufferManager.PooledBufferManager pooledBufferManager = this;
					pooledBufferManager.remainingMemory = pooledBufferManager.remainingMemory - (long)num2 * (long)num;
					if (num >= maxBufferSize)
					{
						break;
					}
					long num3 = (long)num * (long)2;
					num = (num3 <= (long)maxBufferSize ? (int)num3 : maxBufferSize);
				}
				this.bufferPools = bufferPools.ToArray();
				this.bufferSizes = new int[(int)this.bufferPools.Length];
				for (int i = 0; i < (int)this.bufferPools.Length; i++)
				{
					this.bufferSizes[i] = this.bufferPools[i].BufferSize;
				}
			}

			private void ChangeQuota(ref InternalBufferManager.PooledBufferManager.BufferPool bufferPool, int delta)
			{
				InternalBufferManager.PooledBufferManager.BufferPool bufferPool1 = bufferPool;
				int limit = bufferPool1.Limit + delta;
				InternalBufferManager.PooledBufferManager.BufferPool bufferPool2 = InternalBufferManager.PooledBufferManager.BufferPool.CreatePool(bufferPool1.BufferSize, limit);
				for (int i = 0; i < limit; i++)
				{
					byte[] numArray = bufferPool1.Take();
					if (numArray == null)
					{
						break;
					}
					bufferPool2.Return(numArray);
					bufferPool2.IncrementCount();
				}
				InternalBufferManager.PooledBufferManager bufferSize = this;
				bufferSize.remainingMemory = bufferSize.remainingMemory - (long)(bufferPool1.BufferSize * delta);
				bufferPool = bufferPool2;
			}

			public override void Clear()
			{
				for (int i = 0; i < (int)this.bufferPools.Length; i++)
				{
					this.bufferPools[i].Clear();
				}
			}

			private void DecreaseQuota(ref InternalBufferManager.PooledBufferManager.BufferPool bufferPool)
			{
				this.ChangeQuota(ref bufferPool, -1);
			}

			private int FindMostExcessivePool()
			{
				long num = (long)0;
				int num1 = -1;
				for (int i = 0; i < (int)this.bufferPools.Length; i++)
				{
					InternalBufferManager.PooledBufferManager.BufferPool bufferPool = this.bufferPools[i];
					if (bufferPool.Peak < bufferPool.Limit)
					{
						long limit = (long)(bufferPool.Limit - bufferPool.Peak) * (long)bufferPool.BufferSize;
						if (limit > num)
						{
							num1 = i;
							num = limit;
						}
					}
				}
				return num1;
			}

			private int FindMostStarvedPool()
			{
				long num = (long)0;
				int num1 = -1;
				for (int i = 0; i < (int)this.bufferPools.Length; i++)
				{
					InternalBufferManager.PooledBufferManager.BufferPool bufferPool = this.bufferPools[i];
					if (bufferPool.Peak == bufferPool.Limit)
					{
						long misses = (long)bufferPool.Misses * (long)bufferPool.BufferSize;
						if (misses > num)
						{
							num1 = i;
							num = misses;
						}
					}
				}
				return num1;
			}

			private InternalBufferManager.PooledBufferManager.BufferPool FindPool(int desiredBufferSize)
			{
				for (int i = 0; i < (int)this.bufferSizes.Length; i++)
				{
					if (desiredBufferSize <= this.bufferSizes[i])
					{
						return this.bufferPools[i];
					}
				}
				return null;
			}

			private void IncreaseQuota(ref InternalBufferManager.PooledBufferManager.BufferPool bufferPool)
			{
				this.ChangeQuota(ref bufferPool, 1);
			}

			public override void ReturnBuffer(byte[] buffer)
			{
				InternalBufferManager.PooledBufferManager.BufferPool bufferPool = this.FindPool((int)buffer.Length);
				if (bufferPool != null)
				{
					if ((int)buffer.Length != bufferPool.BufferSize)
					{
						throw Fx.Exception.Argument("buffer", SRCore.BufferIsNotRightSizeForBufferManager);
					}
					if (bufferPool.Return(buffer))
					{
						bufferPool.IncrementCount();
					}
				}
			}

			public override byte[] TakeBuffer(int bufferSize)
			{
				InternalBufferManager.PooledBufferManager.BufferPool bufferPool = this.FindPool(bufferSize);
				if (bufferPool == null)
				{
					return InternalBufferManager.AllocateByteArray(bufferSize);
				}
				byte[] numArray = bufferPool.Take();
				if (numArray != null)
				{
					bufferPool.DecrementCount();
					return numArray;
				}
				if (bufferPool.Peak == bufferPool.Limit)
				{
					InternalBufferManager.PooledBufferManager.BufferPool misses = bufferPool;
					misses.Misses = misses.Misses + 1;
					InternalBufferManager.PooledBufferManager pooledBufferManager = this;
					int num = pooledBufferManager.totalMisses + 1;
					int num1 = num;
					pooledBufferManager.totalMisses = num;
					if (num1 >= 8)
					{
						this.TuneQuotas();
					}
				}
				return InternalBufferManager.AllocateByteArray(bufferPool.BufferSize);
			}

			private void TuneQuotas()
			{
				if (this.areQuotasBeingTuned)
				{
					return;
				}
				bool flag = false;
				try
				{
					Monitor.TryEnter(this.tuningLock, ref flag);
					if (!flag || this.areQuotasBeingTuned)
					{
						return;
					}
					else
					{
						this.areQuotasBeingTuned = true;
					}
				}
				finally
				{
					if (flag)
					{
						Monitor.Exit(this.tuningLock);
					}
				}
				int num = this.FindMostStarvedPool();
				if (num >= 0)
				{
					InternalBufferManager.PooledBufferManager.BufferPool bufferPool = this.bufferPools[num];
					if (this.remainingMemory < (long)bufferPool.BufferSize)
					{
						int num1 = this.FindMostExcessivePool();
						if (num1 >= 0)
						{
							this.DecreaseQuota(ref this.bufferPools[num1]);
						}
					}
					if (this.remainingMemory >= (long)bufferPool.BufferSize)
					{
						this.IncreaseQuota(ref this.bufferPools[num]);
					}
				}
				for (int i = 0; i < (int)this.bufferPools.Length; i++)
				{
					this.bufferPools[i].Misses = 0;
				}
				this.totalMisses = 0;
				this.areQuotasBeingTuned = false;
			}

			private abstract class BufferPool
			{
				private int bufferSize;

				private int count;

				private int limit;

				private int misses;

				private int peak;

				public int BufferSize
				{
					get
					{
						return this.bufferSize;
					}
				}

				public int Limit
				{
					get
					{
						return this.limit;
					}
				}

				public int Misses
				{
					get
					{
						return this.misses;
					}
					set
					{
						this.misses = value;
					}
				}

				public int Peak
				{
					get
					{
						return this.peak;
					}
				}

				public BufferPool(int bufferSize, int limit)
				{
					this.bufferSize = bufferSize;
					this.limit = limit;
				}

				public void Clear()
				{
					this.OnClear();
					this.count = 0;
				}

				internal static InternalBufferManager.PooledBufferManager.BufferPool CreatePool(int bufferSize, int limit)
				{
					if (bufferSize < 84976)
					{
						return new InternalBufferManager.PooledBufferManager.BufferPool.SynchronizedBufferPool(bufferSize, limit);
					}
					return new InternalBufferManager.PooledBufferManager.BufferPool.LargeBufferPool(bufferSize, limit);
				}

				public void DecrementCount()
				{
					int num = this.count - 1;
					if (num >= 0)
					{
						this.count = num;
					}
				}

				public void IncrementCount()
				{
					int num = this.count + 1;
					if (num <= this.limit)
					{
						this.count = num;
						if (num > this.peak)
						{
							this.peak = num;
						}
					}
				}

				internal abstract void OnClear();

				internal abstract bool Return(byte[] buffer);

				internal abstract byte[] Take();

				private class LargeBufferPool : InternalBufferManager.PooledBufferManager.BufferPool
				{
					private Stack<byte[]> items;

					private object ThisLock
					{
						get
						{
							return this.items;
						}
					}

					internal LargeBufferPool(int bufferSize, int limit) : base(bufferSize, limit)
					{
						this.items = new Stack<byte[]>(limit);
					}

					internal override void OnClear()
					{
						lock (this.ThisLock)
						{
							this.items.Clear();
						}
					}

					internal override bool Return(byte[] buffer)
					{
						bool flag;
						lock (this.ThisLock)
						{
							if (this.items.Count >= base.Limit)
							{
								return false;
							}
							else
							{
								this.items.Push(buffer);
								flag = true;
							}
						}
						return flag;
					}

					internal override byte[] Take()
					{
						byte[] numArray;
						lock (this.ThisLock)
						{
							if (this.items.Count <= 0)
							{
								return null;
							}
							else
							{
								numArray = this.items.Pop();
							}
						}
						return numArray;
					}
				}

				private class SynchronizedBufferPool : InternalBufferManager.PooledBufferManager.BufferPool
				{
					private SynchronizedPool<byte[]> innerPool;

					internal SynchronizedBufferPool(int bufferSize, int limit) : base(bufferSize, limit)
					{
						this.innerPool = new SynchronizedPool<byte[]>(limit);
					}

					internal override void OnClear()
					{
						this.innerPool.Clear();
					}

					internal override bool Return(byte[] buffer)
					{
						return this.innerPool.Return(buffer);
					}

					internal override byte[] Take()
					{
						return this.innerPool.Take();
					}
				}
			}
		}

		private class PreallocatedBufferManager : InternalBufferManager
		{
			private int maxBufferSize;

			private int medBufferSize;

			private int smallBufferSize;

			private byte[][] buffersList;

			private GCHandle[] handles;

			private ConcurrentStack<byte[]> freeSmallBuffers;

			private ConcurrentStack<byte[]> freeMedianBuffers;

			private ConcurrentStack<byte[]> freeLargeBuffers;

			internal PreallocatedBufferManager(long maxMemoryToPool, int maxBufferSize)
			{
				this.maxBufferSize = maxBufferSize;
				this.medBufferSize = maxBufferSize / 4;
				this.smallBufferSize = maxBufferSize / 16;
				long num = maxMemoryToPool / (long)3;
				long num1 = num / (long)maxBufferSize;
				long num2 = num / (long)this.medBufferSize;
				long num3 = num / (long)this.smallBufferSize;
				long num4 = num1 + num2 + num3;
				this.buffersList = new byte[checked((IntPtr)num4)][];
				this.handles = new GCHandle[checked((IntPtr)num4)];
				this.freeSmallBuffers = new ConcurrentStack<byte[]>();
				this.freeMedianBuffers = new ConcurrentStack<byte[]>();
				this.freeLargeBuffers = new ConcurrentStack<byte[]>();
				int num5 = 0;
				int num6 = 0;
				while ((long)num6 < num1)
				{
					this.buffersList[num6] = new byte[maxBufferSize];
					this.handles[num6] = GCHandle.Alloc(this.buffersList[num6], GCHandleType.Pinned);
					this.freeLargeBuffers.Push(this.buffersList[num6]);
					num6++;
					num5++;
				}
				int num7 = num5;
				int num8 = num5;
				while ((long)num8 < num2 + (long)num5)
				{
					this.buffersList[num8] = new byte[this.medBufferSize];
					this.handles[num8] = GCHandle.Alloc(this.buffersList[num8], GCHandleType.Pinned);
					this.freeMedianBuffers.Push(this.buffersList[num8]);
					num8++;
					num7++;
				}
				for (int i = num7; (long)i < num3 + (long)num7; i++)
				{
					this.buffersList[i] = new byte[this.smallBufferSize];
					this.handles[i] = GCHandle.Alloc(this.buffersList[i], GCHandleType.Pinned);
					this.freeSmallBuffers.Push(this.buffersList[i]);
				}
			}

			public override void Clear()
			{
				for (int i = 0; i < (int)this.buffersList.Length; i++)
				{
					this.handles[i].Free();
					this.buffersList[i] = null;
				}
				this.buffersList = null;
				this.freeSmallBuffers.Clear();
				this.freeMedianBuffers.Clear();
				this.freeLargeBuffers.Clear();
			}

			public override void ReturnBuffer(byte[] buffer)
			{
				if ((int)buffer.Length <= this.smallBufferSize)
				{
					this.freeSmallBuffers.Push(buffer);
					return;
				}
				if ((int)buffer.Length <= this.medBufferSize)
				{
					this.freeMedianBuffers.Push(buffer);
					return;
				}
				this.freeLargeBuffers.Push(buffer);
			}

			public override byte[] TakeBuffer(int bufferSize)
			{
				if (bufferSize > this.maxBufferSize)
				{
					return null;
				}
				byte[] numArray = null;
				if (bufferSize <= this.smallBufferSize)
				{
					this.freeSmallBuffers.TryPop(out numArray);
					return numArray;
				}
				if (bufferSize <= this.medBufferSize)
				{
					this.freeMedianBuffers.TryPop(out numArray);
					return numArray;
				}
				this.freeLargeBuffers.TryPop(out numArray);
				return numArray;
			}
		}
	}
}