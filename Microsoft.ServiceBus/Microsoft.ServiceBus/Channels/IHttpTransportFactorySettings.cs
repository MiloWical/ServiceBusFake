using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal interface IHttpTransportFactorySettings : ITransportFactorySettings, IDefaultCommunicationTimeouts
	{
		int MaxBufferSize
		{
			get;
		}

		System.ServiceModel.TransferMode TransferMode
		{
			get;
		}
	}
}