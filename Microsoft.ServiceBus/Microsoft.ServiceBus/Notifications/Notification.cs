using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Notifications
{
	public abstract class Notification
	{
		internal const string FormatHeaderName = "ServiceBusNotification-Format";

		internal string tag;

		public string Body
		{
			get;
			set;
		}

		public string ContentType
		{
			get;
			set;
		}

		public Dictionary<string, string> Headers
		{
			get;
			set;
		}

		protected abstract string PlatformType
		{
			get;
		}

		[Obsolete("This property is obsolete.")]
		public string Tag
		{
			get
			{
				return this.tag;
			}
			set
			{
				this.tag = value;
			}
		}

		protected Notification(IDictionary<string, string> additionalHeaders, string tag)
		{
			this.Headers = (additionalHeaders != null ? new Dictionary<string, string>(additionalHeaders, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
			this.tag = tag;
			this.ContentType = "application/xml";
		}

		protected void AddOrUpdateHeader(string key, string value)
		{
			if (!this.Headers.ContainsKey(key))
			{
				this.Headers.Add(key, value);
				return;
			}
			this.Headers[key] = value;
		}

		protected abstract void OnValidateAndPopulateHeaders();

		internal void ValidateAndPopulateHeaders()
		{
			this.AddOrUpdateHeader("ServiceBusNotification-Format", this.PlatformType);
			this.OnValidateAndPopulateHeaders();
		}
	}
}