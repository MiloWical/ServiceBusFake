using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Notifications
{
	public sealed class AppleNotification : Notification
	{
		public DateTime? Expiry
		{
			get;
			set;
		}

		protected override string PlatformType
		{
			get
			{
				return "apple";
			}
		}

		public AppleNotification(string jsonPayload) : this(jsonPayload, null)
		{
		}

		[Obsolete("This method is obsolete.")]
		public AppleNotification(string jsonPayload, string tag) : this(jsonPayload, null, tag)
		{
		}

		public AppleNotification(string jsonPayload, DateTime? expiry) : base(null, null)
		{
			if (string.IsNullOrWhiteSpace(jsonPayload))
			{
				throw new ArgumentNullException("jsonPayload");
			}
			this.Expiry = expiry;
			base.Body = jsonPayload;
		}

		[Obsolete("This method is obsolete.")]
		public AppleNotification(string jsonPayload, DateTime? expiry, string tag) : base(null, tag)
		{
			if (string.IsNullOrWhiteSpace(jsonPayload))
			{
				throw new ArgumentNullException("jsonPayload");
			}
			this.Expiry = expiry;
			base.Body = jsonPayload;
		}

		protected override void OnValidateAndPopulateHeaders()
		{
			if (this.Expiry.HasValue)
			{
				DateTime value = this.Expiry.Value;
				base.AddOrUpdateHeader("ServiceBusNotification-Apns-Expiry", value.ToString(CultureInfo.InvariantCulture));
			}
		}
	}
}