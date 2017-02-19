using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Xml;

namespace Microsoft.ServiceBus.Tracing
{
	public sealed class TrackingContext
	{
		internal const string MessagingSubsystemString = "MessagingGatewaySubsystem";

		internal const string MessagingBrokerSubsystemString = "MessagingBrokerSubsystem";

		internal const string NoTrackingId = "NoTrackingId";

		internal const string NoSystemTracker = "NoSystemTracker";

		public const string TrackingIdName = "TrackingId";

		public const string SystemTrackerName = "SystemTracker";

		public const string HeaderNamespace = "http://schemas.microsoft.com/servicebus/2010/08/protocol/";

		private const string AppendRoleFormat = "_";

		private const string MessageIdHeaderName = "MessageId";

		internal readonly static EventTraceActivity NoActivity;

		private static char AppendRolePrefix;

		private readonly static Dictionary<string, Guid> ComponentTrackingGuids;

		private readonly string trackingId;

		private readonly string systemTracker;

		private EventTraceActivity eventTraceActivity;

		internal EventTraceActivity Activity
		{
			get
			{
				if (this.eventTraceActivity == null)
				{
					this.eventTraceActivity = TrackingContext.GetActivity(this.trackingId);
				}
				return this.eventTraceActivity;
			}
		}

		internal static string RoleIdentifier
		{
			get;
			private set;
		}

		internal string SystemTracker
		{
			get
			{
				if (string.IsNullOrEmpty(this.systemTracker))
				{
					return "NoSystemTracker";
				}
				return this.systemTracker;
			}
		}

		internal string TrackingId
		{
			get
			{
				if (string.IsNullOrEmpty(this.trackingId))
				{
					return "NoTrackingId";
				}
				return this.trackingId;
			}
		}

		static TrackingContext()
		{
			TrackingContext.NoActivity = EventTraceActivity.Empty;
			Dictionary<string, Guid> strs = new Dictionary<string, Guid>()
			{
				{ "MessagingGatewaySubsystem", new Guid("{84D15115-E422-4549-8A2D-81691426B744}") },
				{ "MessagingBrokerSubsystem", new Guid("{857063DF-F590-4182-932A-667A230ECF98}") }
			};
			TrackingContext.ComponentTrackingGuids = strs;
		}

		private TrackingContext(string trackingId, string systemTracker)
		{
			this.trackingId = trackingId;
			this.systemTracker = systemTracker;
		}

		internal static string AppendRoleInstanceInformationToTrackingId(string trackingId)
		{
			if (string.IsNullOrEmpty(TrackingContext.RoleIdentifier))
			{
				return trackingId;
			}
			object[] objArray = new object[] { trackingId, "_", TrackingContext.AppendRolePrefix, TrackingContext.RoleIdentifier };
			return string.Concat(objArray);
		}

		internal string CreateClientTrackingExceptionInfo()
		{
			DateTime utcNow = DateTime.UtcNow;
			CultureInfo currentCulture = CultureInfo.CurrentCulture;
			object[] trackingId = new object[] { this.TrackingId, utcNow };
			return string.Format(currentCulture, "TrackingId:{0},TimeStamp:{1}", trackingId);
		}

		internal static EventTraceActivity GetActivity(string id)
		{
			Guid guid;
			EventTraceActivity eventTraceActivity = null;
			if (!string.IsNullOrEmpty(id))
			{
				int num = id.IndexOf('\u005F');
				if (num > 0 && Guid.TryParse(id.Substring(0, num), out guid) || Guid.TryParse(id, out guid))
				{
					eventTraceActivity = new EventTraceActivity(guid);
				}
			}
			return eventTraceActivity ?? new EventTraceActivity();
		}

		internal static TrackingContext GetInstance(Guid guidTrackingId, string overrideSystemTracker)
		{
			string str;
			str = (!string.IsNullOrEmpty(overrideSystemTracker) ? overrideSystemTracker : string.Empty);
			string trackingId = TrackingContext.AppendRoleInstanceInformationToTrackingId(guidTrackingId.ToString());
			return new TrackingContext(trackingId, str);
		}

		internal static TrackingContext GetInstance(Guid guidTrackingId)
		{
			return TrackingContext.GetInstance(guidTrackingId, null);
		}

