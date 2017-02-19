using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpSessionSettings : Begin
	{
		public TimeSpan DispositionInterval
		{
			get;
			set;
		}

		public int DispositionThreshold
		{
			get;
			set;
		}

		public bool IgnoreMissingLinks
		{
			get;
			set;
		}

		public SequenceNumber InitialDeliveryId
		{
			get;
			set;
		}

		public AmqpSessionSettings()
		{
			base.NextOutgoingId = new uint?(1);
			base.IncomingWindow = new uint?(5000);
			base.OutgoingWindow = new uint?(5000);
			this.DispositionThreshold = Math.Min(500, 3333);
			this.DispositionInterval = TimeSpan.FromMilliseconds(20);
		}

		public AmqpSessionSettings Clone()
		{
			AmqpSessionSettings amqpSessionSetting = new AmqpSessionSettings()
			{
				DispositionThreshold = this.DispositionThreshold,
				DispositionInterval = this.DispositionInterval,
				NextOutgoingId = base.NextOutgoingId,
				IncomingWindow = base.IncomingWindow,
				OutgoingWindow = base.OutgoingWindow,
				HandleMax = base.HandleMax,
				OfferedCapabilities = base.OfferedCapabilities,
				DesiredCapabilities = base.DesiredCapabilities,
				Properties = base.Properties
			};
			return amqpSessionSetting;
		}

		public static AmqpSessionSettings Create(Begin begin)
		{
			AmqpSessionSettings amqpSessionSetting = new AmqpSessionSettings();
			uint value = amqpSessionSetting.IncomingWindow.Value;
			uint? outgoingWindow = begin.OutgoingWindow;
			amqpSessionSetting.IncomingWindow = new uint?(Math.Min(value, outgoingWindow.Value));
			uint num = amqpSessionSetting.OutgoingWindow.Value;
			uint? incomingWindow = begin.IncomingWindow;
			amqpSessionSetting.OutgoingWindow = new uint?(Math.Min(num, incomingWindow.Value));
			amqpSessionSetting.Properties = begin.Properties;
			return amqpSessionSetting;
		}
	}
}