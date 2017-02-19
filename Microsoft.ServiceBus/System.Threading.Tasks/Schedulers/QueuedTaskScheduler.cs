using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Schedulers
{
	[DebuggerDisplay("Id={Id}, Queues={DebugQueueCount}, ScheduledTasks = {DebugTaskCount}")]
	[DebuggerTypeProxy(typeof(QueuedTaskScheduler.QueuedTaskSchedulerDebugView))]
	internal sealed class QueuedTaskScheduler : TaskScheduler, IDisposable
	{
		private readonly SortedList<int, QueuedTaskScheduler.QueueGroup> _queueGroups = new SortedList<int, QueuedTaskScheduler.QueueGroup>();

		private readonly CancellationTokenSource _disposeCancellation = new CancellationTokenSource();

		private readonly int _concurrencyLevel;

		private static ThreadLocal<bool> _taskProcessingThread;

		private readonly TaskScheduler _targetScheduler;

		private readonly Queue<Task> _nonthreadsafeTaskQueue;

		private int _delegatesQueuedOrRunning;

		private readonly Thread[] _threads;

		private readonly BlockingCollection<Task> _blockingTaskQueue;

		public override int MaximumConcurrencyLevel
		{
			get
			{
				return this._concurrencyLevel;
			}
		}

		static QueuedTaskScheduler()
		{
			QueuedTaskScheduler._taskProcessingThread = new ThreadLocal<bool>();
		}

		public QueuedTaskScheduler() : this(TaskScheduler.Default, 0)
		{
		}

		public QueuedTaskScheduler(TaskScheduler targetScheduler) : this(targetScheduler, 0)
		{
		}

		public QueuedTaskScheduler(TaskScheduler targetScheduler, int maxConcurrencyLevel)
		{
			if (targetScheduler == null)
			{
				throw new ArgumentNullException("underlyingScheduler");
			}
			if (maxConcurrencyLevel < 0)
			{
				throw new ArgumentOutOfRangeException("concurrencyLevel");
			}
			this._targetScheduler = targetScheduler;
			this._nonthreadsafeTaskQueue = new Queue<Task>();
			this._concurrencyLevel = (maxConcurrencyLevel != 0 ? maxConcurrencyLevel : Environment.ProcessorCount);
			if (targetScheduler.MaximumConcurrencyLevel > 0 && targetScheduler.MaximumConcurrencyLevel < this._concurrencyLevel)
			{
				this._concurrencyLevel = targetScheduler.MaximumConcurrencyLevel;
			}
		}

		public QueuedTaskScheduler(int threadCount) : this(threadCount, string.Empty, false, ThreadPriority.Normal, ApartmentState.MTA, 0, null, null)
		{
		}

		public QueuedTaskScheduler(int threadCount, string threadName = "", bool useForegroundThreads = false, ThreadPriority threadPriority = 2, ApartmentState threadApartmentState = 1, int threadMaxStackSize = 0, Action threadInit = null, Action threadFinally = null)
		{
			QueuedTaskScheduler queuedTaskScheduler = this;
			if (threadCount < 0)
			{
				throw new ArgumentOutOfRangeException("concurrencyLevel");
			}
			if (threadCount != 0)
			{
				this._concurrencyLevel = threadCount;
			}
			else
			{
				this._concurrencyLevel = Environment.ProcessorCount;
			}
			this._blockingTaskQueue = new BlockingCollection<Task>();
			this._threads = new Thread[threadCount];
			for (int i = 0; i < threadCount; i++)
			{
				Thread[] threadArray = this._threads;
				int num = i;
				Thread thread = new Thread(() => queuedTaskScheduler.ThreadBasedDispatchLoop(threadInit, threadFinally), threadMaxStackSize)
				{
					Priority = threadPriority,
					IsBackground = !useForegroundThreads
				};
				threadArray[num] = thread;
				if (threadName != null)
				{
					Thread thread1 = this._threads[i];
					object[] objArray = new object[] { threadName, " (", i, ")" };
					thread1.Name = string.Concat(objArray);
				}
				this._threads[i].SetApartmentState(threadApartmentState);
			}
			Thread[] threadArray1 = this._threads;
			for (int j = 0; j < (int)threadArray1.Length; j++)
			{
				threadArray1[j].Start();
			}
		}

		public TaskScheduler ActivateNewQueue()
		{
			return this.ActivateNewQueue(0);
		}

		public TaskScheduler ActivateNewQueue(int priority)
		{
			QueuedTaskScheduler.QueueGroup queueGroup;
			QueuedTaskScheduler.QueuedTaskSchedulerQueue queuedTaskSchedulerQueue = new QueuedTaskScheduler.QueuedTaskSchedulerQueue(priority, this);
			lock (this._queueGroups)
			{
				if (!this._queueGroups.TryGetValue(priority, out queueGroup))
				{
					queueGroup = new QueuedTaskScheduler.QueueGroup();
					this._queueGroups.Add(priority, queueGroup);
				}
				queueGroup.Add(queuedTaskSchedulerQueue);
			}
			return queuedTaskSchedulerQueue;
		}

		public void Dispose()
		{
			this._disposeCancellation.Cancel();
		}

		private void FindNextTask_NeedsLock(out Task targetTask, out QueuedTaskScheduler.QueuedTaskSchedulerQueue queueForTargetTask)
		{
			targetTask = null;
			queueForTargetTask = null;
			foreach (KeyValuePair<int, QueuedTaskScheduler.QueueGroup> _queueGroup in this._queueGroups)
			{
				QueuedTaskScheduler.QueueGroup value = _queueGroup.Value;
				foreach (int num in value.CreateSearchOrder())
				{
					queueForTargetTask = value[num];
					Queue<Task> tasks = queueForTargetTask._workItems;
					if (tasks.Count <= 0)
					{
						continue;
					}
					targetTask = tasks.Dequeue();
					if (queueForTargetTask._disposed && tasks.Count == 0)
					{
						this.RemoveQueue_NeedsLock(queueForTargetTask);
					}
					value.NextQueueIndex = (value.NextQueueIndex + 1) % _queueGroup.Value.Count;
					return;
				}
			}
		}

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			if (this._targetScheduler == null)
			{
				return (
					from t in this._blockingTaskQueue
					where t != null
					select t).ToList<Task>();
			}
			return (
				from t in this._nonthreadsafeTaskQueue
				where t != null
				select t).ToList<Task>();
		}

		private void NotifyNewWorkItem()
		{
			this.QueueTask(null);
		}

		private void ProcessPrioritizedAndBatchedTasks()
		{
			Task task;
			bool flag = true;
			while (!this._disposeCancellation.IsCancellationRequested && flag)
			{
				try
				{
					QueuedTaskScheduler._taskProcessingThread.Value = true;
					while (!this._disposeCancellation.IsCancellationRequested)
					{
						lock (this._nonthreadsafeTaskQueue)
						{
							if (this._nonthreadsafeTaskQueue.Count != 0)
							{
								task = this._nonthreadsafeTaskQueue.Dequeue();
							}
							else
							{
								break;
							}
						}
						QueuedTaskScheduler.QueuedTaskSchedulerQueue queuedTaskSchedulerQueue = null;
						if (task == null)
						{
							lock (this._queueGroups)
							{
								this.FindNextTask_NeedsLock(out task, out queuedTaskSchedulerQueue);
							}
						}
						if (task == null)
						{
							continue;
						}
						if (queuedTaskSchedulerQueue == null)
						{
							base.TryExecuteTask(task);
						}
						else
						{
							queuedTaskSchedulerQueue.ExecuteTask(task);
						}
					}
				}
				finally
				{
					lock (this._nonthreadsafeTaskQueue)
					{
						if (this._nonthreadsafeTaskQueue.Count == 0)
						{
							QueuedTaskScheduler queuedTaskScheduler = this;
							queuedTaskScheduler._delegatesQueuedOrRunning = queuedTaskScheduler._delegatesQueuedOrRunning - 1;
							flag = false;
							QueuedTaskScheduler._taskProcessingThread.Value = false;
						}
					}
				}
			}
		}

		protected override void QueueTask(Task task)
		{
			if (this._disposeCancellation.IsCancellationRequested)
			{
				throw new ObjectDisposedException(base.GetType().Name);
			}
			if (this._targetScheduler == null)
			{
				this._blockingTaskQueue.Add(task);
				return;
			}
			bool flag = false;
			lock (this._nonthreadsafeTaskQueue)
			{
				this._nonthreadsafeTaskQueue.Enqueue(task);
				if (this._delegatesQueuedOrRunning < this._concurrencyLevel)
				{
					QueuedTaskScheduler queuedTaskScheduler = this;
					queuedTaskScheduler._delegatesQueuedOrRunning = queuedTaskScheduler._delegatesQueuedOrRunning + 1;
					flag = true;
				}
			}
			if (flag)
			{
				Task.Factory.StartNew(new Action(this.ProcessPrioritizedAndBatchedTasks), CancellationToken.None, TaskCreationOptions.None, this._targetScheduler);
			}
		}

		private void RemoveQueue_NeedsLock(QueuedTaskScheduler.QueuedTaskSchedulerQueue queue)
		{
			QueuedTaskScheduler.QueueGroup item = this._queueGroups[queue._priority];
			int num = item.IndexOf(queue);
			if (item.NextQueueIndex >= num)
			{
				QueuedTaskScheduler.QueueGroup nextQueueIndex = item;
				nextQueueIndex.NextQueueIndex = nextQueueIndex.NextQueueIndex - 1;
			}
			item.RemoveAt(num);
		}

		private void ThreadBasedDispatchLoop(Action threadInit, Action threadFinally)
		{
			Task task;
			QueuedTaskScheduler.QueuedTaskSchedulerQueue queuedTaskSchedulerQueue;
			QueuedTaskScheduler._taskProcessingThread.Value = true;
			if (threadInit != null)
			{
				threadInit();
			}
			try
			{
				try
				{
					while (true)
					{
						try
						{
							foreach (Task consumingEnumerable in this._blockingTaskQueue.GetConsumingEnumerable(this._disposeCancellation.Token))
							{
								if (consumingEnumerable == null)
								{
									lock (this._queueGroups)
									{
										this.FindNextTask_NeedsLock(out task, out queuedTaskSchedulerQueue);
									}
									if (task == null)
									{
										continue;
									}
									queuedTaskSchedulerQueue.ExecuteTask(task);
								}
								else
								{
									base.TryExecuteTask(consumingEnumerable);
								}
							}
						}
						catch (ThreadAbortException threadAbortException)
						{
							if (!Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload())
							{
								Thread.ResetAbort();
							}
						}
					}
				}
				catch (OperationCanceledException operationCanceledException)
				{
				}
			}
			finally
			{
				if (threadFinally != null)
				{
					threadFinally();
				}
				QueuedTaskScheduler._taskProcessingThread.Value = false;
			}
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			if (!QueuedTaskScheduler._taskProcessingThread.Value)
			{
				return false;
			}
			return base.TryExecuteTask(task);
		}

		private class QueuedTaskSchedulerDebugView
		{
			private QueuedTaskScheduler _scheduler;

			public IEnumerable<TaskScheduler> Queues
			{
				get
				{
					List<TaskScheduler> taskSchedulers = new List<TaskScheduler>();
					foreach (KeyValuePair<int, QueuedTaskScheduler.QueueGroup> _queueGroup in this._scheduler._queueGroups)
					{
						taskSchedulers.AddRange(_queueGroup.Value);
					}
					return taskSchedulers;
				}
			}

			public IEnumerable<Task> ScheduledTasks
			{
				get
				{
					IEnumerable<Task> tasks;
					if (this._scheduler._targetScheduler != null)
					{
						tasks = this._scheduler._nonthreadsafeTaskQueue;
					}
					else
					{
						tasks = this._scheduler._blockingTaskQueue;
					}
					return (
						from t in tasks
						where t != null
						select t).ToList<Task>();
				}
			}

			public QueuedTaskSchedulerDebugView(QueuedTaskScheduler scheduler)
			{
				if (scheduler == null)
				{
					throw new ArgumentNullException("scheduler");
				}
				this._scheduler = scheduler;
			}
		}

		[DebuggerDisplay("QueuePriority = {_priority}, WaitingTasks = {WaitingTasks}")]
		[DebuggerTypeProxy(typeof(QueuedTaskScheduler.QueuedTaskSchedulerQueue.QueuedTaskSchedulerQueueDebugView))]
		private sealed class QueuedTaskSchedulerQueue : TaskScheduler, IDisposable
		{
			private readonly QueuedTaskScheduler _pool;

			internal readonly Queue<Task> _workItems;

			internal bool _disposed;

			internal int _priority;

			public override int MaximumConcurrencyLevel
			{
				get
				{
					return this._pool.MaximumConcurrencyLevel;
				}
			}

			internal QueuedTaskSchedulerQueue(int priority, QueuedTaskScheduler pool)
			{
				this._priority = priority;
				this._pool = pool;
				this._workItems = new Queue<Task>();
			}

			public void Dispose()
			{
				if (!this._disposed)
				{
					lock (this._pool._queueGroups)
					{
						if (this._workItems.Count == 0)
						{
							this._pool.RemoveQueue_NeedsLock(this);
						}
					}
					this._disposed = true;
				}
			}

			internal void ExecuteTask(Task task)
			{
				base.TryExecuteTask(task);
			}

			protected override IEnumerable<Task> GetScheduledTasks()
			{
				return this._workItems.ToList<Task>();
			}

			protected override void QueueTask(Task task)
			{
				if (this._disposed)
				{
					throw new ObjectDisposedException(base.GetType().Name);
				}
				lock (this._pool._queueGroups)
				{
					this._workItems.Enqueue(task);
				}
				this._pool.NotifyNewWorkItem();
			}

			protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
			{
				if (!QueuedTaskScheduler._taskProcessingThread.Value)
				{
					return false;
				}
				return base.TryExecuteTask(task);
			}

			private sealed class QueuedTaskSchedulerQueueDebugView
			{
				private readonly QueuedTaskScheduler.QueuedTaskSchedulerQueue _queue;

				public QueuedTaskScheduler AssociatedScheduler
				{
					get
					{
						return this._queue._pool;
					}
				}

				public int Id
				{
					get
					{
						return this._queue.Id;
					}
				}

				public int Priority
				{
					get
					{
						return this._queue._priority;
					}
				}

				public IEnumerable<Task> ScheduledTasks
				{
					get
					{
						return this._queue.GetScheduledTasks();
					}
				}

				public QueuedTaskSchedulerQueueDebugView(QueuedTaskScheduler.QueuedTaskSchedulerQueue queue)
				{
					if (queue == null)
					{
						throw new ArgumentNullException("queue");
					}
					this._queue = queue;
				}
			}
		}

		private class QueueGroup : List<QueuedTaskScheduler.QueuedTaskSchedulerQueue>
		{
			public int NextQueueIndex;

			public QueueGroup()
			{
			}

			public IEnumerable<int> CreateSearchOrder()
			{
				for (int num = this.NextQueueIndex; num < base.Count; num++)
				{
					yield return num;
				}
				for (int j = 0; j < this.NextQueueIndex; j++)
				{
					yield return j;
				}
			}
		}
	}
}