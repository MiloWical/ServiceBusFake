using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Common
{
	internal sealed class InputQueue<T> : IDisposable
	where T : class
	{
		private static Action<object> completeOutstandingReadersCallback;

		private static Action<object> completeWaitersFalseCallback;

		private static Action<object> completeWaitersTrueCallback;

		private static Action<object> onDispatchCallback;

		private static Action<object> onInvokeDequeuedCallback;

		private InputQueue<T>.QueueState queueState;

		private InputQueue<T>.ItemQueue itemQueue;

		private Queue<InputQueue<T>.IQueueReader> readerQueue;

		private List<InputQueue<T>.IQueueWaiter> waiterList;

		private Func<Action<AsyncCallback, IAsyncResult>> AsyncCallbackGenerator
		{
			get;
			set;
		}

		public Action<T> DisposeItemCallback
		{
			get;
			set;
		}

		public int PendingCount
		{
			get
			{
				int itemCount;
				lock (this.ThisLock)
				{
					itemCount = this.itemQueue.ItemCount;
				}
				return itemCount;
			}
		}

		public int ReadersQueueCount
		{
			get
			{
				int count;
				lock (this.ThisLock)
				{
					count = this.readerQueue.Count;
				}
				return count;
			}
		}

		private object ThisLock
		{
			get
			{
				return this.itemQueue;
			}
		}

		public InputQueue()
		{
			this.itemQueue = new InputQueue<T>.ItemQueue();
			this.readerQueue = new Queue<InputQueue<T>.IQueueReader>();
			this.waiterList = new List<InputQueue<T>.IQueueWaiter>();
			this.queueState = InputQueue<T>.QueueState.Open;
		}

		public InputQueue(Func<Action<AsyncCallback, IAsyncResult>> asyncCallbackGenerator) : this()
		{
			this.AsyncCallbackGenerator = asyncCallbackGenerator;
		}

		public IAsyncResult BeginDequeue(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			InputQueue<T>.Item item = new InputQueue<T>.Item();
			lock (this.ThisLock)
			{
				if (this.queueState == InputQueue<T>.QueueState.Open)
				{
					if (!this.itemQueue.HasAvailableItem)
					{
						InputQueue<T>.AsyncQueueReader asyncQueueReader = new InputQueue<T>.AsyncQueueReader(this, timeout, callback, state);
						this.readerQueue.Enqueue(asyncQueueReader);
						asyncResult = asyncQueueReader;
						return asyncResult;
					}
					else
					{
						item = this.itemQueue.DequeueAvailableItem();
					}
				}
				else if (this.queueState == InputQueue<T>.QueueState.Shutdown)
				{
					if (this.itemQueue.HasAvailableItem)
					{
						item = this.itemQueue.DequeueAvailableItem();
					}
					else if (this.itemQueue.HasAnyItem)
					{
						InputQueue<T>.AsyncQueueReader asyncQueueReader1 = new InputQueue<T>.AsyncQueueReader(this, timeout, callback, state);
						this.readerQueue.Enqueue(asyncQueueReader1);
						asyncResult = asyncQueueReader1;
						return asyncResult;
					}
				}
				InputQueue<T>.InvokeDequeuedCallback(item.DequeuedCallback);
				return new CompletedAsyncResult<T>(item.GetValue(), callback, state);
			}
			return asyncResult;
		}

		public IAsyncResult BeginWaitForItem(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			lock (this.ThisLock)
			{
				if (this.queueState == InputQueue<T>.QueueState.Open)
				{
					if (!this.itemQueue.HasAvailableItem)
					{
						InputQueue<T>.AsyncQueueWaiter asyncQueueWaiter = new InputQueue<T>.AsyncQueueWaiter(timeout, callback, state);
						this.waiterList.Add(asyncQueueWaiter);
						asyncResult = asyncQueueWaiter;
						return asyncResult;
					}
				}
				else if (this.queueState == InputQueue<T>.QueueState.Shutdown && !this.itemQueue.HasAvailableItem && this.itemQueue.HasAnyItem)
				{
					InputQueue<T>.AsyncQueueWaiter asyncQueueWaiter1 = new InputQueue<T>.AsyncQueueWaiter(timeout, callback, state);
					this.waiterList.Add(asyncQueueWaiter1);
					asyncResult = asyncQueueWaiter1;
					return asyncResult;
				}
				return new CompletedAsyncResult<bool>(true, callback, state);
			}
			return asyncResult;
		}

		public void Close()
		{
			this.Dispose();
		}

		private static void CompleteOutstandingReadersCallback(object state)
		{
			InputQueue<T>.IQueueReader[] queueReaderArray = (InputQueue<T>.IQueueReader[])state;
			for (int i = 0; i < (int)queueReaderArray.Length; i++)
			{
				queueReaderArray[i].Set(new InputQueue<T>.Item());
			}
		}

		private static void CompleteWaiters(bool itemAvailable, InputQueue<T>.IQueueWaiter[] waiters)
		{
			for (int i = 0; i < (int)waiters.Length; i++)
			{
				waiters[i].Set(itemAvailable);
			}
		}

		private static void CompleteWaitersFalseCallback(object state)
		{
			InputQueue<T>.CompleteWaiters(false, (InputQueue<T>.IQueueWaiter[])state);
		}

		private static void CompleteWaitersLater(bool itemAvailable, InputQueue<T>.IQueueWaiter[] waiters)
		{
			if (itemAvailable)
			{
				if (InputQueue<T>.completeWaitersTrueCallback == null)
				{
					InputQueue<T>.completeWaitersTrueCallback = new Action<object>(InputQueue<T>.CompleteWaitersTrueCallback);
				}
				ActionItem.Schedule(InputQueue<T>.completeWaitersTrueCallback, waiters);
				return;
			}
			if (InputQueue<T>.completeWaitersFalseCallback == null)
			{
				InputQueue<T>.completeWaitersFalseCallback = new Action<object>(InputQueue<T>.CompleteWaitersFalseCallback);
			}
			ActionItem.Schedule(InputQueue<T>.completeWaitersFalseCallback, waiters);
		}

		private static void CompleteWaitersTrueCallback(object state)
		{
			InputQueue<T>.CompleteWaiters(true, (InputQueue<T>.IQueueWaiter[])state);
		}

		public T Dequeue(TimeSpan timeout)
		{
			T t;
			if (!this.Dequeue(timeout, out t))
			{
				throw Fx.Exception.AsInformation(new TimeoutException(SRCore.TimeoutInputQueueDequeue(timeout)), null);
			}
			return t;
		}

		public bool Dequeue(TimeSpan timeout, out T value)
		{
			bool flag;
			InputQueue<T>.WaitQueueReader waitQueueReader = null;
			InputQueue<T>.Item item = new InputQueue<T>.Item();
			lock (this.ThisLock)
			{
				if (this.queueState == InputQueue<T>.QueueState.Open)
				{
					if (!this.itemQueue.HasAvailableItem)
					{
						waitQueueReader = new InputQueue<T>.WaitQueueReader(this);
						this.readerQueue.Enqueue(waitQueueReader);
					}
					else
					{
						item = this.itemQueue.DequeueAvailableItem();
					}
				}
				else if (this.queueState != InputQueue<T>.QueueState.Shutdown)
				{
					value = default(T);
					flag = true;
					return flag;
				}
				else if (this.itemQueue.HasAvailableItem)
				{
					item = this.itemQueue.DequeueAvailableItem();
				}
				else if (!this.itemQueue.HasAnyItem)
				{
					value = default(T);
					flag = true;
					return flag;
				}
				else
				{
					waitQueueReader = new InputQueue<T>.WaitQueueReader(this);
					this.readerQueue.Enqueue(waitQueueReader);
				}
				if (waitQueueReader != null)
				{
					return waitQueueReader.Wait(timeout, out value);
				}
				InputQueue<T>.InvokeDequeuedCallback(item.DequeuedCallback);
				value = item.GetValue();
				return true;
			}
			return flag;
		}

		public void Dispatch()
		{
			InputQueue<T>.IQueueReader queueReader = null;
			InputQueue<T>.Item item = new InputQueue<T>.Item();
			InputQueue<T>.IQueueReader[] queueReaderArray = null;
			InputQueue<T>.IQueueWaiter[] queueWaiterArray = null;
			bool flag = true;
			lock (this.ThisLock)
			{
				flag = (this.queueState == InputQueue<T>.QueueState.Closed ? false : this.queueState != InputQueue<T>.QueueState.Shutdown);
				this.GetWaiters(out queueWaiterArray);
				if (this.queueState != InputQueue<T>.QueueState.Closed)
				{
					this.itemQueue.MakePendingItemAvailable();
					if (this.readerQueue.Count > 0)
					{
						item = this.itemQueue.DequeueAvailableItem();
						queueReader = this.readerQueue.Dequeue();
						if (this.queueState == InputQueue<T>.QueueState.Shutdown && this.readerQueue.Count > 0 && this.itemQueue.ItemCount == 0)
						{
							queueReaderArray = new InputQueue<T>.IQueueReader[this.readerQueue.Count];
							this.readerQueue.CopyTo(queueReaderArray, 0);
							this.readerQueue.Clear();
							flag = false;
						}
					}
				}
			}
			if (queueReaderArray != null)
			{
				if (InputQueue<T>.completeOutstandingReadersCallback == null)
				{
					InputQueue<T>.completeOutstandingReadersCallback = new Action<object>(InputQueue<T>.CompleteOutstandingReadersCallback);
				}
				ActionItem.Schedule(InputQueue<T>.completeOutstandingReadersCallback, queueReaderArray);
			}
			if (queueWaiterArray != null)
			{
				InputQueue<T>.CompleteWaitersLater(flag, queueWaiterArray);
			}
			if (queueReader != null)
			{
				InputQueue<T>.InvokeDequeuedCallback(item.DequeuedCallback);
				queueReader.Set(item);
			}
		}

		public void Dispose()
		{
			bool flag = false;
			lock (this.ThisLock)
			{
				if (this.queueState != InputQueue<T>.QueueState.Closed)
				{
					this.queueState = InputQueue<T>.QueueState.Closed;
					flag = true;
				}
			}
			if (flag)
			{
				while (this.readerQueue.Count > 0)
				{
					this.readerQueue.Dequeue().Set(new InputQueue<T>.Item());
				}
				while (this.itemQueue.HasAnyItem)
				{
					InputQueue<T>.Item item = this.itemQueue.DequeueAnyItem();
					this.DisposeItem(item);
					InputQueue<T>.InvokeDequeuedCallback(item.DequeuedCallback);
				}
			}
		}

		private void DisposeItem(InputQueue<T>.Item item)
		{
			T value = item.Value;
			if (value != null)
			{
				IDisposable disposable = (object)value as IDisposable;
				if (disposable != null)
				{
					disposable.Dispose();
					return;
				}
				Action<T> disposeItemCallback = this.DisposeItemCallback;
				if (disposeItemCallback != null)
				{
					disposeItemCallback(value);
				}
			}
		}

		public bool EndDequeue(IAsyncResult result, out T value)
		{
			if (!(result is CompletedAsyncResult<T>))
			{
				return InputQueue<T>.AsyncQueueReader.End(result, out value);
			}
			value = CompletedAsyncResult<T>.End(result);
			return true;
		}

		public T EndDequeue(IAsyncResult result)
		{
			T t;
			if (!this.EndDequeue(result, out t))
			{
				throw Fx.Exception.AsInformation(new TimeoutException(), null);
			}
			return t;
		}

		public bool EndWaitForItem(IAsyncResult result)
		{
			if (result is CompletedAsyncResult<bool>)
			{
				return CompletedAsyncResult<bool>.End(result);
			}
			return InputQueue<T>.AsyncQueueWaiter.End(result);
		}

		public void EnqueueAndDispatch(T item)
		{
			this.EnqueueAndDispatch(item, null);
		}

		public void EnqueueAndDispatch(T item, Action dequeuedCallback)
		{
			this.EnqueueAndDispatch(item, dequeuedCallback, true);
		}

		public void EnqueueAndDispatch(Exception exception, Action dequeuedCallback, bool canDispatchOnThisThread)
		{
			this.EnqueueAndDispatch(new InputQueue<T>.Item(exception, dequeuedCallback), canDispatchOnThisThread);
		}

		public void EnqueueAndDispatch(T item, Action dequeuedCallback, bool canDispatchOnThisThread)
		{
			this.EnqueueAndDispatch(new InputQueue<T>.Item(item, dequeuedCallback), canDispatchOnThisThread);
		}

		private void EnqueueAndDispatch(InputQueue<T>.Item item, bool canDispatchOnThisThread)
		{
			bool flag = false;
			InputQueue<T>.IQueueReader queueReader = null;
			bool flag1 = false;
			InputQueue<T>.IQueueWaiter[] queueWaiterArray = null;
			bool flag2 = true;
			lock (this.ThisLock)
			{
				flag2 = (this.queueState == InputQueue<T>.QueueState.Closed ? false : this.queueState != InputQueue<T>.QueueState.Shutdown);
				this.GetWaiters(out queueWaiterArray);
				if (this.queueState != InputQueue<T>.QueueState.Open)
				{
					flag = true;
				}
				else if (canDispatchOnThisThread)
				{
					if (this.readerQueue.Count != 0)
					{
						queueReader = this.readerQueue.Dequeue();
					}
					else
					{
						this.itemQueue.EnqueueAvailableItem(item);
					}
				}
				else if (this.readerQueue.Count != 0)
				{
					this.itemQueue.EnqueuePendingItem(item);
					flag1 = true;
				}
				else
				{
					this.itemQueue.EnqueueAvailableItem(item);
				}
			}
			if (queueWaiterArray != null)
			{
				if (!canDispatchOnThisThread)
				{
					InputQueue<T>.CompleteWaitersLater(flag2, queueWaiterArray);
				}
				else
				{
					InputQueue<T>.CompleteWaiters(flag2, queueWaiterArray);
				}
			}
			if (queueReader != null)
			{
				InputQueue<T>.InvokeDequeuedCallback(item.DequeuedCallback);
				queueReader.Set(item);
			}
			if (!flag1)
			{
				if (flag)
				{
					InputQueue<T>.InvokeDequeuedCallback(item.DequeuedCallback);
					this.DisposeItem(item);
				}
				return;
			}
			if (InputQueue<T>.onDispatchCallback == null)
			{
				InputQueue<T>.onDispatchCallback = new Action<object>(InputQueue<T>.OnDispatchCallback);
			}
			ActionItem.Schedule(InputQueue<T>.onDispatchCallback, this);
		}

		public bool EnqueueWithoutDispatch(T item, Action dequeuedCallback)
		{
			return this.EnqueueWithoutDispatch(new InputQueue<T>.Item(item, dequeuedCallback));
		}

		public bool EnqueueWithoutDispatch(Exception exception, Action dequeuedCallback)
		{
			return this.EnqueueWithoutDispatch(new InputQueue<T>.Item(exception, dequeuedCallback));
		}

		private bool EnqueueWithoutDispatch(InputQueue<T>.Item item)
		{
			bool flag;
			lock (this.ThisLock)
			{
				if (this.queueState == InputQueue<T>.QueueState.Closed || this.queueState == InputQueue<T>.QueueState.Shutdown)
				{
					this.DisposeItem(item);
					InputQueue<T>.InvokeDequeuedCallbackLater(item.DequeuedCallback);
					return false;
				}
				else if (this.readerQueue.Count != 0 || this.waiterList.Count != 0)
				{
					this.itemQueue.EnqueuePendingItem(item);
					flag = true;
				}
				else
				{
					this.itemQueue.EnqueueAvailableItem(item);
					flag = false;
				}
			}
			return flag;
		}

		private void GetWaiters(out InputQueue<T>.IQueueWaiter[] waiters)
		{
			if (this.waiterList.Count <= 0)
			{
				waiters = null;
				return;
			}
			waiters = this.waiterList.ToArray();
			this.waiterList.Clear();
		}

		private static void InvokeDequeuedCallback(Action dequeuedCallback)
		{
			if (dequeuedCallback != null)
			{
				dequeuedCallback();
			}
		}

		private static void InvokeDequeuedCallbackLater(Action dequeuedCallback)
		{
			if (dequeuedCallback != null)
			{
				if (InputQueue<T>.onInvokeDequeuedCallback == null)
				{
					InputQueue<T>.onInvokeDequeuedCallback = new Action<object>(InputQueue<T>.OnInvokeDequeuedCallback);
				}
				ActionItem.Schedule(InputQueue<T>.onInvokeDequeuedCallback, dequeuedCallback);
			}
		}

		private static void OnDispatchCallback(object state)
		{
			((InputQueue<T>)state).Dispatch();
		}

		private static void OnInvokeDequeuedCallback(object state)
		{
			((Action)state)();
		}

		private bool RemoveReader(InputQueue<T>.IQueueReader reader)
		{
			bool flag;
			lock (this.ThisLock)
			{
				if (this.queueState == InputQueue<T>.QueueState.Open || this.queueState == InputQueue<T>.QueueState.Shutdown)
				{
					bool flag1 = false;
					for (int i = this.readerQueue.Count; i > 0; i--)
					{
						InputQueue<T>.IQueueReader queueReader = this.readerQueue.Dequeue();
						if (!object.ReferenceEquals(queueReader, reader))
						{
							this.readerQueue.Enqueue(queueReader);
						}
						else
						{
							flag1 = true;
						}
					}
					flag = flag1;
				}
				else
				{
					return false;
				}
			}
			return flag;
		}

		public void Shutdown()
		{
			this.Shutdown(null);
		}

		public void Shutdown(Func<Exception> pendingExceptionGenerator)
		{
			Exception exception;
			InputQueue<T>.IQueueReader[] queueReaderArray = null;
			lock (this.ThisLock)
			{
				if (this.queueState == InputQueue<T>.QueueState.Shutdown)
				{
					return;
				}
				else if (this.queueState != InputQueue<T>.QueueState.Closed)
				{
					this.queueState = InputQueue<T>.QueueState.Shutdown;
					if (this.readerQueue.Count > 0 && this.itemQueue.ItemCount == 0)
					{
						queueReaderArray = new InputQueue<T>.IQueueReader[this.readerQueue.Count];
						this.readerQueue.CopyTo(queueReaderArray, 0);
						this.readerQueue.Clear();
					}
				}
				else
				{
					return;
				}
			}
			if (queueReaderArray != null)
			{
				for (int i = 0; i < (int)queueReaderArray.Length; i++)
				{
					if (pendingExceptionGenerator != null)
					{
						exception = pendingExceptionGenerator();
					}
					else
					{
						exception = null;
					}
					queueReaderArray[i].Set(new InputQueue<T>.Item(exception, null));
				}
			}
		}

		public bool WaitForItem(TimeSpan timeout)
		{
			bool flag;
			InputQueue<T>.WaitQueueWaiter waitQueueWaiter = null;
			bool flag1 = false;
			lock (this.ThisLock)
			{
				if (this.queueState == InputQueue<T>.QueueState.Open)
				{
					if (!this.itemQueue.HasAvailableItem)
					{
						waitQueueWaiter = new InputQueue<T>.WaitQueueWaiter();
						this.waiterList.Add(waitQueueWaiter);
					}
					else
					{
						flag1 = true;
					}
				}
				else if (this.queueState != InputQueue<T>.QueueState.Shutdown)
				{
					flag = true;
					return flag;
				}
				else if (this.itemQueue.HasAvailableItem)
				{
					flag1 = true;
				}
				else if (!this.itemQueue.HasAnyItem)
				{
					flag = true;
					return flag;
				}
				else
				{
					waitQueueWaiter = new InputQueue<T>.WaitQueueWaiter();
					this.waiterList.Add(waitQueueWaiter);
				}
				if (waitQueueWaiter == null)
				{
					return flag1;
				}
				return waitQueueWaiter.Wait(timeout);
			}
			return flag;
		}

		private class AsyncQueueReader : AsyncResult, InputQueue<T>.IQueueReader
		{
			private static Action<object> timerCallback;

			private bool expired;

			private InputQueue<T> inputQueue;

			private T item;

			private IOThreadTimer timer;

			static AsyncQueueReader()
			{
				InputQueue<T>.AsyncQueueReader.timerCallback = new Action<object>(InputQueue<T>.AsyncQueueReader.TimerCallback);
			}

			public AsyncQueueReader(InputQueue<T> inputQueue, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				if (inputQueue.AsyncCallbackGenerator != null)
				{
					base.VirtualCallback = new Action<AsyncCallback, IAsyncResult>(inputQueue.AsyncCallbackGenerator().Invoke);
				}
				this.inputQueue = inputQueue;
				if (timeout != TimeSpan.MaxValue)
				{
					this.timer = new IOThreadTimer(InputQueue<T>.AsyncQueueReader.timerCallback, this, false);
					this.timer.Set(timeout);
				}
			}

			public static bool End(IAsyncResult result, out T value)
			{
				InputQueue<T>.AsyncQueueReader asyncQueueReader = AsyncResult.End<InputQueue<T>.AsyncQueueReader>(result);
				if (asyncQueueReader.expired)
				{
					value = default(T);
					return false;
				}
				value = asyncQueueReader.item;
				return true;
			}

			public void Set(InputQueue<T>.Item inputItem)
			{
				this.item = inputItem.Value;
				if (this.timer != null)
				{
					this.timer.Cancel();
				}
				base.Complete(false, inputItem.Exception);
			}

			private static void TimerCallback(object state)
			{
				InputQueue<T>.AsyncQueueReader asyncQueueReader = (InputQueue<T>.AsyncQueueReader)state;
				if (asyncQueueReader.inputQueue.RemoveReader(asyncQueueReader))
				{
					asyncQueueReader.expired = true;
					asyncQueueReader.Complete(false);
				}
			}
		}

		private class AsyncQueueWaiter : AsyncResult, InputQueue<T>.IQueueWaiter
		{
			private static Action<object> timerCallback;

			private bool itemAvailable;

			private IOThreadTimer timer;

			static AsyncQueueWaiter()
			{
				InputQueue<T>.AsyncQueueWaiter.timerCallback = new Action<object>(InputQueue<T>.AsyncQueueWaiter.TimerCallback);
			}

			public AsyncQueueWaiter(TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				if (timeout != TimeSpan.MaxValue)
				{
					this.timer = new IOThreadTimer(InputQueue<T>.AsyncQueueWaiter.timerCallback, this, false);
					this.timer.Set(timeout);
				}
			}

			public static new bool End(IAsyncResult result)
			{
				return AsyncResult.End<InputQueue<T>.AsyncQueueWaiter>(result).itemAvailable;
			}

			public void Set(bool currentItemAvailable)
			{
				bool flag;
				lock (base.ThisLock)
				{
					flag = (this.timer == null ? true : this.timer.Cancel());
					this.itemAvailable = currentItemAvailable;
				}
				if (flag)
				{
					base.Complete(false);
				}
			}

			private static void TimerCallback(object state)
			{
				((InputQueue<T>.AsyncQueueWaiter)state).Complete(false);
			}
		}

		private interface IQueueReader
		{
			void Set(InputQueue<T>.Item item);
		}

		private interface IQueueWaiter
		{
			void Set(bool itemAvailable);
		}

		private struct Item
		{
			private Action dequeuedCallback;

			private Exception exception;

			private T @value;

			public Action DequeuedCallback
			{
				get
				{
					return this.dequeuedCallback;
				}
			}

			public Exception Exception
			{
				get
				{
					return this.exception;
				}
			}

			public T Value
			{
				get
				{
					return this.@value;
				}
			}

			public Item(T value, Action dequeuedCallback) : this(value, null, dequeuedCallback)
			{
			}

			public Item(Exception exception, Action dequeuedCallback) : this(default(T), exception, dequeuedCallback)
			{
			}

			private Item(T value, Exception exception, Action dequeuedCallback)
			{
				this.@value = value;
				this.exception = exception;
				this.dequeuedCallback = dequeuedCallback;
			}

			public T GetValue()
			{
				if (this.exception != null)
				{
					throw Fx.Exception.AsInformation(this.exception, null);
				}
				return this.@value;
			}
		}

		private class ItemQueue
		{
			private int head;

			private InputQueue<T>.Item[] items;

			private int pendingCount;

			private int totalCount;

			public bool HasAnyItem
			{
				get
				{
					return this.totalCount > 0;
				}
			}

			public bool HasAvailableItem
			{
				get
				{
					return this.totalCount > this.pendingCount;
				}
			}

			public int ItemCount
			{
				get
				{
					return this.totalCount;
				}
			}

			public ItemQueue()
			{
				this.items = new InputQueue<T>.Item[1];
			}

			public InputQueue<T>.Item DequeueAnyItem()
			{
				if (this.pendingCount == this.totalCount)
				{
					InputQueue<T>.ItemQueue itemQueue = this;
					itemQueue.pendingCount = itemQueue.pendingCount - 1;
				}
				return this.DequeueItemCore();
			}

			public InputQueue<T>.Item DequeueAvailableItem()
			{
				Fx.AssertAndThrow(this.totalCount != this.pendingCount, "ItemQueue does not contain any available items");
				return this.DequeueItemCore();
			}

			private InputQueue<T>.Item DequeueItemCore()
			{
				Fx.AssertAndThrow(this.totalCount != 0, "ItemQueue does not contain any items");
				InputQueue<T>.Item item = this.items[this.head];
				this.items[this.head] = new InputQueue<T>.Item();
				InputQueue<T>.ItemQueue itemQueue = this;
				itemQueue.totalCount = itemQueue.totalCount - 1;
				this.head = (this.head + 1) % (int)this.items.Length;
				return item;
			}

			public void EnqueueAvailableItem(InputQueue<T>.Item item)
			{
				this.EnqueueItemCore(item);
			}

			private void EnqueueItemCore(InputQueue<T>.Item item)
			{
				if (this.totalCount == (int)this.items.Length)
				{
					InputQueue<T>.Item[] itemArray = new InputQueue<T>.Item[(int)this.items.Length * 2];
					for (int i = 0; i < this.totalCount; i++)
					{
						itemArray[i] = this.items[(this.head + i) % (int)this.items.Length];
					}
					this.head = 0;
					this.items = itemArray;
				}
				int length = (this.head + this.totalCount) % (int)this.items.Length;
				this.items[length] = item;
				InputQueue<T>.ItemQueue itemQueue = this;
				itemQueue.totalCount = itemQueue.totalCount + 1;
			}

			public void EnqueuePendingItem(InputQueue<T>.Item item)
			{
				this.EnqueueItemCore(item);
				InputQueue<T>.ItemQueue itemQueue = this;
				itemQueue.pendingCount = itemQueue.pendingCount + 1;
			}

			public void MakePendingItemAvailable()
			{
				Fx.AssertAndThrow(this.pendingCount != 0, "ItemQueue does not contain any pending items");
				InputQueue<T>.ItemQueue itemQueue = this;
				itemQueue.pendingCount = itemQueue.pendingCount - 1;
			}
		}

		private enum QueueState
		{
			Open,
			Shutdown,
			Closed
		}

		private class WaitQueueReader : InputQueue<T>.IQueueReader, IDisposable
		{
			private Exception exception;

			private InputQueue<T> inputQueue;

			private T item;

			private ManualResetEvent waitEvent;

			public WaitQueueReader(InputQueue<T> inputQueue)
			{
				this.inputQueue = inputQueue;
				this.waitEvent = new ManualResetEvent(false);
			}

			public void Dispose()
			{
				this.waitEvent.Dispose();
				GC.SuppressFinalize(this);
			}

			public void Set(InputQueue<T>.Item newItem)
			{
				lock (this)
				{
					this.exception = newItem.Exception;
					this.item = newItem.Value;
					this.waitEvent.Set();
				}
			}

			public bool Wait(TimeSpan timeout, out T value)
			{
				bool flag;
				bool flag1 = false;
				try
				{
					if (!Microsoft.ServiceBus.Common.TimeoutHelper.WaitOne(this.waitEvent, timeout))
					{
						if (!this.inputQueue.RemoveReader(this))
						{
							this.waitEvent.WaitOne();
						}
						else
						{
							value = default(T);
							flag1 = true;
							flag = false;
							return flag;
						}
					}
					flag1 = true;
					if (this.exception != null)
					{
						throw Fx.Exception.AsInformation(this.exception, null);
					}
					value = this.item;
					return true;
				}
				finally
				{
					if (flag1)
					{
						this.waitEvent.Close();
					}
				}
				return flag;
			}
		}

		private class WaitQueueWaiter : InputQueue<T>.IQueueWaiter, IDisposable
		{
			private bool itemAvailable;

			private ManualResetEvent waitEvent;

			public WaitQueueWaiter()
			{
				this.waitEvent = new ManualResetEvent(false);
			}

			public void Dispose()
			{
				this.waitEvent.Close();
				GC.SuppressFinalize(this);
			}

			public void Set(bool isItemAvailable)
			{
				lock (this)
				{
					this.itemAvailable = isItemAvailable;
					this.waitEvent.Set();
				}
			}

			public bool Wait(TimeSpan timeout)
			{
				if (!Microsoft.ServiceBus.Common.TimeoutHelper.WaitOne(this.waitEvent, timeout))
				{
					return false;
				}
				return this.itemAvailable;
			}
		}
	}
}