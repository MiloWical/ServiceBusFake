using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Async
{
	[DebuggerDisplay("CurrentCount={CurrentCount}, MaximumCount={MaximumCount}, WaitingCount={WaitingCount}")]
	internal sealed class AsyncSemaphore : IDisposable
	{
		private int _currentCount;

		private int _maxCount;

		private Queue<TaskCompletionSource<object>> _waitingTasks;

		public int CurrentCount
		{
			get
			{
				return this._currentCount;
			}
		}

		public int MaximumCount
		{
			get
			{
				return this._maxCount;
			}
		}

		public int WaitingCount
		{
			get
			{
				int count;
				lock (this._waitingTasks)
				{
					count = this._waitingTasks.Count;
				}
				return count;
			}
		}

		public AsyncSemaphore() : this(0)
		{
		}

		public AsyncSemaphore(int initialCount) : this(initialCount, 2147483647)
		{
		}

		public AsyncSemaphore(int initialCount, int maxCount)
		{
			if (maxCount <= 0)
			{
				throw new ArgumentOutOfRangeException("maxCount");
			}
			if (initialCount > maxCount || initialCount < 0)
			{
				throw new ArgumentOutOfRangeException("initialCount");
			}
			this._currentCount = initialCount;
			this._maxCount = maxCount;
			this._waitingTasks = new Queue<TaskCompletionSource<object>>();
		}

		public void CancelAllExisting()
		{
			List<TaskCompletionSource<object>> taskCompletionSources = new List<TaskCompletionSource<object>>();
			lock (this._waitingTasks)
			{
				while (this._waitingTasks.Count > 0)
				{
					taskCompletionSources.Add(this._waitingTasks.Dequeue());
				}
			}
			taskCompletionSources.ForEach((TaskCompletionSource<object> task) => task.TrySetCanceled());
		}

		public void Dispose()
		{
			if (this._maxCount > 0)
			{
				this._maxCount = 0;
				lock (this._waitingTasks)
				{
					while (this._waitingTasks.Count > 0)
					{
						this._waitingTasks.Dequeue().SetCanceled();
					}
				}
			}
		}

		public Task Queue(Action action)
		{
			return this.WaitAsync().ContinueWith((Task _) => {
				try
				{
					action();
				}
				finally
				{
					this.Release();
				}
			});
		}

		public Task<TResult> Queue<TResult>(Func<TResult> function)
		{
			return this.WaitAsync().ContinueWith<TResult>((Task _) => {
				TResult tResult;
				try
				{
					tResult = function();
				}
				finally
				{
					this.Release();
				}
				return tResult;
			});
		}

		public void Release()
		{
			this.ThrowIfDisposed();
			TaskCompletionSource<object> taskCompletionSource = null;
			lock (this._waitingTasks)
			{
				if (this._currentCount == this._maxCount)
				{
					throw new SemaphoreFullException();
				}
				if (this._waitingTasks.Count <= 0)
				{
					AsyncSemaphore asyncSemaphore = this;
					asyncSemaphore._currentCount = asyncSemaphore._currentCount + 1;
				}
				else
				{
					taskCompletionSource = this._waitingTasks.Dequeue();
				}
			}
			if (taskCompletionSource != null)
			{
				taskCompletionSource.SetResult(null);
			}
		}

		private void ThrowIfDisposed()
		{
			if (this._maxCount <= 0)
			{
				throw new ObjectDisposedException(this.GetType().Name);
			}
		}

		public Task WaitAsync()
		{
			Task task;
			this.ThrowIfDisposed();
			lock (this._waitingTasks)
			{
				if (this._currentCount <= 0)
				{
					TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
					this._waitingTasks.Enqueue(taskCompletionSource);
					task = taskCompletionSource.Task;
				}
				else
				{
					AsyncSemaphore asyncSemaphore = this;
					asyncSemaphore._currentCount = asyncSemaphore._currentCount - 1;
					task = CompletedTask.Default;
				}
			}
			return task;
		}
	}
}