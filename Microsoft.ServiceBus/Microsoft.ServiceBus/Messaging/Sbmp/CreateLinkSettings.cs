using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal class CreateLinkSettings
	{
		internal Lazy<SbmpMessageCreator> ControlMessageCreator
		{
			get;
			private set;
		}

		internal string EntityName
		{
			get;
			private set;
		}

		internal string EntityPath
		{
			get;
			private set;
		}

		internal Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo LinkInfo
		{
			get;
			private set;
		}

		internal SbmpMessageCreator MessageCreator
		{
			get;
			private set;
		}

		internal SbmpMessagingFactory MessagingFactory
		{
			get;
			private set;
		}

		public CreateLinkSettings(SbmpMessagingFactory messagingFactory, string entityPath, string entityName, Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo linkInfo, Lazy<SbmpMessageCreator> controlMessageCreator = null)
		{
			this.LinkInfo = linkInfo;
			this.MessagingFactory = messagingFactory;
			EndpointAddress endpointAddress = messagingFactory.CreateEndpointAddress(entityPath);
			this.EntityName = entityName;
			this.EntityPath = entityPath;
			this.ControlMessageCreator = controlMessageCreator;
			MessagingFactorySettings settings = this.MessagingFactory.GetSettings();
			this.MessageCreator = new SbmpMessageCreator(this.MessagingFactory, this.MessagingFactory.BaseAddress, this.MessagingFactory.MessageVersion, this.MessagingFactory.Settings, settings.EnableAdditionalClientTimeout, endpointAddress);
			if (this.LinkInfo != null)
			{
				this.MessageCreator = this.MessageCreator.CreateLinkMessageCreator(this.LinkInfo);
				if (settings.NetMessagingTransportSettings.GatewayMode)
				{
					this.LinkInfo.IsHttp = true;
					this.LinkInfo.ApiVersion = ApiVersionHelper.CurrentRuntimeApiVersion;
				}
			}
		}
	}
}