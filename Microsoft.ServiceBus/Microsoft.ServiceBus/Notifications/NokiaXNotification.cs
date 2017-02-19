using System;

namespace Microsoft.ServiceBus.Notifications
{
	internal sealed class NokiaXNotification : Notification
	{
		protected override string PlatformType
		{
			get
			{
				return "nokiax";
			}
		}

		public NokiaXNotification(string jsonPayload) : base(null, null)
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