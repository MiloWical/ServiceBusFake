using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class Address
	{
		public abstract int EncodeSize
		{
			get;
		}

		protected Address()
		{
		}

		public static Address Decode(ByteBuffer buffer)
		{
			object obj = AmqpEncoding.DecodeObject(buffer);
			if (obj == null)
			{
				return null;
			}
			if (!(obj is string))
			{
				throw new NotSupportedException(obj.GetType().ToString());
			}
			return (string)obj;
		}

		public static void Encode(ByteBuffer buffer, Address address)
		{
			if (address == null)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			address.OnEncode(buffer);
		}

		public static int GetEncodeSize(Address address)
		{
			if (address == null)
			{
				return 1;
			}
			return address.EncodeSize;
		}

		public abstract void OnEncode(ByteBuffer buffer);

		public static implicit operator Address(string value)
		{
			return new Address.AddressString(value);
		}

		private sealed class AddressString : Address
		{
			private string address;

			public override int EncodeSize
			{
				get
				{
					return AmqpCodec.GetStringEncodeSize(this.address);
				}
			}

			public AddressString(string id)
			{
				this.address = id;
			}

			public override void OnEncode(ByteBuffer buffer)
			{
				AmqpCodec.EncodeString(this.address, buffer);
			}

			public override string ToString()
			{
				return this.address;
			}
		}
	}
}