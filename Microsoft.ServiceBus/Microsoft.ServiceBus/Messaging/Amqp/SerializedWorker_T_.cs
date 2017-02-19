using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class SerializedWorker<T>
	where T : class
	{
		private readonly IWorkDelegate<T> workDelegate;

		private readonly LinkedList<T> pendingWorkList;

		private SerializedWorker<T>.State state;

		public int Count
		{
			get
			{
				return this.pendingWorkList.Count;
			}
		}

		private object SyncRoot
		{
			get
			{
				return this.pendingWorkList;
			}
		}

		public SerializedWorker(IWorkDelegate<T> workProcessor)
		{
			this.workDelegate = workProcessor;
			this.state = SerializedWorker<T>.State.Idle;
			this.pendingWorkList = new LinkedList<T>();
		}

		public void Abort()
		{
			lock (this.SyncRoot)
			{
				this.pendingWorkList.Clear();
				this.state = SerializedWorker<T>.State.Idle;
			}
		}

		public void ContinueWork()
		{
			T value = default(T);
			lock (this.SyncRoot)
			{
				if (this.state == SerializedWorker<T>.State.BusyWithContinue)
				{
					return;
				}
				else if (this.state == SerializedWorker<T>.State.Busy)
				{
					this.state = SerializedWorker<T>.State.BusyWithContinue;
					return;
				}
				else if (this.pendingWorkList.First != null)
				{
					value = this.pendingWorkList.First.Value;
					this.state = SerializedWorker<T>.State.Busy;
				}
			}
			if (value != null)
			{
				this.DoWorkInternal(value, true);
			}
		}

		public void DoWork(T work)
		{
			lock (this.SyncRoot)
			{
				if (this.state == SerializedWorker<T>.State.Idle)
				{
					this.state = SerializedWorker<T>.State.Busy;
				}
				else
				{
					this.pendingWorkList.AddLast(work);
					return;
				}
			}
			this.DoWorkInternal(work, false);
		}

		private void DoWorkInternal(T work, bool fromList)
		{
			while (work != null)
			{
				if (!this.workDelegate.Invoke(work))
				{
					lock (this.SyncRoot)
					{
						if (this.state != SerializedWorker<T>.State.BusyWithContinue)
						{
							if (!fromList)
							{
								this.pendingWorkList.AddFirst(work);
							}
							this.state = SerializedWorker<T>.State.WaitingForContinue;
							work = default(T);
						}
						else
						{
							this.state = SerializedWorker<T>.State.Busy;
						}
					}
				}
				else
				{
					lock (this.SyncRoot)
					{
						work = default(T);
						if (fromList && this.pendingWorkList.First != null)
						{
							this.pendingWorkList.RemoveFirst();
						}
						if (this.pendingWorkList.First != null)
						{
							work = this.pendingWorkList.First.Value;
							fromList = true;
						}
						if (work != null)
						{
							this.state = SerializedWorker<T>.State.Busy;
						}
						else
						{
							this.state = SerializedWorker<T>.State.Idle;
							break;
						}
					}
				}
			}
		}

		private enum State
		{
			Idle,
			Busy,
			BusyWithContinue,
			WaitingForContinue
		}
	}
}