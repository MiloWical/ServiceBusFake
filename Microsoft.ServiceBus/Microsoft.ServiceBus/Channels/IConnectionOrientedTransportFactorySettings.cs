using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace Microsoft.ServiceBus.Channels
{
	internal interface IConnectionOrientedTransportFactorySettings : Microsoft.ServiceBus.Channels.ITransportFactorySettings, IDefaultCommunicationTimeouts, Microsoft.ServiceBus.Channels.IConnectionOrientedConnectionSettings
	{
		ServiceSecurityAuditBehavior AuditBehavior
		{
			get;
		}

		int MaxBufferSize
		{
			get;
		}

		System.ServiceModel.TransferMode TransferMode
		{
			get;
		}

		StreamUpgradeProvider Upgrade
		{
			get;
		}
	}
}