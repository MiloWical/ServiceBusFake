using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal class DiagnosticTrace
	{
		private const string DefaultTraceListenerName = "Default";

		private const int MaxTraceSize = 65535;

		private const string subType = "";

		private const string version = "1";

		private const int traceFailureLogThreshold = 1;

		private const string TraceRecordVersion = "http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord";

		private const SourceLevels DefaultLevel = SourceLevels.Off;

		private bool tracingEnabled = true;

		private bool haveListeners;

		private object localSyncObject = new object();

		private DateTime lastFailure = DateTime.MinValue;

		private SourceLevels level;

		private bool calledShutdown;

		private bool shouldUseActivity;

		private string AppDomainFriendlyName;

		private PiiTraceSource traceSource;

		private TraceSourceKind traceSourceType = TraceSourceKind.PiiTraceSource;

		private string TraceSourceName = string.Empty;

		private string eventSourceName = string.Empty;

		private static SortedList<TraceCode, string> traceCodes;

		private static object classLockObject;

		internal static Guid ActivityId
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

		[Obsolete("For SMDiagnostics.dll use only. Call DiagnosticUtility.HaveListeners instead")]
		internal bool HaveListeners
		{
			get
			{
				return this.haveListeners;
			}
		}

		private DateTime LastFailure
		{
			get
			{
				return this.lastFailure;
			}
			set
			{
				this.lastFailure = value;
			}
		}

		[Obsolete("For SMDiagnostics.dll use only. Call DiagnosticUtility.Level instead")]
		internal SourceLevels Level
		{
			get
			{
				if (this.TraceSource != null && this.TraceSource.Switch.Level != this.level)
				{
					this.level = this.TraceSource.Switch.Level;
				}
				return this.level;
			}
			set
			{
				this.SetLevelThreadSafe(value);
			}
		}

		internal static int ProcessId
		{
			get
			{
				int id;
				using (Process currentProcess = Process.GetCurrentProcess())
				{
					id = currentProcess.Id;
				}
				return id;
			}
		}

		internal static string ProcessName
		{
			get
			{
				string processName;
				using (Process currentProcess = Process.GetCurrentProcess())
				{
					processName = currentProcess.ProcessName;
				}
				return processName;
			}
		}

		[Obsolete("For SMDiagnostics.dll use only. Call DiagnosticUtility.ShouldUseActivity instead")]
		internal bool ShouldUseActivity
		{
			get
			{
				return this.shouldUseActivity;
			}
		}

		internal PiiTraceSource TraceSource
		{
			get
			{
				return this.traceSource;
			}
			set
			{
				this.traceSource = value;
			}
		}

		[Obsolete("For SMDiagnostics.dll use only. Call DiagnosticUtility.TracingEnabled instead")]
		internal bool TracingEnabled
		{
			get
			{
				if (!this.tracingEnabled)
				{
					return false;
				}
				return this.traceSource != null;
			}
		}

		static DiagnosticTrace()
		{
			Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.traceCodes = new SortedList<TraceCode, string>();
			Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.classLockObject = new object();
		}

		[Obsolete("For SMDiagnostics.dll use only. Never 'new' this type up unless you are DiagnosticUtility.")]
		internal DiagnosticTrace(TraceSourceKind sourceType, string traceSourceName, string eventSourceName)
		{
			this.traceSourceType = sourceType;
			this.TraceSourceName = traceSourceName;
			this.eventSourceName = eventSourceName;
			this.AppDomainFriendlyName = AppDomain.CurrentDomain.FriendlyName;
			try
			{
				this.CreateTraceSource();
				AppDomain currentDomain = AppDomain.CurrentDomain;
				this.haveListeners = this.TraceSource.Listeners.Count > 0;
				this.tracingEnabled = this.HaveListeners;
				if (this.TracingEnabled)
				{
					currentDomain.UnhandledException += new UnhandledExceptionEventHandler(this.UnhandledExceptionHandler);
					this.SetLevel(this.TraceSource.Switch.Level);
					currentDomain.DomainUnload += new EventHandler(this.ExitOrUnloadEventHandler);
					currentDomain.ProcessExit += new EventHandler(this.ExitOrUnloadEventHandler);
				}
			}
			catch (ConfigurationErrorsException configurationErrorsException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Microsoft.ServiceBus.Diagnostics.EventLogger eventLogger = new Microsoft.ServiceBus.Diagnostics.EventLogger(this.eventSourceName, null);
				string[] str = new string[] { exception.ToString() };
				eventLogger.LogEvent(TraceEventType.Error, EventLogCategory.Tracing, Microsoft.ServiceBus.Diagnostics.EventLogEventId.FailedToSetupTracing, false, str);
			}
		}

		private void AddExceptionToTraceString(XmlWriter xml, Exception exception)
		{
			xml.WriteElementString("ExceptionType", Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.XmlEncode(exception.GetType().AssemblyQualifiedName));
			xml.WriteElementString("Message", Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.XmlEncode(exception.Message));
			xml.WriteElementString("StackTrace", Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.XmlEncode(Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.StackTraceString(exception)));
			xml.WriteElementString("ExceptionString", Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.XmlEncode(exception.ToString()));
			Win32Exception win32Exception = exception as Win32Exception;
			if (win32Exception != null)
			{
				int nativeErrorCode = win32Exception.NativeErrorCode;
				xml.WriteElementString("NativeErrorCode", nativeErrorCode.ToString("X", CultureInfo.InvariantCulture));
			}
			if (exception.Data != null && exception.Data.Count > 0)
			{
				xml.WriteStartElement("DataItems");
				foreach (object key in exception.Data.Keys)
				{
					xml.WriteStartElement("Data");
					xml.WriteElementString("Key", Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.XmlEncode(key.ToString()));
					xml.WriteElementString("Value", Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.XmlEncode(exception.Data[key].ToString()));
					xml.WriteEndElement();
				}
				xml.WriteEndElement();
			}
			if (exception.InnerException != null)
			{
				xml.WriteStartElement("InnerException");
				this.AddExceptionToTraceString(xml, exception.InnerException);
				xml.WriteEndElement();
			}
		}

		private void BuildTrace(TraceEventType type, TraceCode code, string description, TraceRecord trace, Exception exception, object source, out TraceXPathNavigator navigator)
		{
			PlainXmlWriter plainXmlWriter = new PlainXmlWriter(65535);
			navigator = plainXmlWriter.Navigator;
			this.BuildTrace(plainXmlWriter, type, code, description, trace, exception, source);
			if (!this.TraceSource.ShouldLogPii)
			{
				navigator.RemovePii(Microsoft.ServiceBus.Diagnostics.DiagnosticStrings.HeadersPaths);
			}
		}

		private void BuildTrace(PlainXmlWriter xml, TraceEventType type, TraceCode code, string description, TraceRecord trace, Exception exception, object source)
		{
			xml.WriteStartElement("TraceRecord");
			xml.WriteAttributeString("xmlns", "http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord");
			xml.WriteAttributeString("Severity", Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.LookupSeverity(type));
			xml.WriteElementString("TraceIdentifier", Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.GenerateTraceCode(code));
			xml.WriteElementString("Description", description);
			xml.WriteElementString("AppDomain", this.AppDomainFriendlyName);
			if (source != null)
			{
				xml.WriteElementString("Source", Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.CreateSourceString(source));
			}
			if (trace != null)
			{
				xml.WriteStartElement("ExtendedData");
				xml.WriteAttributeString("xmlns", trace.EventId);
				trace.WriteTo(xml);
				xml.WriteEndElement();
			}
			if (exception != null)
			{
				xml.WriteStartElement("Exception");
				this.AddExceptionToTraceString(xml, exception);
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		internal static string CodeToString(TraceCode code)
		{
			string str = null;
			if (!Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.traceCodes.TryGetValue(code, out str))
			{
				lock (Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.classLockObject)
				{
					if (!Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.traceCodes.TryGetValue(code, out str))
					{
						str = code.ToString();
						Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.traceCodes.Add(code, str);
					}
				}
			}
			return str;
		}

		internal static string CreateSourceString(object source)
		{
			string str = source.GetType().ToString();
			int hashCode = source.GetHashCode();
			return string.Concat(str, "/", hashCode.ToString(CultureInfo.CurrentCulture));
		}

		private void CreateTraceSource()
		{
			PiiTraceSource diagnosticTraceSource = null;
			if (this.traceSourceType != TraceSourceKind.PiiTraceSource)
			{
				diagnosticTraceSource = new Microsoft.ServiceBus.Diagnostics.DiagnosticTraceSource(this.TraceSourceName, this.eventSourceName, SourceLevels.Off);
			}
			else
			{
				diagnosticTraceSource = new PiiTraceSource(this.TraceSourceName, this.eventSourceName, SourceLevels.Off);
			}
			diagnosticTraceSource.Listeners.Remove("Default");
			this.TraceSource = diagnosticTraceSource;
		}

		private void ExitOrUnloadEventHandler(object sender, EventArgs e)
		{
			this.ShutdownTracing();
		}

		private static SourceLevels FixLevel(SourceLevels level)
		{
			if (((int)level & -16 & (int)SourceLevels.Verbose) != (int)SourceLevels.Off)
			{
				level = level | SourceLevels.Verbose;
			}
			else if (((int)level & -8 & (int)SourceLevels.Information) != (int)SourceLevels.Off)
			{
				level = level | SourceLevels.Information;
			}
			else if (((int)level & -4 & (int)SourceLevels.Warning) != (int)SourceLevels.Off)
			{
				level = level | SourceLevels.Warning;
			}
			if (((int)level & -2 & (int)SourceLevels.Error) != (int)SourceLevels.Off)
			{
				level = level | SourceLevels.Error;
			}
			if ((level & SourceLevels.Critical) != SourceLevels.Off)
			{
				level = level | SourceLevels.Critical;
			}
			if (level == SourceLevels.ActivityTracing)
			{
				level = SourceLevels.Off;
			}
			return level;
		}

		internal static string GenerateTraceCode(TraceCode code)
		{
			object[] name;
			CultureInfo invariantCulture;
			TraceCode traceCode = (TraceCode)((int)((long)code & (ulong)-65536));
			string empty = null;
			TraceCode traceCode1 = traceCode;
			if (traceCode1 > TraceCode.Security)
			{
				if (traceCode1 <= TraceCode.PortSharing)
				{
					if (traceCode1 == TraceCode.ServiceModel)
					{
						goto Label4;
					}
					if (traceCode1 == TraceCode.Activation)
					{
						empty = "System.ServiceModel.Activation";
						invariantCulture = CultureInfo.InvariantCulture;
						name = new object[] { CultureInfo.CurrentCulture.Name, empty, Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.CodeToString(code) };
						return string.Format(invariantCulture, "http://msdn.microsoft.com/{0}/library/{1}.{2}.aspx", name);
					}
					else
					{
						if (traceCode1 != TraceCode.PortSharing)
						{
							empty = string.Empty;
							invariantCulture = CultureInfo.InvariantCulture;
							name = new object[] { CultureInfo.CurrentCulture.Name, empty, Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.CodeToString(code) };
							return string.Format(invariantCulture, "http://msdn.microsoft.com/{0}/library/{1}.{2}.aspx", name);
						}
						empty = "System.ServiceModel.PortSharing";
						invariantCulture = CultureInfo.InvariantCulture;
						name = new object[] { CultureInfo.CurrentCulture.Name, empty, Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.CodeToString(code) };
						return string.Format(invariantCulture, "http://msdn.microsoft.com/{0}/library/{1}.{2}.aspx", name);
					}
				}
				else if (traceCode1 > TraceCode.IdentityModel)
				{
					if (traceCode1 != TraceCode.IdentityModelSelectors)
					{
						goto Label2;
					}
					empty = "System.IdentityModel.Selectors";
					invariantCulture = CultureInfo.InvariantCulture;
					name = new object[] { CultureInfo.CurrentCulture.Name, empty, Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.CodeToString(code) };
					return string.Format(invariantCulture, "http://msdn.microsoft.com/{0}/library/{1}.{2}.aspx", name);
				}
				else if (traceCode1 == TraceCode.TransactionBridge)
				{
					empty = "Microsoft.Transactions.TransactionBridge";
					invariantCulture = CultureInfo.InvariantCulture;
					name = new object[] { CultureInfo.CurrentCulture.Name, empty, Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.CodeToString(code) };
					return string.Format(invariantCulture, "http://msdn.microsoft.com/{0}/library/{1}.{2}.aspx", name);
				}
				else
				{
					if (traceCode1 != TraceCode.IdentityModel)
					{
						empty = string.Empty;
						invariantCulture = CultureInfo.InvariantCulture;
						name = new object[] { CultureInfo.CurrentCulture.Name, empty, Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.CodeToString(code) };
						return string.Format(invariantCulture, "http://msdn.microsoft.com/{0}/library/{1}.{2}.aspx", name);
					}
					empty = "System.IdentityModel";
					invariantCulture = CultureInfo.InvariantCulture;
					name = new object[] { CultureInfo.CurrentCulture.Name, empty, Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.CodeToString(code) };
					return string.Format(invariantCulture, "http://msdn.microsoft.com/{0}/library/{1}.{2}.aspx", name);
				}
			Label4:
				empty = "System.ServiceModel";
			}
			else if (traceCode1 <= TraceCode.Serialization)
			{
				if (traceCode1 == TraceCode.Administration)
				{
					empty = "System.ServiceModel.Administration";
				}
				else if (traceCode1 == TraceCode.Diagnostics)
				{
					empty = "System.ServiceModel.Diagnostics";
				}
				else
				{
					if (traceCode1 != TraceCode.Serialization)
					{
						empty = string.Empty;
						invariantCulture = CultureInfo.InvariantCulture;
						name = new object[] { CultureInfo.CurrentCulture.Name, empty, Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.CodeToString(code) };
						return string.Format(invariantCulture, "http://msdn.microsoft.com/{0}/library/{1}.{2}.aspx", name);
					}
					empty = "System.Runtime.Serialization";
				}
			}
			else if (traceCode1 == TraceCode.Channels)
			{
				empty = "System.ServiceModel.Channels";
			}
			else if (traceCode1 == TraceCode.ComIntegration)
			{
				empty = "System.ServiceModel.ComIntegration";
			}
			else
			{
				if (traceCode1 != TraceCode.Security)
				{
					empty = string.Empty;
					invariantCulture = CultureInfo.InvariantCulture;
					name = new object[] { CultureInfo.CurrentCulture.Name, empty, Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.CodeToString(code) };
					return string.Format(invariantCulture, "http://msdn.microsoft.com/{0}/library/{1}.{2}.aspx", name);
				}
				empty = "System.ServiceModel.Security";
			}
			invariantCulture = CultureInfo.InvariantCulture;
			name = new object[] { CultureInfo.CurrentCulture.Name, empty, Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.CodeToString(code) };
			return string.Format(invariantCulture, "http://msdn.microsoft.com/{0}/library/{1}.{2}.aspx", name);
		Label2:
			if (traceCode1 == TraceCode.ServiceModelTransaction)
			{
				goto Label4;
			}
			empty = string.Empty;
			invariantCulture = CultureInfo.InvariantCulture;
			name = new object[] { CultureInfo.CurrentCulture.Name, empty, Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.CodeToString(code) };
			return string.Format(invariantCulture, "http://msdn.microsoft.com/{0}/library/{1}.{2}.aspx", name);
		}

		private void LogTraceFailure(string traceString, Exception e)
		{
			TimeSpan timeSpan = TimeSpan.FromMinutes(10);
			try
			{
				lock (this.localSyncObject)
				{
					if (DateTime.UtcNow.Subtract(this.LastFailure) >= timeSpan)
					{
						this.LastFailure = DateTime.UtcNow;
						Microsoft.ServiceBus.Diagnostics.EventLogger eventLogger = new Microsoft.ServiceBus.Diagnostics.EventLogger(this.eventSourceName, this);
						if (e != null)
						{
							string[] strArrays = new string[] { traceString, e.ToString() };
							eventLogger.LogEvent(TraceEventType.Error, EventLogCategory.Tracing, Microsoft.ServiceBus.Diagnostics.EventLogEventId.FailedToTraceEventWithException, false, strArrays);
						}
						else
						{
							string[] strArrays1 = new string[] { traceString };
							eventLogger.LogEvent(TraceEventType.Error, EventLogCategory.Tracing, Microsoft.ServiceBus.Diagnostics.EventLogEventId.FailedToTraceEvent, false, strArrays1);
						}
					}
				}
			}
			catch
			{
			}
		}

		private static string LookupSeverity(TraceEventType type)
		{
			string str;
			TraceEventType traceEventType = type;
			if (traceEventType <= TraceEventType.Verbose)
			{
				switch (traceEventType)
				{
					case TraceEventType.Critical:
					{
						str = "Critical";
						break;
					}
					case TraceEventType.Error:
					{
						str = "Error";
						break;
					}
					case TraceEventType.Critical | TraceEventType.Error:
					{
						str = type.ToString();
						return str;
					}
					case TraceEventType.Warning:
					{
						str = "Warning";
						break;
					}
					default:
					{
						if (traceEventType == TraceEventType.Information)
						{
							str = "Information";
							break;
						}
						else if (traceEventType == TraceEventType.Verbose)
						{
							str = "Verbose";
							break;
						}
						else
						{
							str = type.ToString();
							return str;
						}
					}
				}
			}
			else if (traceEventType <= TraceEventType.Stop)
			{
				if (traceEventType == TraceEventType.Start)
				{
					str = "Start";
				}
				else
				{
					if (traceEventType != TraceEventType.Stop)
					{
						str = type.ToString();
						return str;
					}
					str = "Stop";
				}
			}
			else if (traceEventType == TraceEventType.Suspend)
			{
				str = "Suspend";
			}
			else
			{
				if (traceEventType != TraceEventType.Transfer)
				{
					str = type.ToString();
					return str;
				}
				str = "Transfer";
			}
			return str;
		}

		private void SetLevel(SourceLevels level)
		{
			SourceLevels sourceLevel = Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.FixLevel(level);
			this.level = sourceLevel;
			if (this.TraceSource != null)
			{
				this.haveListeners = this.TraceSource.Listeners.Count > 0;
				if (this.TraceSource.Switch.Level != SourceLevels.Off && level == SourceLevels.Off)
				{
					System.Diagnostics.TraceSource traceSource = this.TraceSource;
					this.CreateTraceSource();
					traceSource.Close();
				}
				this.tracingEnabled = (!this.HaveListeners ? false : sourceLevel != SourceLevels.Off);
				this.TraceSource.Switch.Level = sourceLevel;
				this.shouldUseActivity = (sourceLevel & SourceLevels.ActivityTracing) != SourceLevels.Off;
			}
		}

		private void SetLevelThreadSafe(SourceLevels level)
		{
			lock (this.localSyncObject)
			{
				this.SetLevel(level);
			}
		}

		[Obsolete("For SMDiagnostics.dll use only. Call DiagnosticUtility.ShouldTrace instead")]
		internal bool ShouldTrace(TraceEventType type)
		{
			if (!this.TracingEnabled || this.TraceSource == null)
			{
				return false;
			}
			return (int)SourceLevels.Off != ((int)type & (int)this.Level);
		}

		private void ShutdownTracing()
		{
			if (this.TraceSource != null && !this.calledShutdown)
			{
				try
				{
					if (this.Level != SourceLevels.Off)
					{
						if (this.ShouldTrace(TraceEventType.Information))
						{
							Dictionary<string, string> strs = new Dictionary<string, string>(3);
							strs["AppDomain.FriendlyName"] = AppDomain.CurrentDomain.FriendlyName;
							strs["ProcessName"] = Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.ProcessName;
							int processId = Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.ProcessId;
							strs["ProcessId"] = processId.ToString(CultureInfo.CurrentCulture);
							this.TraceEvent(TraceEventType.Information, TraceCode.AppDomainUnload, Microsoft.ServiceBus.SR.GetString(Resources.TraceCodeAppDomainUnload, new object[0]), new Microsoft.ServiceBus.Diagnostics.DictionaryTraceRecord(strs), null, null);
						}
						this.calledShutdown = true;
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
					this.LogTraceFailure(null, exception);
				}
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
					if (str != null && (str1 == "StackTraceString" || str1 == "AddExceptionToTraceString" || str1 == "BuildTrace" || str1 == "TraceEvent" || str1 == "TraceException"))
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

		internal void TraceEvent(TraceEventType type, TraceCode code, string description, TraceRecord trace, Exception exception, object source)
		{
			TraceXPathNavigator traceXPathNavigator = null;
			try
			{
				if (this.TraceSource != null && this.HaveListeners)
				{
					try
					{
						this.BuildTrace(type, code, description, trace, exception, source, out traceXPathNavigator);
					}
					catch (PlainXmlWriter.MaxSizeExceededException maxSizeExceededException)
					{
						StringTraceRecord stringTraceRecord = new StringTraceRecord("TruncatedTraceId", Microsoft.ServiceBus.Diagnostics.DiagnosticTrace.GenerateTraceCode(code));
						this.TraceEvent(type, TraceCode.TraceTruncatedQuotaExceeded, Microsoft.ServiceBus.SR.GetString(Resources.TraceCodeTraceTruncatedQuotaExceeded, new object[0]), stringTraceRecord);
					}
					this.TraceSource.TraceData(type, (int)code, traceXPathNavigator);
					if (this.calledShutdown)
					{
						this.TraceSource.Flush();
					}
					this.LastFailure = DateTime.MinValue;
				}
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				this.LogTraceFailure((traceXPathNavigator == null ? string.Empty : traceXPathNavigator.ToString()), exception1);
			}
		}

		internal void TraceEvent(TraceEventType type, TraceCode code, string description)
		{
			this.TraceEvent(type, code, description, null, null, null);
		}

		internal void TraceEvent(TraceEventType type, TraceCode code, string description, TraceRecord trace)
		{
			this.TraceEvent(type, code, description, trace, null, null);
		}

		internal void TraceEvent(TraceEventType type, TraceCode code, string description, TraceRecord trace, Exception exception)
		{
			this.TraceEvent(type, code, description, trace, exception, null);
		}

		internal void TraceEvent(TraceEventType type, TraceCode code, string description, TraceRecord trace, Exception exception, Guid activityId, object source)
		{
			Activity activity;
			if (!this.ShouldUseActivity || !(Guid.Empty == activityId))
			{
				activity = Activity.CreateActivity(activityId);
			}
			else
			{
				activity = null;
			}
			using (activity)
			{
				this.TraceEvent(type, code, description, trace, exception, source);
			}
		}

		private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
		{
			Exception exceptionObject = (Exception)args.ExceptionObject;
			this.TraceEvent(TraceEventType.Critical, TraceCode.UnhandledException, Microsoft.ServiceBus.SR.GetString(Resources.UnhandledException, new object[0]), null, exceptionObject, null);
			this.ShutdownTracing();
		}

		internal static string XmlEncode(string text)
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