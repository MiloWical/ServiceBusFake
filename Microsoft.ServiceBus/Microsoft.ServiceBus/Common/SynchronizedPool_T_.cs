using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Threading;

namespace Microsoft.ServiceBus.Common
{
	internal class SynchronizedPool<T>
	where T : class
	{
		private const int maxPendingEntries = 128;

		private const int maxPromotionFailures = 64;

		private const int maxReturnsBeforePromotion = 64;

		private const int maxThreadItemsPerProcessor = 16;

		private SynchronizedPool<T>.Entry[] entries;

		private SynchronizedPool<T>.GlobalPool globalPool;

		private int maxCount;

		private SynchronizedPool<T>.PendingEntry[] pending;

		private int promotionFailures;

		private object ThisLock
		{
			get
			{
				return this;
			}
		}

		public SynchronizedPool(int maxCount)
		{
			int num = maxCount;
			int processorCount = 16 + SynchronizedPool<T>.SynchronizedPoolHelper.ProcessorCount;
			if (num > processorCount)
			{
				num = processorCount;
			}
			this.maxCount = maxCount;
			this.entries = new SynchronizedPool<T>.Entry[num];
			this.pending = new SynchronizedPool<T>.PendingEntry[4];
			this.globalPool = new SynchronizedPool<T>.GlobalPool(maxCount);
		}

		public void Clear()
		{
			SynchronizedPool<T>.Entry[] entryArray = this.entries;
			for (int i = 0; i < (int)entryArray.Length; i++)
			{
				entryArray[i].@value = default(T);
			}
			this.globalPool.Clear();
		}

		private void HandlePromotionFailure(int thisThreadID)
		{
			int num = this.promotionFailures + 1;
			if (num < 64)
			{
				this.promotionFailures = num;
				return;
			}
			lock (this.ThisLock)
			{
				this.entries = new SynchronizedPool<T>.Entry[(int)this.entries.Length];
				this.globalPool.MaxCount = this.maxCount;
			}
			this.PromoteThread(thisThreadID);
		}

		private bool PromoteThread(int thisThreadID)
		{
			bool flag;
			lock (this.ThisLock)
			{
				int num = 0;
				while (num < (int)this.entries.Length)
				{
					int num1 = this.entries[num].threadID;
					if (num1 == thisThreadID)
					{
						flag = true;
						return flag;
					}
					else if (num1 != 0)
					{
						num++;
					}
					else
					{
						this.globalPool.DecrementMaxCount();
						this.entries[num].threadID = thisThreadID;
						flag = true;
						return flag;
					}
				}
				return false;
			}
			return flag;
		}

		private void RecordReturnToGlobalPool(int thisThreadID)
		{
			SynchronizedPool<T>.PendingEntry[] pendingEntryArray = this.pending;
			int num = 0;
			while (num < (int)pendingEntryArray.Length)
			{
				int num1 = pendingEntryArray[num].threadID;
				if (num1 != thisThreadID)
				{
					if (num1 == 0)
					{
						return;
					}
					num++;
				}
				else
				{
					int num2 = pendingEntryArray[num].returnCount + 1;
					if (num2 < 64)
					{
						pendingEntryArray[num].returnCount = num2;
						return;
					}
					pendingEntryArray[num].returnCount = 0;
					if (this.PromoteThread(thisThreadID))
					{
						break;
					}
					this.HandlePromotionFailure(thisThreadID);
					return;
				}
			}
		}

		private void RecordTakeFromGlobalPool(int thisThreadID)
		{
			SynchronizedPool<T>.PendingEntry[] pendingEntryArray = this.pending;
			int num = 0;
			while (true)
			{
				if (num < (int)pendingEntryArray.Length)
				{
					int num1 = pendingEntryArray[num].threadID;
					if (num1 == thisThreadID)
					{
						return;
					}
					if (num1 == 0)
					{
						lock (pendingEntryArray)
						{
							if (pendingEntryArray[num].threadID == 0)
							{
								pendingEntryArray[num].threadID = thisThreadID;
								break;
							}
						}
					}
					num++;
				}
				else
				{
					if ((int)pendingEntryArray.Length >= 128)
					{
						this.pending = new SynchronizedPool<T>.PendingEntry[(int)pendingEntryArray.Length];
						return;
					}
					SynchronizedPool<T>.PendingEntry[] pendingEntryArray1 = new SynchronizedPool<T>.PendingEntry[(int)pendingEntryArray.Length * 2];
					Array.Copy(pendingEntryArray, pendingEntryArray1, (int)pendingEntryArray.Length);
					this.pending = pendingEntryArray1;
					break;
				}
			}
		}

		public bool Return(T value)
		{
			int managedThreadId = Thread.CurrentThread.ManagedThreadId;
			if (managedThreadId == 0)
			{
				return false;
			}
			if (this.ReturnToPerThreadPool(managedThreadId, value))
			{
				return true;
			}
			return this.ReturnToGlobalPool(managedThreadId, value);
		}

		private bool ReturnToGlobalPool(int thisThreadID, T value)
		{
			this.RecordReturnToGlobalPool(thisThreadID);
			return this.globalPool.Return(value);
		}

		private bool ReturnToPerThreadPool(int thisThreadID, T value)
		{
			SynchronizedPool<T>.Entry[] entryArray = this.entries;
			for (int i = 0; i < (int)entryArray.Length; i++)
			{
				int num = entryArray[i].threadID;
				if (num == thisThreadID)
				{
					if (entryArray[i].@value != null)
					{
						return false;
					}
					entryArray[i].@value = value;
					return true;
				}
				if (num == 0)
				{
					break;
				}
			}
			return false;
		}

		public T Take()
		{
			int managedThreadId = Thread.CurrentThread.ManagedThreadId;
			if (managedThreadId == 0)
			{
				return default(T);
			}
			T t = this.TakeFromPerThreadPool(managedThreadId);
			if (t != null)
			{
				return t;
			}
			return this.TakeFromGlobalPool(managedThreadId);
		}

		private T TakeFromGlobalPool(int thisThreadID)
		{
			this.RecordTakeFromGlobalPool(thisThreadID);
			return this.globalPool.Take();
		}

		private T TakeFromPerThreadPool(int thisThreadID)
		{
			SynchronizedPool<T>.Entry[] entryArray = this.entries;
			for (int i = 0; i < (int)entryArray.Length; i++)
			{
				int num = entryArray[i].threadID;
				if (num == thisThreadID)
				{
					T t = entryArray[i].@value;
					if (t == null)
					{
						return default(T);
					}
					entryArray[i].@value = default(T);
					return t;
				}
				if (num == 0)
				{
					break;
				}
			}
			return default(T);
		}

		private struct Entry
		{
			public int threadID;

			public T @value;
		}

		private class GlobalPool
		{
			private Stack<T> items;

			private int maxCount;

			public int MaxCount
			{
				get
				{
					return this.maxCount;
				}
				set
				{
					lock (this.ThisLock)
					{
						while (this.items.Count > value)
						{
							this.items.Pop();
						}
						this.maxCount = value;
					}
				}
			}

			private object ThisLock
			{
				get
				{
					return this;
				}
			}

			public GlobalPool(int maxCount)
			{
				this.items = new Stack<T>();
				this.maxCount = maxCount;
			}

			public void Clear()
			{
				lock (this.ThisLock)
				{
					this.items.Clear();
				}
			}

			public void DecrementMaxCount()
			{
				lock (this.ThisLock)
				{
					if (this.items.Count == this.maxCount)
					{
						this.items.Pop();
					}
					SynchronizedPool<T>.GlobalPool globalPool = this;
					globalPool.maxCount = globalPool.maxCount - 1;
				}
			}

			public bool Return(T value)
			{
				bool flag;
				if (this.items.Count < this.MaxCount)
				{
					lock (this.ThisLock)
					{
						if (this.items.Count >= this.MaxCount)
						{
							return false;
						}
						else
						{
							this.items.Push(value);
							flag = true;
						}
					}
					return flag;
				}
				return false;
			}

			public T Take()
			{
				T t;
				T t1;
				if (this.items.Count > 0)
				{
					lock (this.ThisLock)
					{
						if (this.items.Count <= 0)
						{
							t1 = default(T);
							return t1;
						}
						else
						{
							t = this.items.Pop();
						}
					}
					return t;
				}
				t1 = default(T);
				return t1;
			}
		}

		private struct PendingEntry
		{
			public int returnCount;

			public int threadID;
		}

		private static class SynchronizedPoolHelper
		{
			public readonly static int ProcessorCount;

			static SynchronizedPoolHelper()
			{
				SynchronizedPool<T>.SynchronizedPoolHelper.ProcessorCount = SynchronizedPool<T>.SynchronizedPoolHelper.GetProcessorCount();
			}

			[EnvironmentPermission(SecurityAction.Assert, Read="NUMBER_OF_PROCESSORS")]
			private static int GetProcessorCount()
			{
				return Environment.ProcessorCount;
			}
		}
	}
}