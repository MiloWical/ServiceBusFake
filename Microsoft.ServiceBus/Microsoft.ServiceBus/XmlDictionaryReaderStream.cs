using Microsoft.ServiceBus.Common;
using System;
using System.IO;
using System.Xml;

namespace Microsoft.ServiceBus
{
	internal class XmlDictionaryReaderStream : Stream
	{
		private XmlDictionaryReader reader;

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		public override long Length
		{
			get
			{
				throw Fx.Exception.AsError(new NotImplementedException(), null);
			}
		}

		public override long Position
		{
			get
			{
				throw Fx.Exception.AsError(new NotImplementedException(), null);
			}
			set
			{
				throw Fx.Exception.AsError(new NotImplementedException(), null);
			}
		}

		public XmlDictionaryReaderStream(XmlDictionaryReader reader)
		{
			this.reader = reader;
		}

		public override void Close()
		{
			this.reader.Close();
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return this.reader.ReadContentAsBase64(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw Fx.Exception.AsError(new NotImplementedException(), null);
		}

		public override void SetLength(long value)
		{
			throw Fx.Exception.AsError(new NotImplementedException(), null);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw Fx.Exception.AsError(new NotImplementedException(), null);
		}
	}
}