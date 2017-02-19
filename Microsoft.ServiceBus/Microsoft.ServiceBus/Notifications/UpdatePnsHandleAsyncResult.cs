using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Syndication;
using System.Xml;

namespace Microsoft.ServiceBus.Notifications
{
	internal sealed class UpdatePnsHandleAsyncResult : NotificationRequestAsyncResult<UpdatePnsHandleAsyncResult>
	{
		private UpdatePnsHandlePayload payload;

		public IEnumerable<RegistrationDescription> UpdatedRegistrations
		{
			get;
			private set;
		}

		public UpdatePnsHandleAsyncResult(NotificationHubManager manager, string originalPnsHandle, string newPnsHandle, AsyncCallback callback, object state) : base(manager, true, callback, state)
		{
			UpdatePnsHandlePayload updatePnsHandlePayload = new UpdatePnsHandlePayload()
			{
				OriginalPnsHandle = originalPnsHandle,
				NewPnsHandle = newPnsHandle
			};
			this.payload = updatePnsHandlePayload;
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
			relayHttpsPort.Path = string.Format(invariantCulture, "{0}{1}/registrations/updatepnshandle", path);
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
			base.Request.ContentType = "Application/json";
			base.Request.SetUserAgentHeader();
			base.Request.AddTrackingIdHeader(base.TrackingContext);
			base.Request.AddAuthorizationHeader(base.Manager.tokenProvider, uri, "Send");
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
					SyndicationFeed syndicationFeed = SyndicationFeed.Load<SyndicationFeed>(xmlReader);
					if (syndicationFeed != null)
					{
						this.UpdatedRegistrations = (new NamespaceManager.RegistrationSyndicationFeed(syndicationFeed)).Registrations;
					}
					else
					{
						this.UpdatedRegistrations = new RegistrationDescription[0];
					}
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
			DataContractJsonSerializer dataContractJsonSerializer = new DataContractJsonSerializer(this.payload.GetType());
			dataContractJsonSerializer.WriteObject(base.RequestStream, this.payload);
		}
	}
}