using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Common.Parallel
{
	internal static class TaskHelpers
	{
		private const string TimeoutExceptionExtensionData = "TaskExtension.WithTimeout";

		public static Task CreateTask(Func<AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
		{
			Task task;
			try
			{
				task = Task.Factory.FromAsync(begin, end, null);
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
			return task;
		}

		public static Task<T> CreateTask<T>(Func<AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, T> end)
		{
			Task<T> task;
			try
			{
				task = Task<T>.Factory.FromAsync(begin, end, null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
				taskCompletionSource.SetException(exception);
				task = taskCompletionSource.Task;
			}
			return task;
		}

		public static Task<TResult> CreateTask<TState, TResult>(Func<TState, AsyncCallback, object, IAsyncResult> begin, Func<TState, IAsyncResult, TResult> end, TState state)
		{
			Task<TResult> task;
			try
			{
				task = Task<TResult>.Factory.FromAsync((AsyncCallback c, object s) => begin((TState)s, c, s), (IAsyncResult a) => end((TState)a.AsyncState, a), state);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				TaskCompletionSource<TResult> taskCompletionSource = new TaskCompletionSource<TResult>((object)state);
				taskCompletionSource.SetException(exception);
				task = taskCompletionSource.Task;
			}
			return task;
		}

		public static void EndAsyncResult(IAsyncResult asyncResult)
		{
			Task task = asyncResult as Task;
			if (task == null)
			{
				throw Fx.Exception.AsError(new ArgumentException(Resources.InvalidAsyncResult), null);
			}
			try
			{
				task.Wait();
			}
			catch (AggregateException aggregateException)
			{
				ExceptionDispatcher.Throw(aggregateException.GetBaseException());
			}
		}

		public static TResult EndAsyncResult<TResult>(IAsyncResult asyncResult)
		{
			TResult result;
			Task<TResult> task = asyncResult as Task<TResult>;
			if (task == null)
			{
				throw Fx.Exception.AsError(new ArgumentException(Resources.InvalidAsyncResult), null);
			}
			try
			{
				result = task.Result;
			}
			catch (AggregateException aggregateException1)
			{
				AggregateException aggregateException = aggregateException1;
				ExceptionDispatcher.Throw(aggregateException.GetBaseException());
				throw aggregateException.GetBaseException();
			}
			return result;
		}

		public static Task ExecuteAndGetCompletedTask(Action action)
		{
			TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
			try
			{
				action();
				taskCompletionSource.SetResult(null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				taskCompletionSource.SetException(exception);
			}
			return taskCompletionSource.Task;
		}

		public static Task<TResult> ExecuteAndGetCompletedTask<TResult>(Func<TResult> function)
		{
			TaskCompletionSource<TResult> taskCompletionSource = new TaskCompletionSource<TResult>();
			try
			{
				taskCompletionSource.SetResult(function());
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				taskCompletionSource.SetException(exception);
			}
			return taskCompletionSource.Task;
		}

		public static void Fork(this Task thisTask)
		{
			thisTask.ContinueWith((Task t) => Fx.Exception.TraceHandled(t.Exception, "TaskHelpers.Fork", null), TaskContinuationOptions.OnlyOnFaulted);
		}

		public static Task<T> GetCompletedTask<T>(T val = null)
		where T : class
		{
			if (val == null)
			{
				return CompletedTask<T>.Default;
			}
			TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
			taskCompletionSource.SetResult(val);
			return taskCompletionSource.Task;
		}

		public static IAsyncResult ToAsyncResult(this Task task, AsyncCallback callback, object state)
		{
			if (task.AsyncState != state)
			{
				TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>(state);
				task.ContinueWith((Task _) => {
					if (task.IsFaulted)
					{
						taskCompletionSource.TrySetException(task.Exception.InnerExceptions);
					}
					else if (!task.IsCanceled)
					{
						taskCompletionSource.TrySetResult(null);
					}
					else
					{
						taskCompletionSource.TrySetCanceled();
					}
					if (callback != null)
					{
						callback(taskCompletionSource.Task);
					}
				}, TaskContinuationOptions.ExecuteSynchronously);
				return taskCompletionSource.Task;
			}
			if (callback != null)
			{
				task.ContinueWith((Task t) => callback(task), TaskContinuationOptions.ExecuteSynchronously);
			}
			return task;
		}

		public static IAsyncResult ToAsyncResult<TResult>(this Task<TResult> task, AsyncCallback callback, object state)
		{
			if (task.AsyncState != state)
			{
				TaskCompletionSource<TResult> taskCompletionSource = new TaskCompletionSource<TResult>(state);
				task.ContinueWith((Task<TResult> _) => {
					if (task.IsFaulted)
					{
						taskCompletionSource.TrySetException(task.Exception.InnerExceptions);
					}
					else if (!task.IsCanceled)
					{
						taskCompletionSource.TrySetResult(task.Result);
					}
					else
					{
						taskCompletionSource.TrySetCanceled();
					}
					if (callback != null)
					{
						callback(taskCompletionSource.Task);
					}
				}, TaskContinuationOptions.ExecuteSynchronously);
				return taskCompletionSource.Task;
			}
			if (callback != null)
			{
				task.ContinueWith((Task<TResult> t) => callback(task), TaskContinuationOptions.ExecuteSynchronously);
			}
			return task;
		}

		public static Task<TResult> WithTimeout<TResult>(this Task<TResult> actualTask, TimeSpan timeout, string taskFriendlyName = "Unnamed", Action<TResult, Exception> onCompletionAfterTimeout = null)
		{
			TaskCompletionSource<TResult> taskCompletionSource = new TaskCompletionSource<TResult>(actualTask.AsyncState);
			Timer timer = new Timer((object state) => {
				TimeoutException timeoutException = new TimeoutException(string.Format(CultureInfo.InvariantCulture, "The {0} task timed out", new object[] { this.taskFriendlyName }));
				timeoutException.Data.Add("TaskExtension.WithTimeout", null);
				((TaskCompletionSource<TResult>)state).TrySetException(timeoutException);
			}, taskCompletionSource, timeout, TimeSpan.FromMilliseconds(-1));
			actualTask.ContinueWith((Task<TResult> t) => {
				bool flag = false;
				timer.Dispose();
				flag = (!t.IsFaulted ? (!t.IsCanceled ? !taskCompletionSource.TrySetResult(t.Result) : !taskCompletionSource.TrySetCanceled()) : !taskCompletionSource.TrySetException(t.Exception.InnerException));
				if (flag && onCompletionAfterTimeout != null)
				{
					if (t.IsFaulted)
					{
						onCompletionAfterTimeout(default(TResult), t.Exception.InnerException);
						return;
					}
					onCompletionAfterTimeout(t.Result, null);
				}
			}, TaskContinuationOptions.ExecuteSynchronously);
			return taskCompletionSource.Task;
		}
	}
}