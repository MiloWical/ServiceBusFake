using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus.Channels
{
	internal class ClientDuplexConnectionReader : Microsoft.ServiceBus.Channels.SessionConnectionReader
	{
		private Microsoft.ServiceBus.Channels.ClientDuplexDecoder decoder;

		private int maxBufferSize;

		private BufferManager bufferManager;

		private MessageEncoder messageEncoder;

		private Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel channel;

		public ClientDuplexConnectionReader(Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel channel, Microsoft.ServiceBus.Channels.IConnection connection, Microsoft.ServiceBus.Channels.ClientDuplexDecoder decoder, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings settings, MessageEncoder messageEncoder) : base(connection, null, 0, 0, null)
		{
			this.decoder = decoder;
			this.maxBufferSize = settings.MaxBufferSize;
			this.bufferManager = settings.BufferManager;
			this.messageEncoder = messageEncoder;
			this.channel = channel;
		}

		private static IDisposable CreateProcessActionActivity()
		{
			return null;
		}

		protected override Message DecodeMessage(byte[] buffer, ref int offset, ref int size, ref bool isAtEOF, TimeSpan timeout)
		{
			while (size > 0)
			{
				int num = this.decoder.Decode(buffer, offset, size);
				if (num > 0)
				{
					if (base.EnvelopeBuffer != null)
					{
						if (!object.ReferenceEquals(buffer, base.EnvelopeBuffer))
						{
							Buffer.BlockCopy(buffer, offset, base.EnvelopeBuffer, base.EnvelopeOffset, num);
						}
						Microsoft.ServiceBus.Channels.ClientDuplexConnectionReader envelopeOffset = this;
						envelopeOffset.EnvelopeOffset = envelopeOffset.EnvelopeOffset + num;
					}
					offset = offset + num;
					size = size - num;
				}
				Microsoft.ServiceBus.Channels.ClientFramingDecoderState currentState = this.decoder.CurrentState;
				if (currentState == Microsoft.ServiceBus.Channels.ClientFramingDecoderState.Fault)
				{
					this.channel.Session.CloseOutputSession(((IDefaultCommunicationTimeouts)this.channel).CloseTimeout);
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(Microsoft.ServiceBus.Channels.FaultStringDecoder.GetFaultException(this.decoder.Fault, this.channel.RemoteAddress.Uri.AbsoluteUri, this.messageEncoder.ContentType));
				}
				switch (currentState)
				{
					case Microsoft.ServiceBus.Channels.ClientFramingDecoderState.EnvelopeStart:
					{
						int envelopeSize = this.decoder.EnvelopeSize;
						if (envelopeSize > this.maxBufferSize)
						{
							throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(Microsoft.ServiceBus.Channels.MaxMessageSizeStream.CreateMaxReceivedMessageSizeExceededException((long)this.maxBufferSize));
						}
						base.EnvelopeBuffer = this.bufferManager.TakeBuffer(envelopeSize);
						base.EnvelopeOffset = 0;
						base.EnvelopeSize = envelopeSize;
						continue;
					}
					case Microsoft.ServiceBus.Channels.ClientFramingDecoderState.EnvelopeEnd:
					{
						if (base.EnvelopeBuffer == null)
						{
							continue;
						}
						Message message = null;
						try
						{
							using (IDisposable disposable = Microsoft.ServiceBus.Channels.ClientDuplexConnectionReader.CreateProcessActionActivity())
							{
								message = this.messageEncoder.ReadMessage(new ArraySegment<byte>(base.EnvelopeBuffer, 0, base.EnvelopeSize), this.bufferManager);
							}
						}
						catch (XmlException xmlException1)
						{
							XmlException xmlException = xmlException1;
							throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ProtocolException(Microsoft.ServiceBus.SR.GetString(Resources.MessageXmlProtocolError, new object[0]), xmlException));
						}
						base.EnvelopeBuffer = null;
						return message;
					}
					case Microsoft.ServiceBus.Channels.ClientFramingDecoderState.End:
					{
						isAtEOF = true;
						return null;
					}
					default:
					{
						continue;
					}
				}
			}
			return null;
		}

		protected override void EnsureDecoderAtEof()
		{
			if (this.decoder.CurrentState != Microsoft.ServiceBus.Channels.ClientFramingDecoderState.End && this.decoder.CurrentState != Microsoft.ServiceBus.Channels.ClientFramingDecoderState.EnvelopeEnd && this.decoder.CurrentState != Microsoft.ServiceBus.Channels.ClientFramingDecoderState.ReadingUpgradeRecord && this.decoder.CurrentState != Microsoft.ServiceBus.Channels.ClientFramingDecoderState.UpgradeResponse)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(this.decoder.CreatePrematureEOFException());
			}
		}
	}
}