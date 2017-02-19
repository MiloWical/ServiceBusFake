using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal static class MessageConverter
	{
		private const int GuidSize = 16;

		public const string EnqueuedTimeUtcName = "x-opt-enqueued-time";

		public const string ScheduledEnqueueTimeUtcName = "x-opt-scheduled-enqueue-time";

		public const string SequenceNumberName = "x-opt-sequence-number";

		public const string OffsetName = "x-opt-offset";

		public const string LockTokenName = "x-opt-lock-token";

		public const string LockedUntilName = "x-opt-locked-until";

		public const string PublisherName = "x-opt-publisher";

		public const string PartitionKeyName = "x-opt-partition-key";

		public const string PrefilteredMessageHeadersName = "x-opt-prefiltered-headers";

		public const string PrefilteredMessagePropertiesName = "x-opt-prefiltered-properties";

		public const string TransferSourceName = "x-opt-transfer-source";

		public const string TransferDestinationName = "x-opt-transfer-destination";

		public const string TransferResourceName = "x-opt-transfer-resource";

		public const string TransferSessionName = "x-opt-transfer-session";

		public const string TransferSequenceNumberName = "x-opt-transfer-sn";

		public const string TransferHopCountName = "x-opt-transfer-hop-count";

		public const string TimeSpanName = "com.microsoft:timespan";

		public const string UriName = "com.microsoft:uri";

		public const string DateTimeOffsetName = "com.microsoft:datetime-offset";

		public static IList<AmqpMessage> BrokerExpandBatchMessage(AmqpMessage message)
		{
			List<AmqpMessage> amqpMessages = new List<AmqpMessage>();
			if (message.DataBody == null)
			{
				return amqpMessages;
			}
			foreach (Data dataBody in message.DataBody)
			{
				ArraySegment<byte>[] value = new ArraySegment<byte>[] { (ArraySegment<byte>)dataBody.Value };
				amqpMessages.Add(AmqpMessage.CreateAmqpStreamMessage(new BufferListStream(value), true));
			}
			return amqpMessages;
		}

		public static BrokeredMessage BrokerGetMessage(AmqpMessage amqpMessage, bool shouldOptimizeForPassthrough)
		{
			BrokeredMessage brokeredMessage;
			if (!shouldOptimizeForPassthrough)
			{
				amqpMessage.Deserialize(SectionFlag.All);
				brokeredMessage = new BrokeredMessage(amqpMessage.GetNonBodySectionsStream(), true, amqpMessage.GetBodySectionStream(), true, BrokeredMessageFormat.Amqp);
			}
			else
			{
				amqpMessage.Deserialize(SectionFlag.Header | SectionFlag.DeliveryAnnotations | SectionFlag.MessageAnnotations | SectionFlag.Properties | SectionFlag.ApplicationProperties);
				brokeredMessage = new BrokeredMessage(null, false, amqpMessage.ToStream(), true, BrokeredMessageFormat.PassthroughAmqp);
			}
			if (amqpMessage.RawByteBuffers != null)
			{
				brokeredMessage.AttachDisposables(amqpMessage.RawByteBuffers.GetClones());
			}
			MessageConverter.UpdateBrokeredMessageHeaderAndProperties(amqpMessage, brokeredMessage);
			return brokeredMessage;
		}

		public static AmqpMessage BrokerGetMessage(BrokeredMessage brokeredMessage)
		{
			AmqpMessage messageId = null;
			bool flag = true;
			if (brokeredMessage.MessageFormat == BrokeredMessageFormat.PassthroughAmqp)
			{
				flag = false;
				messageId = AmqpMessage.CreateAmqpStreamMessage((BufferListStream)brokeredMessage.BodyStream, true);
			}
			else if (brokeredMessage.MessageFormat == BrokeredMessageFormat.Amqp)
			{
				messageId = AmqpMessage.CreateAmqpStreamMessage(brokeredMessage.RawHeaderStream, brokeredMessage.BodyStream, true);
			}
			else if (brokeredMessage.MessageFormat == BrokeredMessageFormat.AmqpEventData)
			{
				flag = false;
				BufferListStream bufferListStream = BufferListStream.Create(brokeredMessage.BodyStream, 512, true);
				messageId = AmqpMessage.CreateOutputMessage(bufferListStream, true);
				MessageAnnotations messageAnnotations = messageId.MessageAnnotations;
				messageAnnotations.Map["x-opt-sequence-number"] = brokeredMessage.SequenceNumber;
				Annotations map = messageAnnotations.Map;
				AmqpSymbol amqpSymbol = "x-opt-offset";
				long offset = brokeredMessage.Offset;
				map[amqpSymbol] = offset.ToString(NumberFormatInfo.InvariantInfo);
				messageAnnotations.Map["x-opt-enqueued-time"] = brokeredMessage.EnqueuedTimeUtc;
				if (!string.IsNullOrEmpty(brokeredMessage.Publisher))
				{
					messageAnnotations.Map["x-opt-publisher"] = brokeredMessage.Publisher;
					messageAnnotations.Map["x-opt-partition-key"] = brokeredMessage.Publisher;
				}
			}
			else if (brokeredMessage.MessageFormat == BrokeredMessageFormat.Sbmp)
			{
				brokeredMessage.SetPropertiesAsModifiedByBroker();
				messageId = MessageConverter.CreateAmqpMessageFromSbmpMessage(brokeredMessage);
				messageId.Properties.MessageId = brokeredMessage.MessageId;
				messageId.Properties.CorrelationId = brokeredMessage.CorrelationId;
				messageId.Properties.ContentType = brokeredMessage.ContentType;
				messageId.Properties.Subject = brokeredMessage.Label;
				messageId.Properties.To = brokeredMessage.To;
				messageId.Properties.ReplyTo = brokeredMessage.ReplyTo;
				messageId.Properties.GroupId = brokeredMessage.SessionId;
				messageId.Properties.ReplyToGroupId = brokeredMessage.ReplyToSessionId;
			}
			if (flag)
			{
				MessageConverter.UpdateAmqpMessageHeadersAndProperties(messageId, brokeredMessage, true);
			}
			return messageId;
		}

		public static BrokeredMessage ClientGetMessage(AmqpMessage amqpMessage)
		{
			BrokeredMessage brokeredMessage;
			if ((int)(amqpMessage.BodyType & SectionFlag.Data) != 0 || (int)(amqpMessage.BodyType & SectionFlag.AmqpSequence) != 0)
			{
				brokeredMessage = new BrokeredMessage(MessageConverter.GetMessageBodyStream(amqpMessage), true);
			}
			else if ((int)(amqpMessage.BodyType & SectionFlag.AmqpValue) == 0)
			{
				brokeredMessage = new BrokeredMessage();
			}
			else
			{
				object value = null;
				if (!MessageConverter.TryGetNetObjectFromAmqpObject(amqpMessage.ValueBody.Value, MappingType.MessageBody, out value))
				{
					value = amqpMessage.ValueBody.Value;
				}
				brokeredMessage = new BrokeredMessage(value, amqpMessage.BodyStream);
			}
			SectionFlag sections = amqpMessage.Sections;
			if ((int)(sections & SectionFlag.Header) != 0)
			{
				if (amqpMessage.Header.Ttl.HasValue)
				{
					uint? ttl = amqpMessage.Header.Ttl;
					brokeredMessage.TimeToLive = TimeSpan.FromMilliseconds((double)((float)ttl.Value));
				}
				if (amqpMessage.Header.DeliveryCount.HasValue)
				{
					brokeredMessage.DeliveryCount = (int)(amqpMessage.Header.DeliveryCount.Value + 1);
				}
			}
			if ((int)(sections & SectionFlag.Properties) != 0)
			{
				if (amqpMessage.Properties.MessageId != null)
				{
					brokeredMessage.MessageId = amqpMessage.Properties.MessageId.ToString();
				}
				if (amqpMessage.Properties.CorrelationId != null)
				{
					brokeredMessage.CorrelationId = amqpMessage.Properties.CorrelationId.ToString();
				}
				if (amqpMessage.Properties.ContentType.Value != null)
				{
					brokeredMessage.ContentType = amqpMessage.Properties.ContentType.Value;
				}
				if (amqpMessage.Properties.Subject != null)
				{
					brokeredMessage.Label = amqpMessage.Properties.Subject;
				}
				if (amqpMessage.Properties.To != null)
				{
					brokeredMessage.To = amqpMessage.Properties.To.ToString();
				}
				if (amqpMessage.Properties.ReplyTo != null)
				{
					brokeredMessage.ReplyTo = amqpMessage.Properties.ReplyTo.ToString();
				}
				if (amqpMessage.Properties.GroupId != null)
				{
					brokeredMessage.SessionId = amqpMessage.Properties.GroupId;
				}
				if (amqpMessage.Properties.ReplyToGroupId != null)
				{
					brokeredMessage.ReplyToSessionId = amqpMessage.Properties.ReplyToGroupId;
				}
			}
			if ((int)(sections & SectionFlag.ApplicationProperties) != 0)
			{
				foreach (KeyValuePair<MapKey, object> map in (IEnumerable<KeyValuePair<MapKey, object>>)amqpMessage.ApplicationProperties.Map)
				{
					object obj = null;
					if (!MessageConverter.TryGetNetObjectFromAmqpObject(map.Value, MappingType.ApplicationProperty, out obj))
					{
						continue;
					}
					brokeredMessage.Properties[map.Key.ToString()] = obj;
				}
			}
			if ((int)(sections & SectionFlag.MessageAnnotations) != 0)
			{
				foreach (KeyValuePair<MapKey, object> keyValuePair in (IEnumerable<KeyValuePair<MapKey, object>>)amqpMessage.MessageAnnotations.Map)
				{
					string str = keyValuePair.Key.ToString();
					string str1 = str;
					string str2 = str1;
					if (str1 != null)
					{
						switch (str2)
						{
							case "x-opt-enqueued-time":
							{
								brokeredMessage.EnqueuedTimeUtc = (DateTime)keyValuePair.Value;
								continue;
							}
							case "x-opt-scheduled-enqueue-time":
							{
								brokeredMessage.ScheduledEnqueueTimeUtc = (DateTime)keyValuePair.Value;
								continue;
							}
							case "x-opt-sequence-number":
							{
								brokeredMessage.SequenceNumber = (long)keyValuePair.Value;
								continue;
							}
							case "x-opt-offset":
							{
								brokeredMessage.EnqueuedSequenceNumber = (long)keyValuePair.Value;
								continue;
							}
							case "x-opt-locked-until":
							{
								brokeredMessage.LockedUntilUtc = (DateTime)keyValuePair.Value;
								continue;
							}
							case "x-opt-publisher":
							{
								brokeredMessage.Publisher = (string)keyValuePair.Value;
								continue;
							}
							case "x-opt-partition-key":
							{
								brokeredMessage.PartitionKey = (string)keyValuePair.Value;
								continue;
							}
						}
					}
					object obj1 = null;
					if (!MessageConverter.TryGetNetObjectFromAmqpObject(keyValuePair.Value, MappingType.ApplicationProperty, out obj1))
					{
						continue;
					}
					brokeredMessage.Properties[str] = obj1;
				}
			}
			if (amqpMessage.DeliveryTag.Count == 16)
			{
				byte[] numArray = new byte[16];
				byte[] array = amqpMessage.DeliveryTag.Array;
				ArraySegment<byte> deliveryTag = amqpMessage.DeliveryTag;
				Buffer.BlockCopy((Array)array, deliveryTag.Offset, numArray, 0, 16);
				brokeredMessage.LockToken = new Guid(numArray);
			}
			brokeredMessage.AttachDisposables(new AmqpMessage[] { amqpMessage });
			return brokeredMessage;
		}

		public static AmqpMessage ClientGetMessage(BrokeredMessage brokeredMessage)
		{
			AmqpMessage messageId = MessageConverter.CreateAmqpMessageFromSbmpMessage(brokeredMessage);
			messageId.Properties.MessageId = brokeredMessage.MessageId;
			messageId.Properties.CorrelationId = brokeredMessage.CorrelationId;
			messageId.Properties.ContentType = brokeredMessage.ContentType;
			messageId.Properties.Subject = brokeredMessage.Label;
			messageId.Properties.To = brokeredMessage.To;
			messageId.Properties.ReplyTo = brokeredMessage.ReplyTo;
			messageId.Properties.GroupId = brokeredMessage.SessionId;
			messageId.Properties.ReplyToGroupId = brokeredMessage.ReplyToSessionId;
			if ((int)(brokeredMessage.InitializedMembers & BrokeredMessage.MessageMembers.TimeToLive) != 0)
			{
				messageId.Header.Ttl = new uint?((uint)brokeredMessage.TimeToLive.TotalMilliseconds);
				messageId.Properties.CreationTime = new DateTime?(DateTime.UtcNow);
				if ((AmqpConstants.MaxAbsoluteExpiryTime - messageId.Properties.CreationTime.Value) <= brokeredMessage.TimeToLive)
				{
					messageId.Properties.AbsoluteExpiryTime = new DateTime?(AmqpConstants.MaxAbsoluteExpiryTime);
				}
				else
				{
					Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties properties = messageId.Properties;
					DateTime? creationTime = messageId.Properties.CreationTime;
					properties.AbsoluteExpiryTime = new DateTime?(creationTime.Value + brokeredMessage.TimeToLive);
				}
			}
			if ((int)(brokeredMessage.InitializedMembers & BrokeredMessage.MessageMembers.ScheduledEnqueueTimeUtc) != 0 && brokeredMessage.ScheduledEnqueueTimeUtc > DateTime.MinValue)
			{
				messageId.MessageAnnotations.Map.Add("x-opt-scheduled-enqueue-time", brokeredMessage.ScheduledEnqueueTimeUtc);
			}
			if ((int)(brokeredMessage.InitializedMembers & BrokeredMessage.MessageMembers.Publisher) != 0 && brokeredMessage.Publisher != null)
			{
				messageId.MessageAnnotations.Map.Add("x-opt-publisher", brokeredMessage.Publisher);
			}
			if ((int)(brokeredMessage.InitializedMembers & BrokeredMessage.MessageMembers.PartitionKey) != 0 && brokeredMessage.PartitionKey != null)
			{
				messageId.MessageAnnotations.Map.Add("x-opt-partition-key", brokeredMessage.PartitionKey);
			}
			foreach (KeyValuePair<string, object> property in brokeredMessage.Properties)
			{
				object obj = null;
				if (!MessageConverter.TryGetAmqpObjectFromNetObject(property.Value, MappingType.ApplicationProperty, out obj))
				{
					continue;
				}
				messageId.ApplicationProperties.Map.Add(property.Key, obj);
			}
			return messageId;
		}

		public static BrokeredMessage ConvertAmqpToSbmp(BrokeredMessage brokeredMessage, ReceiveMode receiveMode)
		{
			BrokeredMessage encoder = brokeredMessage.CreateCopy();
			encoder.MessageFormat = BrokeredMessageFormat.Sbmp;
			encoder.MessageEncoder = BrokeredMessageEncoder.GetEncoder(BrokeredMessageFormat.Sbmp);
			if (receiveMode == ReceiveMode.PeekLock && brokeredMessage.IsLockTokenSet)
			{
				encoder.LockToken = brokeredMessage.LockToken;
				encoder.LockedUntilUtc = brokeredMessage.LockedUntilUtc;
			}
			if (encoder.BodyStream != null)
			{
				using (Stream stream = BrokeredMessage.CloneStream(encoder.BodyStream, false))
				{
					using (AmqpMessage amqpMessage = AmqpMessage.CreateAmqpStreamMessageBody(stream))
					{
						encoder.BodyStream = null;
						if ((int)(amqpMessage.BodyType & SectionFlag.Data) != 0 || (int)(amqpMessage.BodyType & SectionFlag.AmqpSequence) != 0)
						{
							encoder.BodyStream = MessageConverter.GetMessageBodyStream(amqpMessage);
						}
						else if ((int)(amqpMessage.BodyType & SectionFlag.AmqpValue) != 0)
						{
							object value = null;
							if (!MessageConverter.TryGetNetObjectFromAmqpObject(amqpMessage.ValueBody.Value, MappingType.MessageBody, out value))
							{
								value = amqpMessage.ValueBody.Value;
							}
							if (value != null)
							{
								DataContractBinarySerializer dataContractBinarySerializer = new DataContractBinarySerializer(value.GetType());
								MemoryStream memoryStream = new MemoryStream(256);
								dataContractBinarySerializer.WriteObject(memoryStream, value);
								memoryStream.Flush();
								memoryStream.Position = (long)0;
								encoder.BodyStream = memoryStream;
							}
						}
					}
				}
			}
			return encoder;
		}

		private static AmqpMessage CreateAmqpMessageFromSbmpMessage(BrokeredMessage brokeredMessage)
		{
			AmqpMessage amqpMessage;
			object obj = brokeredMessage.ClearBodyObject();
			object obj1 = null;
			if (obj != null)
			{
				MessageConverter.TryGetAmqpObjectFromNetObject(obj, MappingType.MessageBody, out obj1);
			}
			if (obj1 != null)
			{
				AmqpValue amqpValue = new AmqpValue()
				{
					Value = obj1
				};
				amqpMessage = AmqpMessage.Create(amqpValue);
			}
			else if (brokeredMessage.BodyStream == null)
			{
				amqpMessage = AmqpMessage.Create();
			}
			else
			{
				if (brokeredMessage.BodyStream.CanSeek && brokeredMessage.BodyStream.Position != (long)0)
				{
					throw new InvalidOperationException(SRClient.CannotSerializeMessageWithPartiallyConsumedBodyStream);
				}
				amqpMessage = AmqpMessage.Create(brokeredMessage.BodyStream, false);
			}
			return amqpMessage;
		}

		public static EventHubRuntimeInformation GetEventHubRuntimeInfo(AmqpMessage amqpMessage)
		{
			EventHubRuntimeInformation eventHubRuntimeInformation = null;
			object obj = null;
			if (MessageConverter.TryGetNetObjectFromAmqpObject(amqpMessage.ValueBody.Value, MappingType.MessageBody, out obj))
			{
				Dictionary<string, object> strs = obj as Dictionary<string, object>;
				if (strs != null)
				{
					eventHubRuntimeInformation = new EventHubRuntimeInformation();
					object obj1 = null;
					if (strs.TryGetValue("name", out obj1))
					{
						eventHubRuntimeInformation.Path = (string)obj1;
					}
					if (strs.TryGetValue("partition_count", out obj1))
					{
						eventHubRuntimeInformation.PartitionCount = (int)obj1;
					}
					if (strs.TryGetValue("created_at", out obj1))
					{
						eventHubRuntimeInformation.CreatedAt = (DateTime)obj1;
					}
					if (strs.TryGetValue("partition_ids", out obj1))
					{
						eventHubRuntimeInformation.PartitionIds = (string[])obj1;
					}
				}
			}
			return eventHubRuntimeInformation;
		}

		public static Filter GetFilter(AmqpFilter amqpFilter)
		{
			Filter sqlFilter;
			if (amqpFilter.DescriptorCode == AmqpSqlFilter.Code)
			{
				sqlFilter = new SqlFilter(((AmqpSqlFilter)amqpFilter).Expression);
			}
			else if (amqpFilter.DescriptorCode == AmqpTrueFilter.Code)
			{
				sqlFilter = new TrueFilter();
			}
			else if (amqpFilter.DescriptorCode != AmqpFalseFilter.Code)
			{
				if (amqpFilter.DescriptorCode != AmqpCorrelationFilter.Code)
				{
					throw new NotSupportedException();
				}
				sqlFilter = new CorrelationFilter(((AmqpCorrelationFilter)amqpFilter).CorrelationId);
			}
			else
			{
				sqlFilter = new FalseFilter();
			}
			return sqlFilter;
		}

		public static AmqpFilter GetFilter(Filter filter)
		{
			AmqpFilter amqpTrueFilter = null;
			if (filter is TrueFilter)
			{
				amqpTrueFilter = new AmqpTrueFilter();
			}
			else if (filter is FalseFilter)
			{
				amqpTrueFilter = new AmqpFalseFilter();
			}
			else if (!(filter is SqlFilter))
			{
				if (!(filter is CorrelationFilter))
				{
					throw new NotSupportedException();
				}
				amqpTrueFilter = new AmqpCorrelationFilter()
				{
					CorrelationId = ((CorrelationFilter)filter).CorrelationId
				};
			}
			else
			{
				AmqpSqlFilter amqpSqlFilter = new AmqpSqlFilter();
				SqlFilter sqlFilter = (SqlFilter)filter;
				amqpSqlFilter.Expression = sqlFilter.SqlExpression;
				amqpSqlFilter.CompatibilityLevel = new int?(sqlFilter.CompatibilityLevel);
				amqpTrueFilter = amqpSqlFilter;
			}
			return amqpTrueFilter;
		}

		private static Stream GetMessageBodyStream(AmqpMessage message)
		{
			if ((int)(message.BodyType & SectionFlag.Data) == 0 || message.DataBody == null)
			{
				return null;
			}
			List<ArraySegment<byte>> arraySegments = new List<ArraySegment<byte>>();
			foreach (Data dataBody in message.DataBody)
			{
				arraySegments.Add((ArraySegment<byte>)dataBody.Value);
			}
			return new BufferListStream(arraySegments.ToArray());
		}

		private static RuleAction GetRuleAction(AmqpRuleAction amqpAction)
		{
			RuleAction @default;
			if (amqpAction.DescriptorCode != AmqpEmptyRuleAction.Code)
			{
				if (amqpAction.DescriptorCode != AmqpSqlRuleAction.Code)
				{
					throw new NotSupportedException();
				}
				AmqpSqlRuleAction amqpSqlRuleAction = (AmqpSqlRuleAction)amqpAction;
				@default = (!amqpSqlRuleAction.CompatibilityLevel.HasValue ? new SqlRuleAction(amqpSqlRuleAction.SqlExpression) : new SqlRuleAction(amqpSqlRuleAction.SqlExpression, amqpSqlRuleAction.CompatibilityLevel.Value));
			}
			else
			{
				@default = EmptyRuleAction.Default;
			}
			return @default;
		}

		private static AmqpRuleAction GetRuleAction(RuleAction ruleAction)
		{
			AmqpRuleAction amqpEmptyRuleAction = null;
			if (ruleAction == null || ruleAction is EmptyRuleAction)
			{
				amqpEmptyRuleAction = new AmqpEmptyRuleAction();
			}
			else
			{
				if (!(ruleAction is SqlRuleAction))
				{
					throw new NotSupportedException();
				}
				AmqpSqlRuleAction amqpSqlRuleAction = new AmqpSqlRuleAction();
				SqlRuleAction sqlRuleAction = (SqlRuleAction)ruleAction;
				amqpSqlRuleAction.SqlExpression = sqlRuleAction.SqlExpression;
				amqpSqlRuleAction.CompatibilityLevel = new int?(sqlRuleAction.CompatibilityLevel);
				amqpEmptyRuleAction = amqpSqlRuleAction;
			}
			return amqpEmptyRuleAction;
		}

		public static RuleDescription GetRuleDescription(AmqpRuleDescription amqpDescription)
		{
			Filter filter = MessageConverter.GetFilter(amqpDescription.Filter);
			RuleAction ruleAction = MessageConverter.GetRuleAction(amqpDescription.Action);
			return new RuleDescription(filter)
			{
				Action = ruleAction
			};
		}

		public static AmqpRuleDescription GetRuleDescription(RuleDescription description)
		{
			AmqpFilter filter = MessageConverter.GetFilter(description.Filter);
			AmqpRuleAction ruleAction = MessageConverter.GetRuleAction(description.Action);
			return new AmqpRuleDescription()
			{
				Filter = filter,
				Action = ruleAction
			};
		}

		public static ArraySegment<byte> ReadStream(Stream stream)
		{
			MemoryStream memoryStream = new MemoryStream();
			byte[] numArray = new byte[512];
			while (true)
			{
				int num = stream.Read(numArray, 0, (int)numArray.Length);
				int num1 = num;
				if (num <= 0)
				{
					break;
				}
				memoryStream.Write(numArray, 0, num1);
			}
			return new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
		}

		public static bool TryGetAmqpObjectFromNetObject(object netObject, MappingType mappingType, out object amqpObject)
		{
			amqpObject = null;
			if (netObject == null)
			{
				return false;
			}
			switch (SerializationUtilities.GetTypeId(netObject))
			{
				case PropertyValueType.Byte:
				case PropertyValueType.SByte:
				case PropertyValueType.Char:
				case PropertyValueType.Int16:
				case PropertyValueType.UInt16:
				case PropertyValueType.Int32:
				case PropertyValueType.UInt32:
				case PropertyValueType.Int64:
				case PropertyValueType.UInt64:
				case PropertyValueType.Single:
				case PropertyValueType.Double:
				case PropertyValueType.Decimal:
				case PropertyValueType.Boolean:
				case PropertyValueType.Guid:
				case PropertyValueType.String:
				case PropertyValueType.DateTime:
				{
					amqpObject = netObject;
					break;
				}
				case PropertyValueType.Uri:
				{
					amqpObject = new DescribedType((object)"com.microsoft:uri", ((Uri)netObject).AbsoluteUri);
					break;
				}
				case PropertyValueType.DateTimeOffset:
				{
					object obj = "com.microsoft:datetime-offset";
					DateTimeOffset dateTimeOffset = (DateTimeOffset)netObject;
					amqpObject = new DescribedType(obj, (object)dateTimeOffset.UtcTicks);
					break;
				}
				case PropertyValueType.TimeSpan:
				{
					object obj1 = "com.microsoft:timespan";
					TimeSpan timeSpan = (TimeSpan)netObject;
					amqpObject = new DescribedType(obj1, (object)timeSpan.Ticks);
					break;
				}
				case PropertyValueType.Stream:
				{
					if (mappingType != MappingType.ApplicationProperty)
					{
						break;
					}
					amqpObject = MessageConverter.ReadStream((Stream)netObject);
					break;
				}
				case PropertyValueType.Unknown:
				{
					if (!(netObject is Stream))
					{
						if (mappingType == MappingType.ApplicationProperty)
						{
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new SerializationException(SRClient.FailedToSerializeUnsupportedType(netObject.GetType().FullName)), null);
						}
						if (netObject is byte[])
						{
							amqpObject = new ArraySegment<byte>((byte[])netObject);
							break;
						}
						else if (!(netObject is IList))
						{
							if (!(netObject is IDictionary))
							{
								break;
							}
							amqpObject = new AmqpMap((IDictionary)netObject);
							break;
						}
						else
						{
							amqpObject = netObject;
							break;
						}
					}
					else
					{
						if (mappingType != MappingType.ApplicationProperty)
						{
							break;
						}
						amqpObject = MessageConverter.ReadStream((Stream)netObject);
						break;
					}
				}
			}
			return amqpObject != null;
		}

		public static bool TryGetNetObjectFromAmqpObject(object amqpObject, MappingType mappingType, out object netObject)
		{
			netObject = null;
			if (amqpObject == null)
			{
				return false;
			}
			switch (SerializationUtilities.GetTypeId(amqpObject))
			{
				case PropertyValueType.Byte:
				case PropertyValueType.SByte:
				case PropertyValueType.Char:
				case PropertyValueType.Int16:
				case PropertyValueType.UInt16:
				case PropertyValueType.Int32:
				case PropertyValueType.UInt32:
				case PropertyValueType.Int64:
				case PropertyValueType.UInt64:
				case PropertyValueType.Single:
				case PropertyValueType.Double:
				case PropertyValueType.Decimal:
				case PropertyValueType.Boolean:
				case PropertyValueType.Guid:
				case PropertyValueType.String:
				case PropertyValueType.DateTime:
				{
					netObject = amqpObject;
					return netObject != null;
				}
				case PropertyValueType.Uri:
				case PropertyValueType.DateTimeOffset:
				case PropertyValueType.TimeSpan:
				case PropertyValueType.Stream:
				{
					return netObject != null;
				}
				case PropertyValueType.Unknown:
				{
					if (amqpObject is AmqpSymbol)
					{
						netObject = ((AmqpSymbol)amqpObject).Value;
						return netObject != null;
					}
					else if (amqpObject is ArraySegment<byte>)
					{
						ArraySegment<byte> nums = (ArraySegment<byte>)amqpObject;
						if (nums.Count != (int)nums.Array.Length)
						{
							byte[] numArray = new byte[nums.Count];
							Buffer.BlockCopy(nums.Array, nums.Offset, numArray, 0, nums.Count);
							netObject = numArray;
							return netObject != null;
						}
						else
						{
							netObject = nums.Array;
							return netObject != null;
						}
					}
					else if (!(amqpObject is DescribedType))
					{
						if (mappingType == MappingType.ApplicationProperty)
						{
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new SerializationException(SRClient.FailedToSerializeUnsupportedType(amqpObject.GetType().FullName)), null);
						}
						if (!(amqpObject is AmqpMap))
						{
							netObject = amqpObject;
							return netObject != null;
						}
						else
						{
							AmqpMap amqpMaps = (AmqpMap)amqpObject;
							Dictionary<string, object> strs = new Dictionary<string, object>();
							foreach (KeyValuePair<MapKey, object> keyValuePair in (IEnumerable<KeyValuePair<MapKey, object>>)amqpMaps)
							{
								strs.Add(keyValuePair.Key.ToString(), keyValuePair.Value);
							}
							netObject = strs;
							return netObject != null;
						}
					}
					else
					{
						DescribedType describedType = (DescribedType)amqpObject;
						if (!(describedType.Descriptor is AmqpSymbol))
						{
							return netObject != null;
						}
						AmqpSymbol descriptor = (AmqpSymbol)describedType.Descriptor;
						if (descriptor.Equals("com.microsoft:uri"))
						{
							netObject = new Uri((string)describedType.Value);
							return netObject != null;
						}
						else if (!descriptor.Equals("com.microsoft:timespan"))
						{
							if (!descriptor.Equals("com.microsoft:datetime-offset"))
							{
								return netObject != null;
							}
							netObject = new DateTimeOffset(new DateTime((long)describedType.Value, DateTimeKind.Utc));
							return netObject != null;
						}
						else
						{
							netObject = new TimeSpan((long)describedType.Value);
							return netObject != null;
						}
					}
				}
				default:
				{
					return netObject != null;
				}
			}
		}

		public static void UpdateAmqpMessageHeadersAndProperties(AmqpMessage message, EventData eventData, bool copyUserProeperties = true)
		{
			if (!string.IsNullOrEmpty(eventData.Publisher))
			{
				message.MessageAnnotations.Map["x-opt-publisher"] = eventData.Publisher;
			}
			if (eventData.PartitionKey != null)
			{
				message.MessageAnnotations.Map["x-opt-partition-key"] = eventData.PartitionKey;
			}
			if (copyUserProeperties && eventData.Properties.Count > 0)
			{
				if (message.ApplicationProperties == null)
				{
					message.ApplicationProperties = new ApplicationProperties();
				}
				foreach (KeyValuePair<string, object> property in eventData.Properties)
				{
					object obj = null;
					if (!MessageConverter.TryGetAmqpObjectFromNetObject(property.Value, MappingType.ApplicationProperty, out obj))
					{
						continue;
					}
					message.ApplicationProperties.Map[property.Key] = obj;
				}
			}
		}

		public static void UpdateAmqpMessageHeadersAndProperties(AmqpMessage message, BrokeredMessage brokeredMessage, bool amqpClient)
		{
			BrokeredMessage.MessageMembers initializedMembers = brokeredMessage.InitializedMembers;
			if ((int)(initializedMembers & BrokeredMessage.MessageMembers.DeliveryCount) != 0)
			{
				message.Header.DeliveryCount = new uint?((uint)(brokeredMessage.DeliveryCount - (amqpClient ? 1 : 0)));
			}
			if ((int)(initializedMembers & BrokeredMessage.MessageMembers.TimeToLive) != 0 && brokeredMessage.TimeToLive != TimeSpan.MaxValue)
			{
				message.Header.Ttl = new uint?((uint)brokeredMessage.TimeToLive.TotalMilliseconds);
			}
			if ((int)(initializedMembers & BrokeredMessage.MessageMembers.EnqueuedTimeUtc) != 0)
			{
				message.MessageAnnotations.Map["x-opt-enqueued-time"] = brokeredMessage.EnqueuedTimeUtc;
			}
			if ((int)(initializedMembers & BrokeredMessage.MessageMembers.SequenceNumber) != 0)
			{
				message.MessageAnnotations.Map["x-opt-sequence-number"] = brokeredMessage.SequenceNumber;
			}
			if (amqpClient && (int)(initializedMembers & BrokeredMessage.MessageMembers.LockedUntilUtc) != 0)
			{
				message.MessageAnnotations.Map["x-opt-locked-until"] = brokeredMessage.LockedUntilUtc;
			}
			if (amqpClient && (int)(initializedMembers & BrokeredMessage.MessageMembers.Publisher) != 0)
			{
				message.MessageAnnotations.Map["x-opt-publisher"] = brokeredMessage.Publisher;
			}
			if (amqpClient && (int)(initializedMembers & BrokeredMessage.MessageMembers.PartitionKey) != 0)
			{
				message.MessageAnnotations.Map["x-opt-partition-key"] = brokeredMessage.PartitionKey;
			}
			if (brokeredMessage.ArePropertiesModifiedByBroker && brokeredMessage.Properties.Count > 0)
			{
				if (message.ApplicationProperties == null)
				{
					message.ApplicationProperties = new ApplicationProperties();
				}
				foreach (KeyValuePair<string, object> property in brokeredMessage.Properties)
				{
					object obj = null;
					if (!MessageConverter.TryGetAmqpObjectFromNetObject(property.Value, MappingType.ApplicationProperty, out obj))
					{
						continue;
					}
					message.ApplicationProperties.Map[property.Key] = obj;
				}
			}
		}

		public static void UpdateBrokeredMessageHeaderAndProperties(AmqpMessage amqpMessage, BrokeredMessage message)
		{
			DateTime dateTime;
			string str;
			string str1;
			SectionFlag sections = amqpMessage.Sections;
			if ((int)(sections & SectionFlag.Header) != 0 && amqpMessage.Header.Ttl.HasValue)
			{
				uint? ttl = amqpMessage.Header.Ttl;
				message.TimeToLive = TimeSpan.FromMilliseconds((double)((float)ttl.Value));
			}
			if ((int)(sections & SectionFlag.Properties) != 0)
			{
				if (amqpMessage.Properties.MessageId != null)
				{
					message.MessageId = amqpMessage.Properties.MessageId.ToString();
				}
				if (amqpMessage.Properties.CorrelationId != null)
				{
					message.CorrelationId = amqpMessage.Properties.CorrelationId.ToString();
				}
				if (amqpMessage.Properties.ContentType.Value != null)
				{
					message.ContentType = amqpMessage.Properties.ContentType.Value;
				}
				if (amqpMessage.Properties.Subject != null)
				{
					message.Label = amqpMessage.Properties.Subject;
				}
				if (amqpMessage.Properties.To != null)
				{
					message.To = amqpMessage.Properties.To.ToString();
				}
				if (amqpMessage.Properties.ReplyTo != null)
				{
					message.ReplyTo = amqpMessage.Properties.ReplyTo.ToString();
				}
				if (amqpMessage.Properties.GroupId != null)
				{
					message.SessionId = amqpMessage.Properties.GroupId;
				}
				if (amqpMessage.Properties.ReplyToGroupId != null)
				{
					message.ReplyToSessionId = amqpMessage.Properties.ReplyToGroupId;
				}
			}
			if ((int)(sections & SectionFlag.MessageAnnotations) != 0)
			{
				if (amqpMessage.MessageAnnotations.Map.TryGetValue<DateTime>("x-opt-scheduled-enqueue-time", out dateTime))
				{
					message.ScheduledEnqueueTimeUtc = dateTime;
				}
				if (amqpMessage.MessageAnnotations.Map.TryGetValue<string>("x-opt-publisher", out str))
				{
					message.Publisher = str;
				}
				if (amqpMessage.MessageAnnotations.Map.TryGetValue<string>("x-opt-partition-key", out str1))
				{
					message.PartitionKey = str1;
				}
			}
			if ((int)(sections & SectionFlag.ApplicationProperties) != 0)
			{
				foreach (KeyValuePair<MapKey, object> map in (IEnumerable<KeyValuePair<MapKey, object>>)amqpMessage.ApplicationProperties.Map)
				{
					object obj = null;
					if (!MessageConverter.TryGetNetObjectFromAmqpObject(map.Value, MappingType.ApplicationProperty, out obj))
					{
						continue;
					}
					message.InternalProperties[map.Key.ToString()] = obj;
				}
			}
		}

		public static void UpdateEventDataHeaderAndProperties(AmqpMessage amqpMessage, EventData data)
		{
			string str;
			string str1;
			DateTime dateTime;
			long num;
			string str2;
			ArraySegment<byte> deliveryTag = amqpMessage.DeliveryTag;
			Fx.AssertAndThrow(true, "AmqpMessage should always contain delivery tag.");
			data.DeliveryTag = amqpMessage.DeliveryTag;
			SectionFlag sections = amqpMessage.Sections;
			if ((int)(sections & SectionFlag.MessageAnnotations) != 0)
			{
				if (amqpMessage.MessageAnnotations.Map.TryGetValue<string>("x-opt-publisher", out str))
				{
					data.Publisher = str;
				}
				if (amqpMessage.MessageAnnotations.Map.TryGetValue<string>("x-opt-partition-key", out str1))
				{
					data.PartitionKey = str1;
				}
				if (amqpMessage.MessageAnnotations.Map.TryGetValue<DateTime>("x-opt-enqueued-time", out dateTime))
				{
					data.EnqueuedTimeUtc = dateTime;
				}
				if (amqpMessage.MessageAnnotations.Map.TryGetValue<long>("x-opt-sequence-number", out num))
				{
					data.SequenceNumber = num;
				}
				if (amqpMessage.MessageAnnotations.Map.TryGetValue<string>("x-opt-offset", out str2))
				{
					data.Offset = str2;
				}
			}
			if ((int)(sections & SectionFlag.ApplicationProperties) != 0)
			{
				foreach (KeyValuePair<MapKey, object> map in (IEnumerable<KeyValuePair<MapKey, object>>)amqpMessage.ApplicationProperties.Map)
				{
					object obj = null;
					if (!MessageConverter.TryGetNetObjectFromAmqpObject(map.Value, MappingType.ApplicationProperty, out obj))
					{
						continue;
					}
					data.Properties[map.Key.ToString()] = obj;
				}
			}
			if ((int)(sections & SectionFlag.Properties) != 0)
			{
				foreach (KeyValuePair<string, object> dictionary in amqpMessage.Properties.ToDictionary())
				{
					if (dictionary.Value is MessageId || dictionary.Value is Address || dictionary.Value is AmqpSymbol)
					{
						data.SystemProperties.Add(dictionary.Key, dictionary.Value.ToString());
					}
					else
					{
						data.SystemProperties.Add(dictionary);
					}
				}
			}
		}
	}
}