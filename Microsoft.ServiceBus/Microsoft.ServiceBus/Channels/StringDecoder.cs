using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;
using System.Text;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class StringDecoder
	{
		private int encodedSize;

		private byte[] encodedBytes;

		private int bytesNeeded;

		private string @value;

		private StringDecoder.State currentState;

		private IntDecoder sizeDecoder;

		private int sizeQuota;

		private int valueLengthInBytes;

		public bool IsValueDecoded
		{
			get
			{
				return this.currentState == StringDecoder.State.Done;
			}
		}

		public string Value
		{
			get
			{
				if (this.currentState != StringDecoder.State.Done)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.@value;
			}
		}

		public StringDecoder(int sizeQuota)
		{
			this.sizeQuota = sizeQuota;
			this.sizeDecoder = new IntDecoder();
			this.currentState = StringDecoder.State.ReadingSize;
			this.Reset();
		}

		private static bool CompareBuffers(byte[] buffer1, byte[] buffer2, int offset)
		{
			for (int i = 0; i < (int)buffer1.Length; i++)
			{
				if (buffer1[i] != buffer2[i + offset])
				{
					return false;
				}
			}
			return true;
		}

		public int Decode(byte[] buffer, int offset, int size)
		{
			int num;
			DecoderHelper.ValidateSize(size);
			switch (this.currentState)
			{
				case StringDecoder.State.ReadingSize:
				{
					num = this.sizeDecoder.Decode(buffer, offset, size);
					if (!this.sizeDecoder.IsValueDecoded)
					{
						break;
					}
					this.encodedSize = this.sizeDecoder.Value;
					if (this.encodedSize > this.sizeQuota)
					{
						Exception exception = this.OnSizeQuotaExceeded(this.encodedSize);
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(exception);
					}
					if (this.encodedBytes == null || (int)this.encodedBytes.Length < this.encodedSize)
					{
						this.encodedBytes = DiagnosticUtility.Utility.AllocateByteArray(this.encodedSize);
						this.@value = null;
					}
					this.currentState = StringDecoder.State.ReadingBytes;
					this.bytesNeeded = this.encodedSize;
					break;
				}
				case StringDecoder.State.ReadingBytes:
				{
					if (this.@value == null || this.valueLengthInBytes != this.encodedSize || this.bytesNeeded != this.encodedSize || size < this.encodedSize || !StringDecoder.CompareBuffers(this.encodedBytes, buffer, offset))
					{
						num = this.bytesNeeded;
						if (size < this.bytesNeeded)
						{
							num = size;
						}
						Buffer.BlockCopy(buffer, offset, this.encodedBytes, this.encodedSize - this.bytesNeeded, num);
						StringDecoder stringDecoder = this;
						stringDecoder.bytesNeeded = stringDecoder.bytesNeeded - num;
						if (this.bytesNeeded != 0)
						{
							break;
						}
						this.@value = Encoding.UTF8.GetString(this.encodedBytes, 0, this.encodedSize);
						this.valueLengthInBytes = this.encodedSize;
						this.OnComplete(this.@value);
						break;
					}
					else
					{
						num = this.bytesNeeded;
						this.OnComplete(this.@value);
						break;
					}
				}
				default:
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.InvalidDecoderStateMachine, new object[0])));
				}
			}
			return num;
		}

		protected virtual void OnComplete(string value)
		{
			this.currentState = StringDecoder.State.Done;
		}

		protected abstract Exception OnSizeQuotaExceeded(int size);

		public void Reset()
		{
			this.currentState = StringDecoder.State.ReadingSize;
			this.sizeDecoder.Reset();
		}

		private enum State
		{
			ReadingSize,
			ReadingBytes,
			Done
		}
	}
}