		internal static TrackingContext GetInstance(string stringTrackingId, string overrideSystemTracker, bool embedRoleInformation)
		{
			string str;
			str = (!string.IsNullOrEmpty(overrideSystemTracker) ? overrideSystemTracker : string.Empty);
			string trackingId = stringTrackingId;
			if (embedRoleInformation)
			{
				trackingId = TrackingContext.AppendRoleInstanceInformationToTrackingId(stringTrackingId);
			}
			return new TrackingContext(trackingId, str);
		}

		internal static TrackingContext GetInstance(string stringTrackingId, bool embedRoleInformation)
		{
			return TrackingContext.GetInstance(stringTrackingId, null, embedRoleInformation);
		}

		internal static TrackingContext GetInstance(Message message, string overrideSystemTracker, bool embedRoleInformation, WebOperationContext webOperationContext = null)
		{
			string str;
			MessageProperties properties = message.Properties;
			MessageHeaders headers = message.Headers;
			string trackingId = TrackingContext.GetTrackingId(properties, headers, webOperationContext);
			str = (!string.IsNullOrEmpty(overrideSystemTracker) ? overrideSystemTracker : TrackingContext.GetSystemTracker(properties, headers));
			string trackingId1 = trackingId;
			if (embedRoleInformation)
			{
				trackingId1 = TrackingContext.AppendRoleInstanceInformationToTrackingId(trackingId);
			}
			return new TrackingContext(trackingId1, str);
		}

		internal static TrackingContext GetInstance(IDictionary<string, object> messageProperties, IDictionary<string, string> messageHeaders)
		{
			string trackingId = TrackingContext.GetTrackingId(messageProperties, messageHeaders);
			return new TrackingContext(trackingId, TrackingContext.GetSystemTracker(messageProperties, messageHeaders));
		}

		internal static TrackingContext GetInstance(Message message, WebOperationContext webOperationContext, bool embedRoleInformation)
		{
			return TrackingContext.GetInstance(message, string.Empty, embedRoleInformation, webOperationContext);
		}

		internal static TrackingContext GetInstance(Message message, bool embedRoleInformation)
		{
			return TrackingContext.GetInstance(message, string.Empty, embedRoleInformation, null);
		}

		internal static TrackingContext GetInstanceFromKey(string key, string overrideSystemTracker)
		{
			Guid guid;
			string str;
			if (!TrackingContext.ComponentTrackingGuids.TryGetValue(key, out guid))
			{
				guid = Guid.NewGuid();
			}
			str = (!string.IsNullOrEmpty(overrideSystemTracker) ? overrideSystemTracker : string.Empty);
			return TrackingContext.GetInstance(guid, str);
		}

		internal static TrackingContext GetInstanceFromKey(string key)
		{
			Guid guid;
			if (!TrackingContext.ComponentTrackingGuids.TryGetValue(key, out guid))
			{
				guid = Guid.NewGuid();
			}
			return TrackingContext.GetInstance(guid, null);
		}

		internal static string GetRoleInstanceInformation()
		{
			if (string.IsNullOrEmpty(TrackingContext.RoleIdentifier))
			{
				return string.Empty;
			}
			return string.Concat("_", TrackingContext.AppendRolePrefix, TrackingContext.RoleIdentifier);
		}

		private static string GetSystemTracker(MessageProperties messageProperties, MessageHeaders messageHeaders)
		{
			string tracker;
			SystemTrackerMessageProperty systemTrackerMessageProperty;
			SystemTrackerHeader systemTrackerHeader;
			if (!SystemTrackerMessageProperty.TryGet<SystemTrackerMessageProperty>(messageProperties, out systemTrackerMessageProperty))
			{
				tracker = (!SystemTrackerHeader.TryRead(messageHeaders, out systemTrackerHeader) ? string.Empty : systemTrackerHeader.Tracker);
			}
			else
			{
				tracker = systemTrackerMessageProperty.Tracker;
			}
			return tracker;
		}

		private static string GetSystemTracker(IDictionary<string, object> messageProperties, IDictionary<string, string> messageHeaders)
		{
			string tracker;
			SystemTrackerMessageProperty systemTrackerMessageProperty;
			string str;
			if (!SystemTrackerMessageProperty.TryGet<SystemTrackerMessageProperty>(messageProperties, out systemTrackerMessageProperty))
			{
				tracker = (!TrackingContext.TryGetHeader(messageHeaders, "SystemTracker", out str) ? string.Empty : str);
			}
			else
			{
				tracker = systemTrackerMessageProperty.Tracker;
			}
			return tracker;
		}

