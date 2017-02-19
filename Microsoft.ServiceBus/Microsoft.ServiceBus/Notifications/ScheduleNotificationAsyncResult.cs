using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Notifications
{
	internal sealed class ScheduleNotificationAsyncResult : NotificationRequestAsyncResult<ScheduleNotificationAsyncResult>
	{
		private readonly Notification notification;

		private readonly string tagExpression;

		private readonly DateTimeOffset scheduledTime;

		public ScheduledNotification Result
		{
			get;
			private set;
		}

		public ScheduleNotificationAsyncResult(NotificationHubManager manager, Notification notification, DateTimeOffset scheduledTime, string tagExpression, AsyncCallback callback, object state) : base(manager, true, callback, state)
		{
			this.notification = notification;
			this.tagExpression = tagExpression;
			this.scheduledTime = scheduledTime;
		}

		private string GetScheduledNotificationId(string locationHeaderValue)
		{
			char[] chrArray = new char[] { '/' };
			int num = locationHeaderValue.Trim(chrArray).LastIndexOf('/');
			string str = locationHeaderValue.Substring(0, locationHeaderValue.LastIndexOf('?'));
			char[] chrArray1 = new char[] { '?' };
			return str.Trim(chrArray1).Substring(num + 1);
		}

		protected override void PrepareRequest()
		{
			UriBuilder uriBuilder = new UriBuilder(base.Manager.baseUri)
			{
				Scheme = Uri.UriSchemeHttps
			};
			UriBuilder relayHttpsPort = uriBuilder;
			MessagingUtilities.EnsureTrailingSlash(relayHttpsPort);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] path = new object[] { relayHttpsPort.Path, base.Manager.notificationHubPath };
			relayHttpsPort.Path = string.Format(invariantCulture, "{0}{1}/schedulednotifications", path);
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
			base.Request.Method = "POST";
			base.Request.ContentType = this.notification.ContentType;
			foreach (KeyValuePair<string, string> header in this.notification.Headers)
			{
				base.Request.Headers.Add(header.Key, header.Value);
			}
			if (!string.IsNullOrWhiteSpace(this.tagExpression))
			{
				base.Request.Headers["ServiceBusNotification-Tags"] = this.tagExpression;
			}
			WebHeaderCollection headers = base.Request.Headers;
			DateTime utcDateTime = this.scheduledTime.UtcDateTime;
			headers["ServiceBusNotification-ScheduleTime"] = utcDateTime.ToString("s", CultureInfo.InvariantCulture);
			base.Request.SetUserAgentHeader();
			base.Request.AddTrackingIdHeader(base.TrackingContext);
			base.Request.AddAuthorizationHeader(base.Manager.tokenProvider, uri, "Send");
		}

		protected override void ProcessResponse()
		{
			try
			{
				string responseHeader = base.Response.GetResponseHeader("Location");
				string scheduledNotificationId = this.GetScheduledNotificationId(responseHeader);
				ScheduledNotification scheduledNotification = new ScheduledNotification()
				{
					ScheduledNotificationId = scheduledNotificationId,
					Tags = this.tagExpression,
					ScheduledTime = this.scheduledTime,
					Payload = this.notification,
					TrackingId = base.TrackingContext.TrackingId
				};
				this.Result = scheduledNotification;
			}
			catch (WebException webException1)
			{
				WebException webException = webException1;
				throw ServiceBusResourceOperations.ConvertWebException(base.TrackingContext, webException, base.Request.Timeout, false);
			}
		}

		protected override void WriteToStream()
		{
			if (string.IsNullOrEmpty(this.notification.Body))
			{
				base.Request.ContentLength = (long)0;
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(base.RequestStream, Encoding.UTF8))
				{
					streamWriter.Write(this.notification.Body);
				}
			}
		}
	}
}