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
	internal sealed class GetRegistrationCountsAsyncResult : NotificationRequestAsyncResult<GetRegistrationCountsAsyncResult>
	{
		private readonly string tag;

		public RegistrationCounts Result
		{
			get;
			private set;
		}

		public GetRegistrationCountsAsyncResult(NotificationHubManager manager, string tag, AsyncCallback callback, object state) : base(manager, false, callback, state)
		{
			this.tag = tag;
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
			object[] path = new object[] { relayHttpsPort.Path, base.Manager.notificationHubPath };
			relayHttpsPort.Path = string.Format(invariantCulture, "{0}{1}/registrations/counts", path);
			if (string.IsNullOrWhiteSpace(this.tag))
			{
				CultureInfo cultureInfo = CultureInfo.InvariantCulture;
				object[] objArray = new object[] { "api-version", "2014-09" };
				relayHttpsPort.Query = string.Format(cultureInfo, "{0}={1}", objArray);
			}
			else
			{
				CultureInfo invariantCulture1 = CultureInfo.InvariantCulture;
				object[] objArray1 = new object[] { "api-version", "2014-09", this.tag };
				relayHttpsPort.Query = string.Format(invariantCulture1, "{0}={1}&tags={2}", objArray1);
			}
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
					DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(RegistrationCounts));
					this.Result = (RegistrationCounts)dataContractSerializer.ReadObject(xmlReader);
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