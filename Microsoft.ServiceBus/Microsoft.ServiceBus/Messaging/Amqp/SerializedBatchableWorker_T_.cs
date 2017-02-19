using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class SerializedBatchableWorker<T>
	{
		private readonly IWorkDelegate<IList<T>> workHandler;

		private readonly List<T> pendingList;

		private bool working;

		private bool closeIssued;

		private object SyncRoot
		{
			get
			{
				return this.pendingList;
			}
		}

		public SerializedBatchableWorker(IWorkDelegate<IList<T>> workHandler)
		{
			this.workHandler = workHandler;
			this.pendingList = new List<T>();
		}

		private bool AddPendingWork(T work, IList<T> workList)
		{
			bool flag;
			bool flag1 = false;
			lock (this.SyncRoot)
			{
				if (!this.closeIssued)
				{
					if (workList == null)
					{
						this.pendingList.Add(work);
					}
					else
					{
						this.pendingList.AddRange(workList);
					}
					if (!this.working)
					{
						flag1 = true;
						this.working = true;
					}
					return flag1;
				}
				else
				{
					flag = false;
				}
			}
			return flag;
		}

		public void ContinueWork()
		{
			this.DoPendingWork();
		}

		private void DoPendingWork()
		{
			T[] array;
			do
			{
				array = null;
				lock (this.SyncRoot)
				{
					if (this.pendingList.Count != 0)
					{
						array = this.pendingList.ToArray();
						this.pendingList.Clear();
					}
					else
					{
						this.working = false;
						if (!this.closeIssued)
						{
							break;
						}
					}
				}
			}
			while (this.workHandler.Invoke(array) && array != null);
		}

		public void DoWork(T work)
		{
			if (this.AddPendingWork(work, null))
			{
				this.DoPendingWork();
			}
		}

		public void DoWork(IList<T> workList)
		{
			if (this.AddPendingWork(default(T), workList))
			{
				this.DoPendingWork();
			}
		}

		public void IssueClose()
		{
			bool flag;
			lock (this.SyncRoot)
			{
				this.closeIssued = true;
				flag = !this.working;
			}
			if (flag)
			{
				this.workHandler.Invoke(null);
			}
		}
	}
}