using System;

namespace System.Threading.Tasks
{
	internal static class CompletedTask<TResult>
	{
		public readonly static Task<TResult> Default;

		static CompletedTask()
		{
			TaskCompletionSource<TResult> taskCompletionSource = new TaskCompletionSource<TResult>();
			taskCompletionSource.TrySetResult(default(TResult));
			CompletedTask<TResult>.Default = taskCompletionSource.Task;
		}
	}
}