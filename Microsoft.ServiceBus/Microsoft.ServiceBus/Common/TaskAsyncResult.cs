using System;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Common
{
	internal sealed class TaskAsyncResult : AsyncResult<TaskAsyncResult>
	{
		private readonly Task task;

		public TaskAsyncResult(Task task, AsyncCallback callback, object state) : base(callback, state)
		{
			this.task = task;
			if (this.task.IsCompleted)
			{
				this.CompleteWithTaskResult(true);
				return;
			}
			this.task.ContinueWith(new Action<Task>(this.OnTaskContinued));
		}

		private void CompleteWithTaskResult(bool completedSynchronously)
		{
			Exception exception;
			if (this.task.Exception == null)
			{
				exception = null;
			}
			else
			{
				exception = this.task.Exception;
				AggregateException aggregateException = exception as AggregateException;
				if (aggregateException != null)
				{
					exception = aggregateException.GetBaseException();
				}
			}
			base.Complete(completedSynchronously, exception);
		}

		private void OnTaskContinued(Task unused)
		{
			this.CompleteWithTaskResult(false);
		}
	}
}