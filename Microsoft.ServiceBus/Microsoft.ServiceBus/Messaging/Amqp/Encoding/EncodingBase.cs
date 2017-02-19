using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal abstract class EncodingBase
	{
		private Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode;

		public Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode FormatCode
		{
			get
			{
				return this.formatCode;
			}
		}

		protected EncodingBase(Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			this.formatCode = formatCode;
		}

		public abstract object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode);

		public abstract void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer);

		public abstract int GetObjectEncodeSize(object value, bool arrayEncoding);

		public static void VerifyFormatCode(Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode expected, int offset)
		{
			if (formatCode != expected)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidFormatCode(formatCode, offset));
			}
		}

		public static void VerifyFormatCode(Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode, int offset, params Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode[] expected)
		{
			bool flag = false;
			Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode[] formatCodeArray = expected;
			int num = 0;
			while (num < (int)formatCodeArray.Length)
			{
				if (formatCode != formatCodeArray[num])
				{
					num++;
				}
				else
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidFormatCode(formatCode, offset));
			}
		}
	}
}