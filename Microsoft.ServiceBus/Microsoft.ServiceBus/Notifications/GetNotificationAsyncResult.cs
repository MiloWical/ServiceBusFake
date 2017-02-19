using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;

namespace Microsoft.ServiceBus.Notifications
{
	internal sealed class GetNotificationAsyncResult : NotificationRequestAsyncResult<GetNotificationAsyncResult>
	{
		private readonly string notificationId;

		public NotificationDetails Result
		{
			get;
			private set;
		}

		public GetNotificationAsyncResult(NotificationHubManager manager, string notificationId, AsyncCallback callback, object state) : base(manager, false, callback, state)
		{
			this.notificationId = notificationId;
			base.Start();
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
			object[] path = new object[] { relayHttpsPort.Path, base.Manager.notificationHubPath, this.notificationId };
			relayHttpsPort.Path = string.Format(invariantCulture, "{0}{1}/messages/{2}", path);
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
			base.Request.Method = "GET";
			base.Request.SetUserAgentHeader();
			base.Request.AddTrackingIdHeader(base.TrackingContext);
			base.Request.AddAuthorizationHeader(base.Manager.tokenProvider, uri, "Manage");
		}

		protected override void ProcessResponse()
		{
			try
			{
				Stream responseStream = base.Response.GetResponseStream();
				XmlReaderSettings xmlReaderSetting = new XmlReaderSettings()
				{
					CloseInput = true
				};
				using (XmlReader xmlReader = XmlReader.Create(responseStream, xmlReaderSetting))
				{
					DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(NotificationDetails));
					this.Result = (NotificationDetails)dataContractSerializer.ReadObject(xmlReader);
				}
			}
			catch (WebException webException1)
			{
				WebException webException = webException1;
				base.Complete(ServiceBusResourceOperations.ConvertWebException(base.TrackingContext, webException, base.Request.Timeout, false));
			}
		}

		protected override void WriteToStream()
		{
		}
	}
}