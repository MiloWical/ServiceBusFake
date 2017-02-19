using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal class PiiTraceSource : TraceSource
	{
		internal const string LogPii = "logKnownPii";

		private string eventSourceName = string.Empty;

		private bool shouldLogPii;

		private bool initialized;

		private object localSyncObject = new object();

		internal bool ShouldLogPii
		{
			get
			{
				if (!this.initialized)
				{
					this.Initialize();
				}
				return this.shouldLogPii;
			}
			set
			{
				this.initialized = true;
				this.shouldLogPii = value;
			}
		}

		internal PiiTraceSource(string name, string eventSourceName) : base(name)
		{
			this.eventSourceName = eventSourceName;
		}

		internal PiiTraceSource(string name, string eventSourceName, SourceLevels levels) : base(name, levels)
		{
			this.eventSourceName = eventSourceName;
		}

		protected override string[] GetSupportedAttributes()
		{
			return new string[] { "logKnownPii" };
		}

		private void Initialize()
		{
			if (!this.initialized)
			{
				lock (this.localSyncObject)
				{
					if (!this.initialized)
					{
						string item = base.Attributes["logKnownPii"];
						bool flag = false;
						if (!string.IsNullOrEmpty(item) && !bool.TryParse(item, out flag))
						{
							flag = false;
						}
						if (flag)
						{
							EventLogger eventLogger = new EventLogger(this.eventSourceName, null);
							if (!MachineSettingsSection.EnableLoggingKnownPii)
							{
								eventLogger.LogEvent(TraceEventType.Error, EventLogCategory.MessageLogging, EventLogEventId.PiiLoggingNotAllowed, false, new string[0]);
							}
							else
							{
								eventLogger.LogEvent(TraceEventType.Information, EventLogCategory.MessageLogging, EventLogEventId.PiiLoggingOn, false, new string[0]);
								this.shouldLogPii = true;
							}
						}
						this.initialized = true;
					}
				}
			}
		}
	}
}