		internal static string GetTrackingId(MessageProperties messageProperties, MessageHeaders messageHeaders, WebOperationContext webOperationContext)
		{
			string id;
			TrackingIdMessageProperty trackingIdMessageProperty;
			TrackingIdHeader trackingIdHeader;
			if (!TrackingIdMessageProperty.TryGet<TrackingIdMessageProperty>(messageProperties, out trackingIdMessageProperty))
			{
				string str = null;
				if (webOperationContext != null)
				{
					str = webOperationContext.IncomingRequest.Headers.Get("TrackingId");
				}
				if (TrackingIdHeader.TryRead(messageHeaders, out trackingIdHeader))
				{
					id = trackingIdHeader.Id;
				}
				else if (messageHeaders.RelatesTo != null)
				{
					Guid trackingId = TrackingContext.GetTrackingId(messageHeaders.RelatesTo);
					id = TrackingContext.AppendRoleInstanceInformationToTrackingId(trackingId.ToString());
				}
				else if (messageHeaders.MessageId != null)
				{
					Guid guid = TrackingContext.GetTrackingId(messageHeaders.MessageId);
					id = TrackingContext.AppendRoleInstanceInformationToTrackingId(guid.ToString());
				}
				else if (string.IsNullOrEmpty(str))
				{
					Guid guid1 = Guid.NewGuid();
					id = TrackingContext.AppendRoleInstanceInformationToTrackingId(guid1.ToString());
				}
				else
				{
					id = TrackingContext.AppendRoleInstanceInformationToTrackingId(str);
				}
				TrackingIdMessageProperty.TryAdd(messageProperties, id);
			}
			else
			{
				id = trackingIdMessageProperty.Id;
			}
			return id;
		}

		private static Guid GetTrackingId(UniqueId uniqueId)
		{
			Guid guid;
			if (!uniqueId.TryGetGuid(out guid))
			{
				return Guid.Empty;
			}
			return guid;
		}

		private static string GetTrackingId(IDictionary<string, object> requestProperties, IDictionary<string, string> requestHeaders)
		{
			TrackingIdMessageProperty trackingIdMessageProperty;
			string trackingId;
			if (!TrackingIdMessageProperty.TryGet<TrackingIdMessageProperty>(requestProperties, out trackingIdMessageProperty))
			{
				if (!TrackingContext.TryGetHeader(requestHeaders, "TrackingId", out trackingId))
				{
					if (!requestHeaders.ContainsKey("MessageId"))
					{
						Guid guid = Guid.NewGuid();
						trackingId = TrackingContext.AppendRoleInstanceInformationToTrackingId(guid.ToString());
						TrackingIdMessageProperty.TryAdd(requestProperties, trackingId);
					}
					else
					{
						UniqueId uniqueId = new UniqueId(requestHeaders["MessageId"]);
						Guid trackingId1 = TrackingContext.GetTrackingId(uniqueId);
						trackingId = TrackingContext.AppendRoleInstanceInformationToTrackingId(trackingId1.ToString());
					}
				}
				TrackingIdMessageProperty.TryAdd(requestProperties, trackingId);
			}
			else
			{
				trackingId = trackingIdMessageProperty.Id;
			}
			return trackingId;
		}

		internal static void SetTrackingContextRoleIdentifier(string roleIdentifier, TrackingContext.RolePrefix rolePrefix)
		{
			if (string.IsNullOrEmpty(TrackingContext.RoleIdentifier))
			{
				TrackingContext.RoleIdentifier = roleIdentifier;
				TrackingContext.AppendRolePrefix = (char)rolePrefix;
			}
		}

		private static bool TryGetHeader(IDictionary<string, string> headersDictionary, string header, out string value)
		{
			value = null;
			if (headersDictionary != null && headersDictionary.ContainsKey(header))
			{
				value = headersDictionary[header];
			}
			return value != null;
		}

		internal enum RolePrefix
		{
			Admin = 65,
			Broker = 66,
			Gateway = 71,
			GeoMaster = 77,
			Push = 80,
			RPGateway = 82
		}
	}
}