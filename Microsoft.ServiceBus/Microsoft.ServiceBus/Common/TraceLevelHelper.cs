using System;
using System.Diagnostics;

namespace Microsoft.ServiceBus.Common
{
	internal class TraceLevelHelper
	{
		private static TraceEventType[] EtwLevelToTraceEventType;

		static TraceLevelHelper()
		{
			TraceEventType[] traceEventTypeArray = new TraceEventType[] { TraceEventType.Critical, TraceEventType.Critical, TraceEventType.Error, TraceEventType.Warning, TraceEventType.Information, TraceEventType.Verbose };
			TraceLevelHelper.EtwLevelToTraceEventType = traceEventTypeArray;
		}

		public TraceLevelHelper()
		{
		}

		private static TraceEventType EtwOpcodeToTraceEventType(TraceEventOpcode opcode)
		{
			if (opcode == TraceEventOpcode.Start)
			{
				return TraceEventType.Start;
			}
			if (opcode == TraceEventOpcode.Stop)
			{
				return TraceEventType.Stop;
			}
			if (opcode == TraceEventOpcode.Suspend)
			{
				return TraceEventType.Suspend;
			}
			if (opcode == TraceEventOpcode.Resume)
			{
				return TraceEventType.Resume;
			}
			return TraceEventType.Information;
		}

		internal static TraceEventType GetTraceEventType(byte level, byte opcode)
		{
			if (opcode != 0)
			{
				return TraceLevelHelper.EtwOpcodeToTraceEventType((TraceEventOpcode)opcode);
			}
			return TraceLevelHelper.EtwLevelToTraceEventType[level];
		}

		internal static TraceEventType GetTraceEventType(TraceEventLevel level)
		{
			return TraceLevelHelper.EtwLevelToTraceEventType[(int)level];
		}

		internal static TraceEventType GetTraceEventType(byte level)
		{
			return TraceLevelHelper.EtwLevelToTraceEventType[level];
		}

		internal static string LookupSeverity(TraceEventLevel level, TraceEventOpcode opcode)
		{
			string str;
			if (opcode != TraceEventOpcode.Info)
			{
				TraceEventOpcode traceEventOpcode = opcode;
				switch (traceEventOpcode)
				{
					case TraceEventOpcode.Start:
					{
						str = "Start";
						break;
					}
					case TraceEventOpcode.Stop:
					{
						str = "Stop";
						break;
					}
					default:
					{
						switch (traceEventOpcode)
						{
							case TraceEventOpcode.Resume:
							{
								str = "Resume";
								break;
							}
							case TraceEventOpcode.Suspend:
							{
								str = "Suspend";
								break;
							}
							default:
							{
								str = opcode.ToString();
								break;
							}
						}
						break;
					}
				}
			}
			else
			{
				switch (level)
				{
					case TraceEventLevel.Critical:
					{
						str = "Critical";
						break;
					}
					case TraceEventLevel.Error:
					{
						str = "Error";
						break;
					}
					case TraceEventLevel.Warning:
					{
						str = "Warning";
						break;
					}
					case TraceEventLevel.Informational:
					{
						str = "Information";
						break;
					}
					case TraceEventLevel.Verbose:
					{
						str = "Verbose";
						break;
					}
					default:
					{
						str = level.ToString();
						break;
					}
				}
			}
			return str;
		}
	}
}