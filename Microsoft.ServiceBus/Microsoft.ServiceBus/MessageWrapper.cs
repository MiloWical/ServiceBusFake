using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus
{
	internal class MessageWrapper
	{
		private MessageEncoder encoder;

		public MessageWrapper(MessageEncoder encoder)
		{
			this.encoder = encoder;
		}

		public Message UnwrapMessage(Message message)
		{
			Stream xmlDictionaryReaderStream = new XmlDictionaryReaderStream(message.GetReaderAtBodyContents());
			Message message1 = this.encoder.ReadMessage(xmlDictionaryReaderStream, 65536);
			message1.Properties.CopyProperties(message.Properties);
			return message1;
		}

		public Message WrapMessage(Message message, string action)
		{
			BodyWriter wrappedMessageBodyWriter = new MessageWrapper.WrappedMessageBodyWriter(message, this.encoder);
			Message message1 = Message.CreateMessage(MessageVersion.Soap12WSAddressing10, action, wrappedMessageBodyWriter);
			message1.Properties.CopyProperties(message.Properties);
			return message1;
		}

		private class WrappedMessageBodyWriter : BodyWriter
		{
			private MessageEncoder encoder;

			private Message message;

			public WrappedMessageBodyWriter(Message message, MessageEncoder encoder) : base(false)
			{
				this.message = message;
				this.encoder = encoder;
			}

			protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
			{
				this.encoder.WriteMessage(this.message, new MessageWrapper.WrappedMessageBodyWriter.XmlDictionaryWriterStream(writer));
			}

			private class XmlDictionaryWriterStream : Stream
			{
				private XmlDictionaryWriter writer;

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
						throw new NotImplementedException();
					}
				}

				public override long Position
				{
					get
					{
						throw new NotImplementedException();
					}
					set
					{
						throw new NotImplementedException();
					}
				}

				public XmlDictionaryWriterStream(XmlDictionaryWriter writer)
				{
					this.writer = writer;
				}

				public override void Close()
				{
					this.writer.Close();
				}

				public override void Flush()
				{
					this.writer.Flush();
				}

				public override int Read(byte[] buffer, int offset, int count)
				{
					throw new NotImplementedException();
				}

				public override long Seek(long offset, SeekOrigin origin)
				{
					throw new NotImplementedException();
				}

				public override void SetLength(long value)
				{
					throw new NotImplementedException();
				}

				public override void Write(byte[] buffer, int offset, int count)
				{
					this.writer.WriteBase64(buffer, offset, count);
				}
			}
		}
	}
}