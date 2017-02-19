using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpConnectionSettings : Open
	{
		public bool IgnoreMissingSessions
		{
			get;
			set;
		}

		public int ReceiveBufferSize
		{
			get;
			set;
		}

		public string RemoteContainerId
		{
			get;
			set;
		}

		public string RemoteHostName
		{
			get;
			set;
		}

		public int SendBufferSize
		{
			get;
			set;
		}

		public AmqpConnectionSettings()
		{
			this.SendBufferSize = 65536;
			this.ReceiveBufferSize = 65536;
			base.MaxFrameSize = new uint?(65536);
			base.ChannelMax = new ushort?(10000);
		}

		public AmqpConnectionSettings Clone()
		{
			AmqpConnectionSettings amqpConnectionSetting = new AmqpConnectionSettings()
			{
				ContainerId = base.ContainerId,
				HostName = base.HostName,
				MaxFrameSize = base.MaxFrameSize,
				ChannelMax = base.ChannelMax,
				IdleTimeOut = base.IdleTimeOut,
				OutgoingLocales = base.OutgoingLocales,
				IncomingLocales = base.IncomingLocales,
				Properties = base.Properties,
				OfferedCapabilities = base.OfferedCapabilities,
				DesiredCapabilities = base.DesiredCapabilities
			};
			amqpConnectionSetting.Properties = base.Properties;
			amqpConnectionSetting.SendBufferSize = this.SendBufferSize;
			amqpConnectionSetting.ReceiveBufferSize = this.ReceiveBufferSize;
			amqpConnectionSetting.IgnoreMissingSessions = this.IgnoreMissingSessions;
			return amqpConnectionSetting;
		}
	}
}