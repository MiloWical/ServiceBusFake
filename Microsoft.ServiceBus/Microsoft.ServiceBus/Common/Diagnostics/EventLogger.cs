using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;

namespace Microsoft.ServiceBus.Common.Diagnostics
{
	internal sealed class EventLogger
	{
		private const int MaxEventLogsInPT = 5;

		[SecurityCritical]
		private static int logCountForPT;

		private static bool canLogEvent;

		private DiagnosticTrace diagnosticTrace;

		[SecurityCritical]
		private string eventLogSourceName;

		private bool isInPartialTrust;

		static EventLogger()
		{
			EventLogger.canLogEvent = true;
		}

		private EventLogger()
		{
			this.isInPartialTrust = EventLogger.IsInPartialTrust();
		}

		[Obsolete("For System.Runtime.dll use only. Call FxTrace.EventLog instead")]
		public EventLogger(string eventLogSourceName, DiagnosticTrace diagnosticTrace)
		{
			try
			{
				this.diagnosticTrace = diagnosticTrace;
				if (EventLogger.canLogEvent)
				{
					this.SafeSetLogSourceName(eventLogSourceName);
				}
			}
			catch (SecurityException securityException)
			{
				EventLogger.canLogEvent = false;
			}
		}

		private static EventLogEntryType EventLogEntryTypeFromEventType(TraceEventType type)
		{
			EventLogEntryType eventLogEntryType = EventLogEntryType.Information;
			switch (type)
			{
				case TraceEventType.Critical:
				case TraceEventType.Error:
				{
					eventLogEntryType = EventLogEntryType.Error;
					return eventLogEntryType;
				}
				case TraceEventType.Critical | TraceEventType.Error:
				{
					return eventLogEntryType;
				}
				case TraceEventType.Warning:
				{
					eventLogEntryType = EventLogEntryType.Warning;
					return eventLogEntryType;
				}
				default:
				{
					return eventLogEntryType;
				}
			}
		}

		private static bool IsInPartialTrust()
		{
			bool flag = false;
			try
			{
				using (Process currentProcess = Process.GetCurrentProcess())
				{
					flag = string.IsNullOrEmpty(currentProcess.ProcessName);
				}
			}
			catch (SecurityException securityException)
			{
				flag = true;
			}
			return flag;
		}

		public void LogEvent(TraceEventType type, ushort eventLogCategory, uint eventId, bool shouldTrace, params string[] values)
		{
			if (EventLogger.canLogEvent)
			{
				try
				{
					this.SafeLogEvent(type, eventLogCategory, eventId, shouldTrace, values);
				}
				catch (SecurityException securityException1)
				{
					SecurityException securityException = securityException1;
					EventLogger.canLogEvent = false;
					if (shouldTrace && this.diagnosticTrace != null && TraceCore.HandledExceptionIsEnabled(this.diagnosticTrace))
					{
						TraceCore.HandledException(this.diagnosticTrace, securityException);
					}
				}
			}
		}

		public void LogEvent(TraceEventType type, ushort eventLogCategory, uint eventId, params string[] values)
		{
			this.LogEvent(type, eventLogCategory, eventId, true, values);
		}

		private static string NormalizeEventLogParameter(string eventLogParameter)
		{
			if (eventLogParameter.IndexOf('%') < 0)
			{
				return eventLogParameter;
			}
			StringBuilder stringBuilder = null;
			int length = eventLogParameter.Length;
			for (int i = 0; i < length; i++)
			{
				char chr = eventLogParameter[i];
				if (chr != '%')
				{
					if (stringBuilder != null)
					{
						stringBuilder.Append(chr);
					}
				}
				else if (i + 1 >= length)
				{
					if (stringBuilder != null)
					{
						stringBuilder.Append(chr);
					}
				}
				else if (eventLogParameter[i + 1] >= '0' && eventLogParameter[i + 1] <= '9')
				{
					if (stringBuilder == null)
					{
						stringBuilder = new StringBuilder(length + 2);
						for (int j = 0; j < i; j++)
						{
							stringBuilder.Append(eventLogParameter[j]);
						}
					}
					stringBuilder.Append(chr);
					stringBuilder.Append(' ');
				}
				else if (stringBuilder != null)
				{
					stringBuilder.Append(chr);
				}
			}
			if (stringBuilder == null)
			{
				return eventLogParameter;
			}
			return stringBuilder.ToString();
		}

