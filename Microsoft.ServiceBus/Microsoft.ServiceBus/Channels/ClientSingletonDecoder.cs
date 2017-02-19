using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;

namespace Microsoft.ServiceBus.Channels
{
	internal class ClientSingletonDecoder : ClientFramingDecoder
	{
		private FaultStringDecoder faultDecoder;

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

		public ClientSingletonDecoder(long streamPosition) : base(streamPosition)
		{
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
					case ClientFramingDecoderState.ReadingEnvelopeSize:
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
					case ClientFramingDecoderState.ReadingFault:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						base.ValidateRecordType(FramingRecordType.Fault, framingRecordType);
						num = 1;
						this.faultDecoder = new FaultStringDecoder();
						base.CurrentState = ClientFramingDecoderState.ReadingFaultString;
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
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.FramingAtEnd, new object[0]))));
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
							base.ValidateRecordType(FramingRecordType.UnsizedEnvelope, framingRecordType);
							num = 1;
							base.CurrentState = ClientFramingDecoderState.EnvelopeStart;
							break;
						}
						else
						{
							num = 0;
							base.CurrentState = ClientFramingDecoderState.ReadingFault;
							break;
						}
					}
					case ClientFramingDecoderState.EnvelopeStart:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.FramingAtEnd, new object[0]))));
					}
					default:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.InvalidDecoderStateMachine, new object[0]))));
					}
				}
				ClientSingletonDecoder streamPosition = this;
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