using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpMessageEncoder : IBrokeredMessageEncoder
	{
		private const string NullMessageId = "nil";

		public AmqpMessageEncoder()
		{
		}

		private static byte[] ReadBytes(XmlReader reader, int bytesToRead)
		{
			int num;
			int num1 = 0;
			byte[] numArray = new byte[bytesToRead];
			do
			{
				if (num1 >= bytesToRead)
				{
					break;
				}
				num = reader.ReadContentAsBase64(numArray, num1, (int)numArray.Length - num1);
				num1 = num1 + num;
			}
			while (num != 0);
			if (num1 >= bytesToRead)
			{
				return numArray;
			}
			byte[] numArray1 = new byte[num1];
			Array.Copy(numArray, numArray1, (int)numArray1.Length);
			return numArray1;
		}

		public long ReadHeader(XmlReader reader, BrokeredMessage brokeredMessage, SerializationTarget serializationTarget)
		{
			byte[] numArray;
			long num;
			List<ArraySegment<byte>> arraySegments = new List<ArraySegment<byte>>();
			long length = (long)0;
			if (serializationTarget == SerializationTarget.Communication)
			{
				reader.ReadStartElement();
			}
			do
			{
				numArray = AmqpMessageEncoder.ReadBytes(reader, 512);
				length = length + (long)((int)numArray.Length);
				arraySegments.Add(new ArraySegment<byte>(numArray, 0, (int)numArray.Length));
			}
			while ((int)numArray.Length >= 512);
			if (serializationTarget == SerializationTarget.Communication)
			{
				reader.ReadEndElement();
			}
			using (BufferListStream bufferListStream = new BufferListStream(arraySegments.ToArray()))
			{
				using (AmqpMessage amqpMessage = AmqpMessage.CreateAmqpStreamMessageHeader(bufferListStream))
				{
					amqpMessage.Deserialize(SectionFlag.NonBody);
					brokeredMessage.MessageId = "nil";
					AmqpMessageEncoder.UpdateBrokeredMessageHeaderAndProperties(amqpMessage, brokeredMessage, serializationTarget);
					brokeredMessage.RawHeaderStream = amqpMessage.GetNonBodySectionsStream();
					num = length;
				}
			}
			return num;
		}

		private static void UpdateAmqpMessageHeadersAndProperties(BrokeredMessage source, AmqpMessage target, SerializationTarget serializationTarget)
		{
			MessageConverter.UpdateAmqpMessageHeadersAndProperties(target, source, false);
			BrokeredMessage.MessageMembers initializedMembers = source.InitializedMembers;
			if ((int)(initializedMembers & BrokeredMessage.MessageMembers.PrefilteredHeaders) != 0)
			{
				target.DeliveryAnnotations.Map["x-opt-prefiltered-headers"] = MessageConverter.ReadStream(source.PrefilteredHeaders);
			}
			if ((int)(initializedMembers & BrokeredMessage.MessageMembers.PrefilteredProperties) != 0)
			{
				target.DeliveryAnnotations.Map["x-opt-prefiltered-properties"] = MessageConverter.ReadStream(source.PrefilteredProperties);
			}
			if ((int)(initializedMembers & BrokeredMessage.MessageMembers.TransferDestination) != 0)
			{
				target.DeliveryAnnotations.Map["x-opt-transfer-destination"] = source.TransferDestination;
			}
			if ((int)(initializedMembers & BrokeredMessage.MessageMembers.TransferSource) != 0)
			{
				target.DeliveryAnnotations.Map["x-opt-transfer-source"] = source.TransferSource;
			}
			if ((int)(initializedMembers & BrokeredMessage.MessageMembers.TransferSequenceNumber) != 0)
			{
				target.DeliveryAnnotations.Map["x-opt-transfer-sn"] = source.TransferSequenceNumber;
				target.DeliveryAnnotations.Map["x-opt-transfer-session"] = source.SessionId;
			}
			if ((int)(initializedMembers & BrokeredMessage.MessageMembers.TransferHopCount) != 0)
			{
				target.DeliveryAnnotations.Map["x-opt-transfer-hop-count"] = source.TransferHopCount;
			}
			if ((int)(initializedMembers & BrokeredMessage.MessageMembers.TransferDestinationEntityId) != 0)
			{
				target.DeliveryAnnotations.Map["x-opt-transfer-resource"] = source.TransferDestinationResourceId;
			}
			if (serializationTarget == SerializationTarget.Communication && (int)(initializedMembers & BrokeredMessage.MessageMembers.LockToken) != 0)
			{
				target.DeliveryAnnotations.Map["x-opt-lock-token"] = source.LockToken;
				target.MessageAnnotations.Map["x-opt-locked-until"] = source.LockedUntilUtc;
			}
		}

		private static void UpdateBrokeredMessageHeaderAndProperties(AmqpMessage source, BrokeredMessage target, SerializationTarget serializationTarget)
		{
			DateTime dateTime;
			long num;
			DateTime dateTime1;
			ArraySegment<byte> nums;
			ArraySegment<byte> nums1;
			string str;
			string str1;
			long num1;
			int num2;
			long num3;
			string str2;
			Guid guid;
			string groupId;
			MessageConverter.UpdateBrokeredMessageHeaderAndProperties(source, target);
			SectionFlag sections = source.Sections;
			if ((int)(sections & SectionFlag.Header) != 0 && source.Header.DeliveryCount.HasValue)
			{
				target.DeliveryCount = (int)source.Header.DeliveryCount.Value;
			}
			if ((int)(sections & SectionFlag.MessageAnnotations) != 0)
			{
				if (source.MessageAnnotations.Map.TryGetValue<DateTime>("x-opt-enqueued-time", out dateTime))
				{
					target.EnqueuedTimeUtc = dateTime;
				}
				if (source.MessageAnnotations.Map.TryGetValue<long>("x-opt-sequence-number", out num))
				{
					target.SequenceNumber = num;
				}
				if (source.MessageAnnotations.Map.TryGetValue<DateTime>("x-opt-locked-until", out dateTime1))
				{
					target.LockedUntilUtc = dateTime1;
				}
			}
			if ((int)(sections & SectionFlag.DeliveryAnnotations) != 0)
			{
				if (source.DeliveryAnnotations.Map.TryGetValue<ArraySegment<byte>>("x-opt-prefiltered-headers", out nums))
				{
					target.PrefilteredHeaders = new MemoryStream(nums.Array, nums.Offset, nums.Count);
				}
				if (source.DeliveryAnnotations.Map.TryGetValue<ArraySegment<byte>>("x-opt-prefiltered-headers", out nums1))
				{
					target.PrefilteredProperties = new MemoryStream(nums1.Array, nums1.Offset, nums1.Count);
				}
				if (source.DeliveryAnnotations.Map.TryGetValue<string>("x-opt-transfer-destination", out str))
				{
					target.TransferDestination = str;
				}
				if (source.DeliveryAnnotations.Map.TryGetValue<string>("x-opt-transfer-source", out str1))
				{
					target.TransferSource = str1;
				}
				if (source.DeliveryAnnotations.Map.TryGetValue<long>("x-opt-transfer-sn", out num1))
				{
					target.TransferSequenceNumber = num1;
				}
				if (source.DeliveryAnnotations.Map.TryGetValue<int>("x-opt-transfer-hop-count", out num2))
				{
					target.TransferHopCount = num2;
				}
				if (source.DeliveryAnnotations.Map.TryGetValue<long>("x-opt-transfer-resource", out num3))
				{
					target.TransferDestinationResourceId = num3;
				}
				if (source.DeliveryAnnotations.Map.TryGetValue<string>("x-opt-transfer-session", out str2))
				{
					target.SessionId = str2;
					BrokeredMessage brokeredMessage = target;
					if (source.Properties == null)
					{
						groupId = null;
					}
					else
					{
						groupId = source.Properties.GroupId;
					}
					brokeredMessage.TransferSessionId = groupId;
				}
				if (source.DeliveryAnnotations.Map.TryGetValue<Guid>("x-opt-lock-token", out guid))
				{
					target.LockToken = guid;
				}
			}
		}

		public long WriteHeader(XmlWriter writer, BrokeredMessage brokeredMessage, SerializationTarget serializationTarget)
		{
			long num;
			using (BufferListStream bufferListStream = BufferListStream.Create(brokeredMessage.RawHeaderStream, 512))
			{
				using (AmqpMessage amqpMessage = AmqpMessage.CreateAmqpStreamMessageHeader(bufferListStream))
				{
					AmqpMessageEncoder.UpdateAmqpMessageHeadersAndProperties(brokeredMessage, amqpMessage, serializationTarget);
					if (serializationTarget == SerializationTarget.Communication)
					{
						writer.WriteStartElement("MessageHeaders");
					}
					long num1 = amqpMessage.Write(writer);
					if (serializationTarget == SerializationTarget.Communication)
					{
						writer.WriteEndElement();
					}
					num = num1;
				}
			}
			return num;
		}
	}
}