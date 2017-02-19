using System;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal class Activity : IDisposable
	{
		public Activity()
		{
		}

		internal static Activity CreateActivity(Guid activityId)
		{
			return new Activity();
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}
	}
}