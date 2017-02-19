using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class ActiveClientLink
	{
		private readonly bool isClientToken;

		private readonly string audience;

		private readonly string endpointUri;

		private readonly string[] requiredClaims;

		private AmqpLink link;

		private DateTime authorizationValidToUtc;

		public string Audience
		{
			get
			{
				return this.audience;
			}
		}

		public DateTime AuthorizationValidToUtc
		{
			get
			{
				return this.authorizationValidToUtc;
			}
			set
			{
				this.authorizationValidToUtc = value;
			}
		}

		public string EndpointUri
		{
			get
			{
				return this.endpointUri;
			}
		}

		public bool IsClientToken
		{
			get
			{
				return this.isClientToken;
			}
		}

		public AmqpLink Link
		{
			get
			{
				return this.link;
			}
		}

		public string[] RequiredClaims
		{
			get
			{
				return (string[])this.requiredClaims.Clone();
			}
		}

		public ActiveClientLink(AmqpLink link, string audience, string endpointUri, string[] requiredClaims, bool isClientToken, DateTime authorizationValidToUtc)
		{
			this.link = link;
			this.audience = audience;
			this.endpointUri = endpointUri;
			this.requiredClaims = requiredClaims;
			this.isClientToken = isClientToken;
			this.authorizationValidToUtc = authorizationValidToUtc;
		}
	}
}