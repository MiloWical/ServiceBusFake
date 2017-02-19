using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal class RequestInfo : IXmlSerializable
	{
		private const int MaximumHeaderFieldSize = 65535;

		private const int RequestInfoVersion1 = 1;

		private const int RequestInfoVersion = 1;

		private static RequestInfo.BinarySerializationItem[] BinarySerializationItems;

		private int version;

		private RequestInfo.RequestInfoFields initializedMembers;

		private int apiVersion;

		private TimeSpan? operationTimeout;

		private TimeSpan? serverTimeout;

		private string messageId;

		private string sessionId;

		private Guid? lockToken;

		private string partitionKey;

		private string viaPartitionKey;

		private short? partitionId;

		private long? fromSequenceNumber;

		private int? skip;

		private int? top;

		private DateTime? lastUpdatedTime;

		private int? messageCount;

		private string transactionId;

		private string destination;

		public int ApiVersion
		{
			get
			{
				return this.apiVersion;
			}
			set
			{
				this.apiVersion = value;
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.ApiVersion;
			}
		}

		public string Destination
		{
			get
			{
				return this.destination;
			}
			set
			{
				this.destination = value;
				if (value == null)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.Destination);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.Destination;
			}
		}

		public DateTime? LastUpdatedTime
		{
			get
			{
				return this.lastUpdatedTime;
			}
			set
			{
				this.lastUpdatedTime = value;
				if (!value.HasValue)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.LastUpdatedTime);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.LastUpdatedTime;
			}
		}

		public Guid? LockToken
		{
			get
			{
				return this.lockToken;
			}
			set
			{
				this.lockToken = value;
				if (!value.HasValue)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.LockToken);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.LockToken;
			}
		}

		public int? MessageCount
		{
			get
			{
				return this.messageCount;
			}
			set
			{
				this.messageCount = value;
				if (!value.HasValue)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.MessageCount);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.MessageCount;
			}
		}

		public string MessageId
		{
			get
			{
				return this.messageId;
			}
			set
			{
				this.messageId = value;
				if (value == null)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.MessageId);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.MessageId;
			}
		}

		public TimeSpan? OperationTimeout
		{
			get
			{
				return this.operationTimeout;
			}
			set
			{
				this.operationTimeout = value;
				if (!value.HasValue)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.OperationTimeout);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.OperationTimeout;
			}
		}

		public short? PartitionId
		{
			get
			{
				return this.partitionId;
			}
			set
			{
				int? nullable;
				this.partitionId = value;
				short? nullable1 = value;
				if (nullable1.HasValue)
				{
					nullable = new int?(nullable1.GetValueOrDefault());
				}
				else
				{
					nullable = null;
				}
				if (!nullable.HasValue)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.PartitionId);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.PartitionId;
			}
		}

		public string PartitionKey
		{
			get
			{
				return this.partitionKey;
			}
			set
			{
				this.partitionKey = value;
				if (value == null)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.PartitionKey);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.PartitionKey;
			}
		}

		public long? SequenceNumber
		{
			get
			{
				return this.fromSequenceNumber;
			}
			set
			{
				this.fromSequenceNumber = value;
				if (!value.HasValue)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.SequenceNumber);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.SequenceNumber;
			}
		}

		public TimeSpan? ServerTimeout
		{
			get
			{
				return this.serverTimeout;
			}
			set
			{
				this.serverTimeout = value;
				if (!value.HasValue)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.ServerTimeout);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.ServerTimeout;
			}
		}

		public string SessionId
		{
			get
			{
				return this.sessionId;
			}
			set
			{
				this.sessionId = value;
				if (value == null)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.SessionId);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.SessionId;
			}
		}

		public int? Skip
		{
			get
			{
				return this.skip;
			}
			set
			{
				this.skip = value;
				if (!value.HasValue)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.Skip);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.Skip;
			}
		}

		public int? Top
		{
			get
			{
				return this.top;
			}
			set
			{
				this.top = value;
				if (!value.HasValue)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.Top);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.Top;
			}
		}

		public string TransactionId
		{
			get
			{
				return this.transactionId;
			}
			set
			{
				this.transactionId = value;
				if (value == null)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.TransactionId);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.TransactionId;
			}
		}

		public string ViaPartitionKey
		{
			get
			{
				return this.viaPartitionKey;
			}
			set
			{
				this.viaPartitionKey = value;
				if (value == null)
				{
					this.ClearInitializedMember(RequestInfo.RequestInfoFields.ViaPartitionKey);
					return;
				}
				RequestInfo requestInfo = this;
				requestInfo.initializedMembers = requestInfo.initializedMembers | RequestInfo.RequestInfoFields.ViaPartitionKey;
			}
		}

		static RequestInfo()
		{
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray = new RequestInfo.BinarySerializationItem[16];
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray1 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.ApiVersion,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.ApiVersion) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.Int32, requestInfo.ApiVersion)
			};
			binarySerializationItemArray1[0] = binarySerializationItem;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray2 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem1 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.OperationTimeout,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.OperationTimeout) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.TimeSpan, requestInfo.OperationTimeout)
			};
			binarySerializationItemArray2[1] = binarySerializationItem1;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray3 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem2 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.ServerTimeout,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.ServerTimeout) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.TimeSpan, requestInfo.ServerTimeout)
			};
			binarySerializationItemArray3[2] = binarySerializationItem2;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray4 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem3 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.MessageId,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.MessageId) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.String, requestInfo.MessageId)
			};
			binarySerializationItemArray4[3] = binarySerializationItem3;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray5 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem4 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.SessionId,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.SessionId) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.String, requestInfo.SessionId)
			};
			binarySerializationItemArray5[4] = binarySerializationItem4;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray6 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem5 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.LockToken,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.LockToken) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.Guid, requestInfo.LockToken)
			};
			binarySerializationItemArray6[5] = binarySerializationItem5;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray7 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem6 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.PartitionKey,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.PartitionKey) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.String, requestInfo.PartitionKey)
			};
			binarySerializationItemArray7[6] = binarySerializationItem6;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray8 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem7 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.ViaPartitionKey,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.ViaPartitionKey) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.String, requestInfo.ViaPartitionKey)
			};
			binarySerializationItemArray8[7] = binarySerializationItem7;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray9 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem8 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.PartitionId,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.PartitionId) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.Int16, requestInfo.PartitionId)
			};
			binarySerializationItemArray9[8] = binarySerializationItem8;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray10 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem9 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.SequenceNumber,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.SequenceNumber) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.Int64, requestInfo.SequenceNumber)
			};
			binarySerializationItemArray10[9] = binarySerializationItem9;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray11 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem10 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.Skip,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.Skip) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.Int32, requestInfo.Skip)
			};
			binarySerializationItemArray11[10] = binarySerializationItem10;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray12 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem11 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.Top,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.Top) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.Int32, requestInfo.Top)
			};
			binarySerializationItemArray12[11] = binarySerializationItem11;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray13 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem12 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.LastUpdatedTime,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.LastUpdatedTime) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.DateTime, requestInfo.LastUpdatedTime)
			};
			binarySerializationItemArray13[12] = binarySerializationItem12;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray14 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem13 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.MessageCount,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.MessageCount) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.Int32, requestInfo.MessageCount)
			};
			binarySerializationItemArray14[13] = binarySerializationItem13;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray15 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem14 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.TransactionId,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.TransactionId) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.String, requestInfo.TransactionId)
			};
			binarySerializationItemArray15[14] = binarySerializationItem14;
			RequestInfo.BinarySerializationItem[] binarySerializationItemArray16 = binarySerializationItemArray;
			RequestInfo.BinarySerializationItem binarySerializationItem15 = new RequestInfo.BinarySerializationItem()
			{
				FieldId = RequestInfo.FieldId.Destination,
				ShouldSerialize = (RequestInfo msg) => (int)(msg.initializedMembers & RequestInfo.RequestInfoFields.Destination) != 0,
				Extractor = (RequestInfo requestInfo) => SerializationUtilities.ConvertNativeValueToByteArray(requestInfo.version, PropertyValueType.String, requestInfo.Destination)
			};
			binarySerializationItemArray16[15] = binarySerializationItem15;
			RequestInfo.BinarySerializationItems = binarySerializationItemArray;
		}

		public RequestInfo()
		{
			this.version = 1;
			this.ApiVersion = ApiVersionHelper.CurrentRuntimeApiVersion;
		}

		private void ClearInitializedMember(RequestInfo.RequestInfoFields memberToClear)
		{
			RequestInfo requestInfo = this;
			requestInfo.initializedMembers = requestInfo.initializedMembers & ~memberToClear;
		}

		public RequestInfo Clone()
		{
			RequestInfo requestInfo = new RequestInfo();
			if (this.IsSet(RequestInfo.RequestInfoFields.ApiVersion))
			{
				requestInfo.ApiVersion = this.ApiVersion;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.OperationTimeout))
			{
				requestInfo.OperationTimeout = this.OperationTimeout;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.ServerTimeout))
			{
				requestInfo.ServerTimeout = this.ServerTimeout;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.MessageId))
			{
				requestInfo.MessageId = this.MessageId;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.SessionId))
			{
				requestInfo.SessionId = this.SessionId;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.LockToken))
			{
				requestInfo.LockToken = this.LockToken;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.PartitionKey))
			{
				requestInfo.PartitionKey = this.PartitionKey;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.ViaPartitionKey))
			{
				requestInfo.ViaPartitionKey = this.ViaPartitionKey;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.PartitionId))
			{
				requestInfo.PartitionId = this.PartitionId;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.SequenceNumber))
			{
				requestInfo.SequenceNumber = this.SequenceNumber;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.Skip))
			{
				requestInfo.Skip = this.Skip;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.Top))
			{
				requestInfo.Top = this.Top;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.LastUpdatedTime))
			{
				requestInfo.LastUpdatedTime = this.LastUpdatedTime;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.MessageCount))
			{
				requestInfo.MessageCount = this.MessageCount;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.TransactionId))
			{
				requestInfo.TransactionId = this.TransactionId;
			}
			if (this.IsSet(RequestInfo.RequestInfoFields.Destination))
			{
				requestInfo.Destination = this.Destination;
			}
			return requestInfo;
		}

		private short GetFieldCount()
		{
			int num = (int)this.initializedMembers;
			short num1 = 0;
			while (num != 0)
			{
				num1 = (short)(num1 + 1);
				num = num & num - 1;
			}
			return num1;
		}

		public XmlSchema GetSchema()
		{
			return null;
		}

		private bool IsSet(RequestInfo.RequestInfoFields requestInfoFields)
		{
			return (int)(this.initializedMembers & requestInfoFields) != 0;
		}

		public void ReadXml(XmlReader reader)
		{
			reader.Read();
			reader.ReadStartElement();
			this.version = BitConverter.ToInt32(SerializationUtilities.ReadBytes(reader, 4), 0);
			int num = BitConverter.ToInt16(SerializationUtilities.ReadBytes(reader, 2), 0);
			for (int i = 0; i < num; i++)
			{
				byte[] numArray = SerializationUtilities.ReadBytes(reader, 3);
				int num1 = BitConverter.ToInt16(numArray, 1);
				byte[] numArray1 = SerializationUtilities.ReadBytes(reader, num1);
				this.SetFieldValue(numArray[0], numArray1);
			}
			reader.ReadEndElement();
		}

		private void SetFieldValue(byte fieldId, byte[] value)
		{
			switch (fieldId)
			{
				case 0:
				{
					this.ApiVersion = (int)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int32, value);
					return;
				}
				case 1:
				{
					this.OperationTimeout = new TimeSpan?((TimeSpan)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.TimeSpan, value));
					return;
				}
				case 2:
				{
					this.ServerTimeout = new TimeSpan?((TimeSpan)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.TimeSpan, value));
					return;
				}
				case 3:
				{
					this.MessageId = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 4:
				{
					this.SessionId = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 5:
				{
					this.LockToken = new Guid?((Guid)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Guid, value));
					return;
				}
				case 6:
				{
					this.PartitionKey = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 7:
				{
					this.ViaPartitionKey = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 8:
				{
					this.PartitionId = new short?((short)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int16, value));
					return;
				}
				case 9:
				{
					this.SequenceNumber = new long?((long)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int64, value));
					return;
				}
				case 10:
				{
					this.Skip = new int?((int)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int32, value));
					return;
				}
				case 11:
				{
					this.Top = new int?((int)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int32, value));
					return;
				}
				case 12:
				{
					this.LastUpdatedTime = new DateTime?((DateTime)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.DateTime, value));
					return;
				}
				case 13:
				{
					this.MessageCount = new int?((int)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int32, value));
					return;
				}
				case 14:
				{
					this.TransactionId = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 15:
				{
					this.Destination = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				default:
				{
					return;
				}
			}
		}

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteStartElement("s");
			writer.WriteBase64(BitConverter.GetBytes(this.version), 0, 4);
			writer.WriteBase64(BitConverter.GetBytes(this.GetFieldCount()), 0, 2);
			byte[] fieldId = new byte[3];
			RequestInfo.BinarySerializationItem[] binarySerializationItems = RequestInfo.BinarySerializationItems;
			for (int i = 0; i < (int)binarySerializationItems.Length; i++)
			{
				RequestInfo.BinarySerializationItem binarySerializationItem = binarySerializationItems[i];
				if (binarySerializationItem.ShouldSerialize(this))
				{
					fieldId[0] = (byte)binarySerializationItem.FieldId;
					byte[] extractor = binarySerializationItem.Extractor(this);
					if ((int)extractor.Length > 65535)
					{
						throw Fx.Exception.AsError(new SerializationException(SRClient.ExceededMessagePropertySizeLimit(binarySerializationItem.FieldId.ToString(), 65535)), null);
					}
					fieldId[1] = (byte)((int)extractor.Length & 255);
					fieldId[2] = (byte)(((int)extractor.Length & 65280) >> 8);
					writer.WriteBase64(fieldId, 0, 3);
					writer.WriteBase64(extractor, 0, (int)extractor.Length);
				}
			}
			writer.Flush();
			writer.WriteEndElement();
		}

		private sealed class BinarySerializationItem
		{
			public Func<RequestInfo, byte[]> Extractor
			{
				get;
				set;
			}

			public RequestInfo.FieldId FieldId
			{
				get;
				set;
			}

			public Func<RequestInfo, bool> ShouldSerialize
			{
				get;
				set;
			}

			public BinarySerializationItem()
			{
			}
		}

		private enum FieldId : byte
		{
			ApiVersion,
			OperationTimeout,
			ServerTimeout,
			MessageId,
			SessionId,
			LockToken,
			PartitionKey,
			ViaPartitionKey,
			PartitionId,
			SequenceNumber,
			Skip,
			Top,
			LastUpdatedTime,
			MessageCount,
			TransactionId,
			Destination
		}

		[Flags]
		private enum RequestInfoFields
		{
			ApiVersion = 1,
			OperationTimeout = 2,
			ServerTimeout = 4,
			MessageId = 8,
			SessionId = 16,
			LockToken = 32,
			PartitionKey = 64,
			ViaPartitionKey = 128,
			PartitionId = 256,
			SequenceNumber = 512,
			Skip = 1024,
			Top = 2048,
			LastUpdatedTime = 4096,
			MessageCount = 8192,
			TransactionId = 16384,
			Destination = 32768
		}
	}
}