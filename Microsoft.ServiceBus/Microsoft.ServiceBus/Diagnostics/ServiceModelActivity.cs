using System;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal class ServiceModelActivity : IDisposable
	{
		internal Microsoft.ServiceBus.Diagnostics.ActivityType ActivityType
		{
			get
			{
				return Microsoft.ServiceBus.Diagnostics.ActivityType.Unknown;
			}
		}

		internal static ServiceModelActivity Current
		{
			get
			{
				return null;
			}
		}

		public ServiceModelActivity()
		{
		}

		internal static Activity BoundOperation(ServiceModelActivity activity)
		{
			return null;
		}

		internal static Activity BoundOperation(ServiceModelActivity activity, bool addTransfer)
		{
			return null;
		}

		internal static ServiceModelActivity CreateActivity()
		{
			return null;
		}

		internal static ServiceModelActivity CreateActivity(bool autoStop)
		{
			return null;
		}

		internal static ServiceModelActivity CreateActivity(bool autoStop, string activityName, Microsoft.ServiceBus.Diagnostics.ActivityType activityType)
		{
			return null;
		}

		internal static ServiceModelActivity CreateBoundedActivity()
		{
			return null;
		}

		internal static ServiceModelActivity CreateBoundedActivity(bool suspendCurrent)
		{
			return null;
		}

		internal static ServiceModelActivity CreateBoundedActivity(Guid activityId)
		{
			return null;
		}

		internal static ServiceModelActivity CreateBoundedActivityWithTransferInOnly(Guid activityId)
		{
			return null;
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		internal void Resume()
		{
		}

		internal void Resume(string activityName)
		{
		}

		internal static void Start(ServiceModelActivity activity, string activityName, Microsoft.ServiceBus.Diagnostics.ActivityType activityType)
		{
		}

		internal void Stop()
		{
		}

		internal static void Stop(ServiceModelActivity activity)
		{
		}

		internal void Suspend()
		{
		}
	}
}