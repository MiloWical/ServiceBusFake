using System;

namespace Microsoft.ServiceBus.Notifications
{
	public sealed class GcmNotification : Notification
	{
		protected override string PlatformType
		{
			get
			{
				return "gcm";
			}
		}

		public GcmNotification(string jsonPayload) : base(null, null)
		{
			if (string.IsNullOrWhiteSpace(jsonPayload))
			{
				throw new ArgumentNullException("jsonPayload");
			}
			base.Body = jsonPayload;
		}

		[Obsolete("This method is obsolete.")]
		public GcmNotification(string jsonPayload, string tag) : base(null, tag)
		{
			if (string.IsNullOrWhiteSpace(jsonPayload))
			{
				throw new ArgumentNullException("jsonPayload");
			}
			base.Body = jsonPayload;
		}

		protected override void OnValidateAndPopulateHeaders()
		{
		}
	}
}