using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal interface ITransportFactorySettings : IDefaultCommunicationTimeouts
	{
		System.ServiceModel.Channels.BufferManager BufferManager
		{
			get;
		}

		bool ManualAddressing
		{
			get;
		}

		long MaxReceivedMessageSize
		{
			get;
		}

		System.ServiceModel.Channels.MessageEncoderFactory MessageEncoderFactory
		{
			get;
		}

		System.ServiceModel.Channels.MessageVersion MessageVersion
		{
			get;
		}
	}
}