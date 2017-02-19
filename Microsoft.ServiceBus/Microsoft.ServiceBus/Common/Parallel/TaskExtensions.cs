using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Common.Parallel
{
	internal static class TaskExtensions
	{
		public static Task Then(this Task task, Func<Task> next)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}
			if (next == null)
			{
				throw new ArgumentNullException("next");
			}
			TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
			task.ContinueWith((Task param0) => {
				if (task.IsFaulted)
				{
					taskCompletionSource.TrySetException(task.Exception.InnerExceptions);
					return;
				}
				if (task.IsCanceled)
				{
					taskCompletionSource.TrySetCanceled();
					return;
				}
				try
				{
					next().ContinueWith<bool>((Task t) => taskCompletionSource.TrySetFromTask<object>(t), TaskScheduler.Default);
				}
				catch (Exception exception)
				{
					taskCompletionSource.TrySetException(exception);
				}
			}, TaskScheduler.Default);
			return taskCompletionSource.Task;
		}

		public static Task Then<T>(this Task<T> task, Func<T, Task> next)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}
			if (next == null)
			{
				throw new ArgumentNullException("next");
			}
			TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
			task.ContinueWith((Task<T> param0) => {
				if (task.IsFaulted)
				{
					taskCompletionSource.TrySetException(task.Exception.InnerExceptions);
					return;
				}
				if (task.IsCanceled)
				{
					taskCompletionSource.TrySetCanceled();
					return;
				}
				try
				{
					next(task.Result).ContinueWith<bool>((Task t) => taskCompletionSource.TrySetFromTask<object>(t), TaskScheduler.Default);
				}
				catch (Exception exception)
				{
					taskCompletionSource.TrySetException(exception);
				}
			}, TaskScheduler.Default);
			return taskCompletionSource.Task;
		}

		public static Task<TResult> Then<TResult>(this Task task, Func<Task<TResult>> next)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}
			if (next == null)
			{
				throw new ArgumentNullException("next");
			}
			TaskCompletionSource<TResult> taskCompletionSource = new TaskCompletionSource<TResult>();
			task.ContinueWith((Task param0) => {
				if (task.IsFaulted)
				{
					taskCompletionSource.TrySetException(task.Exception.InnerExceptions);
					return;
				}
				if (task.IsCanceled)
				{
					taskCompletionSource.TrySetCanceled();
					return;
				}
				try
				{
					next().ContinueWith<bool>((Task<TResult> t) => taskCompletionSource.TrySetFromTask<TResult>(t), TaskScheduler.Default);
				}
				catch (Exception exception)
				{
					taskCompletionSource.TrySetException(exception);
				}
			}, TaskScheduler.Default);
			return taskCompletionSource.Task;
		}

		public static Task<TNewResult> Then<TResult, TNewResult>(this Task<TResult> task, Func<TResult, Task<TNewResult>> next)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}
			if (next == null)
			{
				throw new ArgumentNullException("next");
			}
			TaskCompletionSource<TNewResult> taskCompletionSource = new TaskCompletionSource<TNewResult>();
			task.ContinueWith((Task<TResult> param0) => {
				if (task.IsFaulted)
				{
					taskCompletionSource.TrySetException(task.Exception.InnerExceptions);
					return;
				}
				if (task.IsCanceled)
				{
					taskCompletionSource.TrySetCanceled();
					return;
				}
				try
				{
					next(task.Result).ContinueWith<bool>((Task<TNewResult> t) => taskCompletionSource.TrySetFromTask<TNewResult>(t), TaskScheduler.Default);
				}
				catch (Exception exception)
				{
					taskCompletionSource.TrySetException(exception);
				}
			}, TaskScheduler.Default);
			return taskCompletionSource.Task;
		}
	}
}