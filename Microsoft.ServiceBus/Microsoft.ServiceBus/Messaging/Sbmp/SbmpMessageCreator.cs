using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels.Security;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.IdentityModel.Tokens;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal class SbmpMessageCreator
	{
		private readonly SbmpMessagingFactory messagingFactory;

		private readonly Uri baseAddress;

		private readonly IServiceBusSecuritySettings serviceBusSecuritySettings;

		private readonly EndpointAddress targetAddress;

		private readonly MessageVersion messageVersion;

		private readonly Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo linkInfo;

		private readonly bool disableClientOperationTimeBuffer;

		public bool DisableClientOperationTimeBuffer
		{
			get
			{
				return this.disableClientOperationTimeBuffer;
			}
		}

		public Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo LinkInfo
		{
			get
			{
				return this.linkInfo;
			}
		}

		public SbmpMessageCreator(SbmpMessagingFactory messagingFactory, Uri baseAddress, MessageVersion messageVersion, IServiceBusSecuritySettings serviceBusSecuritySettings, bool disableClientOperationTimeBuffer, EndpointAddress targetAddress)
		{
			if (baseAddress == null)
			{
				throw Fx.Exception.AsError(new ArgumentNullException("baseAddress"), null);
			}
			this.messagingFactory = messagingFactory;
			this.baseAddress = baseAddress;
			this.messageVersion = messageVersion;
			this.serviceBusSecuritySettings = serviceBusSecuritySettings;
			this.targetAddress = targetAddress;
			this.disableClientOperationTimeBuffer = disableClientOperationTimeBuffer;
		}

		private SbmpMessageCreator(SbmpMessagingFactory messagingFactory, Uri baseAddress, MessageVersion messageVersion, IServiceBusSecuritySettings serviceBusSecuritySettings, bool disableClientOperationTimeBuffer, EndpointAddress targetAddress, Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo linkInfo) : this(messagingFactory, baseAddress, messageVersion, serviceBusSecuritySettings, disableClientOperationTimeBuffer, targetAddress)
		{
			this.linkInfo = linkInfo;
		}

		public SbmpMessageCreator CreateLinkMessageCreator(Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo linkInfo)
		{
			return new SbmpMessageCreator(this.messagingFactory, this.baseAddress, this.messageVersion, this.serviceBusSecuritySettings, this.disableClientOperationTimeBuffer, this.targetAddress, linkInfo);
		}

		public Message CreateWcfMessage(string action, object body, RequestInfo requestInfo)
		{
			return this.CreateWcfMessageInternal(action, body, true, null, null, null, requestInfo);
		}

		public Message CreateWcfMessage(string action, object body, string parentLinkId, RetryPolicy policy, TrackingContext trackingContext, RequestInfo requestInfo)
		{
			return this.CreateWcfMessageInternal(action, body, true, parentLinkId, policy, trackingContext, requestInfo);
		}

		private Message CreateWcfMessageInternal(string action, object body, bool includeToken, string parentLinkId, RetryPolicy policy, TrackingContext trackingContext, RequestInfo requestInfo)
		{
			Message message = Message.CreateMessage(this.messageVersion, action, body);
			MessageHeaders headers = message.Headers;
			headers.To = this.targetAddress.Uri;
			string sufficientClaims = this.GetSufficientClaims();
			if (this.linkInfo != null)
			{
				if (!string.IsNullOrEmpty(this.linkInfo.TransferDestinationEntityAddress))
				{
					SecurityToken authorizationToken = this.GetAuthorizationToken(this.linkInfo.TransferDestinationEntityAddress, sufficientClaims);
					if (authorizationToken != null)
					{
						SimpleWebSecurityToken simpleWebSecurityToken = (SimpleWebSecurityToken)authorizationToken;
						if (simpleWebSecurityToken != null)
						{
							this.linkInfo.TransferDestinationAuthorizationToken = simpleWebSecurityToken.Token;
						}
					}
				}
				this.linkInfo.AddTo(headers);
			}
			if (includeToken)
			{
				ServiceBusAuthorizationHeader authorizationHeader = this.GetAuthorizationHeader(sufficientClaims);
				if (authorizationHeader != null)
				{
					headers.Add(authorizationHeader);
				}
			}
			if (this.messagingFactory.FaultInjectionInfo != null)
			{
				this.messagingFactory.FaultInjectionInfo.AddToHeader(message);
			}
			if (!string.IsNullOrWhiteSpace(parentLinkId))
			{
				message.Properties["ParentLinkId"] = parentLinkId;
			}
			if (trackingContext != null)
			{
				TrackingIdHeader.TryAddOrUpdate(headers, trackingContext.TrackingId);
			}
			message.AddHeaderIfNotNull<RequestInfo>("RequestInfo", "http://schemas.microsoft.com/netservices/2011/06/servicebus", requestInfo);
			return message;
		}

		public Message CreateWcfMessageNoTokenHeader(string action, object body, RequestInfo requestInfo)
		{
			return this.CreateWcfMessageInternal(action, body, false, null, null, null, requestInfo);
		}

		private ServiceBusAuthorizationHeader GetAuthorizationHeader(string action)
		{
			SecurityToken authorizationToken = this.GetAuthorizationToken(this.targetAddress.Uri.AbsoluteUri, action);
			if (authorizationToken == null)
			{
				return null;
			}
			return new ServiceBusAuthorizationHeader((SimpleWebSecurityToken)authorizationToken);
		}

		private SecurityToken GetAuthorizationToken(string appliesTo, string action)
		{
			SecurityToken messagingToken = null;
			if (this.serviceBusSecuritySettings != null && this.serviceBusSecuritySettings.TokenProvider != null)
			{
				messagingToken = this.serviceBusSecuritySettings.TokenProvider.GetMessagingToken(this.baseAddress, appliesTo, action, false, Constants.TokenRequestOperationTimeout);
			}
			return messagingToken;
		}

		private string GetSufficientClaims()
		{
			switch (this.linkInfo.LinkType)
			{
				case LinkType.Receive:
				{
					return "Listen";
				}
				case LinkType.Send:
				{
					return "Send";
				}
				case LinkType.Control:
				{
					return "Listen";
				}
			}
			throw new NotImplementedException(SRClient.UnsupportedGetClaim(this.linkInfo.LinkType));
		}

		public string GetTokenString()
		{
			ServiceBusAuthorizationHeader authorizationHeader = this.GetAuthorizationHeader(this.GetSufficientClaims());
			if (authorizationHeader == null)
			{
				return null;
			}
			return authorizationHeader.TokenString;
		}
	}
}