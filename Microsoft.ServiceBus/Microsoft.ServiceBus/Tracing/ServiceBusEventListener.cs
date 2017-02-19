using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Xml;

namespace Microsoft.ServiceBus.Tracing
{
	internal class ServiceBusEventListener : EventListener
	{
		private const string UtcDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'";

		private const string DiagnosticsTracingSettingsKey = "Microsoft.ServiceBus.Tracing.UseDiagnosticsTracing";

		private const string TraceSourceName = "Microsoft.ServiceBus";

		private const string TruncatedString = "#TRUNCATED#\" />";

		private const int MaxTraceSize = 31744;

		private const char HighChar = '\uDFFF';

		private const char LowChar = '\uD800';

		private static EventDescriptor manifestRequestDescriptor;

		private readonly static XmlWriterSettings xmlWriterSettings;

		private readonly static TraceSource traceSource;

		private EventSource eventSource;

		private static object initializeLock;

		private static bool useDiagnosticsTracing;

		private static bool initialized;

		private IDictionary<int, string> messageDictionary;

		internal bool EnableDiagnosticsTracing
		{
			get;
			set;
		}

		internal Guid ProviderId
		{
			get;
			private set;
		}

		static ServiceBusEventListener()
		{
			ServiceBusEventListener.manifestRequestDescriptor = new EventDescriptor(65534, 1, 0, 0, 254, 65534, (long)-1);
			ServiceBusEventListener.xmlWriterSettings = new XmlWriterSettings()
			{
				OmitXmlDeclaration = true
			};
			ServiceBusEventListener.traceSource = new TraceSource("Microsoft.ServiceBus");
			ServiceBusEventListener.initializeLock = new object();
		}

		public ServiceBusEventListener(Guid providerId)
		{
			this.ProviderId = providerId;
		}

