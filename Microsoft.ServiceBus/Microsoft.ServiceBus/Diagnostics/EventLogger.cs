using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal class EventLogger
	{
		private DiagnosticTrace diagnosticTrace;

		private string eventLogSourceName;

		[Obsolete("For SMDiagnostics.dll use only. Call DiagnosticUtility.EventLog instead")]
		internal EventLogger(string eventLogSourceName, object diagnosticTrace)
		{
			this.eventLogSourceName = eventLogSourceName;
			this.diagnosticTrace = (DiagnosticTrace)diagnosticTrace;
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

		internal void LogEvent(TraceEventType type, EventLogCategory category, EventLogEventId eventId, bool shouldTrace, params string[] values)
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
				string str1 = EventLogger.NormalizeEventLogParameter(DiagnosticTrace.ProcessName);
				strArrays[(int)strArrays.Length - 2] = str1;
				length = length + str1.Length + 1;
				string str2 = DiagnosticTrace.ProcessId.ToString(CultureInfo.InvariantCulture);
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
				using (SafeEventLogWriteHandle safeEventLogWriteHandle = SafeEventLogWriteHandle.RegisterEventSource(null, this.eventLogSourceName))
				{
					if (safeEventLogWriteHandle != null)
					{
						SecurityIdentifier user = WindowsIdentity.GetCurrent().User;
						byte[] numArray = new byte[user.BinaryLength];
						user.GetBinaryForm(numArray, 0);
						IntPtr[] intPtrArray = new IntPtr[(int)strArrays.Length];
						GCHandle gCHandle = GCHandle.Alloc(intPtrArray, GCHandleType.Pinned);
						GCHandle[] gCHandleArray = null;
						try
						{
							gCHandleArray = new GCHandle[(int)strArrays.Length];
							for (int k = 0; k < (int)strArrays.Length; k++)
							{
								gCHandleArray[k] = GCHandle.Alloc(strArrays[k], GCHandleType.Pinned);
								intPtrArray[k] = gCHandleArray[k].AddrOfPinnedObject();
							}
							HandleRef handleRef = new HandleRef(safeEventLogWriteHandle, gCHandle.AddrOfPinnedObject());
							Microsoft.ServiceBus.Diagnostics.NativeMethods.ReportEvent(safeEventLogWriteHandle, (ushort)EventLogger.EventLogEntryTypeFromEventType(type), (ushort)category, (uint)eventId, numArray, (ushort)((int)strArrays.Length), 0, handleRef, null);
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
					}
				}
				if (shouldTrace && this.diagnosticTrace != null)
				{
					Dictionary<string, string> strs = new Dictionary<string, string>((int)strArrays.Length + 4);
					strs["CategoryID.Name"] = category.ToString();
					strs["CategoryID.Value"] = category.ToString(CultureInfo.InvariantCulture);
					strs["InstanceID.Name"] = eventId.ToString();
					strs["InstanceID.Value"] = eventId.ToString(CultureInfo.InvariantCulture);
					for (int m = 0; m < (int)values.Length; m++)
					{
						strs.Add(string.Concat("Value", m.ToString(CultureInfo.InvariantCulture)), (values[m] == null ? string.Empty : DiagnosticTrace.XmlEncode(values[m])));
					}
					this.diagnosticTrace.TraceEvent(type, TraceCode.EventLog, Microsoft.ServiceBus.SR.GetString(Resources.TraceCodeEventLog, new object[0]), new DictionaryTraceRecord(strs), null, null);
				}
			}
			catch (Exception exception)
			{
				if (Fx.IsFatal(exception))
				{
					throw;
				}
			}
		}

		internal void LogEvent(TraceEventType type, EventLogCategory category, EventLogEventId eventId, params string[] values)
		{
			this.LogEvent(type, category, eventId, true, values);
		}

		internal static string NormalizeEventLogParameter(string param)
		{
			if (param.IndexOf('%') < 0)
			{
				return param;
			}
			StringBuilder stringBuilder = null;
			int length = param.Length;
			for (int i = 0; i < length; i++)
			{
				char chr = param[i];
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
				else if (param[i + 1] >= '0' && param[i + 1] <= '9')
				{
					if (stringBuilder == null)
					{
						stringBuilder = new StringBuilder(length + 2);
						for (int j = 0; j < i; j++)
						{
							stringBuilder.Append(param[j]);
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
				return param;
			}
			return stringBuilder.ToString();
		}
	}
}