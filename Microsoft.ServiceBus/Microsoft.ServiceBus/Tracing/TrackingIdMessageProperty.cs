using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Tracing
{
	internal class TrackingIdMessageProperty
	{
		public string Id
		{
			get;
			private set;
		}

		public TrackingIdMessageProperty() : this(Guid.NewGuid().ToString())
		{
		}

		public TrackingIdMessageProperty(string trackingId)
		{
			this.Id = trackingId;
		}

		public static TrackingIdMessageProperty Read(IDictionary<string, object> messageProperties)
		{
			TrackingIdMessageProperty trackingIdMessageProperty;
			if (!TrackingIdMessageProperty.TryGet<TrackingIdMessageProperty>(messageProperties, out trackingIdMessageProperty))
			{
				throw new ArgumentException(SRClient.TrackingIDPropertyMissing, "messageProperties");
			}
			return trackingIdMessageProperty;
		}

		public static bool Remove(IDictionary<string, object> messageProperties)
		{
			if (messageProperties == null)
			{
				return false;
			}
			return messageProperties.Remove("TrackingId");
		}

		public static bool TryAdd(IDictionary<string, object> messageProperties, string trackingId)
		{
			TrackingIdMessageProperty trackingIdMessageProperty;
			if (messageProperties == null || TrackingIdMessageProperty.TryGet<TrackingIdMessageProperty>(messageProperties, out trackingIdMessageProperty))
			{
				return false;
			}
			messageProperties.Add("TrackingId", new TrackingIdMessageProperty(trackingId));
			return true;
		}

		public static bool TryGet<T>(IDictionary<string, object> messageProperties, out T property)
		where T : class
		{
			object obj;
			property = default(T);
			if (messageProperties != null)
			{
				if (!messageProperties.TryGetValue("TrackingId", out obj))
				{
					return false;
				}
				property = (T)(obj as T);
			}
			return property != null;
		}
	}
}