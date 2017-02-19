using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal static class DiagnosticUtility
	{
		private const string TraceSourceName = "System.ServiceModel";

		internal const string EventSourceName = "System.ServiceModel 3.0.0.0";

		private static Microsoft.ServiceBus.Diagnostics.DiagnosticTrace diagnosticTrace;

		private static Microsoft.ServiceBus.Diagnostics.ExceptionUtility exceptionUtility;

		private static Microsoft.ServiceBus.Diagnostics.Utility utility;

		private static object lockObject;

		internal static Microsoft.ServiceBus.Diagnostics.DiagnosticTrace DiagnosticTrace
		{
			get
			{
				return DiagnosticUtility.diagnosticTrace;
			}
		}

		internal static Microsoft.ServiceBus.Diagnostics.ExceptionUtility ExceptionUtility
		{
			get
			{
				return DiagnosticUtility.exceptionUtility ?? DiagnosticUtility.GetExceptionUtility();
			}
		}

		internal static bool ShouldTraceCritical
		{
			get
			{
				if (DiagnosticUtility.DiagnosticTrace == null)
				{
					return false;
				}
				return DiagnosticUtility.DiagnosticTrace.ShouldTrace(TraceEventType.Critical);
			}
		}

		internal static bool ShouldTraceError
		{
			get
			{
				if (DiagnosticUtility.DiagnosticTrace == null)
				{
					return false;
				}
				return DiagnosticUtility.DiagnosticTrace.ShouldTrace(TraceEventType.Error);
			}
		}

		internal static bool ShouldTraceInformation
		{
			get
			{
				if (DiagnosticUtility.DiagnosticTrace == null)
				{
					return false;
				}
				return DiagnosticUtility.DiagnosticTrace.ShouldTrace(TraceEventType.Information);
			}
		}

		internal static bool ShouldTraceVerbose
		{
			get
			{
				if (DiagnosticUtility.DiagnosticTrace == null)
				{
					return false;
				}
				return DiagnosticUtility.DiagnosticTrace.ShouldTrace(TraceEventType.Verbose);
			}
		}

		internal static bool ShouldTraceWarning
		{
			get
			{
				if (DiagnosticUtility.DiagnosticTrace == null)
				{
					return false;
				}
				return DiagnosticUtility.DiagnosticTrace.ShouldTrace(TraceEventType.Warning);
			}
		}

		internal static bool ShouldUseActivity
		{
			get
			{
				if (DiagnosticUtility.DiagnosticTrace == null)
				{
					return false;
				}
				return DiagnosticUtility.DiagnosticTrace.ShouldTrace(TraceEventType.Start);
			}
		}

		internal static bool TracingEnabled
		{
			get
			{
				if (DiagnosticUtility.DiagnosticTrace == null)
				{
					return false;
				}
				return DiagnosticUtility.DiagnosticTrace.TracingEnabled;
			}
		}

		internal static Microsoft.ServiceBus.Diagnostics.Utility Utility
		{
			get
			{
				return DiagnosticUtility.utility ?? DiagnosticUtility.GetUtility();
			}
		}

		static DiagnosticUtility()
		{
			DiagnosticUtility.diagnosticTrace = DiagnosticUtility.InitializeTracing();
			DiagnosticUtility.lockObject = new object();
		}

		public static void DebugAssert(bool condition, string debugText)
		{
		}

		public static void DebugAssert(string debugText)
		{
		}

		private static Microsoft.ServiceBus.Diagnostics.ExceptionUtility GetExceptionUtility()
		{
			lock (DiagnosticUtility.lockObject)
			{
				if (DiagnosticUtility.exceptionUtility == null)
				{
					DiagnosticUtility.exceptionUtility = new Microsoft.ServiceBus.Diagnostics.ExceptionUtility(DiagnosticUtility.diagnosticTrace);
				}
			}
			return DiagnosticUtility.exceptionUtility;
		}

		private static Microsoft.ServiceBus.Diagnostics.Utility GetUtility()
		{
			lock (DiagnosticUtility.lockObject)
			{
				if (DiagnosticUtility.utility == null)
				{
					DiagnosticUtility.utility = new Microsoft.ServiceBus.Diagnostics.Utility(DiagnosticUtility.ExceptionUtility);
				}
			}
			return DiagnosticUtility.utility;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void InitDiagnosticTraceImpl(TraceSourceKind sourceType, string traceSourceName)
		{
			DiagnosticUtility.diagnosticTrace = new Microsoft.ServiceBus.Diagnostics.DiagnosticTrace(sourceType, traceSourceName, "System.ServiceModel 3.0.0.0");
		}

		private static Microsoft.ServiceBus.Diagnostics.DiagnosticTrace InitializeTracing()
		{
			DiagnosticUtility.InitDiagnosticTraceImpl(TraceSourceKind.DiagnosticTraceSource, "System.ServiceModel");
			if (!DiagnosticUtility.diagnosticTrace.HaveListeners)
			{
				DiagnosticUtility.diagnosticTrace = null;
			}
			return DiagnosticUtility.diagnosticTrace;
		}

		internal static bool ShouldTrace(TraceEventType type)
		{
			bool shouldTraceCritical = false;
			if (DiagnosticUtility.TracingEnabled)
			{
				TraceEventType traceEventType = type;
				switch (traceEventType)
				{
					case TraceEventType.Critical:
					{
						shouldTraceCritical = DiagnosticUtility.ShouldTraceCritical;
						break;
					}
					case TraceEventType.Error:
					{
						shouldTraceCritical = DiagnosticUtility.ShouldTraceError;
						break;
					}
					case TraceEventType.Critical | TraceEventType.Error:
					{
						break;
					}
					case TraceEventType.Warning:
					{
						shouldTraceCritical = DiagnosticUtility.ShouldTraceWarning;
						break;
					}
					default:
					{
						if (traceEventType == TraceEventType.Information)
						{
							shouldTraceCritical = DiagnosticUtility.ShouldTraceInformation;
							break;
						}
						else if (traceEventType == TraceEventType.Verbose)
						{
							shouldTraceCritical = DiagnosticUtility.ShouldTraceVerbose;
							break;
						}
						else
						{
							break;
						}
					}
				}
			}
			return shouldTraceCritical;
		}

		internal static AsyncCallback ThunkAsyncCallback(AsyncCallback callback)
		{
			return callback;
		}
	}
}