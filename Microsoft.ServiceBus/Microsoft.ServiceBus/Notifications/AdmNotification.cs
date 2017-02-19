using System;

namespace Microsoft.ServiceBus.Notifications
{
	public sealed class AdmNotification : Notification
	{
		protected override string PlatformType
		{
			get
			{
				return "adm";
			}
		}

		public AdmNotification(string jsonPayload) : base(null, null)
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