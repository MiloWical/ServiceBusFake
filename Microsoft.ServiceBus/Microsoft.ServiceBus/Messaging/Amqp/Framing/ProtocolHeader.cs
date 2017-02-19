using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Globalization;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class ProtocolHeader : IAmqpSerializable
	{
		private const uint AmqpPrefix = 1095586128;

		public readonly static ProtocolHeader Amqp100;

		public readonly static ProtocolHeader AmqpTls100;

		public readonly static ProtocolHeader AmqpSasl100;

		private Microsoft.ServiceBus.Messaging.Amqp.ProtocolId protocolId;

		private AmqpVersion version;

		public int EncodeSize
		{
			get
			{
				return 8;
			}
		}

		public Microsoft.ServiceBus.Messaging.Amqp.ProtocolId ProtocolId
		{
			get
			{
				return this.protocolId;
			}
		}

		public AmqpVersion Version
		{
			get
			{
				return this.version;
			}
		}

		static ProtocolHeader()
		{
			ProtocolHeader.Amqp100 = new ProtocolHeader(Microsoft.ServiceBus.Messaging.Amqp.ProtocolId.Amqp, new AmqpVersion(1, 0, 0));
			ProtocolHeader.AmqpTls100 = new ProtocolHeader(Microsoft.ServiceBus.Messaging.Amqp.ProtocolId.AmqpTls, new AmqpVersion(1, 0, 0));
			ProtocolHeader.AmqpSasl100 = new ProtocolHeader(Microsoft.ServiceBus.Messaging.Amqp.ProtocolId.AmqpSasl, new AmqpVersion(1, 0, 0));
		}

		public ProtocolHeader()
		{
		}

		public ProtocolHeader(Microsoft.ServiceBus.Messaging.Amqp.ProtocolId id, AmqpVersion version)
		{
			this.protocolId = id;
			this.version = version;
		}

		public void Decode(ByteBuffer buffer)
		{
			if (buffer.Length < this.EncodeSize)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInsufficientBufferSize(this.EncodeSize, buffer.Length));
			}
			if (AmqpBitConverter.ReadUInt(buffer) != 1095586128)
			{
				throw AmqpEncoding.GetEncodingException("ProtocolName");
			}
			this.protocolId = (Microsoft.ServiceBus.Messaging.Amqp.ProtocolId)AmqpBitConverter.ReadUByte(buffer);
			this.version = new AmqpVersion(AmqpBitConverter.ReadUByte(buffer), AmqpBitConverter.ReadUByte(buffer), AmqpBitConverter.ReadUByte(buffer));
		}

		public void Encode(ByteBuffer buffer)
		{
			AmqpBitConverter.WriteUInt(buffer, 1095586128);
			AmqpBitConverter.WriteUByte(buffer, (byte)this.protocolId);
			AmqpBitConverter.WriteUByte(buffer, this.version.Major);
			AmqpBitConverter.WriteUByte(buffer, this.version.Minor);
			AmqpBitConverter.WriteUByte(buffer, this.version.Revision);
		}

		public override bool Equals(object obj)
		{
			ProtocolHeader protocolHeader = obj as ProtocolHeader;
			if (protocolHeader == null)
			{
				return false;
			}
			if (protocolHeader.protocolId != this.protocolId)
			{
				return false;
			}
			return protocolHeader.version.Equals(this.version);
		}

		public override int GetHashCode()
		{
			int major = ((byte)this.protocolId << 24) + (this.version.Major << 16) + (this.version.Minor << 8) + this.version.Revision;
			return major.GetHashCode();
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { (byte)this.protocolId, this.version };
			return string.Format(invariantCulture, "AMQP {0} {1}", objArray);
		}
	}
}