using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Threading;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal class ExceptionUtility
	{
		private readonly Microsoft.ServiceBus.Diagnostics.DiagnosticTrace diagnosticTrace;

		public ExceptionUtility(object diagnosticTrace)
		{
			this.diagnosticTrace = (Microsoft.ServiceBus.Diagnostics.DiagnosticTrace)diagnosticTrace;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal static bool IsInfrastructureException(Exception exception)
		{
			if (exception == null)
			{
				return false;
			}
			if (exception is ThreadAbortException)
			{
				return true;
			}
			return exception is AppDomainUnloadedException;
		}

		internal Exception ThrowHelper(Exception exception, TraceEventType eventType, TraceRecord extendedData)
		{
			TraceEventType traceEventType = eventType;
			if (traceEventType <= TraceEventType.Start)
			{
				if (traceEventType <= TraceEventType.Information)
				{
					switch (traceEventType)
					{
						case TraceEventType.Critical:
						case TraceEventType.Error:
						{
							return Fx.Exception.AsError(exception, null);
						}
						case TraceEventType.Critical | TraceEventType.Error:
						{
							break;
						}
						case TraceEventType.Warning:
						{
							return Fx.Exception.AsWarning(exception, null);
						}
						default:
						{
							if (traceEventType == TraceEventType.Information)
							{
								return Fx.Exception.AsInformation(exception, null);
							}
							break;
						}
					}
				}
				else if (traceEventType != TraceEventType.Verbose && traceEventType != TraceEventType.Start)
				{
				}
			}
			else if (traceEventType <= TraceEventType.Suspend)
			{
				if (traceEventType != TraceEventType.Stop && traceEventType != TraceEventType.Suspend)
				{
				}
			}
			else if (traceEventType != TraceEventType.Resume && traceEventType != TraceEventType.Transfer)
			{
			}
			return Fx.Exception.AsVerbose(exception, null);
		}

		internal Exception ThrowHelper(Exception exception, TraceEventType eventType)
		{
			return this.ThrowHelper(exception, eventType, null);
		}

		internal ArgumentException ThrowHelperArgument(string paramName, string message)
		{
			return Fx.Exception.Argument(paramName, message);
		}

		internal ArgumentNullException ThrowHelperArgumentNull(string paramName)
		{
			return Fx.Exception.ArgumentNull(paramName);
		}

		internal ArgumentNullException ThrowHelperArgumentNull(string paramName, string message)
		{
			return Fx.Exception.ArgumentNull(paramName, message);
		}

		internal Exception ThrowHelperCallback(Exception innerException)
		{
			return this.ThrowHelperCallback(Microsoft.ServiceBus.SR.GetString(Resources.GenericCallbackException, new object[0]), innerException);
		}

		internal Exception ThrowHelperCallback(string message, Exception innerException)
		{
			return this.ThrowHelperCritical(new CallbackException(message, innerException));
		}

		internal Exception ThrowHelperCritical(Exception exception)
		{
			return this.ThrowHelper(exception, TraceEventType.Critical);
		}

		internal Exception ThrowHelperError(Exception exception)
		{
			return this.ThrowHelper(exception, TraceEventType.Error);
		}

		internal void TraceHandledException(Exception exception, TraceEventType eventType)
		{
			if (this.diagnosticTrace != null)
			{
				this.diagnosticTrace.TraceEvent(eventType, TraceCode.TraceHandledException, "Handled exception", null, exception);
			}
		}
	}
}