		[SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)]
		private void SafeLogEvent(TraceEventType type, ushort eventLogCategory, uint eventId, bool shouldTrace, params string[] values)
		{
			this.UnsafeLogEvent(type, eventLogCategory, eventId, shouldTrace, values);
		}

		[SecurityPermission(SecurityAction.Demand, UnmanagedCode=true)]
		private void SafeSetLogSourceName(string updatedEventLogSourceName)
		{
			this.eventLogSourceName = updatedEventLogSourceName;
		}

		[SecurityCritical]
		private void SetLogSourceName(string updatedEventLogSourceName, DiagnosticTrace updatedDiagnosticTrace)
		{
			this.eventLogSourceName = updatedEventLogSourceName;
			this.diagnosticTrace = updatedDiagnosticTrace;
		}

		[SecurityCritical]
		public static EventLogger UnsafeCreateEventLogger(string eventLogSourceName, DiagnosticTrace diagnosticTrace)
		{
			EventLogger eventLogger = new EventLogger();
			eventLogger.SetLogSourceName(eventLogSourceName, diagnosticTrace);
			return eventLogger;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
		private static int UnsafeGetProcessId()
		{
			int id = -1;
			using (Process currentProcess = Process.GetCurrentProcess())
			{
				id = currentProcess.Id;
			}
			return id;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
		private static string UnsafeGetProcessName()
		{
			string processName = null;
			using (Process currentProcess = Process.GetCurrentProcess())
			{
				processName = currentProcess.ProcessName;
			}
			return processName;
		}

		[SecurityCritical]
		public void UnsafeLogEvent(TraceEventType type, ushort eventLogCategory, uint eventId, bool shouldTrace, params string[] values)
		{
			if (EventLogger.logCountForPT < 5)
			{
				try
				{
					int length = 0;
					string[] strArrays = new string[(int)values.Length + 2];
					for (int i = 0; i < (int)values.Length; i++)
					{
						string str = values[i];
						str = (string.IsNullOrEmpty(str) ? string.Empty : EventLogger.NormalizeEventLogParameter(str));
						strArrays[i] = str;
						length = length + str.Length + 1;
					}
					string str1 = EventLogger.NormalizeEventLogParameter(EventLogger.UnsafeGetProcessName());
					strArrays[(int)strArrays.Length - 2] = str1;
					length = length + str1.Length + 1;
					string str2 = EventLogger.UnsafeGetProcessId().ToString(CultureInfo.InvariantCulture);
					strArrays[(int)strArrays.Length - 1] = str2;
					length = length + str2.Length + 1;
					if (length > 25600)
					{
						int num = 25600 / (int)strArrays.Length - 1;
						for (int j = 0; j < (int)strArrays.Length; j++)
						{
							if (strArrays[j].Length > num)
							{
								strArrays[j] = strArrays[j].Substring(0, num);
							}
						}
					}
					SecurityIdentifier user = WindowsIdentity.GetCurrent().User;
					byte[] numArray = new byte[user.BinaryLength];
					user.GetBinaryForm(numArray, 0);
					IntPtr[] intPtrArray = new IntPtr[(int)strArrays.Length];
					GCHandle gCHandle = new GCHandle();
					GCHandle[] gCHandleArray = null;
					try
					{
						gCHandle = GCHandle.Alloc(intPtrArray, GCHandleType.Pinned);
						gCHandleArray = new GCHandle[(int)strArrays.Length];
						for (int k = 0; k < (int)strArrays.Length; k++)
						{
							gCHandleArray[k] = GCHandle.Alloc(strArrays[k], GCHandleType.Pinned);
							intPtrArray[k] = gCHandleArray[k].AddrOfPinnedObject();
						}
						this.UnsafeWriteEventLog(type, eventLogCategory, eventId, strArrays, numArray, gCHandle);
					}
					finally
					{
						if (gCHandle.AddrOfPinnedObject() != IntPtr.Zero)
						{
							gCHandle.Free();
						}
						if (gCHandleArray != null)
						{
							GCHandle[] gCHandleArray1 = gCHandleArray;
							for (int l = 0; l < (int)gCHandleArray1.Length; l++)
							{
								gCHandleArray1[l].Free();
							}
						}
					}
					if (shouldTrace && this.diagnosticTrace != null && (TraceCore.TraceCodeEventLogCriticalIsEnabled(this.diagnosticTrace) || TraceCore.TraceCodeEventLogVerboseIsEnabled(this.diagnosticTrace) || TraceCore.TraceCodeEventLogInfoIsEnabled(this.diagnosticTrace) || TraceCore.TraceCodeEventLogWarningIsEnabled(this.diagnosticTrace) || TraceCore.TraceCodeEventLogErrorIsEnabled(this.diagnosticTrace)))
					{
						Dictionary<string, string> strs = new Dictionary<string, string>((int)strArrays.Length + 4);
						strs["CategoryID.Name"] = "EventLogCategory";
						strs["CategoryID.Value"] = eventLogCategory.ToString(CultureInfo.InvariantCulture);
						strs["InstanceID.Name"] = "EventId";
						strs["InstanceID.Value"] = eventId.ToString(CultureInfo.InvariantCulture);
						for (int m = 0; m < (int)values.Length; m++)
						{
							strs.Add(string.Concat("Value", m.ToString(CultureInfo.InvariantCulture)), (values[m] == null ? string.Empty : DiagnosticTrace.XmlEncode(values[m])));
						}
						TraceRecord dictionaryTraceRecord = new DictionaryTraceRecord(strs);
						TraceEventType traceEventType = type;
						switch (traceEventType)
						{
							case TraceEventType.Critical:
							{
								TraceCore.TraceCodeEventLogCritical(this.diagnosticTrace, dictionaryTraceRecord);
								break;
							}
							case TraceEventType.Error:
							{
								TraceCore.TraceCodeEventLogError(this.diagnosticTrace, dictionaryTraceRecord);
								break;
							}
							case TraceEventType.Critical | TraceEventType.Error:
							{
								break;
							}
							case TraceEventType.Warning:
							{
								TraceCore.TraceCodeEventLogWarning(this.diagnosticTrace, dictionaryTraceRecord);
								break;
							}
							default:
							{
								if (traceEventType == TraceEventType.Information)
								{
									TraceCore.TraceCodeEventLogInfo(this.diagnosticTrace, dictionaryTraceRecord);
									break;
								}
								else if (traceEventType == TraceEventType.Verbose)
								{
									TraceCore.TraceCodeEventLogVerbose(this.diagnosticTrace, dictionaryTraceRecord);
									break;
								}
								else
								{
									break;
								}
							}
						}
					}
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
				}
				if (this.isInPartialTrust)
				{
					EventLogger.logCountForPT = EventLogger.logCountForPT + 1;
				}
			}
		}

		[SecurityCritical]
		[SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
		private void UnsafeWriteEventLog(TraceEventType type, ushort eventLogCategory, uint eventId, string[] logValues, byte[] sidBA, GCHandle stringsRootHandle)
		{
			using (SafeEventLogWriteHandle safeEventLogWriteHandle = SafeEventLogWriteHandle.RegisterEventSource(null, this.eventLogSourceName))
			{
				if (safeEventLogWriteHandle != null)
				{
					HandleRef handleRef = new HandleRef(safeEventLogWriteHandle, stringsRootHandle.AddrOfPinnedObject());
					UnsafeNativeMethods.ReportEvent(safeEventLogWriteHandle, (ushort)EventLogger.EventLogEntryTypeFromEventType(type), eventLogCategory, eventId, sidBA, (ushort)((int)logValues.Length), 0, handleRef, null);
				}
			}
		}
	}
}