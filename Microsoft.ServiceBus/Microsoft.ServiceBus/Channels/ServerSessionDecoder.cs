using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;

namespace Microsoft.ServiceBus.Channels
{
	internal class ServerSessionDecoder : FramingDecoder
	{
		private ViaStringDecoder viaDecoder;

		private StringDecoder contentTypeDecoder;

		private IntDecoder sizeDecoder;

		private ServerSessionDecoder.State currentState;

		private string contentType;

		private int envelopeBytesNeeded;

		private int envelopeSize;

		private string upgrade;

		public string ContentType
		{
			get
			{
				if (this.currentState < ServerSessionDecoder.State.PreUpgradeStart)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.contentType;
			}
		}

		public ServerSessionDecoder.State CurrentState
		{
			get
			{
				return this.currentState;
			}
		}

		protected override string CurrentStateAsString
		{
			get
			{
				return this.currentState.ToString();
			}
		}

		public int EnvelopeSize
		{
			get
			{
				if (this.currentState < ServerSessionDecoder.State.EnvelopeStart)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.envelopeSize;
			}
		}

		public string Upgrade
		{
			get
			{
				if (this.currentState != ServerSessionDecoder.State.UpgradeRequest)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.upgrade;
			}
		}

		public Uri Via
		{
			get
			{
				if (this.currentState < ServerSessionDecoder.State.ReadingContentTypeRecord)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.viaDecoder.ValueAsUri;
			}
		}

		public ServerSessionDecoder(long streamPosition, int maxViaLength, int maxContentTypeLength) : base(streamPosition)
		{
			this.viaDecoder = new ViaStringDecoder(maxViaLength);
			this.contentTypeDecoder = new ContentTypeStringDecoder(maxContentTypeLength);
			this.sizeDecoder = new IntDecoder();
			this.currentState = ServerSessionDecoder.State.ReadingViaRecord;
		}

