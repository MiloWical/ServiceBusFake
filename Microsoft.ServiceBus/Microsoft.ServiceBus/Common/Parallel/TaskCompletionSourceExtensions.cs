using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Common.Parallel
{
	internal static class TaskCompletionSourceExtensions
	{
		public static bool TrySetFromTask<TResult>(this TaskCompletionSource<TResult> resultSetter, Task task)
		{
			switch (task.Status)
			{
				case TaskStatus.RanToCompletion:
				{
					return resultSetter.TrySetResult((task is Task<TResult> ? ((Task<TResult>)task).Result : default(TResult)));
				}
				case TaskStatus.Canceled:
				{
					return resultSetter.TrySetCanceled();
				}
				case TaskStatus.Faulted:
				{
					return resultSetter.TrySetException(task.Exception.InnerExceptions);
				}
			}
			throw new InvalidOperationException("The task was not completed.");
		}
	}
}