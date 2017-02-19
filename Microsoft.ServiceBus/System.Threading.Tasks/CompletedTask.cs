using System;

namespace System.Threading.Tasks
{
	internal static class CompletedTask
	{
		public readonly static Task Default;

		static CompletedTask()
		{
			CompletedTask.Default = CompletedTask<object>.Default;
		}
	}
}