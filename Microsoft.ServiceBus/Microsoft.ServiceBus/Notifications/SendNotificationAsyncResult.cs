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
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Microsoft.ServiceBus.Notifications
{
	internal sealed class SendNotificationAsyncResult : NotificationRequestAsyncResult<SendNotificationAsyncResult>
	{
		private readonly Notification notification;

		private readonly bool testSend;

		private readonly string tagExpression;

		public NotificationOutcome Result
		{
			get;
			private set;
		}

		public SendNotificationAsyncResult(NotificationHubManager manager, Notification notification, bool testSend, string tagExpression, AsyncCallback callback, object state) : base(manager, true, callback, state)
		{
			this.notification = notification;
			this.testSend = testSend;
			this.tagExpression = tagExpression;
		}

		private string GetNotificationIdFromResponse()
		{
			string responseHeader = base.Response.GetResponseHeader("Location");
			if (!string.IsNullOrEmpty(responseHeader))
			{
				char[] chrArray = new char[] { '/' };
				Uri uri = new Uri(responseHeader.Trim(chrArray));
				if ((int)uri.Segments.Length > 0)
				{
					return uri.Segments[(int)uri.Segments.Length - 1];
				}
			}
			return string.Empty;
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
			relayHttpsPort.Path = string.Format(invariantCulture, "{0}{1}/messages", path);
			string str = (this.testSend ? "test&{0}={1}" : "{0}={1}");
			CultureInfo cultureInfo = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { "api-version", "2014-09" };
			relayHttpsPort.Query = string.Format(cultureInfo, str, objArray);
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
			base.Request.SetUserAgentHeader();
			base.Request.AddTrackingIdHeader(base.TrackingContext);
			base.Request.AddAuthorizationHeader(base.Manager.tokenProvider, uri, "Send");
		}

		protected override void ProcessResponse()
		{
			try
			{
				if (!this.testSend)
				{
					NotificationOutcome notificationOutcome = new NotificationOutcome()
					{
						State = NotificationOutcomeState.Enqueued,
						TrackingId = base.TrackingContext.TrackingId
					};
					this.Result = notificationOutcome;
				}
				else
				{
					Stream responseStream = base.Response.GetResponseStream();
					XmlReaderSettings xmlReaderSetting = new XmlReaderSettings()
					{
						CloseInput = true
					};
					using (XmlReader xmlReader = XmlReader.Create(responseStream, xmlReaderSetting))
					{
						DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(NotificationOutcome));
						this.Result = (NotificationOutcome)dataContractSerializer.ReadObject(xmlReader);
						this.Result.State = NotificationOutcomeState.DetailedStateAvailable;
						this.Result.TrackingId = base.TrackingContext.TrackingId;
					}
				}
			}
			catch (XmlException xmlException)
			{
				throw new MessagingException(SRClient.InvalidXmlFormat, xmlException);
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