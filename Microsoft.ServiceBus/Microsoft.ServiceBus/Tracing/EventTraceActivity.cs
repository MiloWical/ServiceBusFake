using System;
using System.Diagnostics;

namespace Microsoft.ServiceBus.Tracing
{
	internal class EventTraceActivity
	{
		private static EventTraceActivity empty;

		public Guid ActivityId;

		public static EventTraceActivity Empty
		{
			get
			{
				if (EventTraceActivity.empty == null)
				{
					EventTraceActivity.empty = new EventTraceActivity(Guid.Empty);
				}
				return EventTraceActivity.empty;
			}
		}

		public static string Name
		{
			get
			{
				return "E2EActivity";
			}
		}

		public EventTraceActivity() : this(Guid.NewGuid())
		{
		}

		public EventTraceActivity(Guid activityId)
		{
			this.ActivityId = activityId;
		}

		public static EventTraceActivity CreateFromThread()
		{
			Guid activityId = Trace.CorrelationManager.ActivityId;
			if (activityId == Guid.Empty)
			{
				return EventTraceActivity.Empty;
			}
			return new EventTraceActivity(activityId);
		}
	}
}