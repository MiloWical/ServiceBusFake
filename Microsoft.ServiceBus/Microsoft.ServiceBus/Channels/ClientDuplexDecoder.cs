using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;

namespace Microsoft.ServiceBus.Channels
{
	internal class ClientDuplexDecoder : ClientFramingDecoder
	{
		private IntDecoder sizeDecoder;

		private FaultStringDecoder faultDecoder;

		private int envelopeBytesNeeded;

		private int envelopeSize;

		public int EnvelopeSize
		{
			get
			{
				if (base.CurrentState < ClientFramingDecoderState.EnvelopeStart)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.envelopeSize;
			}
		}

		public override string Fault
		{
			get
			{
				if (base.CurrentState < ClientFramingDecoderState.Fault)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.faultDecoder.Value;
			}
		}

		public ClientDuplexDecoder(long streamPosition) : base(streamPosition)
		{
			this.sizeDecoder = new IntDecoder();
		}

		public override int Decode(byte[] bytes, int offset, int size)
		{
			int num;
			FramingRecordType framingRecordType;
			int num1;
			DecoderHelper.ValidateSize(size);
			try
			{
				switch (base.CurrentState)
				{
					case ClientFramingDecoderState.ReadingUpgradeRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						if (framingRecordType != FramingRecordType.UpgradeResponse)
						{
							num = 0;
							base.CurrentState = ClientFramingDecoderState.ReadingAckRecord;
							break;
						}
						else
						{
							num = 1;
							base.CurrentState = ClientFramingDecoderState.UpgradeResponse;
							break;
						}
					}
					case ClientFramingDecoderState.ReadingUpgradeMode:
					case ClientFramingDecoderState.ReadingFault:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.InvalidDecoderStateMachine, new object[0]))));
					}
					case ClientFramingDecoderState.UpgradeResponse:
					{
						num = 0;
						base.CurrentState = ClientFramingDecoderState.ReadingUpgradeRecord;
						break;
					}
					case ClientFramingDecoderState.ReadingAckRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						if (framingRecordType != FramingRecordType.Fault)
						{
							base.ValidatePreambleAck(framingRecordType);
							num = 1;
							base.CurrentState = ClientFramingDecoderState.Start;
							break;
						}
						else
						{
							num = 1;
							this.faultDecoder = new FaultStringDecoder();
							base.CurrentState = ClientFramingDecoderState.ReadingFaultString;
							break;
						}
					}
					case ClientFramingDecoderState.Start:
					{
						num = 0;
						base.CurrentState = ClientFramingDecoderState.ReadingEnvelopeRecord;
						break;
					}
					case ClientFramingDecoderState.ReadingFaultString:
					{
						num = this.faultDecoder.Decode(bytes, offset, size);
						if (!this.faultDecoder.IsValueDecoded)
						{
							break;
						}
						base.CurrentState = ClientFramingDecoderState.Fault;
						break;
					}
					case ClientFramingDecoderState.Fault:
					{
						num = 0;
						base.CurrentState = ClientFramingDecoderState.ReadingEndRecord;
						break;
					}
					case ClientFramingDecoderState.ReadingEnvelopeRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						if (framingRecordType == FramingRecordType.End)
						{
							num = 1;
							base.CurrentState = ClientFramingDecoderState.End;
							break;
						}
						else if (framingRecordType != FramingRecordType.Fault)
						{
							base.ValidateRecordType(FramingRecordType.SizedEnvelope, framingRecordType);
							num = 1;
							base.CurrentState = ClientFramingDecoderState.ReadingEnvelopeSize;
							this.sizeDecoder.Reset();
							break;
						}
						else
						{
							num = 1;
							this.faultDecoder = new FaultStringDecoder();
							base.CurrentState = ClientFramingDecoderState.ReadingFaultString;
							break;
						}
					}
					case ClientFramingDecoderState.ReadingEnvelopeSize:
					{
						num = this.sizeDecoder.Decode(bytes, offset, size);
						if (!this.sizeDecoder.IsValueDecoded)
						{
							break;
						}
						base.CurrentState = ClientFramingDecoderState.EnvelopeStart;
						this.envelopeSize = this.sizeDecoder.Value;
						this.envelopeBytesNeeded = this.envelopeSize;
						break;
					}
					case ClientFramingDecoderState.EnvelopeStart:
					{
						num = 0;
						base.CurrentState = ClientFramingDecoderState.ReadingEnvelopeBytes;
						break;
					}
					case ClientFramingDecoderState.ReadingEnvelopeBytes:
					{
						num = size;
						if (num > this.envelopeBytesNeeded)
						{
							num = this.envelopeBytesNeeded;
						}
						ClientDuplexDecoder clientDuplexDecoder = this;
						clientDuplexDecoder.envelopeBytesNeeded = clientDuplexDecoder.envelopeBytesNeeded - num;
						if (this.envelopeBytesNeeded != 0)
						{
							break;
						}
						base.CurrentState = ClientFramingDecoderState.EnvelopeEnd;
						break;
					}
					case ClientFramingDecoderState.EnvelopeEnd:
					{
						num = 0;
						base.CurrentState = ClientFramingDecoderState.ReadingEnvelopeRecord;
						break;
					}
					case ClientFramingDecoderState.ReadingEndRecord:
					{
						base.ValidateRecordType(FramingRecordType.End, (FramingRecordType)bytes[offset]);
						num = 1;
						base.CurrentState = ClientFramingDecoderState.End;
						break;
					}
					case ClientFramingDecoderState.End:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.FramingAtEnd, new object[0]))));
					}
					default:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.InvalidDecoderStateMachine, new object[0]))));
					}
				}
				ClientDuplexDecoder streamPosition = this;
				streamPosition.StreamPosition = streamPosition.StreamPosition + (long)num;
				num1 = num;
			}
			catch (InvalidDataException invalidDataException)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(invalidDataException));
			}
			return num1;
		}
	}
}