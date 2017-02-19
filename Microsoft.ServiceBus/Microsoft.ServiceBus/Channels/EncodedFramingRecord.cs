using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Text;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class EncodedFramingRecord
	{
		private byte[] encodedBytes;

		public byte[] EncodedBytes
		{
			get
			{
				return this.encodedBytes;
			}
		}

		protected EncodedFramingRecord(byte[] encodedBytes)
		{
			this.encodedBytes = encodedBytes;
		}

		internal EncodedFramingRecord(FramingRecordType recordType, string value)
		{
			int byteCount = Encoding.UTF8.GetByteCount(value);
			int encodedSize = IntEncoder.GetEncodedSize(byteCount);
			this.encodedBytes = DiagnosticUtility.Utility.AllocateByteArray(checked(checked(1 + encodedSize) + byteCount));
			this.encodedBytes[0] = (byte)recordType;
			int num = 1;
			num = num + IntEncoder.Encode(byteCount, this.encodedBytes, num);
			Encoding.UTF8.GetBytes(value, 0, value.Length, this.encodedBytes, num);
			this.SetEncodedBytes(this.encodedBytes);
		}

		public override bool Equals(object o)
		{
			if (!(o is EncodedFramingRecord))
			{
				return false;
			}
			return this.Equals((EncodedFramingRecord)o);
		}

		public bool Equals(EncodedFramingRecord other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			byte[] numArray = other.encodedBytes;
			if ((int)this.encodedBytes.Length != (int)numArray.Length)
			{
				return false;
			}
			for (int i = 0; i < (int)this.encodedBytes.Length; i++)
			{
				if (this.encodedBytes[i] != numArray[i])
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			return this.encodedBytes[0] << 16 | this.encodedBytes[(int)this.encodedBytes.Length / 2] << 8 | this.encodedBytes[(int)this.encodedBytes.Length - 1];
		}

		protected void SetEncodedBytes(byte[] encodedBytes)
		{
			this.encodedBytes = encodedBytes;
		}
	}
}