using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.ServiceBus.Common.Diagnostics
{
	internal sealed class DiagnosticTrace
	{
		private const string DefaultTraceListenerName = "Default";

		private const string TraceRecordVersion = "http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord";

		private const int WindowsVistaMajorNumber = 6;

		private const string EventSourceVersion = "4.0.0.0";

		private const ushort TracingEventLogCategory = 4;

		private const string defaultEtwId = "A307C7A2-A4CD-4D22-8093-94DB72934152";

		[SecurityCritical]
		private static Guid defaultEtwProviderId;

		private static Hashtable etwProviderCache;

		private static bool isVistaOrGreater;

		private static string appDomainFriendlyName;

		private bool calledShutdown;

		private bool haveListeners;

		private object thisLock;

		private SourceLevels level;

		private DiagnosticTraceSource traceSource;

		[SecurityCritical]
		private Microsoft.ServiceBus.Common.Diagnostics.EtwProvider etwProvider;

		private string TraceSourceName;

		[SecurityCritical]
		private string eventSourceName;

		public static Guid ActivityId
		{
			get
			{
				object activityId = Trace.CorrelationManager.ActivityId;
				if (activityId == null)
				{
					return Guid.Empty;
				}
				return (Guid)activityId;
			}
			set
			{
				Trace.CorrelationManager.ActivityId = value;
			}
		}

		public static Guid DefaultEtwProviderId
		{
			get
			{
				return DiagnosticTrace.defaultEtwProviderId;
			}
			[SecurityCritical]
			set
			{
				DiagnosticTrace.defaultEtwProviderId = value;
			}
		}

		public Microsoft.ServiceBus.Common.Diagnostics.EtwProvider EtwProvider
		{
			[SecurityCritical]
			get
			{
				return this.etwProvider;
			}
		}

		private bool EtwTracingEnabled
		{
			get
			{
				return this.etwProvider != null;
			}
		}

		public bool HaveListeners
		{
			get
			{
				return this.haveListeners;
			}
		}

		public bool IsEtwProviderEnabled
		{
			get
			{
				if (!this.EtwTracingEnabled)
				{
					return false;
				}
				return this.etwProvider.IsEnabled();
			}
		}

		private DateTime LastFailure
		{
			get;
			set;
		}

		public SourceLevels Level
		{
			get
			{
				if (this.TraceSource != null)
				{
					this.level = this.TraceSource.Switch.Level;
				}
				return this.level;
			}
		}

		private static int ProcessId
		{
			get
			{
				int id = -1;
				using (Process currentProcess = Process.GetCurrentProcess())
				{
					id = currentProcess.Id;
				}
				return id;
			}
		}

		private static string ProcessName
		{
			get
			{
				string processName = null;
				using (Process currentProcess = Process.GetCurrentProcess())
				{
					processName = currentProcess.ProcessName;
				}
				return processName;
			}
		}

		public DiagnosticTraceSource TraceSource
		{
			get
			{
				return this.traceSource;
			}
		}

		public bool TracingEnabled
		{
			get
			{
				return this.traceSource != null;
			}
		}

		[SecurityCritical]
		static DiagnosticTrace()
		{
			DiagnosticTrace.defaultEtwProviderId = (MessagingClientEtwProvider.IsEtwEnabled() ? MessagingClientEtwProvider.Provider.Guid : new Guid("A307C7A2-A4CD-4D22-8093-94DB72934152"));
			DiagnosticTrace.etwProviderCache = new Hashtable();
			DiagnosticTrace.isVistaOrGreater = Environment.OSVersion.Version.Major >= 6;
			DiagnosticTrace.appDomainFriendlyName = AppDomain.CurrentDomain.FriendlyName;
		}

		public DiagnosticTrace(string traceSourceName, Guid etwProviderId)
		{
			try
			{
				this.thisLock = new object();
				this.TraceSourceName = traceSourceName;
				this.eventSourceName = string.Concat(this.TraceSourceName, " ", "4.0.0.0");
				this.LastFailure = DateTime.MinValue;
				this.CreateTraceSource();
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				EventLogger eventLogger = new EventLogger(this.eventSourceName, null);
				string[] str = new string[] { exception.ToString() };
				eventLogger.LogEvent(TraceEventType.Error, 4, -1073676188, false, str);
			}
			try
			{
				this.CreateEtwProvider(etwProviderId);
			}
			catch (Exception exception3)
			{
				Exception exception2 = exception3;
				if (Fx.IsFatal(exception2))
				{
					throw;
				}
				this.etwProvider = null;
				EventLogger eventLogger1 = new EventLogger(this.eventSourceName, null);
				string[] strArrays = new string[] { exception2.ToString() };
				eventLogger1.LogEvent(TraceEventType.Error, 4, -1073676188, false, strArrays);
			}
			if (this.TracingEnabled || this.EtwTracingEnabled)
			{
				this.AddDomainEventHandlersForCleanup();
			}
		}

		[Obsolete("For SMDiagnostics.dll use only")]
		private void AddDomainEventHandlersForCleanup()
		{
			AppDomain currentDomain = AppDomain.CurrentDomain;
			if (this.TracingEnabled)
			{
				currentDomain.UnhandledException += new UnhandledExceptionEventHandler(this.UnhandledExceptionHandler);
				currentDomain.DomainUnload += new EventHandler(this.ExitOrUnloadEventHandler);
				currentDomain.ProcessExit += new EventHandler(this.ExitOrUnloadEventHandler);
			}
		}

		[SecurityCritical]
		private static string BuildTrace(ref System.Diagnostics.Eventing.EventDescriptor eventDescriptor, string description, TracePayload payload)
		{
			StringBuilder stringBuilder = new StringBuilder();
			XmlTextWriter xmlTextWriter = new XmlTextWriter(new StringWriter(stringBuilder, CultureInfo.CurrentCulture));
			xmlTextWriter.WriteStartElement("TraceRecord");
			xmlTextWriter.WriteAttributeString("xmlns", "http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord");
			xmlTextWriter.WriteAttributeString("Severity", TraceLevelHelper.LookupSeverity((TraceEventLevel)eventDescriptor.Level, (TraceEventOpcode)eventDescriptor.Opcode));
			xmlTextWriter.WriteAttributeString("Channel", DiagnosticTrace.LookupChannel((TraceChannel)eventDescriptor.Channel));
			xmlTextWriter.WriteElementString("TraceIdentifier", DiagnosticTrace.GenerateTraceCode(ref eventDescriptor));
			xmlTextWriter.WriteElementString("Description", description);
			xmlTextWriter.WriteElementString("AppDomain", payload.AppDomainFriendlyName);
			if (!string.IsNullOrEmpty(payload.EventSource))
			{
				xmlTextWriter.WriteElementString("Source", payload.EventSource);
			}
			if (!string.IsNullOrEmpty(payload.ExtendedData))
			{
				xmlTextWriter.WriteRaw(payload.ExtendedData);
			}
			if (!string.IsNullOrEmpty(payload.SerializedException))
			{
				xmlTextWriter.WriteRaw(payload.SerializedException);
			}
			xmlTextWriter.WriteEndElement();
			return stringBuilder.ToString();
		}

		private void CreateEtwProvider(Guid etwProviderId)
		{
			if (etwProviderId != Guid.Empty && DiagnosticTrace.isVistaOrGreater)
			{
				this.etwProvider = (Microsoft.ServiceBus.Common.Diagnostics.EtwProvider)DiagnosticTrace.etwProviderCache[etwProviderId];
				if (this.etwProvider == null)
				{
					lock (DiagnosticTrace.etwProviderCache)
					{
						this.etwProvider = (Microsoft.ServiceBus.Common.Diagnostics.EtwProvider)DiagnosticTrace.etwProviderCache[etwProviderId];
						if (this.etwProvider == null)
						{
							this.etwProvider = new Microsoft.ServiceBus.Common.Diagnostics.EtwProvider(etwProviderId);
							DiagnosticTrace.etwProviderCache.Add(etwProviderId, this.etwProvider);
						}
					}
				}
			}
		}

		private static string CreateSourceString(object source)
		{
			string str = source.GetType().ToString();
			int hashCode = source.GetHashCode();
			return string.Concat(str, "/", hashCode.ToString(CultureInfo.CurrentCulture));
		}

		private void CreateTraceSource()
		{
			if (!string.IsNullOrEmpty(this.TraceSourceName))
			{
				this.traceSource = new DiagnosticTraceSource(this.TraceSourceName);
				if (this.traceSource != null)
				{
					this.traceSource.Listeners.Remove("Default");
					this.haveListeners = this.traceSource.Listeners.Count > 0;
					this.level = this.traceSource.Switch.Level;
				}
			}
		}

		public void Event(int eventId, TraceEventLevel traceEventLevel, TraceChannel channel, string description)
		{
			if (this.TracingEnabled)
			{
				System.Diagnostics.Eventing.EventDescriptor eventDescriptor = DiagnosticTrace.GetEventDescriptor(eventId, channel, traceEventLevel);
				this.Event(ref eventDescriptor, description);
			}
		}

		[SecurityCritical]
		public void Event(ref System.Diagnostics.Eventing.EventDescriptor eventDescriptor, string description)
		{
			if (this.TracingEnabled)
			{
				TracePayload serializedPayload = DiagnosticTrace.GetSerializedPayload(null, null, null);
				this.WriteTraceSource(ref eventDescriptor, description, serializedPayload);
			}
		}

		private static string ExceptionToTraceString(Exception exception)
		{
			StringBuilder stringBuilder = new StringBuilder();
			XmlTextWriter xmlTextWriter = new XmlTextWriter(new StringWriter(stringBuilder, CultureInfo.CurrentCulture));
			xmlTextWriter.WriteStartElement("Exception");
			xmlTextWriter.WriteElementString("ExceptionType", DiagnosticTrace.XmlEncode(exception.GetType().AssemblyQualifiedName));
			xmlTextWriter.WriteElementString("Message", DiagnosticTrace.XmlEncode(exception.Message));
			xmlTextWriter.WriteElementString("StackTrace", DiagnosticTrace.XmlEncode(DiagnosticTrace.StackTraceString(exception)));
			xmlTextWriter.WriteElementString("ExceptionString", DiagnosticTrace.XmlEncode(exception.ToString()));
			Win32Exception win32Exception = exception as Win32Exception;
			if (win32Exception != null)
			{
				int nativeErrorCode = win32Exception.NativeErrorCode;
				xmlTextWriter.WriteElementString("NativeErrorCode", nativeErrorCode.ToString("X", CultureInfo.InvariantCulture));
			}
			if (exception.Data != null && exception.Data.Count > 0)
			{
				xmlTextWriter.WriteStartElement("DataItems");
				foreach (object key in exception.Data.Keys)
				{
					xmlTextWriter.WriteStartElement("Data");
					xmlTextWriter.WriteElementString("Key", DiagnosticTrace.XmlEncode(key.ToString()));
					xmlTextWriter.WriteElementString("Value", DiagnosticTrace.XmlEncode(exception.Data[key].ToString()));
					xmlTextWriter.WriteEndElement();
				}
				xmlTextWriter.WriteEndElement();
			}
			if (exception.InnerException != null)
			{
				xmlTextWriter.WriteStartElement("InnerException");
				xmlTextWriter.WriteRaw(DiagnosticTrace.ExceptionToTraceString(exception.InnerException));
				xmlTextWriter.WriteEndElement();
			}
			xmlTextWriter.WriteEndElement();
			return stringBuilder.ToString();
		}

		private void ExitOrUnloadEventHandler(object sender, EventArgs e)
		{
			this.ShutdownTracing();
		}

		[SecurityCritical]
		private static string GenerateTraceCode(ref System.Diagnostics.Eventing.EventDescriptor eventDescriptor)
		{
			return eventDescriptor.EventId.ToString(CultureInfo.InvariantCulture);
		}

		[SecurityCritical]
		private static System.Diagnostics.Eventing.EventDescriptor GetEventDescriptor(int eventId, TraceChannel channel, TraceEventLevel traceEventLevel)
		{
			long num = (long)0;
			if (channel == TraceChannel.Admin)
			{
				num = num | -9223372036854775808L;
			}
			else if (channel == TraceChannel.Operational)
			{
				num = num | 4611686018427387904L;
			}
			else if (channel == TraceChannel.Analytic)
			{
				num = num | 2305843009213693952L;
			}
			else if (channel == TraceChannel.Debug)
			{
				num = num | 72057594037927936L;
			}
			else if (channel == TraceChannel.Perf)
			{
				num = num | 576460752303423488L;
			}
			return new System.Diagnostics.Eventing.EventDescriptor(eventId, 0, (byte)channel, (byte)traceEventLevel, 0, 0, num);
		}

		public static TracePayload GetSerializedPayload(object source, TraceRecord traceRecord, Exception exception)
		{
			return DiagnosticTrace.GetSerializedPayload(source, traceRecord, exception, false);
		}

		public static TracePayload GetSerializedPayload(object source, TraceRecord traceRecord, Exception exception, bool getServiceReference)
		{
			string str = null;
			string str1 = null;
			string traceString = null;
			if (source != null)
			{
				str = DiagnosticTrace.CreateSourceString(source);
			}
			if (traceRecord != null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				XmlTextWriter xmlTextWriter = new XmlTextWriter(new StringWriter(stringBuilder, CultureInfo.CurrentCulture));
				xmlTextWriter.WriteStartElement("ExtendedData");
				traceRecord.WriteTo(xmlTextWriter);
				xmlTextWriter.WriteEndElement();
				str1 = stringBuilder.ToString();
			}
			if (exception != null)
			{
				traceString = DiagnosticTrace.ExceptionToTraceString(exception);
			}
			return new TracePayload(traceString, str, DiagnosticTrace.appDomainFriendlyName, str1, string.Empty);
		}

		public bool IsEtwEventEnabled(ref System.Diagnostics.Eventing.EventDescriptor eventDescriptor)
		{
			if (!this.EtwTracingEnabled)
			{
				return false;
			}
			return this.etwProvider.IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords);
		}

		private void LogTraceFailure(string traceString, Exception exception)
		{
			TimeSpan timeSpan = TimeSpan.FromMinutes(10);
			try
			{
				lock (this.thisLock)
				{
					if (DateTime.UtcNow.Subtract(this.LastFailure) >= timeSpan)
					{
						this.LastFailure = DateTime.UtcNow;
						EventLogger eventLogger = EventLogger.UnsafeCreateEventLogger(this.eventSourceName, this);
						if (exception != null)
						{
							string[] strArrays = new string[] { traceString, exception.ToString() };
							eventLogger.UnsafeLogEvent(TraceEventType.Error, 4, -1073676183, false, strArrays);
						}
						else
						{
							string[] strArrays1 = new string[] { traceString };
							eventLogger.UnsafeLogEvent(TraceEventType.Error, 4, -1073676184, false, strArrays1);
						}
					}
				}
			}
			catch (Exception exception1)
			{
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
			}
		}

		private static string LookupChannel(TraceChannel traceChannel)
		{
			string str;
			TraceChannel traceChannel1 = traceChannel;
			if (traceChannel1 == TraceChannel.Application)
			{
				str = "Application";
			}
			else
			{
				switch (traceChannel1)
				{
					case TraceChannel.Admin:
					{
						str = "Admin";
						break;
					}
					case TraceChannel.Operational:
					{
						str = "Operational";
						break;
					}
					case TraceChannel.Analytic:
					{
						str = "Analytic";
						break;
					}
					case TraceChannel.Debug:
					{
						str = "Debug";
						break;
					}
					case TraceChannel.Perf:
					{
						str = "Perf";
						break;
					}
					default:
					{
						str = traceChannel.ToString();
						break;
					}
				}
			}
			return str;
		}

		public bool ShouldTrace(TraceEventLevel eventLevel)
		{
			if (this.ShouldTraceToTraceSource(eventLevel))
			{
				return true;
			}
			return this.ShouldTraceToEtw(eventLevel);
		}

		public bool ShouldTraceToEtw(TraceEventLevel traceEventLevel)
		{
			if (this.EtwProvider == null)
			{
				return false;
			}
			return this.EtwProvider.IsEnabled((byte)traceEventLevel, (long)0);
		}

		public bool ShouldTraceToTraceSource(TraceEventLevel eventLevel)
		{
			if (!this.HaveListeners || this.TraceSource == null)
			{
				return false;
			}
			return (int)SourceLevels.Off != ((int)TraceLevelHelper.GetTraceEventType(eventLevel) & (int)this.Level);
		}

		private void ShutdownEtwProvider()
		{
			try
			{
				if (this.etwProvider != null)
				{
					this.etwProvider.Dispose();
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.LogTraceFailure(null, exception);
			}
		}

		private void ShutdownTraceSource()
		{
			try
			{
				MessagingClientEtwProvider.Provider.EventWriteAppDomainUnload(AppDomain.CurrentDomain.FriendlyName, DiagnosticTrace.ProcessName, DiagnosticTrace.ProcessId);
				this.TraceSource.Flush();
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.LogTraceFailure(null, exception);
			}
		}

		private void ShutdownTracing()
		{
			if (!this.calledShutdown)
			{
				this.calledShutdown = true;
				this.ShutdownTraceSource();
				this.ShutdownEtwProvider();
			}
		}

		private static string StackTraceString(Exception exception)
		{
			string stackTrace = exception.StackTrace;
			if (string.IsNullOrEmpty(stackTrace))
			{
				StackFrame[] frames = (new StackTrace(false)).GetFrames();
				int num = 0;
				bool flag = false;
				StackFrame[] stackFrameArray = frames;
				for (int i = 0; i < (int)stackFrameArray.Length; i++)
				{
					string name = stackFrameArray[i].GetMethod().Name;
					string str = name;
					string str1 = str;
					if (str != null && (str1 == "StackTraceString" || str1 == "AddExceptionToTraceString" || str1 == "GetAdditionalPayload"))
					{
						num++;
					}
					else if (!name.StartsWith("ThrowHelper", StringComparison.Ordinal))
					{
						flag = true;
					}
					else
					{
						num++;
					}
					if (flag)
					{
						break;
					}
				}
				stackTrace = (new StackTrace(num, false)).ToString();
			}
			return stackTrace;
		}

		private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
		{
			TraceCore.UnhandledException(this, (Exception)args.ExceptionObject);
			this.ShutdownTracing();
		}

		[SecurityCritical]
		public void WriteTraceSource(ref System.Diagnostics.Eventing.EventDescriptor eventDescriptor, string description, TracePayload payload)
		{
			if (this.TracingEnabled)
			{
				XPathNavigator xPathNavigator = null;
				try
				{
					string str = DiagnosticTrace.BuildTrace(ref eventDescriptor, description, payload);
					XmlDocument xmlDocument = new XmlDocument();
					StringReader stringReader = new StringReader(str);
					XmlReaderSettings xmlReaderSetting = new XmlReaderSettings()
					{
						DtdProcessing = DtdProcessing.Prohibit
					};
					using (XmlReader xmlReader = XmlReader.Create(stringReader, xmlReaderSetting))
					{
						xmlDocument.Load(xmlReader);
					}
					xPathNavigator = xmlDocument.CreateNavigator();
					this.TraceSource.TraceData(TraceLevelHelper.GetTraceEventType(eventDescriptor.Level, eventDescriptor.Opcode), eventDescriptor.EventId, xPathNavigator);
					if (this.calledShutdown)
					{
						this.TraceSource.Flush();
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					this.LogTraceFailure((xPathNavigator == null ? string.Empty : xPathNavigator.ToString()), exception);
				}
			}
		}

		public static string XmlEncode(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return text;
			}
			int length = text.Length;
			StringBuilder stringBuilder = new StringBuilder(length + 8);
			for (int i = 0; i < length; i++)
			{
				char chr = text[i];
				char chr1 = chr;
				if (chr1 == '&')
				{
					stringBuilder.Append("&amp;");
				}
				else
				{
					switch (chr1)
					{
						case '<':
						{
							stringBuilder.Append("&lt;");
							break;
						}
						case '=':
						{
							stringBuilder.Append(chr);
							break;
						}
						case '>':
						{
							stringBuilder.Append("&gt;");
							break;
						}
						default:
						{
							goto case '=';
						}
					}
				}
			}
			return stringBuilder.ToString();
		}
	}
}