using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Properties : DescribedList
	{
		private const int Fields = 13;

		public readonly static string Name;

		public readonly static ulong Code;

		private readonly static string MessageIdName;

		private readonly static string UserIdName;

		private readonly static string ToName;

		private readonly static string SubjectName;

		private readonly static string ReplyToName;

		private readonly static string CorrelationIdName;

		private readonly static string ContentTypeName;

		private readonly static string ContentEncodingName;

		private readonly static string AbsoluteExpiryTimeName;

		private readonly static string CreationTimeName;

		private readonly static string GroupIdName;

		private readonly static string GroupSequenceName;

		private readonly static string ReplyToGroupIdName;

		public DateTime? AbsoluteExpiryTime
		{
			get;
			set;
		}

		public AmqpSymbol ContentEncoding
		{
			get;
			set;
		}

		public AmqpSymbol ContentType
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageId CorrelationId
		{
			get;
			set;
		}

		public DateTime? CreationTime
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 13;
			}
		}

		public string GroupId
		{
			get;
			set;
		}

		public uint? GroupSequence
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageId MessageId
		{
			get;
			set;
		}

		public Address ReplyTo
		{
			get;
			set;
		}

		public string ReplyToGroupId
		{
			get;
			set;
		}

		public string Subject
		{
			get;
			set;
		}

		public Address To
		{
			get;
			set;
		}

		public ArraySegment<byte> UserId
		{
			get;
			set;
		}

		static Properties()
		{
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.Name = "amqp:properties:list";
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.Code = (ulong)115;
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.MessageIdName = "message-id";
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.UserIdName = "user-id";
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ToName = "to";
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.SubjectName = "subject";
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ReplyToName = "reply-to";
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.CorrelationIdName = "correlation-id";
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ContentTypeName = "content-type";
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ContentEncodingName = "content-encoding";
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.AbsoluteExpiryTimeName = "absolute-expiry-time";
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.CreationTimeName = "creation-time";
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.GroupIdName = "group-id";
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.GroupSequenceName = "group-sequence";
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ReplyToGroupIdName = "reply-to-group-id";
		}

		public Properties() : base(Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.Name, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.Code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.MessageId = Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageId.Decode(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.UserId = AmqpCodec.DecodeBinary(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.To = Address.Decode(buffer);
			}
			int num3 = count;
			count = num3 - 1;
			if (num3 > 0)
			{
				this.Subject = AmqpCodec.DecodeString(buffer);
			}
			int num4 = count;
			count = num4 - 1;
			if (num4 > 0)
			{
				this.ReplyTo = Address.Decode(buffer);
			}
			int num5 = count;
			count = num5 - 1;
			if (num5 > 0)
			{
				this.CorrelationId = Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageId.Decode(buffer);
			}
			int num6 = count;
			count = num6 - 1;
			if (num6 > 0)
			{
				this.ContentType = AmqpCodec.DecodeSymbol(buffer);
			}
			int num7 = count;
			count = num7 - 1;
			if (num7 > 0)
			{
				this.ContentEncoding = AmqpCodec.DecodeSymbol(buffer);
			}
			int num8 = count;
			count = num8 - 1;
			if (num8 > 0)
			{
				this.AbsoluteExpiryTime = AmqpCodec.DecodeTimeStamp(buffer);
			}
			int num9 = count;
			count = num9 - 1;
			if (num9 > 0)
			{
				this.CreationTime = AmqpCodec.DecodeTimeStamp(buffer);
			}
			int num10 = count;
			count = num10 - 1;
			if (num10 > 0)
			{
				this.GroupId = AmqpCodec.DecodeString(buffer);
			}
			int num11 = count;
			count = num11 - 1;
			if (num11 > 0)
			{
				this.GroupSequence = AmqpCodec.DecodeUInt(buffer);
			}
			int num12 = count;
			count = num12 - 1;
			if (num12 > 0)
			{
				this.ReplyToGroupId = AmqpCodec.DecodeString(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageId.Encode(buffer, this.MessageId);
			AmqpCodec.EncodeBinary(this.UserId, buffer);
			Address.Encode(buffer, this.To);
			AmqpCodec.EncodeString(this.Subject, buffer);
			Address.Encode(buffer, this.ReplyTo);
			Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageId.Encode(buffer, this.CorrelationId);
			AmqpCodec.EncodeSymbol(this.ContentType, buffer);
			AmqpCodec.EncodeSymbol(this.ContentEncoding, buffer);
			AmqpCodec.EncodeTimeStamp(this.AbsoluteExpiryTime, buffer);
			AmqpCodec.EncodeTimeStamp(this.CreationTime, buffer);
			AmqpCodec.EncodeString(this.GroupId, buffer);
			AmqpCodec.EncodeUInt(this.GroupSequence, buffer);
			AmqpCodec.EncodeString(this.ReplyToGroupId, buffer);
		}

		protected override int OnValueSize()
		{
			int encodeSize = 0;
			encodeSize = Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageId.GetEncodeSize(this.MessageId);
			encodeSize = encodeSize + AmqpCodec.GetBinaryEncodeSize(this.UserId);
			encodeSize = encodeSize + Address.GetEncodeSize(this.To);
			encodeSize = encodeSize + AmqpCodec.GetStringEncodeSize(this.Subject);
			encodeSize = encodeSize + Address.GetEncodeSize(this.ReplyTo);
			encodeSize = encodeSize + Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageId.GetEncodeSize(this.CorrelationId);
			encodeSize = encodeSize + AmqpCodec.GetSymbolEncodeSize(this.ContentType);
			encodeSize = encodeSize + AmqpCodec.GetSymbolEncodeSize(this.ContentEncoding);
			encodeSize = encodeSize + AmqpCodec.GetTimeStampEncodeSize(this.AbsoluteExpiryTime);
			encodeSize = encodeSize + AmqpCodec.GetTimeStampEncodeSize(this.CreationTime);
			encodeSize = encodeSize + AmqpCodec.GetStringEncodeSize(this.GroupId);
			encodeSize = encodeSize + AmqpCodec.GetUIntEncodeSize(this.GroupSequence);
			return encodeSize + AmqpCodec.GetStringEncodeSize(this.ReplyToGroupId);
		}

		public IDictionary<string, object> ToDictionary()
		{
			IDictionary<string, object> strs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			strs.Add(this.MessageId != null, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.MessageIdName, this.MessageId);
			ArraySegment<byte> userId = this.UserId;
			strs.Add(userId.Array != null, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.UserIdName, this.UserId);
			strs.Add(this.To != null, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ToName, this.To);
			strs.Add(this.Subject != null, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.SubjectName, this.Subject);
			strs.Add(this.ReplyTo != null, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ReplyToName, this.ReplyTo);
			strs.Add(this.CorrelationId != null, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.CorrelationIdName, this.CorrelationId);
			AmqpSymbol contentType = this.ContentType;
			strs.Add(contentType.Value != null, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ContentTypeName, this.ContentType);
			AmqpSymbol contentEncoding = this.ContentEncoding;
			strs.Add(contentEncoding.Value != null, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ContentEncodingName, this.ContentEncoding);
			DateTime? absoluteExpiryTime = this.AbsoluteExpiryTime;
			strs.Add(absoluteExpiryTime.HasValue, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.AbsoluteExpiryTimeName, this.AbsoluteExpiryTime);
			DateTime? creationTime = this.CreationTime;
			strs.Add(creationTime.HasValue, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.CreationTimeName, this.CreationTime);
			strs.Add(this.GroupId != null, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.GroupIdName, this.GroupId);
			uint? groupSequence = this.GroupSequence;
			strs.Add(groupSequence.HasValue, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.GroupSequenceName, this.GroupSequence);
			strs.Add(this.ReplyToGroupId != null, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ReplyToGroupIdName, this.ReplyToGroupId);
			return strs;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("properties(");
			int num = 0;
			base.AddFieldToString(this.MessageId != null, stringBuilder, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.MessageIdName, this.MessageId, ref num);
			ArraySegment<byte> userId = this.UserId;
			base.AddFieldToString(userId.Array != null, stringBuilder, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.UserIdName, this.UserId, ref num);
			base.AddFieldToString(this.To != null, stringBuilder, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ToName, this.To, ref num);
			base.AddFieldToString(this.Subject != null, stringBuilder, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.SubjectName, this.Subject, ref num);
			base.AddFieldToString(this.ReplyTo != null, stringBuilder, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ReplyToName, this.ReplyTo, ref num);
			base.AddFieldToString(this.CorrelationId != null, stringBuilder, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.CorrelationIdName, this.CorrelationId, ref num);
			AmqpSymbol contentType = this.ContentType;
			base.AddFieldToString(contentType.Value != null, stringBuilder, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ContentTypeName, this.ContentType, ref num);
			AmqpSymbol contentEncoding = this.ContentEncoding;
			base.AddFieldToString(contentEncoding.Value != null, stringBuilder, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ContentEncodingName, this.ContentEncoding, ref num);
			DateTime? absoluteExpiryTime = this.AbsoluteExpiryTime;
			base.AddFieldToString(absoluteExpiryTime.HasValue, stringBuilder, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.AbsoluteExpiryTimeName, this.AbsoluteExpiryTime, ref num);
			DateTime? creationTime = this.CreationTime;
			base.AddFieldToString(creationTime.HasValue, stringBuilder, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.CreationTimeName, this.CreationTime, ref num);
			base.AddFieldToString(this.GroupId != null, stringBuilder, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.GroupIdName, this.GroupId, ref num);
			uint? groupSequence = this.GroupSequence;
			base.AddFieldToString(groupSequence.HasValue, stringBuilder, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.GroupSequenceName, this.GroupSequence, ref num);
			base.AddFieldToString(this.ReplyToGroupId != null, stringBuilder, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.ReplyToGroupIdName, this.ReplyToGroupId, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}