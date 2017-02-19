using Microsoft.ServiceBus.Common.Diagnostics;
using System;
using System.Diagnostics.Eventing;
using System.Globalization;
using System.Resources;
using System.Security;

namespace Microsoft.ServiceBus.Common
{
	internal class TraceCore
	{
		private static System.Resources.ResourceManager resourceManager;

		private static CultureInfo resourceCulture;

		[SecurityCritical]
		private static EventDescriptor[] eventDescriptors;

		internal static CultureInfo Culture
		{
			get
			{
				return TraceCore.resourceCulture;
			}
			set
			{
				TraceCore.resourceCulture = value;
			}
		}

		private static System.Resources.ResourceManager ResourceManager
		{
			get
			{
				if (object.ReferenceEquals(TraceCore.resourceManager, null))
				{
					TraceCore.resourceManager = new System.Resources.ResourceManager("Microsoft.ServiceBus.Messaging.TraceCore", typeof(TraceCore).Assembly);
				}
				return TraceCore.resourceManager;
			}
		}

		private TraceCore()
		{
		}

		private static void EnsureEventDescriptors()
		{
			if (object.ReferenceEquals(TraceCore.eventDescriptors, null))
			{
				EventDescriptor[] eventDescriptor = new EventDescriptor[] { new EventDescriptor(30200, 0, 19, 4, 0, 0, 1152921504606846976L), new EventDescriptor(30201, 0, 19, 4, 0, 0, 1152921504606846976L), new EventDescriptor(30202, 0, 19, 2, 0, 0, 1152921504606846976L), new EventDescriptor(30203, 0, 19, 2, 0, 0, 1152921504606846976L), new EventDescriptor(30204, 0, 19, 2, 0, 0, 1152921504606846976L), new EventDescriptor(30205, 0, 19, 3, 0, 0, 1152921504606846976L), new EventDescriptor(30206, 0, 19, 1, 0, 0, 1152921504606846976L), new EventDescriptor(30207, 0, 19, 2, 0, 0, 1152921504606846976L), new EventDescriptor(30208, 0, 19, 4, 0, 0, 1152921504606846976L), new EventDescriptor(30209, 0, 19, 5, 0, 0, 1152921504606846976L), new EventDescriptor(30210, 0, 19, 3, 0, 0, 1152921504606846976L), new EventDescriptor(30211, 0, 19, 3, 0, 0, 1152921504606846976L) };
				TraceCore.eventDescriptors = eventDescriptor;
			}
		}

		internal static void HandledException(DiagnosticTrace trace, Exception exception)
		{
			TracePayload serializedPayload = DiagnosticTrace.GetSerializedPayload(null, null, exception);
			if (TraceCore.IsEtwEventEnabled(trace, 1))
			{
				TraceCore.WriteEtwEvent(trace, 1, serializedPayload.SerializedException, serializedPayload.AppDomainFriendlyName);
			}
			if (trace.ShouldTraceToTraceSource(TraceEventLevel.Informational))
			{
				string str = string.Format(TraceCore.Culture, TraceCore.ResourceManager.GetString("HandledException", TraceCore.Culture), new object[0]);
				TraceCore.WriteTraceSource(trace, 1, str, serializedPayload);
			}
		}

		internal static bool HandledExceptionIsEnabled(DiagnosticTrace trace)
		{
			if (trace.ShouldTrace(TraceEventLevel.Informational))
			{
				return true;
			}
			return TraceCore.IsEtwEventEnabled(trace, 1);
		}

		private static bool IsEtwEventEnabled(DiagnosticTrace trace, int eventIndex)
		{
			TraceCore.EnsureEventDescriptors();
			return trace.IsEtwEventEnabled(ref TraceCore.eventDescriptors[eventIndex]);
		}

		internal static void TraceCodeEventLogCritical(DiagnosticTrace trace, TraceRecord traceRecord)
		{
			TracePayload serializedPayload = DiagnosticTrace.GetSerializedPayload(null, traceRecord, null);
			if (TraceCore.IsEtwEventEnabled(trace, 6))
			{
				TraceCore.WriteEtwEvent(trace, 6, serializedPayload.ExtendedData, serializedPayload.AppDomainFriendlyName);
			}
			if (trace.ShouldTraceToTraceSource(TraceEventLevel.Critical))
			{
				string str = string.Format(TraceCore.Culture, TraceCore.ResourceManager.GetString("TraceCodeEventLogCritical", TraceCore.Culture), new object[0]);
				TraceCore.WriteTraceSource(trace, 6, str, serializedPayload);
			}
		}

		internal static bool TraceCodeEventLogCriticalIsEnabled(DiagnosticTrace trace)
		{
			if (trace.ShouldTrace(TraceEventLevel.Critical))
			{
				return true;
			}
			return TraceCore.IsEtwEventEnabled(trace, 6);
		}

		internal static void TraceCodeEventLogError(DiagnosticTrace trace, TraceRecord traceRecord)
		{
			TracePayload serializedPayload = DiagnosticTrace.GetSerializedPayload(null, traceRecord, null);
			if (TraceCore.IsEtwEventEnabled(trace, 7))
			{
				TraceCore.WriteEtwEvent(trace, 7, serializedPayload.ExtendedData, serializedPayload.AppDomainFriendlyName);
			}
			if (trace.ShouldTraceToTraceSource(TraceEventLevel.Error))
			{
				string str = string.Format(TraceCore.Culture, TraceCore.ResourceManager.GetString("TraceCodeEventLogError", TraceCore.Culture), new object[0]);
				TraceCore.WriteTraceSource(trace, 7, str, serializedPayload);
			}
		}

