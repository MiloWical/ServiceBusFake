using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Globalization;
using System.Net;

namespace Microsoft.ServiceBus.Notifications
{
	internal sealed class CancelScheduledNotificationAsyncResult : NotificationRequestAsyncResult<CancelScheduledNotificationAsyncResult>
	{
		private readonly string scheduledNotificationId;

		public CancelScheduledNotificationAsyncResult(NotificationHubManager manager, string scheduledNotificationId, AsyncCallback callback, object state) : base(manager, true, callback, state)
		{
			this.scheduledNotificationId = scheduledNotificationId;
		}

		protected override void PrepareRequest()
		{
			UriBuilder uriBuilder = new UriBuilder(base.Manager.baseUri)
			{
				Scheme = Uri.UriSchemeHttps
			};
			UriBuilder relayHttpsPort = uriBuilder;
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] path = new object[] { relayHttpsPort.Path, base.Manager.notificationHubPath, this.scheduledNotificationId };
			relayHttpsPort.Path = string.Format(invariantCulture, "{0}{1}/schedulednotifications/{2}", path);
			CultureInfo cultureInfo = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { "api-version", "2014-09" };
			relayHttpsPort.Query = string.Format(cultureInfo, "{0}={1}", objArray);
			if (relayHttpsPort.Port == -1)
			{
				relayHttpsPort.Port = RelayEnvironment.RelayHttpsPort;
			}
			Uri uri = relayHttpsPort.Uri;
			base.Request = (HttpWebRequest)WebRequest.Create(uri);
			base.Request.ServicePoint.MaxIdleTime = Constants.ServicePointMaxIdleTimeMilliSeconds;
			base.Request.Method = "DELETE";
			base.Request.SetUserAgentHeader();
			base.Request.AddTrackingIdHeader(base.TrackingContext);
			base.Request.AddAuthorizationHeader(base.Manager.tokenProvider, uri, "Send");
		}

		protected override void ProcessResponse()
		{
		}

		protected override void WriteToStream()
		{
		}
	}
}