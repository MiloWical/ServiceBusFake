using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common.Diagnostics;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal class TraceUtility
	{
		private static bool shouldPropagateActivity;

		public static bool PropagateUserActivity
		{
			get
			{
				if (TraceUtility.ShouldPropagateActivity)
				{
					return TraceUtility.PropagateUserActivityCore;
				}
				return false;
			}
		}

		private static bool PropagateUserActivityCore
		{
			get
			{
				return false;
			}
		}

		internal static bool ShouldPropagateActivity
		{
			get
			{
				return TraceUtility.shouldPropagateActivity;
			}
		}

		static TraceUtility()
		{
		}

		public TraceUtility()
		{
		}

		internal static void AddAmbientActivityToMessage(Message message)
		{
		}

		private static string Description(TraceCode traceCode)
		{
			string str = string.Concat("TraceCode", Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.CodeToString(traceCode));
			return Microsoft.ServiceBus.SR.GetString(str, new object[0]);
		}

		internal static Exception ThrowHelperError(Exception exception, Message message)
		{
			return exception;
		}

		internal static Exception ThrowHelperError(Exception exception, Guid activityId, object source)
		{
			return exception;
		}

		internal static Exception ThrowHelperWarning(Exception exception, Message message)
		{
			return exception;
		}

		internal static void TraceEvent(TraceEventType severity, TraceCode traceCode, object source)
		{
			TraceUtility.TraceEvent(severity, traceCode, null, source, null);
		}

		internal static void TraceEvent(TraceEventType severity, TraceCode traceCode, object source, Exception exception)
		{
			TraceUtility.TraceEvent(severity, traceCode, null, source, exception);
		}

		internal static void TraceEvent(TraceEventType severity, TraceCode traceCode, Message message)
		{
			if (message == null)
			{
				TraceUtility.TraceEvent(severity, traceCode, null, (Exception)null);
				return;
			}
			TraceUtility.TraceEvent(severity, traceCode, message, message);
		}

		internal static void TraceEvent(TraceEventType severity, TraceCode traceCode, object source, Message message)
		{
			Guid empty = Guid.Empty;
			if (DiagnosticUtility.ShouldTrace(severity))
			{
				DiagnosticUtility.DiagnosticTrace.TraceEvent(severity, traceCode, TraceUtility.Description(traceCode), new MessageTraceRecord(message), null, empty, message);
			}
		}

		internal static void TraceEvent(TraceEventType severity, TraceCode traceCode, Exception exception, Message message)
		{
			Guid empty = Guid.Empty;
			if (DiagnosticUtility.ShouldTrace(severity))
			{
				DiagnosticUtility.DiagnosticTrace.TraceEvent(severity, traceCode, TraceUtility.Description(traceCode), new MessageTraceRecord(message), exception, empty, null);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void TraceEvent(TraceEventType severity, TraceCode traceCode, TraceRecord extendedData, object source, Exception exception)
		{
			if (DiagnosticUtility.ShouldTrace(severity))
			{
				DiagnosticUtility.DiagnosticTrace.TraceEvent(severity, traceCode, TraceUtility.Description(traceCode), extendedData, exception, source);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void TraceEvent(TraceEventType severity, TraceCode traceCode, TraceRecord extendedData, object source, Exception exception, Message message)
		{
			Guid empty = Guid.Empty;
			if (DiagnosticUtility.ShouldTrace(severity))
			{
				DiagnosticUtility.DiagnosticTrace.TraceEvent(severity, traceCode, TraceUtility.Description(traceCode), extendedData, exception, empty, source);
			}
		}

		internal class TracingAsyncCallbackState
		{
			private object innerState;

			private Guid activityId;

			internal Guid ActivityId
			{
				get
				{
					return this.activityId;
				}
			}

			internal object InnerState
			{
				get
				{
					return this.innerState;
				}
			}

			internal TracingAsyncCallbackState(object innerState)
			{
				this.innerState = innerState;
				this.activityId = Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.ActivityId;
			}
		}
	}
}