using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Globalization;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class MessageId
	{
		public abstract int EncodeSize
		{
			get;
		}

		protected MessageId()
		{
		}

		public static MessageId Decode(ByteBuffer buffer)
		{
			object obj = AmqpEncoding.DecodeObject(buffer);
			if (obj == null)
			{
				return null;
			}
			if (obj is ulong)
			{
				return (ulong)obj;
			}
			if (obj is Guid)
			{
				return (Guid)obj;
			}
			if (obj is ArraySegment<byte>)
			{
				return (ArraySegment<byte>)obj;
			}
			if (!(obj is string))
			{
				throw new NotSupportedException(obj.GetType().ToString());
			}
			return (string)obj;
		}

		public static void Encode(ByteBuffer buffer, MessageId messageId)
		{
			if (messageId == null)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			messageId.OnEncode(buffer);
		}

		public static int GetEncodeSize(MessageId messageId)
		{
			if (messageId == null)
			{
				return 1;
			}
			return messageId.EncodeSize;
		}

		public abstract void OnEncode(ByteBuffer buffer);

		public static implicit operator MessageId(ulong value)
		{
			return new MessageId.MessageIdUlong(value);
		}

		public static implicit operator MessageId(Guid value)
		{
			return new MessageId.MessageIdUuid(value);
		}

		public static implicit operator MessageId(ArraySegment<byte> value)
		{
			return new MessageId.MessageIdBinary(value);
		}

		public static implicit operator MessageId(string value)
		{
			return new MessageId.MessageIdString(value);
		}

		private sealed class MessageIdBinary : MessageId
		{
			private ArraySegment<byte> id;

			public override int EncodeSize
			{
				get
				{
					return AmqpCodec.GetBinaryEncodeSize(this.id);
				}
			}

			public MessageIdBinary(ArraySegment<byte> id)
			{
				this.id = id;
			}

			public override bool Equals(object obj)
			{
				MessageId.MessageIdBinary messageIdBinary = obj as MessageId.MessageIdBinary;
				if (messageIdBinary == null)
				{
					return false;
				}
				return ByteArrayComparer.Instance.Equals(this.id, messageIdBinary.id);
			}

			public override int GetHashCode()
			{
				return ByteArrayComparer.Instance.GetHashCode(this.id);
			}

			public override void OnEncode(ByteBuffer buffer)
			{
				AmqpCodec.EncodeBinary(this.id, buffer);
			}

			public override string ToString()
			{
				return this.id.GetString();
			}
		}

		private sealed class MessageIdString : MessageId
		{
			private string id;

			public override int EncodeSize
			{
				get
				{
					return AmqpCodec.GetStringEncodeSize(this.id);
				}
			}

			public MessageIdString(string id)
			{
				this.id = id;
			}

			public override bool Equals(object obj)
			{
				MessageId.MessageIdString messageIdString = obj as MessageId.MessageIdString;
				if (messageIdString == null)
				{
					return false;
				}
				return this.id.Equals(messageIdString.id);
			}

			public override int GetHashCode()
			{
				return this.id.GetHashCode();
			}

			public override void OnEncode(ByteBuffer buffer)
			{
				AmqpCodec.EncodeString(this.id, buffer);
			}

			public override string ToString()
			{
				return this.id;
			}
		}

		private sealed class MessageIdUlong : MessageId
		{
			private ulong id;

			public override int EncodeSize
			{
				get
				{
					return AmqpCodec.GetULongEncodeSize(new ulong?(this.id));
				}
			}

			public MessageIdUlong(ulong id)
			{
				this.id = id;
			}

			public override bool Equals(object obj)
			{
				MessageId.MessageIdUlong messageIdUlong = obj as MessageId.MessageIdUlong;
				if (messageIdUlong == null)
				{
					return false;
				}
				return this.id == messageIdUlong.id;
			}

			public override int GetHashCode()
			{
				return this.id.GetHashCode();
			}

			public override void OnEncode(ByteBuffer buffer)
			{
				AmqpCodec.EncodeULong(new ulong?(this.id), buffer);
			}

			public override string ToString()
			{
				return this.id.ToString(CultureInfo.InvariantCulture);
			}
		}

		private sealed class MessageIdUuid : MessageId
		{
			private Guid id;

			public override int EncodeSize
			{
				get
				{
					return AmqpCodec.GetUuidEncodeSize(new Guid?(this.id));
				}
			}

			public MessageIdUuid(Guid id)
			{
				this.id = id;
			}

			public override bool Equals(object obj)
			{
				MessageId.MessageIdUuid messageIdUuid = obj as MessageId.MessageIdUuid;
				if (messageIdUuid == null)
				{
					return false;
				}
				return this.id == messageIdUuid.id;
			}

			public override int GetHashCode()
			{
				return this.id.GetHashCode();
			}

			public override void OnEncode(ByteBuffer buffer)
			{
				AmqpCodec.EncodeUuid(new Guid?(this.id), buffer);
			}

			public override string ToString()
			{
				return this.id.ToString();
			}
		}
	}
}