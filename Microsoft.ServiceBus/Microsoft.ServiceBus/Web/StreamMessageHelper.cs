using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace Microsoft.ServiceBus.Web
{
	public static class StreamMessageHelper
	{
		public static Message CreateJsonMessage(MessageVersion version, string action, Stream jsonStream)
		{
			Message message = Message.CreateMessage(version, action, new JsonStreamBodyWriter(jsonStream));
			message.Properties.Add("WebBodyFormatMessageProperty", new WebBodyFormatMessageProperty(WebContentFormat.Json));
			return message;
		}

		public static Message CreateMessage(MessageVersion version, string action, StreamWriterDelegate writer)
		{
			Message message = Message.CreateMessage(version, action, new DelegateBodyWriter(writer));
			message.Properties.Add("BodyFormat", new RelayedHttpBodyFormatHeader(RelayedHttpUtility.FormatStringRaw));
			return message;
		}

		public static Message CreateMessage(MessageVersion version, string action, Stream stream)
		{
			Message message = Message.CreateMessage(version, action, new Microsoft.ServiceBus.Web.StreamBodyWriter(stream));
			message.Properties.Add("BodyFormat", new RelayedHttpBodyFormatHeader(RelayedHttpUtility.FormatStringRaw));
			message.Properties.Add("StreamMessageProperty", new StreamMessageProperty(stream));
			return message;
		}

		public static Stream GetStream(Message message)
		{
			return StreamMessageHelper.GetStream(message.GetReaderAtBodyContents());
		}

		public static Stream GetStream(XmlDictionaryReader reader)
		{
			return new BinaryXmlReaderStream(reader);
		}

		internal static Stream GetXmlStream(XmlDictionaryReader reader)
		{
			return new StreamMessageHelper.XmlReaderStream(reader);
		}

		private class XmlReaderStream : Stream
		{
			private readonly MemoryStream bufferStream;

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
					return this.bufferStream.Position;
				}
				set
				{
					throw Fx.Exception.AsError(new NotSupportedException(), null);
				}
			}

			public XmlReaderStream(XmlDictionaryReader xmlReader)
			{
				this.bufferStream = new MemoryStream();
				using (XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(this.bufferStream, Encoding.UTF8, false))
				{
					xmlDictionaryWriter.WriteNode(xmlReader, true);
				}
				this.bufferStream.Flush();
				this.bufferStream.Position = (long)0;
			}

			public override void Flush()
			{
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return this.bufferStream.Read(buffer, offset, count);
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
				throw Fx.Exception.AsError(new NotSupportedException(), null);
			}
		}
	}
}