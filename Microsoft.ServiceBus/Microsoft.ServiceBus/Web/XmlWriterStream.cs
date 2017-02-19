using Microsoft.ServiceBus.Common;
using System;
using System.IO;
using System.Xml;

namespace Microsoft.ServiceBus.Web
{
	internal class XmlWriterStream : Stream
	{
		private XmlDictionaryWriter innerWriter;

		public override bool CanRead
		{
			get
			{
				return false;
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
				return true;
			}
		}

		public override long Length
		{
			get
			{
				throw Fx.Exception.AsError(new NotSupportedException(), null);
			}
		}

		public override long Position
		{
			get
			{
				throw Fx.Exception.AsError(new NotSupportedException(), null);
			}
			set
			{
				throw Fx.Exception.AsError(new NotSupportedException(), null);
			}
		}

		internal XmlWriterStream(XmlDictionaryWriter xmlWriter)
		{
			this.innerWriter = xmlWriter;
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw Fx.Exception.AsError(new NotImplementedException(), null);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw Fx.Exception.AsError(new NotSupportedException(), null);
		}

		public override void SetLength(long value)
		{
			throw Fx.Exception.AsError(new NotSupportedException(), null);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			this.innerWriter.WriteBase64(buffer, offset, count);
		}
	}
}