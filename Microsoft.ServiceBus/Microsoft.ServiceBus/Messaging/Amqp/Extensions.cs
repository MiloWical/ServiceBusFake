using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Transaction;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal static class Extensions
	{
		public static bool Aborted(this Transfer transfer)
		{
			if (!transfer.Aborted.HasValue)
			{
				return false;
			}
			return transfer.Aborted.Value;
		}

		public static DateTime AbsoluteExpiryTime(this Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties properties)
		{
			if (!properties.AbsoluteExpiryTime.HasValue)
			{
				return new DateTime();
			}
			return properties.AbsoluteExpiryTime.Value;
		}

		public static void AddProperty(this Attach attach, AmqpSymbol symbol, object value)
		{
			if (attach.Properties == null)
			{
				attach.Properties = new Fields();
			}
			attach.Properties.Add(symbol, value);
		}

		public static void AddProperty(this Open open, AmqpSymbol symbol, object value)
		{
			if (open.Properties == null)
			{
				open.Properties = new Fields();
			}
			open.Properties.Add(symbol, value);
		}

		public static Address Address(this Attach attach)
		{
			if (attach.IsReceiver())
			{
				return ((Source)attach.Source).Address;
			}
			return ((Target)attach.Target).Address;
		}

		public static bool Batchable(this Transfer transfer)
		{
			if (!transfer.Batchable.HasValue)
			{
				return false;
			}
			return transfer.Batchable.Value;
		}

		public static bool Batchable(this Disposition disposition)
		{
			if (!disposition.Batchable.HasValue)
			{
				return false;
			}
			return disposition.Batchable.Value;
		}

		public static ushort ChannelMax(this Open open)
		{
			int? nullable;
			ushort? channelMax = open.ChannelMax;
			if (channelMax.HasValue)
			{
				nullable = new int?((int)channelMax.GetValueOrDefault());
			}
			else
			{
				nullable = null;
			}
			if (!nullable.HasValue)
			{
				return (ushort)65535;
			}
			return open.ChannelMax.Value;
		}

		public static Attach Clone(this Attach attach)
		{
			Attach attach1 = new Attach()
			{
				LinkName = attach.LinkName,
				Role = attach.Role,
				SndSettleMode = attach.SndSettleMode,
				RcvSettleMode = attach.RcvSettleMode,
				Source = attach.Source,
				Target = attach.Target,
				Unsettled = attach.Unsettled,
				IncompleteUnsettled = attach.IncompleteUnsettled,
				InitialDeliveryCount = attach.InitialDeliveryCount,
				MaxMessageSize = attach.MaxMessageSize,
				OfferedCapabilities = attach.OfferedCapabilities,
				DesiredCapabilities = attach.DesiredCapabilities,
				Properties = attach.Properties
			};
			return attach1;
		}

		public static AmqpLinkSettings Clone(this AmqpLinkSettings settings, bool deepClone)
		{
			AmqpLinkSettings amqpLinkSetting = new AmqpLinkSettings()
			{
				LinkName = settings.LinkName,
				Role = settings.Role,
				SndSettleMode = settings.SndSettleMode,
				RcvSettleMode = settings.RcvSettleMode,
				Source = settings.Source,
				Target = settings.Target,
				Unsettled = settings.Unsettled,
				IncompleteUnsettled = settings.IncompleteUnsettled,
				InitialDeliveryCount = settings.InitialDeliveryCount,
				MaxMessageSize = settings.MaxMessageSize,
				OfferedCapabilities = settings.OfferedCapabilities,
				DesiredCapabilities = settings.DesiredCapabilities
			};
			if (!deepClone)
			{
				amqpLinkSetting.Properties = settings.Properties;
			}
			else
			{
				amqpLinkSetting.Properties = new Fields();
				foreach (KeyValuePair<MapKey, object> property in (IEnumerable<KeyValuePair<MapKey, object>>)settings.Properties)
				{
					amqpLinkSetting.Properties[property.Key] = property.Value;
				}
			}
			amqpLinkSetting.TotalLinkCredit = settings.TotalLinkCredit;
			amqpLinkSetting.FlowThreshold = settings.FlowThreshold;
			amqpLinkSetting.AutoSendFlow = settings.AutoSendFlow;
			amqpLinkSetting.SettleType = settings.SettleType;
			return amqpLinkSetting;
		}

		public static Source Clone(this Source source)
		{
			Source source1 = new Source()
			{
				Address = source.Address,
				Durable = source.Durable,
				ExpiryPolicy = source.ExpiryPolicy,
				Timeout = source.Timeout,
				DistributionMode = source.DistributionMode,
				FilterSet = source.FilterSet,
				DefaultOutcome = source.DefaultOutcome,
				Outcomes = source.Outcomes,
				Capabilities = source.Capabilities
			};
			return source1;
		}

		public static Target Clone(this Target target)
		{
			Target target1 = new Target()
			{
				Address = target.Address,
				Durable = target.Durable,
				ExpiryPolicy = target.ExpiryPolicy,
				Timeout = target.Timeout,
				Capabilities = target.Capabilities
			};
			return target1;
		}

		public static bool Closed(this Detach detach)
		{
			if (!detach.Closed.HasValue)
			{
				return false;
			}
			return detach.Closed.Value;
		}

		public static DateTime CreationTime(this Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties properties)
		{
			if (!properties.CreationTime.HasValue)
			{
				return new DateTime();
			}
			return properties.CreationTime.Value;
		}

		public static uint DeliveryCount(this Header header)
		{
			if (!header.DeliveryCount.HasValue)
			{
				return (uint)0;
			}
			return header.DeliveryCount.Value;
		}

		public static bool Durable(this Header header)
		{
			if (!header.Durable.HasValue)
			{
				return false;
			}
			return header.Durable.Value;
		}

		public static bool Durable(this Source source)
		{
			if (!source.Durable.HasValue)
			{
				return false;
			}
			return source.Durable.Value == 0;
		}

		public static bool Durable(this Target target)
		{
			if (!target.Durable.HasValue)
			{
				return false;
			}
			return target.Durable.Value == 0;
		}

		public static bool Dynamic(this Attach attach)
		{
			if (attach.IsReceiver())
			{
				return ((Source)attach.Source).Dynamic();
			}
			return ((Target)attach.Target).Dynamic();
		}

		public static bool Dynamic(this Source source)
		{
			if (!source.Dynamic.HasValue)
			{
				return false;
			}
			return source.Dynamic.Value;
		}

		public static bool Dynamic(this Target target)
		{
			if (!target.Dynamic.HasValue)
			{
				return false;
			}
			return target.Dynamic.Value;
		}

		public static bool Echo(this Flow flow)
		{
			if (!flow.Echo.HasValue)
			{
				return false;
			}
			return flow.Echo.Value;
		}

		public static bool FirstAcquirer(this Header header)
		{
			if (!header.FirstAcquirer.HasValue)
			{
				return false;
			}
			return header.FirstAcquirer.Value;
		}

		internal static IEnumerable<ByteBuffer> GetClones(this IEnumerable<ByteBuffer> buffers)
		{
			if (buffers == null)
			{
				return null;
			}
			List<ByteBuffer> byteBuffers = new List<ByteBuffer>();
			foreach (ByteBuffer buffer in buffers)
			{
				byteBuffers.Add((ByteBuffer)buffer.Clone());
			}
			return byteBuffers;
		}

		public static string GetString(this ArraySegment<byte> binary)
		{
			StringBuilder stringBuilder = new StringBuilder(binary.Count * 2);
			for (int i = 0; i < binary.Count; i++)
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] array = new object[] { binary.Array[binary.Offset + i] };
				stringBuilder.AppendFormat(invariantCulture, "{0:X2}", array);
			}
			return stringBuilder.ToString();
		}

		public static SequenceNumber GroupSequence(this Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties properties)
		{
			uint value;
			if (!properties.GroupSequence.HasValue)
			{
				value = 0;
			}
			else
			{
				value = properties.GroupSequence.Value;
			}
			return value;
		}

		public static uint HandleMax(this Begin begin)
		{
			if (!begin.HandleMax.HasValue)
			{
				return (uint)-1;
			}
			return begin.HandleMax.Value;
		}

		public static uint IdleTimeOut(this Open open)
		{
			if (!open.IdleTimeOut.HasValue || open.IdleTimeOut.Value == 0)
			{
				return (uint)-1;
			}
			return open.IdleTimeOut.Value;
		}

		public static uint IncomingWindow(this Begin begin)
		{
			if (!begin.IncomingWindow.HasValue)
			{
				return (uint)-1;
			}
			return begin.IncomingWindow.Value;
		}

		public static bool IncompleteUnsettled(this Attach attach)
		{
			if (!attach.IncompleteUnsettled.HasValue)
			{
				return false;
			}
			return attach.IncompleteUnsettled.Value;
		}

		public static bool IsReceiver(this Attach attach)
		{
			return attach.Role.Value;
		}

		public static uint LinkCredit(this Flow flow)
		{
			if (!flow.LinkCredit.HasValue)
			{
				return (uint)-1;
			}
			return flow.LinkCredit.Value;
		}

		public static uint MaxFrameSize(this Open open)
		{
			if (!open.MaxFrameSize.HasValue)
			{
				return (uint)-1;
			}
			return open.MaxFrameSize.Value;
		}

		public static ulong MaxMessageSize(this Attach attach)
		{
			if (!attach.MaxMessageSize.HasValue)
			{
				return (ulong)-1;
			}
			return attach.MaxMessageSize.Value;
		}

		public static bool More(this Transfer transfer)
		{
			if (!transfer.More.HasValue)
			{
				return false;
			}
			return transfer.More.Value;
		}

		public static uint OutgoingWindow(this Begin begin)
		{
			if (!begin.OutgoingWindow.HasValue)
			{
				return (uint)-1;
			}
			return begin.OutgoingWindow.Value;
		}

		public static byte Priority(this Header header)
		{
			int? nullable;
			byte? priority = header.Priority;
			if (priority.HasValue)
			{
				nullable = new int?((int)priority.GetValueOrDefault());
			}
			else
			{
				nullable = null;
			}
			if (!nullable.HasValue)
			{
				return (byte)0;
			}
			return header.Priority.Value;
		}

		public static ushort RemoteChannel(this Begin begin)
		{
			int? nullable;
			ushort? remoteChannel = begin.RemoteChannel;
			if (remoteChannel.HasValue)
			{
				nullable = new int?((int)remoteChannel.GetValueOrDefault());
			}
			else
			{
				nullable = null;
			}
			if (!nullable.HasValue)
			{
				return (ushort)0;
			}
			return begin.RemoteChannel.Value;
		}

		public static bool Resume(this Transfer transfer)
		{
			if (!transfer.Resume.HasValue)
			{
				return false;
			}
			return transfer.Resume.Value;
		}

		public static bool Settled(this Transfer transfer)
		{
			if (!transfer.Settled.HasValue)
			{
				return false;
			}
			return transfer.Settled.Value;
		}

		public static bool Settled(this Disposition disposition)
		{
			if (!disposition.Settled.HasValue)
			{
				return false;
			}
			return disposition.Settled.Value;
		}

		public static SettleMode SettleType(this Attach attach)
		{
			SenderSettleMode value;
			ReceiverSettleMode receiverSettleMode;
			if (attach.SndSettleMode.HasValue)
			{
				value = (SenderSettleMode)attach.SndSettleMode.Value;
			}
			else
			{
				value = SenderSettleMode.Mixed;
			}
			SenderSettleMode senderSettleMode = value;
			if (attach.RcvSettleMode.HasValue)
			{
				receiverSettleMode = (ReceiverSettleMode)attach.RcvSettleMode.Value;
			}
			else
			{
				receiverSettleMode = ReceiverSettleMode.First;
			}
			ReceiverSettleMode receiverSettleMode1 = receiverSettleMode;
			if (senderSettleMode == SenderSettleMode.Settled)
			{
				return SettleMode.SettleOnSend;
			}
			if (receiverSettleMode1 == ReceiverSettleMode.First)
			{
				return SettleMode.SettleOnReceive;
			}
			return SettleMode.SettleOnDispose;
		}

		public static Terminus Terminus(this Attach attach)
		{
			if (attach.IsReceiver())
			{
				Source source = attach.Source as Source;
				if (source == null)
				{
					return null;
				}
				return new Terminus(source);
			}
			Target target = attach.Target as Target;
			if (target == null)
			{
				return null;
			}
			return new Terminus(target);
		}

		internal static ByteBuffer[] ToByteBufferArray(this ArraySegment<byte>[] bufferList)
		{
			if (bufferList == null)
			{
				return null;
			}
			ByteBuffer[] byteBuffer = new ByteBuffer[(int)bufferList.Length];
			for (int i = 0; i < (int)bufferList.Length; i++)
			{
				byteBuffer[i] = new ByteBuffer(bufferList[i]);
			}
			return byteBuffer;
		}

		public static string TrackingId(this Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties properties)
		{
			if (properties.CorrelationId != null)
			{
				return properties.CorrelationId.ToString();
			}
			if (properties.MessageId != null)
			{
				return properties.MessageId.ToString();
			}
			properties.MessageId = Guid.NewGuid();
			return properties.MessageId.ToString();
		}

		public static bool Transactional(this Delivery delivery)
		{
			if (delivery.State == null)
			{
				return false;
			}
			return delivery.State.DescriptorCode == TransactionalState.Code;
		}

		public static uint Ttl(this Header header)
		{
			if (!header.Ttl.HasValue)
			{
				return (uint)0;
			}
			return header.Ttl.Value;
		}

		public static void UpsertProperty(this Attach attach, AmqpSymbol symbol, object value)
		{
			if (attach.Properties == null)
			{
				attach.Properties = new Fields();
			}
			attach.Properties[symbol] = value;
		}
	}
}