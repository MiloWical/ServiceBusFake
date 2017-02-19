using System;

namespace Microsoft.ServiceBus.Notifications
{
	public sealed class BaiduNotification : Notification
	{
		protected override string PlatformType
		{
			get
			{
				return "baidu";
			}
		}

		public BaiduNotification(string message) : base(null, null)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentNullException("baidu notification message.description");
			}
			base.Body = message;
			base.ContentType = "application/x-www-form-urlencoded";
		}

		protected override void OnValidateAndPopulateHeaders()
		{
		}
	}
}