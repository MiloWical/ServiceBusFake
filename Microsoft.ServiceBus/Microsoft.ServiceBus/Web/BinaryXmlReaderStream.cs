using Microsoft.ServiceBus.Common;
using System;
using System.IO;
using System.Xml;

namespace Microsoft.ServiceBus.Web
{
	internal class BinaryXmlReaderStream : Stream
	{
		private XmlDictionaryReader innerReader;

		private bool readCalled;

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

		internal BinaryXmlReaderStream(XmlDictionaryReader xmlReader)
		{
			this.innerReader = xmlReader;
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (!this.readCalled)
			{
				this.innerReader.MoveToContent();
				this.innerReader.Read();
				this.readCalled = true;
			}
			if (this.innerReader.NodeType != XmlNodeType.Text)
			{
				return 0;
			}
			return this.innerReader.ReadContentAsBase64(buffer, offset, count);
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
			throw Fx.Exception.AsError(new NotImplementedException(), null);
		}
	}
}