		public int Decode(byte[] bytes, int offset, int size)
		{
			int num;
			FramingRecordType framingRecordType;
			int num1;
			DecoderHelper.ValidateSize(size);
			try
			{
				switch (this.currentState)
				{
					case ServerSessionDecoder.State.ReadingViaRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						base.ValidateRecordType(FramingRecordType.Via, framingRecordType);
						num = 1;
						this.viaDecoder.Reset();
						this.currentState = ServerSessionDecoder.State.ReadingViaString;
						break;
					}
					case ServerSessionDecoder.State.ReadingViaString:
					{
						num = this.viaDecoder.Decode(bytes, offset, size);
						if (!this.viaDecoder.IsValueDecoded)
						{
							break;
						}
						this.currentState = ServerSessionDecoder.State.ReadingContentTypeRecord;
						break;
					}
					case ServerSessionDecoder.State.ReadingContentTypeRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						if (framingRecordType != FramingRecordType.KnownEncoding)
						{
							base.ValidateRecordType(FramingRecordType.ExtensibleEncoding, framingRecordType);
							num = 1;
							this.contentTypeDecoder.Reset();
							this.currentState = ServerSessionDecoder.State.ReadingContentTypeString;
							break;
						}
						else
						{
							num = 1;
							this.currentState = ServerSessionDecoder.State.ReadingContentTypeByte;
							break;
						}
					}
					case ServerSessionDecoder.State.ReadingContentTypeString:
					{
						num = this.contentTypeDecoder.Decode(bytes, offset, size);
						if (!this.contentTypeDecoder.IsValueDecoded)
						{
							break;
						}
						this.currentState = ServerSessionDecoder.State.PreUpgradeStart;
						this.contentType = this.contentTypeDecoder.Value;
						break;
					}
					case ServerSessionDecoder.State.ReadingContentTypeByte:
					{
						this.contentType = ContentTypeStringDecoder.GetString((FramingEncodingType)bytes[offset]);
						num = 1;
						this.currentState = ServerSessionDecoder.State.PreUpgradeStart;
						break;
					}
					case ServerSessionDecoder.State.PreUpgradeStart:
					{
						num = 0;
						this.currentState = ServerSessionDecoder.State.ReadingUpgradeRecord;
						break;
					}
					case ServerSessionDecoder.State.ReadingUpgradeRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						if (framingRecordType != FramingRecordType.UpgradeRequest)
						{
							num = 0;
							this.currentState = ServerSessionDecoder.State.ReadingPreambleEndRecord;
							break;
						}
						else
						{
							num = 1;
							this.contentTypeDecoder.Reset();
							this.currentState = ServerSessionDecoder.State.ReadingUpgradeString;
							break;
						}
					}
					case ServerSessionDecoder.State.ReadingUpgradeString:
					{
						num = this.contentTypeDecoder.Decode(bytes, offset, size);
						if (!this.contentTypeDecoder.IsValueDecoded)
						{
							break;
						}
						this.currentState = ServerSessionDecoder.State.UpgradeRequest;
						this.upgrade = this.contentTypeDecoder.Value;
						break;
					}
					case ServerSessionDecoder.State.UpgradeRequest:
					{
						num = 0;
						this.currentState = ServerSessionDecoder.State.ReadingUpgradeRecord;
						break;
					}
					case ServerSessionDecoder.State.ReadingPreambleEndRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						base.ValidateRecordType(FramingRecordType.PreambleEnd, framingRecordType);
						num = 1;
						this.currentState = ServerSessionDecoder.State.Start;
						break;
					}
					case ServerSessionDecoder.State.Start:
					{
						num = 0;
						this.currentState = ServerSessionDecoder.State.ReadingEndRecord;
						break;
					}
					case ServerSessionDecoder.State.ReadingEnvelopeRecord:
					{
						base.ValidateRecordType(FramingRecordType.SizedEnvelope, (FramingRecordType)bytes[offset]);
						num = 1;
						this.currentState = ServerSessionDecoder.State.ReadingEnvelopeSize;
						this.sizeDecoder.Reset();
						break;
					}
					case ServerSessionDecoder.State.ReadingEnvelopeSize:
					{
						num = this.sizeDecoder.Decode(bytes, offset, size);
						if (!this.sizeDecoder.IsValueDecoded)
						{
							break;
						}
						this.currentState = ServerSessionDecoder.State.EnvelopeStart;
						this.envelopeSize = this.sizeDecoder.Value;
						this.envelopeBytesNeeded = this.envelopeSize;
						break;
					}
					case ServerSessionDecoder.State.EnvelopeStart:
					{
						num = 0;
						this.currentState = ServerSessionDecoder.State.ReadingEnvelopeBytes;
						break;
					}
					case ServerSessionDecoder.State.ReadingEnvelopeBytes:
					{
						num = size;
						if (num > this.envelopeBytesNeeded)
						{
							num = this.envelopeBytesNeeded;
						}
						ServerSessionDecoder serverSessionDecoder = this;
						serverSessionDecoder.envelopeBytesNeeded = serverSessionDecoder.envelopeBytesNeeded - num;
						if (this.envelopeBytesNeeded != 0)
						{
							break;
						}
						this.currentState = ServerSessionDecoder.State.EnvelopeEnd;
						break;
					}
					case ServerSessionDecoder.State.EnvelopeEnd:
					{
						num = 0;
						this.currentState = ServerSessionDecoder.State.ReadingEndRecord;
						break;
					}
					case ServerSessionDecoder.State.ReadingEndRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						if (framingRecordType != FramingRecordType.End)
						{
							num = 0;
							this.currentState = ServerSessionDecoder.State.ReadingEnvelopeRecord;
							break;
						}
						else
						{
							num = 1;
							this.currentState = ServerSessionDecoder.State.End;
							break;
						}
					}
					case ServerSessionDecoder.State.End:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.FramingAtEnd, new object[0]))));
					}
					default:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.InvalidDecoderStateMachine, new object[0]))));
					}
				}
				ServerSessionDecoder streamPosition = this;
				streamPosition.StreamPosition = streamPosition.StreamPosition + (long)num;
				num1 = num;
			}
			catch (InvalidDataException invalidDataException)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(invalidDataException));
			}
			return num1;
		}

		public void Reset(long streamPosition)
		{
			base.StreamPosition = streamPosition;
			this.currentState = ServerSessionDecoder.State.ReadingViaRecord;
		}

		public enum State
		{
			ReadingViaRecord,
			ReadingViaString,
			ReadingContentTypeRecord,
			ReadingContentTypeString,
			ReadingContentTypeByte,
			PreUpgradeStart,
			ReadingUpgradeRecord,
			ReadingUpgradeString,
			UpgradeRequest,
			ReadingPreambleEndRecord,
			Start,
			ReadingEnvelopeRecord,
			ReadingEnvelopeSize,
			EnvelopeStart,
			ReadingEnvelopeBytes,
			EnvelopeEnd,
			ReadingEndRecord,
			End
		}
	}
}