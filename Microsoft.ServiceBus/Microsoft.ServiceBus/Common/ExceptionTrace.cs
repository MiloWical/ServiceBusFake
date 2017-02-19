using Microsoft.ServiceBus.Common.Diagnostics;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Common
{
	internal class ExceptionTrace
	{
		private const ushort FailFastEventLogCategory = 6;

		private readonly string eventSourceName;

		public ExceptionTrace(string eventSourceName)
		{
			this.eventSourceName = eventSourceName;
		}

		public ArgumentException Argument(string paramName, string message)
		{
			return this.TraceException<ArgumentException>(new ArgumentException(message, paramName), TraceEventType.Error, null);
		}

		public ArgumentNullException ArgumentNull(string paramName)
		{
			return this.TraceException<ArgumentNullException>(new ArgumentNullException(paramName), TraceEventType.Error, null);
		}

		public ArgumentNullException ArgumentNull(string paramName, string message)
		{
			return this.TraceException<ArgumentNullException>(new ArgumentNullException(paramName, message), TraceEventType.Error, null);
		}

		public ArgumentException ArgumentNullOrEmpty(string paramName)
		{
			return this.Argument(paramName, SRCore.ArgumentNullOrEmpty(paramName));
		}

		public ArgumentException ArgumentNullOrWhiteSpace(string paramName)
		{
			return this.Argument(paramName, SRCore.ArgumentNullOrWhiteSpace(paramName));
		}

		public ArgumentOutOfRangeException ArgumentOutOfRange(string paramName, object actualValue, string message)
		{
			return this.TraceException<ArgumentOutOfRangeException>(new ArgumentOutOfRangeException(paramName, actualValue, message), TraceEventType.Error, null);
		}

		public Exception AsError(Exception exception, EventTraceActivity activity = null)
		{
			return this.TraceException<Exception>(exception, TraceEventType.Error, activity);
		}

		public Exception AsInformation(Exception exception, EventTraceActivity activity = null)
		{
			return this.TraceException<Exception>(exception, TraceEventType.Information, activity);
		}

		public Exception AsVerbose(Exception exception, EventTraceActivity activity = null)
		{
			return this.TraceException<Exception>(exception, TraceEventType.Verbose, activity);
		}

		public Exception AsWarning(Exception exception, EventTraceActivity activity = null)
		{
			return this.TraceException<Exception>(exception, TraceEventType.Warning, activity);
		}

		internal void BreakOnException(Exception exception)
		{
		}

		private static string GetDetailsForThrownException(Exception e)
		{
			string str = e.GetType().ToString();
			StackTrace stackTrace = new StackTrace();
			string str1 = stackTrace.ToString();
			if (stackTrace.FrameCount > 10)
			{
				string[] newLine = new string[] { Environment.NewLine };
				string[] strArrays = str1.Split(newLine, 11, StringSplitOptions.RemoveEmptyEntries);
				str1 = string.Concat(string.Join(Environment.NewLine, strArrays, 0, 10), "...");
			}
			str = string.Concat(str, Environment.NewLine, str1);
			str = string.Concat(str, Environment.NewLine, "Exception ToString:", Environment.NewLine);
			str = string.Concat(str, e.ToStringSlim());
			return str;
		}

		public ObjectDisposedException ObjectDisposed(string message)
		{
			return this.TraceException<ObjectDisposedException>(new ObjectDisposedException(null, message), TraceEventType.Error, null);
		}

		public TException TraceException<TException>(TException exception, TraceEventType level, EventTraceActivity activity = null)
		where TException : Exception
		{
			if (!exception.Data.Contains(this.eventSourceName))
			{
				exception.Data[this.eventSourceName] = this.eventSourceName;
				switch (level)
				{
					case TraceEventType.Critical:
					case TraceEventType.Error:
					{
						if (!MessagingClientEtwProvider.Provider.IsEnabled(EventLevel.Error, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
						{
							break;
						}
						MessagingClientEtwProvider.Provider.ThrowingExceptionError(activity, ExceptionTrace.GetDetailsForThrownException(exception));
						break;
					}
					case TraceEventType.Warning:
					{
						if (!MessagingClientEtwProvider.Provider.IsEnabled(EventLevel.Warning, 140737488355328L, EventChannel.Application | EventChannel.Security | EventChannel.Setup))
						{
							break;
						}
						MessagingClientEtwProvider.Provider.ThrowingExceptionWarning(activity, ExceptionTrace.GetDetailsForThrownException(exception));
						break;
					}
				}
			}
			this.BreakOnException(exception);
			return exception;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public void TraceFailFast(string message)
		{
			EventLogger eventLogger = null;
			eventLogger = new EventLogger(this.eventSourceName, Fx.Trace);
			this.TraceFailFast(message, eventLogger);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal void TraceFailFast(string message, EventLogger logger)
		{
			if (logger != null)
			{
				try
				{
					string str = null;
					try
					{
						try
						{
							str = (new StackTrace()).ToString();
						}
						catch (Exception exception1)
						{
							Exception exception = exception1;
							str = exception.Message;
							if (Fx.IsFatal(exception))
							{
								throw;
							}
						}
					}
					finally
					{
						string[] strArrays = new string[] { message, str };
						logger.LogEvent(TraceEventType.Critical, 6, -1073676186, strArrays);
					}
				}
				catch (Exception exception3)
				{
					Exception exception2 = exception3;
					string[] str1 = new string[] { exception2.ToString() };
					logger.LogEvent(TraceEventType.Critical, 6, -1073676185, str1);
					if (Fx.IsFatal(exception2))
					{
						throw;
					}
				}
			}
		}

		public void TraceHandled(Exception exception, string catchLocation, EventTraceActivity activity = null)
		{
			MessagingClientEtwProvider.Provider.HandledExceptionWithFunctionName(activity, catchLocation, exception.ToStringSlim(), string.Empty);
			this.BreakOnException(exception);
		}

		public void TraceUnhandled(Exception exception)
		{
			MessagingClientEtwProvider.Provider.EventWriteUnhandledException(string.Concat(this.eventSourceName, ": ", exception.ToStringSlim()));
		}
	}
}