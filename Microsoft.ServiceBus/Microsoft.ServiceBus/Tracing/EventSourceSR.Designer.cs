using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Resources;

namespace Microsoft.ServiceBus.Tracing
{
	internal class EventSourceSR
	{
		private static System.Resources.ResourceManager resourceManager;

		private static CultureInfo resourceCulture;

		[GeneratedCode("StrictResXFileCodeGenerator", "4.0.0.0")]
		internal static CultureInfo Culture
		{
			get
			{
				return EventSourceSR.resourceCulture;
			}
			set
			{
				EventSourceSR.resourceCulture = value;
			}
		}

		internal static string Event_IllegalID
		{
			get
			{
				return EventSourceSR.ResourceManager.GetString("Event_IllegalID", EventSourceSR.Culture);
			}
		}

		internal static string Event_IllegalOpcode
		{
			get
			{
				return EventSourceSR.ResourceManager.GetString("Event_IllegalOpcode", EventSourceSR.Culture);
			}
		}

		internal static string Event_ListenerNotFound
		{
			get
			{
				return EventSourceSR.ResourceManager.GetString("Event_ListenerNotFound", EventSourceSR.Culture);
			}
		}

		internal static System.Resources.ResourceManager ResourceManager
		{
			get
			{
				if (object.ReferenceEquals(EventSourceSR.resourceManager, null))
				{
					EventSourceSR.resourceManager = new System.Resources.ResourceManager("Microsoft.ServiceBus.Tracing.EventSourceSR", typeof(EventSourceSR).Assembly);
				}
				return EventSourceSR.resourceManager;
			}
		}

		private EventSourceSR()
		{
		}

		internal static string ArgumentOutOfRange_MaxArgExceeded(object param0)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("ArgumentOutOfRange_MaxArgExceeded", EventSourceSR.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string ArgumentOutOfRange_MaxStringsExceeded(object param0)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("ArgumentOutOfRange_MaxStringsExceeded", EventSourceSR.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string Event_EventNotReturnVoid(object param0)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("Event_EventNotReturnVoid", EventSourceSR.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string Event_FailedWithErrorCode(object param0)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("Event_FailedWithErrorCode", EventSourceSR.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string Event_IllegalEventArg(object param0, object param1, object param2)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("Event_IllegalEventArg", EventSourceSR.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string Event_IllegalField(object param0, object param1)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("Event_IllegalField", EventSourceSR.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string Event_KeywordValue(object param0, object param1)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("Event_KeywordValue", EventSourceSR.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string Event_SourceWithUsedGuid(object param0)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("Event_SourceWithUsedGuid", EventSourceSR.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string Event_UndefinedKeyword(object param0, object param1)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("Event_UndefinedKeyword", EventSourceSR.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string Event_UnsupportType(object param0)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("Event_UnsupportType", EventSourceSR.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string Event_UsedEventID(object param0, object param1)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("Event_UsedEventID", EventSourceSR.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string Event_UsedEventName(object param0)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("Event_UsedEventName", EventSourceSR.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string EventSource_UndefinedChannel(object param0, object param1)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("EventSource_UndefinedChannel", EventSourceSR.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string ProviderGuidNotSpecified(object param0)
		{
			CultureInfo culture = EventSourceSR.Culture;
			string str = EventSourceSR.ResourceManager.GetString("ProviderGuidNotSpecified", EventSourceSR.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}
	}
}