		internal static bool TraceCodeEventLogErrorIsEnabled(DiagnosticTrace trace)
		{
			if (trace.ShouldTrace(TraceEventLevel.Error))
			{
				return true;
			}
			return TraceCore.IsEtwEventEnabled(trace, 7);
		}

		internal static void TraceCodeEventLogInfo(DiagnosticTrace trace, TraceRecord traceRecord)
		{
			TracePayload serializedPayload = DiagnosticTrace.GetSerializedPayload(null, traceRecord, null);
			if (TraceCore.IsEtwEventEnabled(trace, 8))
			{
				TraceCore.WriteEtwEvent(trace, 8, serializedPayload.ExtendedData, serializedPayload.AppDomainFriendlyName);
			}
			if (trace.ShouldTraceToTraceSource(TraceEventLevel.Informational))
			{
				string str = string.Format(TraceCore.Culture, TraceCore.ResourceManager.GetString("TraceCodeEventLogInfo", TraceCore.Culture), new object[0]);
				TraceCore.WriteTraceSource(trace, 8, str, serializedPayload);
			}
		}

		internal static bool TraceCodeEventLogInfoIsEnabled(DiagnosticTrace trace)
		{
			if (trace.ShouldTrace(TraceEventLevel.Informational))
			{
				return true;
			}
			return TraceCore.IsEtwEventEnabled(trace, 8);
		}

		internal static void TraceCodeEventLogVerbose(DiagnosticTrace trace, TraceRecord traceRecord)
		{
			TracePayload serializedPayload = DiagnosticTrace.GetSerializedPayload(null, traceRecord, null);
			if (TraceCore.IsEtwEventEnabled(trace, 9))
			{
				TraceCore.WriteEtwEvent(trace, 9, serializedPayload.ExtendedData, serializedPayload.AppDomainFriendlyName);
			}
			if (trace.ShouldTraceToTraceSource(TraceEventLevel.Verbose))
			{
				string str = string.Format(TraceCore.Culture, TraceCore.ResourceManager.GetString("TraceCodeEventLogVerbose", TraceCore.Culture), new object[0]);
				TraceCore.WriteTraceSource(trace, 9, str, serializedPayload);
			}
		}

		internal static bool TraceCodeEventLogVerboseIsEnabled(DiagnosticTrace trace)
		{
			if (trace.ShouldTrace(TraceEventLevel.Verbose))
			{
				return true;
			}
			return TraceCore.IsEtwEventEnabled(trace, 9);
		}

		internal static void TraceCodeEventLogWarning(DiagnosticTrace trace, TraceRecord traceRecord)
		{
			TracePayload serializedPayload = DiagnosticTrace.GetSerializedPayload(null, traceRecord, null);
			if (TraceCore.IsEtwEventEnabled(trace, 10))
			{
				TraceCore.WriteEtwEvent(trace, 10, serializedPayload.ExtendedData, serializedPayload.AppDomainFriendlyName);
			}
			if (trace.ShouldTraceToTraceSource(TraceEventLevel.Warning))
			{
				string str = string.Format(TraceCore.Culture, TraceCore.ResourceManager.GetString("TraceCodeEventLogWarning", TraceCore.Culture), new object[0]);
				TraceCore.WriteTraceSource(trace, 10, str, serializedPayload);
			}
		}

		internal static bool TraceCodeEventLogWarningIsEnabled(DiagnosticTrace trace)
		{
			if (trace.ShouldTrace(TraceEventLevel.Warning))
			{
				return true;
			}
			return TraceCore.IsEtwEventEnabled(trace, 10);
		}

		internal static void UnhandledException(DiagnosticTrace trace, Exception exception)
		{
			TracePayload serializedPayload = DiagnosticTrace.GetSerializedPayload(null, null, exception);
			if (TraceCore.IsEtwEventEnabled(trace, 4))
			{
				TraceCore.WriteEtwEvent(trace, 4, serializedPayload.SerializedException, serializedPayload.AppDomainFriendlyName);
			}
			if (trace.ShouldTraceToTraceSource(TraceEventLevel.Error))
			{
				string str = string.Format(TraceCore.Culture, TraceCore.ResourceManager.GetString("UnhandledException", TraceCore.Culture), new object[0]);
				TraceCore.WriteTraceSource(trace, 4, str, serializedPayload);
			}
		}

		private static bool WriteEtwEvent(DiagnosticTrace trace, int eventIndex, string eventParam0, string eventParam1)
		{
			TraceCore.EnsureEventDescriptors();
			EtwProvider etwProvider = trace.EtwProvider;
			object[] objArray = new object[] { eventParam0, eventParam1 };
			return etwProvider.WriteEvent(ref TraceCore.eventDescriptors[eventIndex], objArray);
		}

		private static void WriteTraceSource(DiagnosticTrace trace, int eventIndex, string description, TracePayload payload)
		{
			TraceCore.EnsureEventDescriptors();
			trace.WriteTraceSource(ref TraceCore.eventDescriptors[eventIndex], description, payload);
		}
	}
}