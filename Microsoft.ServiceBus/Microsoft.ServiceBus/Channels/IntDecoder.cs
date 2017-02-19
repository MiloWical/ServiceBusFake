using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;

namespace Microsoft.ServiceBus.Channels
{
	internal struct IntDecoder
	{
		private const int LastIndex = 4;

		private int @value;

		private short index;

		private bool isValueDecoded;

		public bool IsValueDecoded
		{
			get
			{
				return this.isValueDecoded;
			}
		}

		public int Value
		{
			get
			{
				if (!this.isValueDecoded)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.@value;
			}
		}

		public int Decode(byte[] buffer, int offset, int size)
		{
			DecoderHelper.ValidateSize(size);
			if (this.isValueDecoded)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
			}
			int num = 0;
			while (num < size)
			{
				int num1 = buffer[offset];
				IntDecoder intDecoder = this;
				intDecoder.@value = intDecoder.@value | (num1 & 127) << (this.index * 7 & 31);
				num++;
				if (this.index == 4 && (num1 & 248) != 0)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.FramingSizeTooLarge, new object[0])));
				}
				IntDecoder intDecoder1 = this;
				intDecoder1.index = (short)(intDecoder1.index + 1);
				if ((num1 & 128) != 0)
				{
					offset++;
				}
				else
				{
					this.isValueDecoded = true;
					break;
				}
			}
			return num;
		}

		public void Reset()
		{
			this.index = 0;
			this.@value = 0;
			this.isValueDecoded = false;
		}
	}
}