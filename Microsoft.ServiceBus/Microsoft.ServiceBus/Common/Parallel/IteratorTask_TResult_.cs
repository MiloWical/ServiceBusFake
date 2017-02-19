using Microsoft.ServiceBus.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Common.Parallel
{
	internal abstract class IteratorTask<TResult> : TaskCompletionSource<TResult>
	{
		private IEnumerator<IteratorTask<TResult>.TaskStep> steps;

		private IteratorTask<TResult>.TaskStep currentStep;

		protected Action<IteratorTask<TResult>, Exception> OnSetException;

		protected System.Threading.Tasks.Task LastTask
		{
			get
			{
				return this.currentStep.Task;
			}
		}

		protected IteratorTask()
		{
		}

		protected IteratorTask<TResult>.TaskStep CallTask(System.Threading.Tasks.Task task, IteratorTask<TResult>.ExceptionPolicy policy = 0)
		{
			return new IteratorTask<TResult>.TaskStep(task, policy);
		}

		protected IteratorTask<TResult>.TaskStep CallTask<TState>(Func<TState, System.Threading.Tasks.Task> taskFunc, TState state, IteratorTask<TResult>.ExceptionPolicy policy = 0)
		{
			System.Threading.Tasks.Task task;
			if (policy != IteratorTask<TResult>.ExceptionPolicy.Continue)
			{
				task = taskFunc(state);
			}
			else
			{
				try
				{
					task = taskFunc(state);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
					taskCompletionSource.SetException(exception);
					task = taskCompletionSource.Task;
				}
			}
			return new IteratorTask<TResult>.TaskStep(task, policy);
		}

		private void DoSetException(Exception exception)
		{
			if (this.OnSetException != null)
			{
				try
				{
					this.OnSetException(this, exception);
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					exception = exception1;
				}
			}
			base.TrySetException(exception);
		}

		private void EnumerateSteps()
		{
			if (base.Task.IsCompleted)
			{
				return;
			}
			if (!this.steps.MoveNext())
			{
				this.StepsComplete();
				return;
			}
			this.currentStep = this.steps.Current;
			this.currentStep.Task.ContinueWith((System.Threading.Tasks.Task t) => {
				if (t.IsCanceled)
				{
					if (this.currentStep.Policy == IteratorTask<TResult>.ExceptionPolicy.Transfer)
					{
						base.TrySetCanceled();
						this.StepsComplete();
					}
					return;
				}
				if (t.IsFaulted && this.currentStep.Policy == IteratorTask<TResult>.ExceptionPolicy.Transfer)
				{
					this.DoSetException(t.Exception.GetBaseException());
					this.StepsComplete();
					return;
				}
				try
				{
					this.EnumerateSteps();
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					this.DoSetException(exception);
				}
			}, TaskContinuationOptions.ExecuteSynchronously);
		}

		protected abstract IEnumerator<IteratorTask<TResult>.TaskStep> GetTasks();

		protected TTaskResult LastTaskResult<TTaskResult>()
		{
			return ((Task<TTaskResult>)this.LastTask).Result;
		}

		public Task<TResult> Start()
		{
			try
			{
				this.steps = this.GetTasks();
				this.EnumerateSteps();
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.DoSetException(exception);
			}
			return base.Task;
		}

		private void StepsComplete()
		{
			this.steps.Dispose();
			if (!base.Task.IsCompleted)
			{
				base.TrySetResult(default(TResult));
			}
		}

		protected enum ExceptionPolicy
		{
			Transfer,
			Continue
		}

		[DebuggerStepThrough]
		protected struct TaskStep
		{
			private readonly IteratorTask<TResult>.ExceptionPolicy policy;

			private readonly System.Threading.Tasks.Task task;

			public IteratorTask<TResult>.ExceptionPolicy Policy
			{
				get
				{
					return (IteratorTask<TResult>.ExceptionPolicy)this.policy;
				}
			}

			public System.Threading.Tasks.Task Task
			{
				get
				{
					return this.task;
				}
			}

			public TaskStep(System.Threading.Tasks.Task task, IteratorTask<TResult>.ExceptionPolicy policy)
			{
				this.task = task;
				this.policy = policy;
			}
		}
	}
}