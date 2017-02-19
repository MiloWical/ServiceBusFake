using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Tracing
{
	internal class SystemTrackerMessageProperty
	{
		public string Tracker
		{
			get;
			private set;
		}

		public SystemTrackerMessageProperty() : this(string.Empty)
		{
		}

		public SystemTrackerMessageProperty(string tracker)
		{
			this.Tracker = tracker;
		}

		public static SystemTrackerMessageProperty Read(IDictionary<string, object> messageProperties)
		{
			SystemTrackerMessageProperty systemTrackerMessageProperty;
			if (!SystemTrackerMessageProperty.TryGet<SystemTrackerMessageProperty>(messageProperties, out systemTrackerMessageProperty))
			{
				throw new ArgumentException(SRClient.SystemTrackerPropertyMissing, "messageProperties");
			}
			return systemTrackerMessageProperty;
		}

		public static bool Remove(IDictionary<string, object> messageProperties)
		{
			if (messageProperties == null)
			{
				return false;
			}
			return messageProperties.Remove("SystemTracker");
		}

		public static bool TryAdd(IDictionary<string, object> messageProperties, string systemTracker)
		{
			SystemTrackerMessageProperty systemTrackerMessageProperty;
			if (messageProperties == null || SystemTrackerMessageProperty.TryGet<SystemTrackerMessageProperty>(messageProperties, out systemTrackerMessageProperty))
			{
				return false;
			}
			messageProperties.Add("SystemTracker", new SystemTrackerMessageProperty(systemTracker));
			return true;
		}

		public static bool TryGet<T>(IDictionary<string, object> messageProperties, out T property)
		where T : class
		{
			object obj;
			property = default(T);
			if (messageProperties != null)
			{
				if (!messageProperties.TryGetValue("SystemTracker", out obj))
				{
					return false;
				}
				property = (T)(obj as T);
			}
			return property != null;
		}
	}
}