		private string ConvertMessageToXmlFormattedString(EventWrittenEventArgs eventDescriptor)
		{
			StringBuilder stringBuilder = new StringBuilder();
			using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, ServiceBusEventListener.xmlWriterSettings))
			{
				xmlWriter.WriteStartElement("Trc");
				int eventId = eventDescriptor.EventId;
				xmlWriter.WriteAttributeString("Id", eventId.ToString(CultureInfo.InvariantCulture));
				xmlWriter.WriteAttributeString("Ch", ServiceBusEventListener.GetEventChannel(eventDescriptor).ToString());
				xmlWriter.WriteAttributeString("Lvl", ServiceBusEventListener.GetEventLevel(eventDescriptor).ToString());
				xmlWriter.WriteAttributeString("Kw", eventDescriptor.Keywords.ToString("x"));
				DateTime utcNow = DateTime.UtcNow;
				xmlWriter.WriteAttributeString("UTC", utcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'", CultureInfo.InvariantCulture));
				string item = this.messageDictionary[eventDescriptor.EventId];
				if (item != null)
				{
					string str = string.Format(CultureInfo.InvariantCulture, item, ServiceBusEventListener.GetObjectAgsWithoutActivity(eventDescriptor.Payload));
					xmlWriter.WriteAttributeString("Msg", SecurityElement.Escape(str));
					xmlWriter.WriteEndElement();
				}
			}
			string str1 = stringBuilder.ToString();
			if (str1.Length > 31744)
			{
				string str2 = str1.Substring(0, 31744 - "#TRUNCATED#\" />".Length);
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] objArray = new object[] { str2, "#TRUNCATED#\" />" };
				str1 = string.Format(invariantCulture, "{0}{1}", objArray);
			}
			return str1;
		}

		private static void EnsureInitialized()
		{
			if (!ServiceBusEventListener.initialized)
			{
				ServiceBusEventListener.InitializeCore();
			}
		}

		private static ServiceBusEventListener.TraceEventChannel GetEventChannel(EventWrittenEventArgs eventDescriptor)
		{
			ServiceBusEventListener.TraceEventChannel traceEventChannel = ServiceBusEventListener.TraceEventChannel.Debug;
			switch (eventDescriptor.Channel)
			{
				case 16:
				{
					traceEventChannel = ServiceBusEventListener.TraceEventChannel.Admin;
					break;
				}
				case 17:
				{
					traceEventChannel = ServiceBusEventListener.TraceEventChannel.Operational;
					break;
				}
				case 18:
				{
					traceEventChannel = ServiceBusEventListener.TraceEventChannel.Analytic;
					break;
				}
				case EventChannel.Application | EventChannel.Security | EventChannel.Setup:
				{
					traceEventChannel = ServiceBusEventListener.TraceEventChannel.Debug;
					break;
				}
			}
			return traceEventChannel;
		}

		private static TraceEventType GetEventLevel(EventWrittenEventArgs eventDescriptor)
		{
			TraceEventType traceEventType = TraceEventType.Verbose;
			switch (eventDescriptor.Level)
			{
				case EventLevel.LogAlways:
				{
					traceEventType = TraceEventType.Verbose;
					break;
				}
				case EventLevel.Critical:
				{
					traceEventType = TraceEventType.Critical;
					break;
				}
				case EventLevel.Error:
				{
					traceEventType = TraceEventType.Error;
					break;
				}
				case EventLevel.Warning:
				{
					traceEventType = TraceEventType.Warning;
					break;
				}
				case EventLevel.Informational:
				{
					traceEventType = TraceEventType.Information;
					break;
				}
				case EventLevel.Verbose:
				{
					traceEventType = TraceEventType.Verbose;
					break;
				}
			}
			return traceEventType;
		}

		private static IDictionary<int, string> GetEventMessageDictionary(Type eventSourceType)
		{
			Dictionary<int, string> nums = new Dictionary<int, string>();
			MethodInfo[] methods = eventSourceType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			EventSourceAttribute customAttribute = (EventSourceAttribute)Attribute.GetCustomAttribute(eventSourceType, typeof(EventSourceAttribute), false);
			ResourceManager resourceManager = new ResourceManager(customAttribute.LocalizationResources, eventSourceType.Assembly);
			for (int i = 0; i < (int)methods.Length; i++)
			{
				MethodInfo methodInfo = methods[i];
				methodInfo.GetParameters();
				EventAttribute eventAttribute = (EventAttribute)Attribute.GetCustomAttribute(methodInfo, typeof(EventAttribute), false);
				if (eventAttribute != null)
				{
					string str = methodInfo.Name.Replace("EventWrite", string.Empty);
					nums[eventAttribute.EventId] = resourceManager.GetString(string.Concat("event_", str));
				}
			}
			return nums;
		}

		private static object[] GetObjectAgsWithoutActivity(IEnumerable<object> readOnlyCollection)
		{
			return (
				from e in readOnlyCollection
				where !(e is EventTraceActivity)
				select e).ToArray<object>();
		}

		private static void InitializeCore()
		{
			lock (ServiceBusEventListener.initializeLock)
			{
				if (!ServiceBusEventListener.initialized)
				{
					string item = ConfigurationManager.AppSettings["Microsoft.ServiceBus.Tracing.UseDiagnosticsTracing"];
					bool flag = false;
					if (item != null && bool.TryParse(item, out flag))
					{
						ServiceBusEventListener.useDiagnosticsTracing = flag;
					}
					ServiceBusEventListener.initialized = true;
				}
			}
		}

		public bool IsEnabled()
		{
			ServiceBusEventListener.EnsureInitialized();
			if (ServiceBusEventListener.useDiagnosticsTracing)
			{
				return true;
			}
			return this.EnableDiagnosticsTracing;
		}

		public bool IsEnabled(byte level, long keywords)
		{
			ServiceBusEventListener.EnsureInitialized();
			if (!this.EnableDiagnosticsTracing)
			{
				return ServiceBusEventListener.useDiagnosticsTracing;
			}
			return true;
		}

		protected internal override void OnEventSourceCreated(EventSource eventSource)
		{
			if (eventSource != null && this.ProviderId == eventSource.Guid)
			{
				this.eventSource = eventSource;
				this.messageDictionary = ServiceBusEventListener.GetEventMessageDictionary(this.eventSource.GetType());
				if (this.IsEnabled())
				{
					base.EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)((long)-1));
				}
			}
			base.OnEventSourceCreated(eventSource);
		}

		protected internal override void OnEventWritten(EventWrittenEventArgs eventData)
		{
			if (eventData.EventId == ServiceBusEventListener.manifestRequestDescriptor.EventId)
			{
				return;
			}
			this.WriteEvent(eventData);
		}

		public void WriteEvent(EventWrittenEventArgs eventDescriptor)
		{
			ServiceBusEventListener.EnsureInitialized();
			if (ServiceBusEventListener.useDiagnosticsTracing)
			{
				this.WriteToDiagnosticsTrace(eventDescriptor, true);
				return;
			}
			if (this.EnableDiagnosticsTracing)
			{
				this.WriteToDiagnosticsTrace(eventDescriptor, false);
			}
		}

		private bool WriteToDiagnosticsTrace(EventWrittenEventArgs eventDescriptor, bool writeToConfigSource)
		{
			TraceEventType eventLevel = ServiceBusEventListener.GetEventLevel(eventDescriptor);
			ServiceBusEventListener.TraceEventChannel eventChannel = ServiceBusEventListener.GetEventChannel(eventDescriptor);
			if (writeToConfigSource && (eventChannel == ServiceBusEventListener.TraceEventChannel.Operational || eventChannel == ServiceBusEventListener.TraceEventChannel.Admin))
			{
				string xmlFormattedString = this.ConvertMessageToXmlFormattedString(eventDescriptor);
				ServiceBusEventListener.traceSource.TraceEvent(eventLevel, eventDescriptor.EventId, xmlFormattedString);
			}
			if (this.EnableDiagnosticsTracing)
			{
				string str = this.ConvertMessageToXmlFormattedString(eventDescriptor);
				if (eventLevel == TraceEventType.Error || eventLevel == TraceEventType.Critical)
				{
					Trace.TraceError(str);
				}
				else if (eventLevel != TraceEventType.Warning)
				{
					Trace.TraceInformation(str);
				}
				else
				{
					Trace.TraceWarning(str);
				}
			}
			return true;
		}

		private enum TraceEventChannel
		{
			Admin = 1,
			Operational = 2,
			Analytic = 3,
			Debug = 4
		}
	}
}